using static BuildLevelFunctions;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using Unity.Jobs;

public static class RunLevelFunctions
{
    public struct Triangle
    {
        public float3 v0, v1, v2;
        public float4 uv0, uv1, uv2;
    };

    public struct TrianglesMeta
    {
        public int triangleStartIndex;
        public int triangleCount;

        public int planeStartIndex;
        public int planeCount;

        public int sectorId;
    };

    public struct PortalMeta
    {
        public int polygonStartIndex;
        public int polygonCount;

        public int edgeStartIndex;
        public int edgeCount;

        public int connectedSectorId;
        public int sectorId;

        public int planeStartIndex;
        public int planeCount;

        public int portalContact;
    };

    [BurstCompile]
    public struct SectorsJob : IJobParallelFor
    {
        [ReadOnly] public float3 point;
        [ReadOnly] public NativeArray<SectorMeta> currentSectors;
        [ReadOnly] public NativeArray<PolygonMeta> polygons;
        [ReadOnly] public NativeArray<SectorMeta> sectors;
        [ReadOnly] public NativeArray<SectorMeta> contains;
        [ReadOnly] public NativeArray<MathematicalPlane> planes;

        public NativeList<TrianglesMeta>.ParallelWriter rawTriangles;
        public NativeList<PortalMeta>.ParallelWriter rawPortals;

        public void Execute(int index)
        {
            SectorMeta sector = currentSectors[index];

            for (int a = sector.polygonStartIndex; a < sector.polygonStartIndex + sector.polygonCount; a++)
            {
                PolygonMeta polygon = polygons[a];

                float planeDistance = math.dot(planes[polygon.plane].normal, point) + planes[polygon.plane].distance;

                if (planeDistance <= 0)
                {
                    continue;
                }

                int render = polygon.renderCount;

                int connectedsector = polygon.connectedSectorId;

                if (render != -1)
                {
                    rawTriangles.AddNoResize(new TrianglesMeta
                    {
                        triangleStartIndex = polygon.renderStartIndex,
                        triangleCount = polygon.renderCount,

                        planeStartIndex = sector.planeStartIndex,
                        planeCount = sector.planeCount,

                        sectorId = polygon.sectorId
                    });
                }

                if (connectedsector != -1)
                {
                    SectorMeta sectorpolygon = sectors[connectedsector];

                    int contact = 1;

                    for (int b = 0; b < contains.Length; b++)
                    {
                        if (contains[b].sectorId == sectorpolygon.sectorId)
                        {
                            contact = 0;
                            break;
                        }
                    }

                    rawPortals.AddNoResize(new PortalMeta
                    {
                        polygonStartIndex = sectorpolygon.polygonStartIndex,
                        polygonCount = sectorpolygon.polygonCount,

                        edgeStartIndex = polygon.edgeStartIndex,
                        edgeCount = polygon.edgeCount,

                        connectedSectorId = polygon.connectedSectorId,
                        sectorId = polygon.sectorId,

                        planeStartIndex = sector.planeStartIndex,
                        planeCount = sector.planeCount,

                        portalContact = contact
                    });
                }
            }
        }
    }

    [BurstCompile]
    public struct ClipTrianglesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<TrianglesMeta> rawTriangles;
        [ReadOnly] public NativeArray<MathematicalPlane> currentFrustums;
        [ReadOnly] public NativeArray<float3> vertices;
        [ReadOnly] public NativeArray<float4> textures;
        [ReadOnly] public NativeArray<int> render;

        [NativeDisableParallelForRestriction] public NativeArray<float3> processvertices;
        [NativeDisableParallelForRestriction] public NativeArray<float4> processtextures;
        [NativeDisableParallelForRestriction] public NativeArray<bool> processbool;

        [NativeDisableParallelForRestriction] public NativeArray<float3> temporaryvertices;
        [NativeDisableParallelForRestriction] public NativeArray<float4> temporarytextures;

        public NativeList<Triangle>.ParallelWriter finalTriangles;

        public void Execute(int index)
        {
            const int MaxVertsPerTri = 256;

            int baseIndex = index * MaxVertsPerTri;

            if (baseIndex >= processvertices.Length)
            {
                return;
            }

            TrianglesMeta tm = rawTriangles[index];

            for (int a = tm.triangleStartIndex; a < tm.triangleStartIndex + tm.triangleCount; a += 3)
            {
                int processverticescount = 0;
                int processtexturescount = 0;
                int processboolcount = 0;

                processvertices[baseIndex + processverticescount] = vertices[render[a]];
                processvertices[baseIndex + processverticescount + 1] = vertices[render[a + 1]];
                processvertices[baseIndex + processverticescount + 2] = vertices[render[a + 2]];
                processverticescount += 3;
                processtextures[baseIndex + processtexturescount] = textures[render[a]];
                processtextures[baseIndex + processtexturescount + 1] = textures[render[a + 1]];
                processtextures[baseIndex + processtexturescount + 2] = textures[render[a + 2]];
                processtexturescount += 3;
                processbool[baseIndex + processboolcount] = true;
                processbool[baseIndex + processboolcount + 1] = true;
                processbool[baseIndex + processboolcount + 2] = true;
                processboolcount += 3;

                for (int b = tm.planeStartIndex; b < tm.planeStartIndex + tm.planeCount; b++)
                {
                    int addTriangles = 0;

                    int temporaryverticescount = 0;
                    int temporarytexturescount = 0;

                    for (int c = baseIndex; c < baseIndex + processverticescount; c += 3)
                    {
                        if (processbool[c] == false && processbool[c + 1] == false && processbool[c + 2] == false)
                        {
                            continue;
                        }

                        float3 v0 = processvertices[c];
                        float3 v1 = processvertices[c + 1];
                        float3 v2 = processvertices[c + 2];

                        float4 uv0 = processtextures[c];
                        float4 uv1 = processtextures[c + 1];
                        float4 uv2 = processtextures[c + 2];

                        float d0 = math.dot(currentFrustums[b].normal, v0) + currentFrustums[b].distance;
                        float d1 = math.dot(currentFrustums[b].normal, v1) + currentFrustums[b].distance;
                        float d2 = math.dot(currentFrustums[b].normal, v2) + currentFrustums[b].distance;

                        bool b0 = d0 >= 0;
                        bool b1 = d1 >= 0;
                        bool b2 = d2 >= 0;

                        if (b0 && b1 && b2)
                        {
                            continue;
                        }
                        else if ((b0 && !b1 && !b2) || (!b0 && b1 && !b2) || (!b0 && !b1 && b2))
                        {
                            float3 inV, outV1, outV2;
                            float4 inUV, outUV1, outUV2;
                            float inD, outD1, outD2;

                            if (b0)
                            {
                                inV = v0;
                                inUV = uv0;
                                inD = d0;
                                outV1 = v1;
                                outUV1 = uv1;
                                outD1 = d1;
                                outV2 = v2;
                                outUV2 = uv2;
                                outD2 = d2;
                            }
                            else if (b1)
                            {
                                inV = v1;
                                inUV = uv1;
                                inD = d1;
                                outV1 = v2;
                                outUV1 = uv2;
                                outD1 = d2;
                                outV2 = v0;
                                outUV2 = uv0;
                                outD2 = d0;
                            }
                            else
                            {
                                inV = v2;
                                inUV = uv2;
                                inD = d2;
                                outV1 = v0;
                                outUV1 = uv0;
                                outD1 = d0;
                                outV2 = v1;
                                outUV2 = uv1;
                                outD2 = d1;
                            }

                            float t1 = inD / (inD - outD1);
                            float t2 = inD / (inD - outD2);

                            temporaryvertices[baseIndex + temporaryverticescount] = inV;
                            temporaryvertices[baseIndex + temporaryverticescount + 1] = math.lerp(inV, outV1, t1);
                            temporaryvertices[baseIndex + temporaryverticescount + 2] = math.lerp(inV, outV2, t2);
                            temporaryverticescount += 3;
                            temporarytextures[baseIndex + temporarytexturescount] = inUV;
                            temporarytextures[baseIndex + temporarytexturescount + 1] = math.lerp(inUV, outUV1, t1);
                            temporarytextures[baseIndex + temporarytexturescount + 2] = math.lerp(inUV, outUV2, t2);
                            temporarytexturescount += 3;
                            processbool[c] = false;
                            processbool[c + 1] = false;
                            processbool[c + 2] = false;

                            addTriangles += 1;
                        }
                        else if ((!b0 && b1 && b2) || (b0 && !b1 && b2) || (b0 && b1 && !b2))
                        {
                            float3 inV1, inV2, outV;
                            float4 inUV1, inUV2, outUV;
                            float inD1, inD2, outD;

                            if (!b0)
                            {
                                outV = v0;
                                outUV = uv0;
                                outD = d0;
                                inV1 = v1;
                                inUV1 = uv1;
                                inD1 = d1;
                                inV2 = v2;
                                inUV2 = uv2;
                                inD2 = d2;
                            }
                            else if (!b1)
                            {
                                outV = v1;
                                outUV = uv1;
                                outD = d1;
                                inV1 = v2;
                                inUV1 = uv2;
                                inD1 = d2;
                                inV2 = v0;
                                inUV2 = uv0;
                                inD2 = d0;
                            }
                            else
                            {
                                outV = v2;
                                outUV = uv2;
                                outD = d2;
                                inV1 = v0;
                                inUV1 = uv0;
                                inD1 = d0;
                                inV2 = v1;
                                inUV2 = uv1;
                                inD2 = d1;
                            }

                            float t1 = inD1 / (inD1 - outD);
                            float t2 = inD2 / (inD2 - outD);

                            float3 vA = math.lerp(inV1, outV, t1);
                            float3 vB = math.lerp(inV2, outV, t2);

                            float4 uvA = math.lerp(inUV1, outUV, t1);
                            float4 uvB = math.lerp(inUV2, outUV, t2);

                            temporaryvertices[baseIndex + temporaryverticescount] = inV1;
                            temporaryvertices[baseIndex + temporaryverticescount + 1] = inV2;
                            temporaryvertices[baseIndex + temporaryverticescount + 2] = vA;
                            temporaryverticescount += 3;
                            temporarytextures[baseIndex + temporarytexturescount] = inUV1;
                            temporarytextures[baseIndex + temporarytexturescount + 1] = inUV2;
                            temporarytextures[baseIndex + temporarytexturescount + 2] = uvA;
                            temporarytexturescount += 3;
                            temporaryvertices[baseIndex + temporaryverticescount] = vA;
                            temporaryvertices[baseIndex + temporaryverticescount + 1] = inV2;
                            temporaryvertices[baseIndex + temporaryverticescount + 2] = vB;
                            temporaryverticescount += 3;
                            temporarytextures[baseIndex + temporarytexturescount] = uvA;
                            temporarytextures[baseIndex + temporarytexturescount + 1] = inUV2;
                            temporarytextures[baseIndex + temporarytexturescount + 2] = uvB;
                            temporarytexturescount += 3;
                            processbool[c] = false;
                            processbool[c + 1] = false;
                            processbool[c + 2] = false;

                            addTriangles += 2;
                        }
                        else
                        {
                            processbool[c] = false;
                            processbool[c + 1] = false;
                            processbool[c + 2] = false;
                        }
                    }

                    if (addTriangles > 0)
                    {
                        for (int d = baseIndex; d < baseIndex + temporaryverticescount; d += 3)
                        {
                            processvertices[baseIndex + processverticescount] = temporaryvertices[d];
                            processvertices[baseIndex + processverticescount + 1] = temporaryvertices[d + 1];
                            processvertices[baseIndex + processverticescount + 2] = temporaryvertices[d + 2];
                            processverticescount += 3;
                            processtextures[baseIndex + processtexturescount] = temporarytextures[d];
                            processtextures[baseIndex + processtexturescount + 1] = temporarytextures[d + 1];
                            processtextures[baseIndex + processtexturescount + 2] = temporarytextures[d + 2];
                            processtexturescount += 3;
                            processbool[baseIndex + processboolcount] = true;
                            processbool[baseIndex + processboolcount + 1] = true;
                            processbool[baseIndex + processboolcount + 2] = true;
                            processboolcount += 3;
                        }
                    }
                }

                for (int e = baseIndex; e < baseIndex + processverticescount; e += 3)
                {
                    if (processbool[e] == true && processbool[e + 1] == true && processbool[e + 2] == true)
                    {
                        finalTriangles.AddNoResize(new Triangle
                        {
                            v0 = processvertices[e],
                            v1 = processvertices[e + 1],
                            v2 = processvertices[e + 2],
                            uv0 = processtextures[e],
                            uv1 = processtextures[e + 1],
                            uv2 = processtextures[e + 2]
                        });
                    }
                }
            }
        }
    }

    [BurstCompile]
    public struct ClipPortalsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<PortalMeta> rawPortals;
        [ReadOnly] public NativeArray<MathematicalPlane> currentFrustums;
        [ReadOnly] public NativeArray<MathematicalPlane> originalFrustum;
        [ReadOnly] public NativeArray<float3> vertices;
        [ReadOnly] public NativeArray<int> edges;
        [ReadOnly] public float3 point;

        [NativeDisableParallelForRestriction] public NativeArray<float3> outedges;
        [NativeDisableParallelForRestriction] public NativeArray<float3> processedgevertices;
        [NativeDisableParallelForRestriction] public NativeArray<bool> processedgebool;
        [NativeDisableParallelForRestriction] public NativeArray<float3> temporaryedgevertices;
        [NativeDisableParallelForRestriction] public NativeArray<MathematicalPlane> nextFrustums;

        public NativeList<SectorMeta>.ParallelWriter nextSectors;

        public void Execute(int index)
        {
            const int MaxVertsPerEdge = 256;

            int baseIndex = index * MaxVertsPerEdge;

            if (baseIndex >= processedgevertices.Length)
            {
                return;
            }

            PortalMeta portal = rawPortals[index];

            int connectedstart = portal.polygonStartIndex;
            int connectedcount = portal.polygonCount;
            int connectedsector = portal.connectedSectorId;

            if (portal.portalContact == 0)
            {
                for (int i = 0; i < originalFrustum.Length; i++)
                {
                    nextFrustums[baseIndex + i] = originalFrustum[i];
                }

                nextSectors.AddNoResize(new SectorMeta
                {
                    polygonStartIndex = connectedstart,
                    polygonCount = connectedcount,
                    planeStartIndex = baseIndex,
                    planeCount = originalFrustum.Length,
                    sectorId = connectedsector
                });

                return;
            }

            int outedgescount = 0;
            int processedgescount = 0;
            int processedgesboolcount = 0;

            for (int a = portal.edgeStartIndex; a < portal.edgeStartIndex + portal.edgeCount; a += 2)
            {
                processedgevertices[baseIndex + processedgescount] = vertices[edges[a]];
                processedgevertices[baseIndex + processedgescount + 1] = vertices[edges[a + 1]];
                processedgescount += 2;
                processedgebool[baseIndex + processedgesboolcount] = true;
                processedgebool[baseIndex + processedgesboolcount + 1] = true;
                processedgesboolcount += 2;
            }

            for (int b = portal.planeStartIndex; b < portal.planeStartIndex + portal.planeCount; b++)
            {
                int intersection = 0;
                int temporaryverticescount = 0;

                float3 intersectionPoint1 = float3.zero;
                float3 intersectionPoint2 = float3.zero;

                for (int c = baseIndex; c < baseIndex + processedgescount; c += 2)
                {
                    if (processedgebool[c] == false && processedgebool[c + 1] == false)
                    {
                        continue;
                    }

                    float3 p1 = processedgevertices[c];
                    float3 p2 = processedgevertices[c + 1];

                    float d1 = math.dot(currentFrustums[b].normal, p1) + currentFrustums[b].distance;
                    float d2 = math.dot(currentFrustums[b].normal, p2) + currentFrustums[b].distance;

                    bool b0 = d1 >= 0;
                    bool b1 = d2 >= 0;

                    if (b0 && b1)
                    {
                        continue;
                    }
                    else if ((b0 && !b1) || (!b0 && b1))
                    {
                        float3 point1;
                        float3 point2;

                        float t = d1 / (d1 - d2);

                        float3 intersectionPoint = math.lerp(p1, p2, t);

                        if (b0)
                        {
                            point1 = p1;
                            point2 = intersectionPoint;
                            intersectionPoint1 = intersectionPoint;
                        }
                        else
                        {
                            point1 = intersectionPoint;
                            point2 = p2;
                            intersectionPoint2 = intersectionPoint;
                        }

                        temporaryedgevertices[baseIndex + temporaryverticescount] = point1;
                        temporaryedgevertices[baseIndex + temporaryverticescount + 1] = point2;
                        temporaryverticescount += 2;

                        processedgebool[c] = false;
                        processedgebool[c + 1] = false;

                        intersection += 1;
                    }
                    else
                    {
                        processedgebool[c] = false;
                        processedgebool[c + 1] = false;
                    }
                }

                if (intersection == 2)
                {
                    for (int d = baseIndex; d < baseIndex + temporaryverticescount; d += 2)
                    {
                        processedgevertices[baseIndex + processedgescount] = temporaryedgevertices[d];
                        processedgevertices[baseIndex + processedgescount + 1] = temporaryedgevertices[d + 1];
                        processedgescount += 2;
                        processedgebool[baseIndex + processedgesboolcount] = true;
                        processedgebool[baseIndex + processedgesboolcount + 1] = true;
                        processedgesboolcount += 2;
                    }

                    processedgevertices[baseIndex + processedgescount] = intersectionPoint1;
                    processedgevertices[baseIndex + processedgescount + 1] = intersectionPoint2;
                    processedgescount += 2;
                    processedgebool[baseIndex + processedgesboolcount] = true;
                    processedgebool[baseIndex + processedgesboolcount + 1] = true;
                    processedgesboolcount += 2;
                }
            }

            for (int e = baseIndex; e < baseIndex + processedgescount; e += 2)
            {
                if (processedgebool[e] == true && processedgebool[e + 1] == true)
                {
                    outedges[baseIndex + outedgescount] = processedgevertices[e];
                    outedges[baseIndex + outedgescount + 1] = processedgevertices[e + 1];
                    outedgescount += 2;
                }
            }

            if (outedgescount < 6 || outedgescount % 2 == 1)
            {
                return;
            }

            int indexCount = 0;

            for (int f = baseIndex; f < baseIndex + outedgescount; f += 2)
            {
                float3 p0 = outedges[f];
                float3 p1 = outedges[f + 1];

                float3 normal = math.cross(p0 - p1, point - p1);
                float magnitude = math.length(normal);

                if (magnitude < 0.01f)
                {
                    continue;
                }

                float3 normalized = normal / magnitude;
                float distance = -math.dot(normalized, p0);

                nextFrustums[baseIndex + indexCount] = new MathematicalPlane
                {
                    normal = normalized,
                    distance = distance
                };
                indexCount += 1;
            }

            nextSectors.AddNoResize(new SectorMeta
            {
                polygonStartIndex = connectedstart,
                polygonCount = connectedcount,
                planeStartIndex = baseIndex,
                planeCount = indexCount,
                sectorId = connectedsector
            });
        }
    }

    private static MathematicalPlane FromVec4(float4 aVec)
    {
        float3 n = new float3(aVec.x, aVec.y, aVec.z);
        float l = math.length(n);
        return new MathematicalPlane
        {
            normal = n / l,
            distance = aVec.w / l
        };
    }

    public static void SetFrustumPlanes(NativeList<MathematicalPlane> planes, Matrix4x4 m)
    {
        var r0 = m.GetRow(0);
        var r1 = m.GetRow(1);
        var r2 = m.GetRow(2);
        var r3 = m.GetRow(3);

        planes.Add(FromVec4(r3 - r0)); // Right
        planes.Add(FromVec4(r3 + r0)); // Left
        planes.Add(FromVec4(r3 - r1)); // Top
        planes.Add(FromVec4(r3 + r1)); // Bottom
        planes.Add(FromVec4(r3 - r2)); // Far
        planes.Add(FromVec4(r3 + r2)); // Near
    }

    public static void ReadFrustumPlanes(Camera cam, NativeList<MathematicalPlane> planes)
    {
        SetFrustumPlanes(planes, cam.projectionMatrix * cam.worldToCameraMatrix);
    }

    public static void PlayerInput(CharacterController Player, Camera Cam, ref float3 targetMovement, ref float2 targetRotation, ref float2 currentRotation, ref float3 currentForce, float speed, float jumpHeight, float gravity, float sensitivity, float clampAngle, float smoothFactor)
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        if (Input.GetKeyDown(KeyCode.Space) && Player.isGrounded)
        {
            currentForce.y = jumpHeight;
        }

        float mousex = Input.GetAxisRaw("Mouse X");
        float mousey = Input.GetAxisRaw("Mouse Y");

        targetRotation.x -= mousey * sensitivity;
        targetRotation.y += mousex * sensitivity;

        targetRotation.x = Mathf.Clamp(targetRotation.x, -clampAngle, clampAngle);

        currentRotation = Vector2.Lerp(currentRotation, targetRotation, smoothFactor * Time.deltaTime);

        Cam.transform.localRotation = Quaternion.Euler(currentRotation.x, 0f, 0f);
        Player.transform.rotation = Quaternion.Euler(0f, currentRotation.y, 0f);

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        targetMovement = (Player.transform.right * horizontal + Player.transform.forward * vertical).normalized;

        Player.Move((targetMovement + currentForce) * speed * Time.deltaTime);
    }

    public static bool CheckRadius(SectorMeta asector, Vector3 campoint, NativeList<MathematicalPlane> planes, NativeList<PolygonMeta> polygons)
    {
        for (int i = asector.polygonStartIndex; i < asector.polygonStartIndex + asector.polygonCount; i++)
        {
            if (GetPlaneSignedDistanceToPoint(planes[polygons[i].plane], campoint) < -0.6f)
            {
                return false;
            }
        }
        return true;
    }

    public static bool CheckSector(SectorMeta asector, Vector3 campoint, NativeList<MathematicalPlane> planes, NativeList<PolygonMeta> polygons)
    {
        for (int i = asector.polygonStartIndex; i < asector.polygonStartIndex + asector.polygonCount; i++)
        {
            if (GetPlaneSignedDistanceToPoint(planes[polygons[i].plane], campoint) < 0)
            {
                return false;
            }
        }
        return true;
    }

    public static bool SectorsContains(int sectorID, NativeList<SectorMeta> contains)
    {
        for (int i = 0; i < contains.Length; i++)
        {
            if (contains[i].sectorId == sectorID)
            {
                return true;
            }
        }
        return false;
    }

    public static bool SectorsDoNotEqual(NativeList<SectorMeta> contains, NativeList<SectorMeta> oldContains)
    {
        if (contains.Length != oldContains.Length)
        {
            return true;
        }

        for (int i = 0; i < contains.Length; i++)
        {
            if (contains[i].sectorId != oldContains[i].sectorId)
            {
                return true;
            }
        }
        return false;
    }

    public static void GetSectors(ref SectorMeta ASector, NativeList<SectorMeta> contains, NativeList<SectorMeta> oldContains, CharacterController Player, List<MeshCollider> CollisionSectors, NativeList<SectorMeta> sectors, NativeList<PolygonMeta> polygons, NativeList<MathematicalPlane> planes, Vector3 campoint, List<List<SectorMeta>> ListOfSectorLists)
    {
        int input = 0;
        int output = 1;

        contains.Clear();

        ListOfSectorLists[input].Clear();
        ListOfSectorLists[output].Clear();

        ListOfSectorLists[input].Add(ASector);

        for (int a = 0; a < oldContains.Length; a++)
        {
            Physics.IgnoreCollision(Player, CollisionSectors[oldContains[a].sectorId], true);
        }

        for (int b = 0; b < 256; b++)
        {
            if (b % 2 == 0)
            {
                input = 0;
                output = 1;
            }
            else
            {
                input = 1;
                output = 0;
            }

            ListOfSectorLists[output].Clear();

            if (ListOfSectorLists[input].Count == 0)
            {
                break;
            }

            for (int c = 0; c < ListOfSectorLists[input].Count; c++)
            {
                SectorMeta sector = ListOfSectorLists[input][c];

                contains.Add(sector);

                Physics.IgnoreCollision(Player, CollisionSectors[sector.sectorId], false);

                for (int d = sector.polygonStartIndex; d < sector.polygonStartIndex + sector.polygonCount; d++)
                {
                    int connectedsector = polygons[d].connectedSectorId;

                    if (connectedsector == -1)
                    {
                        continue;
                    }

                    SectorMeta portalsector = sectors[connectedsector];

                    if (SectorsContains(portalsector.sectorId, contains))
                    {
                        continue;
                    }

                    bool radius = CheckRadius(portalsector, campoint, planes, polygons);

                    if (radius)
                    {
                        ListOfSectorLists[output].Add(portalsector);
                    }
                }

                bool check = CheckSector(sector, campoint, planes, polygons);

                if (check)
                {
                    ASector = sector;
                }
            }
        }

        if (SectorsDoNotEqual(contains, oldContains))
        {
            oldContains.Clear();

            for (int e = 0; e < contains.Length; e++)
            {
                oldContains.Add(contains[e]);
            }
        }
    }

    public static void GetPolygons(float3 campoint, SectorMeta ASector, NativeList<SectorMeta> sideA, NativeList<SectorMeta> sideB, NativeArray<MathematicalPlane> planeA, NativeArray<MathematicalPlane> planeB, NativeList<TrianglesMeta> rawTriangles, NativeList<PortalMeta> rawPortals, NativeList<Triangle> outTriangles, NativeList<MathematicalPlane> OriginalFrustum, NativeList<SectorMeta> contains, NativeList<float3> vertices, NativeList<float4> textures, NativeList<MathematicalPlane> planes, NativeList<SectorMeta> sectors, NativeList<PolygonMeta> polygons, NativeList<int> render, NativeList<int> edges, NativeArray<float3> outEdges, NativeArray<float3> processVertices, NativeArray<float4> processTextures, NativeArray<float3> temporaryVertices, NativeArray<float4> temporaryTextures, NativeArray<bool> processBool, NativeArray<float3> processEdgeVertices, NativeArray<float3> temporaryEdgeVertices, NativeArray<bool> processEdgeBool)
    {
        sideA.Clear();
        sideB.Clear();
        outTriangles.Clear();

        planeA[0] = OriginalFrustum[0];
        planeA[1] = OriginalFrustum[1];
        planeA[2] = OriginalFrustum[2];
        planeA[3] = OriginalFrustum[3];

        sideA.Add(ASector);

        NativeList<SectorMeta> currentSectors = sideA;
        NativeList<SectorMeta> nextSectors = sideB;
        NativeArray<MathematicalPlane> currentFrustums = planeA;
        NativeArray<MathematicalPlane> nextFrustums = planeB;

        for (int i = 0; i < 256; i++)
        {
            if (i % 2 == 0)
            {
                currentSectors = sideA;
                nextSectors = sideB;
                currentFrustums = planeA;
                nextFrustums = planeB;
            }
            else
            {
                currentSectors = sideB;
                nextSectors = sideA;
                currentFrustums = planeB;
                nextFrustums = planeA;
            }

            if (currentSectors.Length == 0)
            {
                break;
            }

            nextSectors.Clear();
            rawTriangles.Clear();
            rawPortals.Clear();

            JobHandle h1 = new SectorsJob
            {
                point = campoint,
                planes = planes.AsDeferredJobArray(),
                polygons = polygons.AsDeferredJobArray(),
                contains = contains.AsDeferredJobArray(),
                sectors = sectors.AsDeferredJobArray(),
                currentSectors = currentSectors.AsDeferredJobArray(),
                rawPortals = rawPortals.AsParallelWriter(),
                rawTriangles = rawTriangles.AsParallelWriter()
            }.Schedule(currentSectors.Length, 32);

            h1.Complete();

            JobHandle h2 = new ClipTrianglesJob
            {
                rawTriangles = rawTriangles.AsDeferredJobArray(),
                vertices = vertices.AsDeferredJobArray(),
                textures = textures.AsDeferredJobArray(),
                render = render.AsDeferredJobArray(),
                processvertices = processVertices,
                processtextures = processTextures,
                processbool = processBool,
                temporaryvertices = temporaryVertices,
                temporarytextures = temporaryTextures,
                currentFrustums = currentFrustums,
                finalTriangles = outTriangles.AsParallelWriter()
            }.Schedule(rawTriangles.Length, 64);

            JobHandle h3 = new ClipPortalsJob
            {
                point = campoint,
                rawPortals = rawPortals.AsDeferredJobArray(),
                vertices = vertices.AsDeferredJobArray(),
                originalFrustum = OriginalFrustum.AsDeferredJobArray(),
                edges = edges.AsDeferredJobArray(),
                outedges = outEdges,
                processedgebool = processEdgeBool,
                temporaryedgevertices = temporaryEdgeVertices,
                processedgevertices = processEdgeVertices,
                currentFrustums = currentFrustums,
                nextFrustums = nextFrustums,
                nextSectors = nextSectors.AsParallelWriter()
            }.Schedule(rawPortals.Length, 64);

            JobHandle.CombineDependencies(h2, h3).Complete();
        }
    }
}
