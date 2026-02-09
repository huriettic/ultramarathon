using System;
using System.Collections.Generic;
using System.IO;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Weland;

public struct Triangle
{
    public float3 v0, v1, v2;
    public float4 uv0, uv1, uv2;
};

public struct MathematicalPlane
{
    public float3 normal;
    public float distance;
};

public struct StartPosition
{
    public float3 playerStart;
    public int sectorId;
};

public struct LevelLight
{
    public Color TriangleLight;
};

public struct PolygonMeta
{
    public int edgeStartIndex;
    public int edgeCount;

    public int triangleStartIndex;
    public int triangleCount;

    public int connectedSectorId;
    public int sectorId;

    public int collider;
    public int opaque;

    public int plane;
};

public struct SectorMeta
{
    public int polygonStartIndex;
    public int polygonCount;

    public int planeStartIndex;
    public int planeCount;

    public int sectorId;
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

            int render = polygon.opaque;

            int connectedsector = polygon.connectedSectorId;

            if (render != -1)
            {
                rawTriangles.AddNoResize(new TrianglesMeta
                {
                    triangleStartIndex = polygon.triangleStartIndex,
                    triangleCount = polygon.triangleCount,

                    planeStartIndex = sector.planeStartIndex,
                    planeCount = sector.planeCount,

                    sectorId = polygon.sectorId
                });

                continue;
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
    [ReadOnly] public NativeArray<int> triangles;

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

            processvertices[baseIndex + processverticescount] = vertices[triangles[a]];
            processvertices[baseIndex + processverticescount + 1] = vertices[triangles[a + 1]];
            processvertices[baseIndex + processverticescount + 2] = vertices[triangles[a + 2]];
            processverticescount += 3;
            processtextures[baseIndex + processtexturescount] = textures[triangles[a]];
            processtextures[baseIndex + processtexturescount + 1] = textures[triangles[a + 1]];
            processtextures[baseIndex + processtexturescount + 2] = textures[triangles[a + 2]];
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

public class BuildAndRunLevel : MonoBehaviour
{
    public bool SaveTheLevel = false;

    public bool LevelIsSaved = false;

    public string Name = "Hyper Cube";

    public string Textures = "Textures";

    public Level level;

    public int LevelNumber;

    public float speed = 7f;
    public float jumpHeight = 2f;
    public float gravity = 5f;
    public float sensitivity = 10f;
    public float clampAngle = 90f;
    public float smoothFactor = 25f;

    private Vector2 targetRotation;
    private Vector3 targetMovement;
    private Vector2 currentRotation;
    private Vector3 currentForce;

    private GraphicsBuffer triBuffer;

    // Default scale is 2.5, but Unreal tournament is 128
    private float Scale = 2.5f;

    private CharacterController Player;

    private bool radius;

    private bool check;

    private Color[] LightColor;

    private Camera Cam;

    private Vector3 CamPoint;

    private SectorMeta CurrentSector;

    private GameObject CollisionObjects;

    private NativeArray<bool> processbool;
    private NativeArray<float3> processvertices;
    private NativeArray<float4> processtextures;
    private NativeArray<float3> temporaryvertices;
    private NativeArray<float4> temporarytextures;
    private NativeArray<float3> outEdges;
    private NativeArray<float3> processedgevertices;
    private NativeArray<bool> processedgebool;
    private NativeArray<float3> temporaryedgevertices;
    private NativeArray<MathematicalPlane> planeA;
    private NativeArray<MathematicalPlane> planeB;
    private NativeList<SectorMeta> sideA;
    private NativeList<SectorMeta> sideB;
    private NativeList<Triangle> outTriangles;
    private NativeList<TrianglesMeta> rawTriangles;
    private NativeList<PortalMeta> rawPortals;
    private NativeList<SectorMeta> contains;
    private NativeList<SectorMeta> oldContains;
    private NativeList<MathematicalPlane> OriginalFrustum;

    private List<List<SectorMeta>> ListOfSectorLists = new List<List<SectorMeta>>();

    private List<Vector3> OpaqueVertices = new List<Vector3>();

    private List<int> OpaqueTriangles = new List<int>();

    private List<MeshCollider> CollisionSectors = new List<MeshCollider>();

    private Material opaquematerial;

    private List<Mesh> CollisionMesh = new List<Mesh>();

    private TopLevelLists LevelLists;

    [Serializable]
    public class TopLevelLists
    {
        public NativeList<float3> vertices;
        public NativeList<float4> textures;
        public NativeList<int> triangles;
        public NativeList<int> edges;
        public NativeList<MathematicalPlane> planes;
        public NativeList<PolygonMeta> polygons;
        public NativeList<SectorMeta> sectors;
        public NativeList<StartPosition> positions;
        public NativeList<LevelLight> colors;
    }

    private Plane TopPlane;

    private Plane LeftPlane;

    private List<int> walltri;

    private List<int> MeshTexture = new List<int>();

    private List<int> MeshTextureCollection = new List<int>();

    private List<Mesh> meshes = new List<Mesh>();

    private List<Vector3> CW = new List<Vector3>();

    private List<Vector2> CWUV = new List<Vector2>();

    private List<Vector2> CWUVOffset = new List<Vector2>();

    private List<Vector4> CWUVOffsetZ = new List<Vector4>();

    private List<Vector2> ceilinguvs = new List<Vector2>();

    private List<Vector4> ceilinguvsz = new List<Vector4>();

    private List<Vector2> flooruvs = new List<Vector2>();

    private List<Vector4> flooruvsz = new List<Vector4>();

    private List<Vector3> ceilingverts = new List<Vector3>();

    private List<int> ceilingtri = new List<int>();

    private List<Vector3> floorverts = new List<Vector3>();

    private List<int> floortri = new List<int>();

    private List<int> Plane = new List<int>();

    private List<int> Portal = new List<int>();

    private List<int> Render = new List<int>();

    private List<int> Collision = new List<int>();

    private List<int> Transparent = new List<int>();

    private List<Vector4> uvVector4 = new List<Vector4>();

    // Start is called before the first frame update
    void Start()
    {
        int strideTriangle = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle));

        triBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (LevelLists.polygons.Length * 32) * 32, strideTriangle);

        opaquematerial.SetBuffer("outputTriangleBuffer", triBuffer);

        for (int i = 0; i < 2; i++)
        {
            ListOfSectorLists.Add(new List<SectorMeta>());
        }

        for (int i = 0; i < LevelLists.sectors.Length; i++)
        {
            Physics.IgnoreCollision(Player, CollisionSectors[LevelLists.sectors[i].sectorId], true);
        }
    }

    void Update()
    {
        PlayerInput();

        if (Cam.transform.hasChanged)
        {
            CamPoint = Cam.transform.position;

            GetSectors(CurrentSector);

            OriginalFrustum.Clear();

            ReadFrustumPlanes(Cam, OriginalFrustum);

            OriginalFrustum.RemoveAt(5);

            OriginalFrustum.RemoveAt(4);

            GetPolygons(CurrentSector);

            Cam.transform.hasChanged = false;
        }
    }

    void OnDestroy()
    {
        triBuffer?.Dispose();

        if (LevelLists.sectors.IsCreated)
        {
            LevelLists.sectors.Dispose();
        }
        if (LevelLists.polygons.IsCreated)
        {
            LevelLists.polygons.Dispose();
        }
        if (LevelLists.vertices.IsCreated)
        {
            LevelLists.vertices.Dispose();
        }
        if (LevelLists.textures.IsCreated)
        {
            LevelLists.textures.Dispose();
        }
        if (LevelLists.triangles.IsCreated)
        {
            LevelLists.triangles.Dispose();
        }
        if (LevelLists.edges.IsCreated)
        {
            LevelLists.edges.Dispose();
        }
        if (LevelLists.positions.IsCreated)
        {
            LevelLists.positions.Dispose();
        }
        if (LevelLists.planes.IsCreated)
        {
            LevelLists.planes.Dispose();
        }
        if (contains.IsCreated)
        {
            contains.Dispose();
        }
        if (processbool.IsCreated)
        {
            processbool.Dispose();
        }
        if (processvertices.IsCreated)
        {
            processvertices.Dispose();
        }
        if (processtextures.IsCreated)
        {
            processtextures.Dispose();
        }
        if (temporaryvertices.IsCreated)
        {
            temporaryvertices.Dispose();
        }
        if (temporarytextures.IsCreated)
        {
            temporarytextures.Dispose();
        }
        if (outEdges.IsCreated)
        {
            outEdges.Dispose();
        }
        if (planeA.IsCreated)
        {
            planeA.Dispose();
        }
        if (planeB.IsCreated)
        {
            planeB.Dispose();
        }
        if (sideA.IsCreated)
        {
            sideA.Dispose();
        }
        if (sideB.IsCreated)
        {
            sideB.Dispose();
        }
        if (outTriangles.IsCreated)
        {
            outTriangles.Dispose();
        }
        if (OriginalFrustum.IsCreated)
        {
            OriginalFrustum.Dispose();
        }
        if (oldContains.IsCreated)
        {
            oldContains.Dispose();
        }
        if (rawTriangles.IsCreated)
        {
            rawTriangles.Dispose();
        }
        if (rawPortals.IsCreated)
        {
            rawPortals.Dispose();
        }
        if (processedgevertices.IsCreated)
        {
            processedgevertices.Dispose();
        }
        if (temporaryedgevertices.IsCreated)
        {
            temporaryedgevertices.Dispose();
        }
        if (processedgebool.IsCreated)
        {
            processedgebool.Dispose();
        }
        if (LevelLists.colors.IsCreated)
        {
            LevelLists.colors.Dispose();
        }
    }

    void OnRenderObject()
    {
        triBuffer.SetData(outTriangles.AsArray());

        opaquematerial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, outTriangles.Length * 3);
    }

    void Awake()
    {
        walltri = new List<int>()
        {
            0, 1, 2, 0, 2, 3
        };

        Player = GameObject.Find("Player").GetComponent<CharacterController>();

        Player.GetComponent<CharacterController>().enabled = true;

        Cursor.lockState = CursorLockMode.Locked;

        Cam = Camera.main;

        LevelLists = new TopLevelLists();

        LevelLists.edges = new NativeList<int>(Allocator.Persistent);
        LevelLists.triangles = new NativeList<int>(Allocator.Persistent);
        LevelLists.vertices = new NativeList<float3>(Allocator.Persistent);
        LevelLists.textures = new NativeList<float4>(Allocator.Persistent);
        LevelLists.sectors = new NativeList<SectorMeta>(Allocator.Persistent);
        LevelLists.planes = new NativeList<MathematicalPlane>(Allocator.Persistent);
        LevelLists.polygons = new NativeList<PolygonMeta>(Allocator.Persistent);
        LevelLists.positions = new NativeList<StartPosition>(Allocator.Persistent);
        LevelLists.colors = new NativeList<LevelLight>(Allocator.Persistent);

        CollisionObjects = new GameObject("Collision Meshes");

        LoadLevel();

        LightColor = new Color[LevelLists.colors.Length];

        CreateMaterial();

        BuildColliders();

        PlayerStart();

        processbool = new NativeArray<bool>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        processvertices = new NativeArray<float3>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        processtextures = new NativeArray<float4>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        temporaryvertices = new NativeArray<float3>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        temporarytextures = new NativeArray<float4>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        processedgebool = new NativeArray<bool>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        processedgevertices = new NativeArray<float3>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        temporaryedgevertices = new NativeArray<float3>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        outEdges = new NativeArray<float3>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        planeA = new NativeArray<MathematicalPlane>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        planeB = new NativeArray<MathematicalPlane>(LevelLists.polygons.Length * 256, Allocator.Persistent);
        contains = new NativeList<SectorMeta>(Allocator.Persistent);
        oldContains = new NativeList<SectorMeta>(Allocator.Persistent);
        sideA = new NativeList<SectorMeta>(LevelLists.sectors.Length * 32, Allocator.Persistent);
        sideB = new NativeList<SectorMeta>(LevelLists.sectors.Length * 32, Allocator.Persistent);
        outTriangles = new NativeList<Triangle>((LevelLists.polygons.Length * 32) * 32, Allocator.Persistent);
        OriginalFrustum = new NativeList<MathematicalPlane>(6, Allocator.Persistent);
        rawTriangles = new NativeList<TrianglesMeta>(LevelLists.polygons.Length * 32, Allocator.Persistent);
        rawPortals = new NativeList<PortalMeta>(LevelLists.polygons.Length * 32, Allocator.Persistent);
    }

    void FixedUpdate()
    {
        if (!Player.isGrounded)
        {
            currentForce.y -= gravity * Time.deltaTime;
        }
    }

    public void SaveLevel()
    {
        try
        {
            string saveData = JsonUtility.ToJson(LevelLists, true);
            string path = Path.Combine(Application.persistentDataPath, Name + ".txt");

            File.WriteAllText(path, saveData);
            Debug.Log("Data saved successfully to " + path);
        }
        catch (Exception exit)
        {
            Debug.LogError("Failed to save data: " + exit.Message);
        }
    }

    public void LoadLevel()
    {
        if (LevelIsSaved == false)
        {
            Debug.Log("This is a .sceA file.");

            MapFile map = new MapFile();

            level = new Level();

            try
            {
                // Change name to load a different map
                map.Load(Path.Combine(Application.streamingAssetsPath, Name + ".sceA"));
                Debug.Log("Map loaded successfully!");
            }
            catch (Exception exit)
            {
                Debug.LogError("Failed to load Map: " + exit.Message);
            }

            try
            {
                // Change the map directory number if the map has more than one level 
                level.Load(map.Directory[LevelNumber]);
                Debug.Log("Level loaded successfully!");
            }
            catch (Exception exit)
            {
                Debug.LogError("Failed to load level: " + exit.Message);
            }

            BuildTheLists();

            BuildObjects();

            BuildLights();

            BuildGeometry();

            if (SaveTheLevel)
            {
                SaveLevel();
            }
        }
        else if (LevelIsSaved)
        {
            Debug.Log("This is a .txt file.");

            string path = Path.Combine(Application.persistentDataPath, Name + ".txt");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                LevelLists = JsonUtility.FromJson<TopLevelLists>(json);
            }
        }
    }

    public void CreateMaterial()
    {
        Shader shader = Resources.Load<Shader>("TriangleTexArray");

        for (int i = 0; i < LevelLists.colors.Length; i++)
        {
            LightColor[i] = new Color(LevelLists.colors[i].TriangleLight.r, LevelLists.colors[i].TriangleLight.g, LevelLists.colors[i].TriangleLight.b, 1.0f);
        }

        opaquematerial = new Material(shader);

        opaquematerial.mainTexture = Resources.Load<Texture2DArray>(Textures);

        opaquematerial.SetColorArray("_ColorArray", LightColor);
    }

    public void PlayerStart()
    {
        if (LevelLists.positions.Length == 0)
        {
            Debug.LogError("No player starts available.");

            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, LevelLists.positions.Length);

        StartPosition selectedPosition = LevelLists.positions[randomIndex];

        CurrentSector = LevelLists.sectors[selectedPosition.sectorId];

        Player.transform.position = new Vector3(selectedPosition.playerStart.x, selectedPosition.playerStart.y + 1.10f, selectedPosition.playerStart.z);
    }

    private MathematicalPlane FromVec4(Vector4 aVec)
    {
        Vector3 n = new Vector3(aVec.x, aVec.y, aVec.z);
        float l = n.magnitude;
        return new MathematicalPlane
        {
            normal = n / l,
            distance = aVec.w / l
        };
    }

    public void SetFrustumPlanes(NativeList<MathematicalPlane> planes, Matrix4x4 m)
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

    public void ReadFrustumPlanes(Camera cam, NativeList<MathematicalPlane> planes)
    {
        SetFrustumPlanes(planes, cam.projectionMatrix * cam.worldToCameraMatrix);
    }

    public void BuildColliders()
    {
        float tinyNumber = 1e-6f;

        for (int i = 0; i < LevelLists.sectors.Length; i++)
        {
            OpaqueVertices.Clear();

            OpaqueTriangles.Clear();

            int triangleCount = 0;

            for (int e = LevelLists.sectors[i].polygonStartIndex; e < LevelLists.sectors[i].polygonStartIndex + LevelLists.sectors[i].polygonCount; e++)
            {
                if (LevelLists.polygons[e].collider != -1)
                {
                    for (int f = LevelLists.polygons[e].triangleStartIndex; f < LevelLists.polygons[e].triangleStartIndex + LevelLists.polygons[e].triangleCount; f += 3)
                    {
                        Vector3 v0 = LevelLists.vertices[LevelLists.triangles[f]];
                        Vector3 v1 = LevelLists.vertices[LevelLists.triangles[f + 1]];
                        Vector3 v2 = LevelLists.vertices[LevelLists.triangles[f + 2]];

                        Vector3 e0 = v1 - v0;
                        Vector3 e1 = v2 - v1;
                        Vector3 e2 = v2 - v0;

                        if (e0.sqrMagnitude < tinyNumber || e1.sqrMagnitude < tinyNumber || e2.sqrMagnitude < tinyNumber)
                        {
                            continue;
                        }

                        OpaqueVertices.Add(v0);
                        OpaqueVertices.Add(v1);
                        OpaqueVertices.Add(v2);
                        OpaqueTriangles.Add(triangleCount);
                        OpaqueTriangles.Add(triangleCount + 1);
                        OpaqueTriangles.Add(triangleCount + 2);
                        triangleCount += 3;
                    }
                }
            }

            Mesh combinedmesh = new Mesh();

            CollisionMesh.Add(combinedmesh);

            combinedmesh.SetVertices(OpaqueVertices);

            combinedmesh.SetTriangles(OpaqueTriangles, 0);

            GameObject meshObject = new GameObject("Collision " + i);

            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();

            meshCollider.sharedMesh = combinedmesh;

            CollisionSectors.Add(meshCollider);

            meshObject.transform.SetParent(CollisionObjects.transform);
        }
    }

    public void PlayerInput()
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

    public float GetPlaneSignedDistanceToPoint(MathematicalPlane plane, Vector3 point)
    {
        return Vector3.Dot(plane.normal, point) + plane.distance;
    }

    public bool CheckRadius(SectorMeta asector, Vector3 campoint)
    {
        for (int i = asector.polygonStartIndex; i < asector.polygonStartIndex + asector.polygonCount; i++)
        {
            if (GetPlaneSignedDistanceToPoint(LevelLists.planes[LevelLists.polygons[i].plane], campoint) < -0.6f)
            {
                return false;
            }
        }
        return true;
    }

    public bool CheckSector(SectorMeta asector, Vector3 campoint)
    {
        for (int i = asector.polygonStartIndex; i < asector.polygonStartIndex + asector.polygonCount; i++)
        {
            if (GetPlaneSignedDistanceToPoint(LevelLists.planes[LevelLists.polygons[i].plane], campoint) < 0)
            {
                return false;
            }
        }
        return true;
    }

    public bool SectorsContains(int sectorID)
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

    public bool SectorsDoNotEqual()
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

    public void GetSectors(SectorMeta ASector)
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
                    int connectedsector = LevelLists.polygons[d].connectedSectorId;

                    if (connectedsector == -1)
                    {
                        continue;
                    }

                    SectorMeta portalsector = LevelLists.sectors[connectedsector];

                    if (SectorsContains(portalsector.sectorId))
                    {
                        continue;
                    }

                    radius = CheckRadius(portalsector, CamPoint);

                    if (radius)
                    {
                        ListOfSectorLists[output].Add(portalsector);
                    }
                }

                check = CheckSector(sector, CamPoint);

                if (check)
                {
                    CurrentSector = sector;
                }
            }
        }

        if (SectorsDoNotEqual())
        {
            oldContains.Clear();

            for (int e = 0; e < contains.Length; e++)
            {
                oldContains.Add(contains[e]);
            }
        }
    }

    public void GetPolygons(SectorMeta ASector)
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
                point = CamPoint,
                planes = LevelLists.planes.AsDeferredJobArray(),
                polygons = LevelLists.polygons.AsDeferredJobArray(),
                contains = contains.AsDeferredJobArray(),
                sectors = LevelLists.sectors.AsDeferredJobArray(),
                currentSectors = currentSectors.AsDeferredJobArray(),
                rawPortals = rawPortals.AsParallelWriter(),
                rawTriangles = rawTriangles.AsParallelWriter()
            }.Schedule(currentSectors.Length, 32);

            h1.Complete();

            int rawTrianglesCount = rawTriangles.Length;

            if (outTriangles.Capacity < rawTrianglesCount)
            {
                outTriangles.Capacity = rawTrianglesCount;
            }

            JobHandle h2 = new ClipTrianglesJob
            {
                rawTriangles = rawTriangles.AsDeferredJobArray(),
                vertices = LevelLists.vertices.AsDeferredJobArray(),
                textures = LevelLists.textures.AsDeferredJobArray(),
                triangles = LevelLists.triangles.AsDeferredJobArray(),
                processvertices = processvertices,
                processtextures = processtextures,
                processbool = processbool,
                temporaryvertices = temporaryvertices,
                temporarytextures = temporarytextures,
                currentFrustums = currentFrustums,
                finalTriangles = outTriangles.AsParallelWriter()
            }.Schedule(rawTriangles.Length, 64);

            int rawPortalsCount = rawPortals.Length;

            if (nextSectors.Capacity < rawPortalsCount)
            {
                nextSectors.Capacity = rawPortalsCount;
            }

            JobHandle h3 = new ClipPortalsJob
            {
                point = CamPoint,
                rawPortals = rawPortals.AsDeferredJobArray(),
                vertices = LevelLists.vertices.AsDeferredJobArray(),
                originalFrustum = OriginalFrustum.AsDeferredJobArray(),
                edges = LevelLists.edges.AsDeferredJobArray(),
                outedges = outEdges,
                processedgebool = processedgebool,
                temporaryedgevertices = temporaryedgevertices,
                processedgevertices = processedgevertices,
                currentFrustums = currentFrustums,
                nextFrustums = nextFrustums,
                nextSectors = nextSectors.AsParallelWriter()
            }.Schedule(rawPortals.Length, 64);

            JobHandle.CombineDependencies(h2, h3).Complete();
        }
    }

    public void BuildGeometry()
    {
        int planeStart = 0;
        int polygonStart = 0;

        for (int a = 0; a < level.Polygons.Count; a++)
        {
            int polygonCount = 0;

            for (int b = 0; b < Plane.Count; b++)
            {
                if (Plane[b] != a)
                {
                    continue;
                }

                PolygonMeta meta = new PolygonMeta();
                Mesh mesh = meshes[b];

                if (mesh.vertices == null || mesh.vertices.Length == 0)
                {
                    continue;
                }

                int vertexStart = LevelLists.vertices.Length;
                int vertexCount = mesh.vertices.Length;

                for (int c = 0; c < mesh.vertices.Length; c++)
                {
                    LevelLists.vertices.Add(mesh.vertices[c]);
                }

                uvVector4.Clear();
                mesh.GetUVs(0, uvVector4);

                for (int c = 0; c < uvVector4.Count; c++)
                {
                    LevelLists.textures.Add(uvVector4[c]);
                }

                MathematicalPlane plane = new MathematicalPlane
                {
                    normal = mesh.normals[0],
                    distance = -Vector3.Dot(mesh.normals[0], mesh.vertices[0])
                };

                LevelLists.planes.Add(plane);

                if (Render[b] == a && Collision[b] == a && Portal[b] == -1)
                {
                    int triangleStart = LevelLists.triangles.Length;

                    meta.triangleStartIndex = triangleStart;
                    meta.triangleCount = mesh.triangles.Length;

                    for (int d = 0; d < mesh.triangles.Length; d++)
                    {
                        LevelLists.triangles.Add(vertexStart + mesh.triangles[d]);
                    }

                    meta.edgeStartIndex = -1;
                    meta.edgeCount = -1;

                    meta.opaque = a;
                    meta.collider = a;
                }
                else if (Render[b] == -1 && Collision[b] == a && Portal[b] != -1)
                {
                    int triangleStart = LevelLists.triangles.Length;

                    meta.triangleStartIndex = triangleStart;
                    meta.triangleCount = mesh.triangles.Length;

                    for (int d = 0; d < mesh.triangles.Length; d++)
                    {
                        LevelLists.triangles.Add(vertexStart + mesh.triangles[d]);
                    }

                    int edgeStart = LevelLists.edges.Length;

                    meta.edgeStartIndex = edgeStart;
                    meta.edgeCount = 8;

                    LevelLists.edges.Add(vertexStart);
                    LevelLists.edges.Add(vertexStart + 1);
                    LevelLists.edges.Add(vertexStart + 1);
                    LevelLists.edges.Add(vertexStart + 2);
                    LevelLists.edges.Add(vertexStart + 2);
                    LevelLists.edges.Add(vertexStart + 3);
                    LevelLists.edges.Add(vertexStart + 3);
                    LevelLists.edges.Add(vertexStart);

                    meta.opaque = -1;
                    meta.collider = a;
                }
                else if (Render[b] == -1 && Collision[b] == -1 && Portal[b] != -1)
                {
                    int edgeStart = LevelLists.edges.Length;

                    meta.edgeStartIndex = edgeStart;
                    meta.edgeCount = 8;

                    LevelLists.edges.Add(vertexStart);
                    LevelLists.edges.Add(vertexStart + 1);
                    LevelLists.edges.Add(vertexStart + 1);
                    LevelLists.edges.Add(vertexStart + 2);
                    LevelLists.edges.Add(vertexStart + 2);
                    LevelLists.edges.Add(vertexStart + 3);
                    LevelLists.edges.Add(vertexStart + 3);
                    LevelLists.edges.Add(vertexStart);

                    meta.opaque = -1;
                    meta.collider = -1;
                }

                meta.sectorId = a;
                meta.connectedSectorId = Portal[b];

                meta.plane = planeStart;

                LevelLists.polygons.Add(meta);
                polygonCount += 1;
                planeStart += 1;
            }

            SectorMeta sectorMeta = new SectorMeta
            {
                sectorId = a,
                polygonStartIndex = polygonStart,
                polygonCount = polygonCount,
                planeStartIndex = 0,
                planeCount = 4
            };

            LevelLists.sectors.Add(sectorMeta);

            polygonStart += polygonCount;
        }

        Debug.Log("Level built successfully!");
    }

    public void BuildLights()
    {
        for (int i = 0; i < level.Lights.Count; i++)
        {
            Weland.Light.Function alight = level.Lights[i].PrimaryActive;

            LevelLight color = new LevelLight();

            color.TriangleLight = new Color((float)alight.Intensity, (float)alight.Intensity, (float)alight.Intensity, 1.0f);

            LevelLists.colors.Add(color);
        }
    }

    public void BuildObjects()
    {
        for (int i = 0; i < level.Objects.Count; i++)
        {
            if (level.Objects[i].Type == ObjectType.Player)
            {
                StartPosition start = new StartPosition
                {
                    playerStart = new Vector3((float)level.Objects[i].X / 1024 * Scale, (float)level.Polygons[level.Objects[i].PolygonIndex].FloorHeight / 1024 * Scale, (float)level.Objects[i].Y / 1024 * Scale * -1),

                    sectorId = level.Objects[i].PolygonIndex
                };

                LevelLists.positions.Add(start);
            }
        }
    }

    public void BuildTheLists()
    {
        for (int i = 0; i < level.Polygons.Count; ++i)
        {
            var polygon = level.Polygons[i];

            for (int e = 0; e < polygon.VertexCount; e++)
            {
                int lineIndex = polygon.LineIndexes[e];

                var line = level.Lines[lineIndex];

                int vA;
                int vB;
                int sideIndex;
                int ownerA;
                int ownerB;

                if (line.ClockwisePolygonOwner == i)
                {
                    vA = line.EndpointIndexes[0];
                    vB = line.EndpointIndexes[1];
                    ownerA = line.ClockwisePolygonOwner;
                    ownerB = line.CounterclockwisePolygonOwner;
                    sideIndex = line.ClockwisePolygonSideIndex;
                }
                else if (line.CounterclockwisePolygonOwner == i)
                {
                    vA = line.EndpointIndexes[1];
                    vB = line.EndpointIndexes[0];
                    ownerA = line.CounterclockwisePolygonOwner;
                    ownerB = line.ClockwisePolygonOwner;
                    sideIndex = line.CounterclockwisePolygonSideIndex;
                }
                else
                {
                    continue;
                }

                double X1 = (float)level.Endpoints[vA].X / 1024 * Scale;
                double Z1 = (float)level.Endpoints[vA].Y / 1024 * Scale * -1;

                double X0 = (float)level.Endpoints[vB].X / 1024 * Scale;
                double Z0 = (float)level.Endpoints[vB].Y / 1024 * Scale * -1;

                if (ownerB == -1)
                {
                    double V0 = (float)polygon.CeilingHeight / 1024 * Scale;
                    double V1 = (float)polygon.FloorHeight / 1024 * Scale;
                    
                    CW.Clear();
                    CWUV.Clear();
                    CWUVOffset.Clear();
                    CWUVOffsetZ.Clear();

                    GetVerts(X0, X1, V1, V0, Z0, Z1);

                    if (sideIndex != -1)
                    {
                        MakeSides(level.Sides[sideIndex].Primary, level.Sides[sideIndex].PrimaryLightsourceIndex);

                        if (level.Sides[sideIndex].Primary.Texture.Collection == 27 || level.Sides[sideIndex].Primary.Texture.Collection == 28 ||
                            level.Sides[sideIndex].Primary.Texture.Collection == 29 || level.Sides[sideIndex].Primary.Texture.Collection == 30)
                        {
                            Render.Add(-1);

                            MeshTexture.Add(-1);

                            MeshTextureCollection.Add(-1);
                        }
                        else
                        {
                            Render.Add(ownerA);

                            MeshTexture.Add(level.Sides[sideIndex].Primary.Texture.Bitmap);

                            MeshTextureCollection.Add(level.Sides[sideIndex].Primary.Texture.Collection);
                        }
                    }
                    else
                    {
                        Render.Add(-1);

                        MeshTexture.Add(-1);

                        MeshTextureCollection.Add(-1);
                    }

                    Plane.Add(ownerA);

                    Portal.Add(-1);

                    Collision.Add(ownerA);

                    Transparent.Add(-1);

                    Mesh mesh = new Mesh();

                    mesh.SetVertices(CW);

                    if (sideIndex == -1)
                    {
                        mesh.SetUVs(0, CWUV);
                    }
                    else
                    {
                        if (level.Sides[sideIndex].Primary.Texture.IsEmpty())
                        {
                            mesh.SetUVs(0, CWUV);
                        }
                        else
                        {
                            mesh.SetUVs(0, CWUVOffsetZ);
                        }
                    }

                    mesh.SetTriangles(walltri, 0);
                    mesh.RecalculateNormals();

                    meshes.Add(mesh);
                }
                else
                {
                    if (polygon.CeilingHeight > line.LowestAdjacentCeiling)
                    {
                        if (polygon.FloorHeight < line.LowestAdjacentCeiling)
                        {
                            double YC0 = (float)polygon.CeilingHeight / 1024 * Scale;
                            double YC1 = (float)line.LowestAdjacentCeiling / 1024 * Scale;

                            CW.Clear();
                            CWUV.Clear();
                            CWUVOffset.Clear();
                            CWUVOffsetZ.Clear();

                            GetVerts(X0, X1, YC1, YC0, Z0, Z1);

                            if (sideIndex != -1)
                            {
                                MakeSides(level.Sides[sideIndex].Primary, level.Sides[sideIndex].PrimaryLightsourceIndex);

                                if (level.Sides[sideIndex].Primary.Texture.Collection == 27 || level.Sides[sideIndex].Primary.Texture.Collection == 28 ||
                                    level.Sides[sideIndex].Primary.Texture.Collection == 29 || level.Sides[sideIndex].Primary.Texture.Collection == 30)
                                {
                                    Render.Add(-1);

                                    MeshTexture.Add(-1);

                                    MeshTextureCollection.Add(-1);
                                }
                                else
                                {
                                    Render.Add(ownerA);

                                    MeshTexture.Add(level.Sides[sideIndex].Primary.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[sideIndex].Primary.Texture.Collection);
                                }
                            }
                            else
                            {
                                Render.Add(-1);

                                MeshTexture.Add(-1);

                                MeshTextureCollection.Add(-1);
                            }

                            Plane.Add(ownerA);

                            Portal.Add(-1);

                            Collision.Add(ownerA);

                            Transparent.Add(-1);

                            Mesh mesh = new Mesh();

                            mesh.SetVertices(CW);

                            if (sideIndex == -1)
                            {
                                mesh.SetUVs(0, CWUV);
                            }
                            else
                            {
                                if (level.Sides[sideIndex].Primary.Texture.IsEmpty())
                                {
                                    mesh.SetUVs(0, CWUV);
                                }
                                else
                                {
                                    mesh.SetUVs(0, CWUVOffsetZ);
                                }
                            }

                            mesh.SetTriangles(walltri, 0);
                            mesh.RecalculateNormals();

                            meshes.Add(mesh);
                        }
                        else
                        {
                            double YC0 = (float)polygon.CeilingHeight / 1024 * Scale;
                            double YC1 = (float)polygon.FloorHeight / 1024 * Scale;

                            CW.Clear();
                            CWUV.Clear();
                            CWUVOffset.Clear();
                            CWUVOffsetZ.Clear();

                            GetVerts(X0, X1, YC1, YC0, Z0, Z1);

                            if (sideIndex != -1)
                            {
                                MakeSides(level.Sides[sideIndex].Primary, level.Sides[sideIndex].PrimaryLightsourceIndex);

                                if (level.Sides[sideIndex].Primary.Texture.Collection == 27 || level.Sides[sideIndex].Primary.Texture.Collection == 28 ||
                                    level.Sides[sideIndex].Primary.Texture.Collection == 29 || level.Sides[sideIndex].Primary.Texture.Collection == 30)
                                {
                                    Render.Add(-1);

                                    MeshTexture.Add(-1);

                                    MeshTextureCollection.Add(-1);
                                }
                                else
                                {
                                    Render.Add(ownerA);

                                    MeshTexture.Add(level.Sides[sideIndex].Primary.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[sideIndex].Primary.Texture.Collection);
                                }
                            }
                            else
                            {
                                Render.Add(-1);

                                MeshTexture.Add(-1);

                                MeshTextureCollection.Add(-1);
                            }

                            Plane.Add(ownerA);

                            Portal.Add(-1);

                            Collision.Add(ownerA);

                            Transparent.Add(-1);

                            Mesh mesh = new Mesh();

                            mesh.SetVertices(CW);

                            if (sideIndex == -1)
                            {
                                mesh.SetUVs(0, CWUV);
                            }
                            else
                            {
                                if (level.Sides[sideIndex].Primary.Texture.IsEmpty())
                                {
                                    mesh.SetUVs(0, CWUV);
                                }
                                else
                                {
                                    mesh.SetUVs(0, CWUVOffsetZ);
                                }
                            }

                            mesh.SetTriangles(walltri, 0);
                            mesh.RecalculateNormals();

                            meshes.Add(mesh);
                        }
                    }
                    if (line.LowestAdjacentCeiling != line.HighestAdjacentFloor)
                    {
                        if (polygon.CeilingHeight > line.HighestAdjacentFloor &&
                            polygon.FloorHeight < line.LowestAdjacentCeiling)
                        {
                            double YC = (float)line.LowestAdjacentCeiling / 1024 * Scale;
                            double YF = (float)line.HighestAdjacentFloor / 1024 * Scale;

                            CW.Clear();
                            CWUV.Clear();
                            CWUVOffset.Clear();
                            CWUVOffsetZ.Clear();

                            GetVerts(X0, X1, YF, YC, Z0, Z1);

                            Plane.Add(ownerA);

                            Portal.Add(ownerB);

                            if (sideIndex != -1)
                            {
                                if (!level.Sides[sideIndex].Transparent.Texture.IsEmpty())
                                {
                                    if (level.Sides[sideIndex].Transparent.Texture.Collection == 27 || level.Sides[sideIndex].Transparent.Texture.Collection == 28 ||
                                        level.Sides[sideIndex].Transparent.Texture.Collection == 29 || level.Sides[sideIndex].Transparent.Texture.Collection == 30)
                                    {
                                        Render.Add(-1);

                                        Transparent.Add(-1);
                                    }
                                    else
                                    {
                                        Render.Add(-1);

                                        Transparent.Add(ownerA);
                                    }

                                    MakeSides(level.Sides[sideIndex].Transparent, level.Sides[sideIndex].TransparentLightsourceIndex);

                                    MeshTexture.Add(level.Sides[sideIndex].Transparent.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[sideIndex].Transparent.Texture.Collection);
                                }
                                else
                                {
                                    Render.Add(-1);

                                    Transparent.Add(-1);

                                    MeshTexture.Add(-1);

                                    MeshTextureCollection.Add(-1);
                                }
                            }
                            else
                            {
                                Render.Add(-1);

                                Transparent.Add(-1);

                                MeshTexture.Add(-1);

                                MeshTextureCollection.Add(-1);
                            }

                            if (line.Solid == true)
                            {
                                Collision.Add(ownerA);
                            }
                            else
                            {
                                Collision.Add(-1);
                            }

                            Mesh mesh = new Mesh();

                            mesh.SetVertices(CW);

                            if (sideIndex != -1)
                            {
                                if (!level.Sides[sideIndex].Transparent.Texture.IsEmpty())
                                {
                                    mesh.SetUVs(0, CWUVOffsetZ);
                                }
                                else
                                {
                                    mesh.SetUVs(0, CWUV);
                                }
                            }
                            else
                            {
                                mesh.SetUVs(0, CWUV);
                            }

                            mesh.SetTriangles(walltri, 0);
                            mesh.RecalculateNormals();

                            meshes.Add(mesh);
                        }
                    }

                    if (polygon.FloorHeight < line.HighestAdjacentFloor)
                    {
                        if (polygon.CeilingHeight > line.HighestAdjacentFloor)
                        {
                            double YF0 = (float)polygon.FloorHeight / 1024 * Scale;
                            double YF1 = (float)line.HighestAdjacentFloor / 1024 * Scale;

                            CW.Clear();
                            CWUV.Clear();
                            CWUVOffset.Clear();
                            CWUVOffsetZ.Clear();

                            GetVerts(X0, X1, YF0, YF1, Z0, Z1);

                            if (sideIndex != -1)
                            {
                                if (level.Sides[sideIndex].Type == SideType.Low)
                                {
                                    MakeSides(level.Sides[sideIndex].Primary, level.Sides[sideIndex].PrimaryLightsourceIndex);

                                    if (level.Sides[sideIndex].Primary.Texture.Collection == 27 || level.Sides[sideIndex].Primary.Texture.Collection == 28 ||
                                        level.Sides[sideIndex].Primary.Texture.Collection == 29 || level.Sides[sideIndex].Primary.Texture.Collection == 30)
                                    {
                                        Render.Add(-1);
                                    }
                                    else
                                    {
                                        Render.Add(ownerA);
                                    }

                                    MeshTexture.Add(level.Sides[sideIndex].Primary.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[sideIndex].Primary.Texture.Collection);
                                }
                                else if (level.Sides[sideIndex].Type == SideType.Split)
                                {
                                    MakeSides(level.Sides[sideIndex].Secondary, level.Sides[sideIndex].SecondaryLightsourceIndex);

                                    if (level.Sides[sideIndex].Secondary.Texture.Collection == 27 || level.Sides[sideIndex].Secondary.Texture.Collection == 28 ||
                                        level.Sides[sideIndex].Secondary.Texture.Collection == 29 || level.Sides[sideIndex].Secondary.Texture.Collection == 30)
                                    {
                                        Render.Add(-1);
                                    }
                                    else
                                    {
                                        Render.Add(ownerA);
                                    }

                                    MeshTexture.Add(level.Sides[sideIndex].Secondary.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[sideIndex].Secondary.Texture.Collection);
                                }
                                else
                                {
                                    Render.Add(-1);

                                    MeshTexture.Add(-1);

                                    MeshTextureCollection.Add(-1);
                                }
                            }
                            else
                            {
                                Render.Add(-1);

                                MeshTexture.Add(-1);

                                MeshTextureCollection.Add(-1);
                            }

                            Plane.Add(ownerA);

                            Portal.Add(-1);

                            Collision.Add(ownerA);

                            Transparent.Add(-1);

                            Mesh mesh = new Mesh();

                            mesh.SetVertices(CW);

                            if (sideIndex != -1)
                            {
                                if (level.Sides[sideIndex].Type == SideType.Low)
                                {
                                    if (sideIndex == -1)
                                    {
                                        mesh.SetUVs(0, CWUV);
                                    }
                                    else
                                    {
                                        if (level.Sides[sideIndex].Primary.Texture.IsEmpty())
                                        {
                                            mesh.SetUVs(0, CWUV);
                                        }
                                        else
                                        {
                                            mesh.SetUVs(0, CWUVOffsetZ);
                                        }
                                    }
                                }
                                else if (level.Sides[sideIndex].Type == SideType.Split)
                                {
                                    if (sideIndex == -1)
                                    {
                                        mesh.SetUVs(0, CWUV);
                                    }
                                    else
                                    {
                                        if (level.Sides[sideIndex].Secondary.Texture.IsEmpty())
                                        {
                                            mesh.SetUVs(0, CWUV);
                                        }
                                        else
                                        {
                                            mesh.SetUVs(0, CWUVOffsetZ);
                                        }
                                    }
                                }
                                else
                                {
                                    mesh.SetUVs(0, CWUV);
                                }
                            }
                            else
                            {
                                mesh.SetUVs(0, CWUV);
                            }

                            mesh.SetTriangles(walltri, 0);
                            mesh.RecalculateNormals();

                            meshes.Add(mesh);
                        }
                        else
                        {
                            double YF0 = (float)polygon.FloorHeight / 1024 * Scale;
                            double YF1 = (float)polygon.CeilingHeight / 1024 * Scale;

                            CW.Clear();
                            CWUV.Clear();
                            CWUVOffset.Clear();
                            CWUVOffsetZ.Clear();

                            GetVerts(X0, X1, YF0, YF1, Z0, Z1);

                            if (sideIndex != -1)
                            {
                                if (level.Sides[sideIndex].Type == SideType.Low)
                                {
                                    MakeSides(level.Sides[sideIndex].Primary, level.Sides[sideIndex].PrimaryLightsourceIndex);

                                    if (level.Sides[sideIndex].Primary.Texture.Collection == 27 || level.Sides[sideIndex].Primary.Texture.Collection == 28 ||
                                        level.Sides[sideIndex].Primary.Texture.Collection == 29 || level.Sides[sideIndex].Primary.Texture.Collection == 30)
                                    {
                                        Render.Add(-1);
                                    }
                                    else
                                    {
                                        Render.Add(ownerA);
                                    }

                                    MeshTexture.Add(level.Sides[sideIndex].Primary.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[sideIndex].Primary.Texture.Collection);
                                }
                                else if (level.Sides[sideIndex].Type == SideType.Split)
                                {
                                    MakeSides(level.Sides[sideIndex].Secondary, level.Sides[sideIndex].SecondaryLightsourceIndex);

                                    if (level.Sides[sideIndex].Secondary.Texture.Collection == 27 || level.Sides[sideIndex].Secondary.Texture.Collection == 28 ||
                                        level.Sides[sideIndex].Secondary.Texture.Collection == 29 || level.Sides[sideIndex].Secondary.Texture.Collection == 30)
                                    {
                                        Render.Add(-1);
                                    }
                                    else
                                    {
                                        Render.Add(ownerA);
                                    }

                                    MeshTexture.Add(level.Sides[sideIndex].Secondary.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[sideIndex].Secondary.Texture.Collection);
                                }
                                else
                                {
                                    Render.Add(-1);

                                    MeshTexture.Add(-1);

                                    MeshTextureCollection.Add(-1);
                                }
                            }
                            else
                            {
                                Render.Add(-1);

                                MeshTexture.Add(-1);

                                MeshTextureCollection.Add(-1);
                            }

                            Plane.Add(ownerA);

                            Portal.Add(-1);

                            Collision.Add(ownerA);

                            Transparent.Add(-1);

                            Mesh mesh = new Mesh();

                            mesh.SetVertices(CW);

                            if (sideIndex != -1)
                            {
                                if (level.Sides[sideIndex].Type == SideType.Low)
                                {
                                    if (sideIndex == -1)
                                    {
                                        mesh.SetUVs(0, CWUV);
                                    }
                                    else
                                    {
                                        if (level.Sides[sideIndex].Primary.Texture.IsEmpty())
                                        {
                                            mesh.SetUVs(0, CWUV);
                                        }
                                        else
                                        {
                                            mesh.SetUVs(0, CWUVOffsetZ);
                                        }
                                    }
                                }
                                else if (level.Sides[sideIndex].Type == SideType.Split)
                                {
                                    if (sideIndex == -1)
                                    {
                                        mesh.SetUVs(0, CWUV);
                                    }
                                    else
                                    {
                                        if (level.Sides[sideIndex].Secondary.Texture.IsEmpty())
                                        {
                                            mesh.SetUVs(0, CWUV);
                                        }
                                        else
                                        {
                                            mesh.SetUVs(0, CWUVOffsetZ);
                                        }
                                    }
                                }
                                else
                                {
                                    mesh.SetUVs(0, CWUV);
                                }
                            }
                            else
                            {
                                mesh.SetUVs(0, CWUV);
                            }

                            mesh.SetTriangles(walltri, 0);
                            mesh.RecalculateNormals();

                            meshes.Add(mesh);
                        }
                    }
                }
            }

            if (polygon.FloorHeight != polygon.CeilingHeight)
            {
                floorverts.Clear();
                flooruvs.Clear();
                flooruvsz.Clear();
                ceilingverts.Clear();
                ceilinguvs.Clear();
                ceilinguvsz.Clear();

                float tinyNumber = 1e-6f;

                for (int e = 0; e < polygon.VertexCount; ++e)
                {
                    float YF = (float)polygon.FloorHeight / 1024 * Scale;
                    float YC = (float)polygon.CeilingHeight / 1024 * Scale;
                    float X = (float)level.Endpoints[polygon.EndpointIndexes[e]].X / 1024 * Scale;
                    float Z = (float)level.Endpoints[polygon.EndpointIndexes[e]].Y / 1024 * Scale * -1;

                    float YFOX = (float)(level.Endpoints[polygon.EndpointIndexes[e]].X + polygon.FloorOrigin.X) / 1024 * -1;
                    float YFOY = (float)(level.Endpoints[polygon.EndpointIndexes[e]].Y + polygon.FloorOrigin.Y) / 1024;
                    float YCOX = (float)(level.Endpoints[polygon.EndpointIndexes[e]].X + polygon.CeilingOrigin.X) / 1024 * -1;
                    float YCOY = (float)(level.Endpoints[polygon.EndpointIndexes[e]].Y + polygon.CeilingOrigin.Y) / 1024;

                    floorverts.Add(new Vector3(X, YF, Z));
                    flooruvs.Add(new Vector2(YFOY, YFOX));
                    ceilingverts.Add(new Vector3(X, YC, Z));
                    ceilinguvs.Add(new Vector2(YCOY, YCOX));
                }

                if (floorverts.Count > 2)
                {
                    floortri.Clear();

                    for (int e = 0; e < floorverts.Count - 2; e++)
                    {
                        Vector3 v0 = floorverts[0];
                        Vector3 v1 = floorverts[e + 1];
                        Vector3 v2 = floorverts[e + 2];

                        Vector3 e0 = v1 - v0;
                        Vector3 e1 = v2 - v1;
                        Vector3 e2 = v2 - v0;

                        if (e0.sqrMagnitude < tinyNumber || e1.sqrMagnitude < tinyNumber || e2.sqrMagnitude < tinyNumber)
                        {
                            continue;
                        }

                        Vector3 edges = Vector3.Cross(e0, e2);

                        if (edges.sqrMagnitude < tinyNumber)
                        {
                            continue;
                        }

                        floortri.Add(0);
                        floortri.Add(e + 1);
                        floortri.Add(e + 2);
                    }

                    Plane.Add(i);

                    Portal.Add(-1);

                    if (polygon.FloorTexture.Collection == 27 || polygon.FloorTexture.Collection == 28 ||
                        polygon.FloorTexture.Collection == 29 || polygon.FloorTexture.Collection == 30)
                    {
                        Render.Add(-1);
                    }
                    else
                    {
                        Render.Add(i);
                    }

                    if (polygon.FloorTexture.Collection == 17)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, polygon.FloorTexture.Bitmap, polygon.FloorLight));
                        }
                    }
                    if (polygon.FloorTexture.Collection == 18)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, polygon.FloorTexture.Bitmap + 30, polygon.FloorLight));
                        }
                    }
                    if (polygon.FloorTexture.Collection == 19)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, polygon.FloorTexture.Bitmap + 60, polygon.FloorLight));
                        }
                    }
                    if (polygon.FloorTexture.Collection == 20)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, polygon.FloorTexture.Bitmap + 90, polygon.FloorLight));
                        }
                    }
                    if (polygon.FloorTexture.Collection == 21)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, polygon.FloorTexture.Bitmap + 125, polygon.FloorLight));
                        }
                    }
                    if (polygon.FloorTexture.Collection == 27 || polygon.FloorTexture.Collection == 28 ||
                        polygon.FloorTexture.Collection == 29 || polygon.FloorTexture.Collection == 30)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, polygon.FloorTexture.Bitmap, polygon.FloorLight));
                        }
                    }

                    MeshTexture.Add(polygon.FloorTexture.Bitmap);

                    MeshTextureCollection.Add(polygon.FloorTexture.Collection);

                    Collision.Add(i);

                    Transparent.Add(-1);

                    Mesh mesh = new Mesh();

                    mesh.SetVertices(floorverts);
                    mesh.SetUVs(0, flooruvsz);
                    mesh.SetTriangles(floortri, 0);
                    mesh.RecalculateNormals();

                    meshes.Add(mesh);
                }

                if (ceilingverts.Count > 2)
                {
                    ceilingverts.Reverse();

                    ceilinguvs.Reverse();

                    ceilingtri.Clear();

                    for (int e = 0; e < ceilingverts.Count - 2; e++)
                    {
                        Vector3 v0 = ceilingverts[0];
                        Vector3 v1 = ceilingverts[e + 1];
                        Vector3 v2 = ceilingverts[e + 2];

                        Vector3 e0 = v1 - v0;
                        Vector3 e1 = v2 - v1;
                        Vector3 e2 = v2 - v0;

                        if (e0.sqrMagnitude < tinyNumber || e1.sqrMagnitude < tinyNumber || e2.sqrMagnitude < tinyNumber)
                        {
                            continue;
                        }

                        Vector3 edges = Vector3.Cross(e0, e2);

                        if (edges.sqrMagnitude < tinyNumber)
                        {
                            continue;
                        }

                        ceilingtri.Add(0);
                        ceilingtri.Add(e + 1);
                        ceilingtri.Add(e + 2);
                    }

                    Plane.Add(i);

                    Portal.Add(-1);

                    if (polygon.CeilingTexture.Collection == 27 || polygon.CeilingTexture.Collection == 28 ||
                        polygon.CeilingTexture.Collection == 29 || polygon.CeilingTexture.Collection == 30)
                    {
                        Render.Add(-1);
                    }
                    else
                    {
                        Render.Add(i);
                    }

                    if (polygon.CeilingTexture.Collection == 17)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, polygon.CeilingTexture.Bitmap, polygon.CeilingLight));
                        }
                    }
                    if (polygon.CeilingTexture.Collection == 18)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, polygon.CeilingTexture.Bitmap + 30, polygon.CeilingLight));
                        }
                    }
                    if (polygon.CeilingTexture.Collection == 19)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, polygon.CeilingTexture.Bitmap + 60, polygon.CeilingLight));
                        }
                    }
                    if (polygon.CeilingTexture.Collection == 20)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, polygon.CeilingTexture.Bitmap + 90, polygon.CeilingLight));
                        }
                    }
                    if (polygon.CeilingTexture.Collection == 21)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, polygon.CeilingTexture.Bitmap + 125, polygon.CeilingLight));
                        }
                    }
                    if (polygon.CeilingTexture.Collection == 27 || polygon.CeilingTexture.Collection == 28 ||
                        polygon.CeilingTexture.Collection == 29 || polygon.CeilingTexture.Collection == 30)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, polygon.CeilingTexture.Bitmap, polygon.CeilingLight));
                        }
                    }

                    MeshTexture.Add(polygon.CeilingTexture.Bitmap);

                    MeshTextureCollection.Add(polygon.CeilingTexture.Collection);

                    Collision.Add(i);

                    Transparent.Add(-1);

                    Mesh mesh = new Mesh();

                    mesh.SetVertices(ceilingverts);
                    mesh.SetUVs(0, ceilinguvsz);
                    mesh.SetTriangles(ceilingtri, 0);
                    mesh.RecalculateNormals();

                    meshes.Add(mesh);
                }
            }
        }
    }

    public void GetVerts(double X0, double X1, double V0, double V1, double Z0, double Z1)
    {
        CW.Add(new Vector3((float)X1, (float)V0, (float)Z1));
        CW.Add(new Vector3((float)X1, (float)V1, (float)Z1));
        CW.Add(new Vector3((float)X0, (float)V1, (float)Z0));
        CW.Add(new Vector3((float)X0, (float)V0, (float)Z0));

        LeftPlane = new Plane((CW[2] - CW[1]).normalized, CW[1]);
        TopPlane = new Plane((CW[1] - CW[0]).normalized, CW[1]);

        CWUV.Add(new Vector2(LeftPlane.GetDistanceToPoint(CW[0]) / Scale, TopPlane.GetDistanceToPoint(CW[0]) / Scale));
        CWUV.Add(new Vector2(LeftPlane.GetDistanceToPoint(CW[1]) / Scale, TopPlane.GetDistanceToPoint(CW[1]) / Scale));
        CWUV.Add(new Vector2(LeftPlane.GetDistanceToPoint(CW[2]) / Scale, TopPlane.GetDistanceToPoint(CW[2]) / Scale));
        CWUV.Add(new Vector2(LeftPlane.GetDistanceToPoint(CW[3]) / Scale, TopPlane.GetDistanceToPoint(CW[3]) / Scale));
    }

    public void MakeSides(Side.TextureDefinition sideDef, int Light)
    {
        for (int e = 0; e < CWUV.Count; e++)
        {
            CWUVOffset.Add(new Vector2(CWUV[e].x + (float)sideDef.X / 1024,
            CWUV[e].y + (float)sideDef.Y / 1024 * -1));
        }

        if (sideDef.Texture.Collection == 17)
        {
            for (int e = 0; e < CWUVOffset.Count; e++)
            {
                CWUVOffsetZ.Add(new Vector4(CWUVOffset[e].x, CWUVOffset[e].y, sideDef.Texture.Bitmap, Light));
            }
        }
        if (sideDef.Texture.Collection == 18)
        {
            for (int e = 0; e < CWUVOffset.Count; e++)
            {
                CWUVOffsetZ.Add(new Vector4(CWUVOffset[e].x, CWUVOffset[e].y, sideDef.Texture.Bitmap + 30, Light));
            }
        }
        if (sideDef.Texture.Collection == 19)
        {
            for (int e = 0; e < CWUVOffset.Count; e++)
            {
                CWUVOffsetZ.Add(new Vector4(CWUVOffset[e].x, CWUVOffset[e].y, sideDef.Texture.Bitmap + 60, Light));
            }
        }
        if (sideDef.Texture.Collection == 20)
        {
            for (int e = 0; e < CWUVOffset.Count; e++)
            {
                CWUVOffsetZ.Add(new Vector4(CWUVOffset[e].x, CWUVOffset[e].y, sideDef.Texture.Bitmap + 90, Light));
            }
        }
        if (sideDef.Texture.Collection == 21)
        {
            for (int e = 0; e < CWUVOffset.Count; e++)
            {
                CWUVOffsetZ.Add(new Vector4(CWUVOffset[e].x, CWUVOffset[e].y, sideDef.Texture.Bitmap + 125, Light));
            }
        }

        if (sideDef.Texture.Collection == 27 || sideDef.Texture.Collection == 28 ||
            sideDef.Texture.Collection == 29 || sideDef.Texture.Collection == 30)
        {
            for (int e = 0; e < CWUVOffset.Count; e++)
            {
                CWUVOffsetZ.Add(new Vector4(CWUVOffset[e].x, CWUVOffset[e].y, sideDef.Texture.Bitmap, Light));
            }
        }
    }
}
