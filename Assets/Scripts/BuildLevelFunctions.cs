using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.IO;
using System;
using Weland;

public static class BuildLevelFunctions
{
    public struct StartPosition
    {
        public float3 playerStart;
        public int sectorId;
    };

    public struct LevelLight
    {
        public Color TriangleLight;
    };

    public struct MathematicalPlane
    {
        public float3 normal;
        public float distance;
    };

    public struct PolygonMeta
    {
        public int renderStartIndex;
        public int renderCount;

        public int collideStartIndex;
        public int collideCount;

        public int edgeStartIndex;
        public int edgeCount;

        public int connectedSectorId;
        public int sectorId;

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

    public static SectorMeta PlayerStart(CharacterController Player, NativeList<StartPosition> positions, NativeList<SectorMeta> sectors)
    {
        int randomIndex = UnityEngine.Random.Range(0, positions.Length);

        StartPosition selectedPosition = positions[randomIndex];

        SectorMeta currentSector = sectors[selectedPosition.sectorId];

        Player.enabled = false;

        Player.transform.SetPositionAndRotation(new Vector3(selectedPosition.playerStart.x + 0.01f, selectedPosition.playerStart.y + 1.10f, selectedPosition.playerStart.z + 0.01f), Quaternion.identity);

        Player.enabled = true;

        return currentSector;
    }

    public static Material CreateMaterial(NativeList<LevelLight> colors, Color[] LightColor, String Textures)
    {
        Shader shader = Resources.Load<Shader>("TriangleTexArray");

        for (int i = 0; i < colors.Length; i++)
        {
            LightColor[i] = new Color(colors[i].TriangleLight.r, colors[i].TriangleLight.g, colors[i].TriangleLight.b, 1.0f);
        }

        Material opaquematerial = new Material(shader);

        opaquematerial.mainTexture = Resources.Load<Texture2DArray>(Textures);

        opaquematerial.SetColorArray("_ColorArray", LightColor);

        return opaquematerial;
    }

    public static void BuildLights(NativeList<LevelLight> colors, Level level)
    {
        for (int i = 0; i < level.Lights.Count; i++)
        {
            Weland.Light.Function alight = level.Lights[i].PrimaryActive;

            LevelLight color = new LevelLight();

            color.TriangleLight = new Color((float)alight.Intensity, (float)alight.Intensity, (float)alight.Intensity, 1.0f);

            colors.Add(color);
        }
    }

    public static void BuildObjects(NativeList<StartPosition> positions, Level level, float Scale)
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

                positions.Add(start);
            }
        }
    }

    public static Level LoadLevel(string Name, int LevelNumber)
    {
        MapFile map = new MapFile();
        Level level = new Level();

        try
        {
            map.Load(Path.Combine(Application.streamingAssetsPath, Name + ".sceA"));
            Debug.Log("Map loaded successfully!");
        }
        catch (Exception exit)
        {
            Debug.LogError("Failed to load Map: " + exit.Message);
        }

        try
        {
            level.Load(map.Directory[LevelNumber]);
            Debug.Log("Level loaded successfully!");
        }
        catch (Exception exit)
        {
            Debug.LogError("Failed to load level: " + exit.Message);
        }

        return level;
    }

    public static void BuildEdges(NativeList<SectorMeta> sectors, NativeList<PolygonMeta> polygons, NativeList<float3> vertices, NativeList<int> edges, Material linematerial, GameObject edgeObject)
    {
        List<Vector3> linevertices = new List<Vector3>();

        List<int> lines = new List<int>();

        for (int i = 0; i < sectors.Length; i++)
        {
            linevertices.Clear();

            lines.Clear();

            int lineCount = 0;

            for (int e = sectors[i].polygonStartIndex; e < sectors[i].polygonStartIndex + sectors[i].polygonCount; e++)
            {
                if (polygons[e].connectedSectorId != -1)
                {
                    for (int f = polygons[e].edgeStartIndex; f < polygons[e].edgeStartIndex + polygons[e].edgeCount; f += 2)
                    {
                        linevertices.Add(vertices[edges[f]]);
                        linevertices.Add(vertices[edges[f + 1]]);
                        lines.Add(lineCount);
                        lines.Add(lineCount + 1);
                        lineCount += 2;
                    }
                }
            }

            Mesh combinedmesh = new Mesh();

            combinedmesh.SetVertices(linevertices);

            combinedmesh.SetIndices(lines, MeshTopology.Lines, 0);

            GameObject meshObject = new GameObject("Edges " + i);

            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

            meshRenderer.sharedMaterial = linematerial;

            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();

            meshFilter.sharedMesh = combinedmesh;

            meshObject.transform.SetParent(edgeObject.transform);
        }
    }

    public static void BuildOpaques(NativeList<float3> vertices, NativeList<float4> textures, NativeList<int> render, NativeList<SectorMeta> sectors, NativeList<PolygonMeta> polygons, Material opaquematerial, GameObject renderObject)
    {
        float tinyNumber = 1e-6f;

        List<Vector3> opaquevertices = new List<Vector3>();

        List<Vector4> opaquetextures = new List<Vector4>();

        List<int> opaquetriangles = new List<int>();

        for (int i = 0; i < sectors.Length; i++)
        {
            opaquevertices.Clear();

            opaquetextures.Clear();

            opaquetriangles.Clear();

            int triangleCount = 0;

            for (int e = sectors[i].polygonStartIndex; e < sectors[i].polygonStartIndex + sectors[i].polygonCount; e++)
            {
                if (polygons[e].renderCount != -1)
                {
                    for (int f = polygons[e].renderStartIndex; f < polygons[e].renderStartIndex + polygons[e].renderCount; f += 3)
                    {
                        Vector3 v0 = vertices[render[f]];
                        Vector3 v1 = vertices[render[f + 1]];
                        Vector3 v2 = vertices[render[f + 2]];

                        Vector3 e0 = v1 - v0;
                        Vector3 e1 = v2 - v1;
                        Vector3 e2 = v2 - v0;

                        if (e0.sqrMagnitude < tinyNumber || e1.sqrMagnitude < tinyNumber || e2.sqrMagnitude < tinyNumber)
                        {
                            continue;
                        }

                        opaquevertices.Add(v0);
                        opaquevertices.Add(v1);
                        opaquevertices.Add(v2);
                        opaquetextures.Add(textures[render[f]]);
                        opaquetextures.Add(textures[render[f + 1]]);
                        opaquetextures.Add(textures[render[f + 2]]);
                        opaquetriangles.Add(triangleCount);
                        opaquetriangles.Add(triangleCount + 1);
                        opaquetriangles.Add(triangleCount + 2);
                        triangleCount += 3;
                    }
                }
            }

            Mesh combinedmesh = new Mesh();

            combinedmesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            combinedmesh.SetVertices(opaquevertices);
            combinedmesh.SetUVs(0, opaquetextures);
            combinedmesh.SetTriangles(opaquetriangles, 0);

            GameObject meshObject = new GameObject("Render Mesh " + i);

            meshObject.AddComponent<MeshFilter>();
            meshObject.AddComponent<MeshRenderer>();

            Renderer MeshRend = meshObject.GetComponent<Renderer>();

            MeshRend.sharedMaterial = opaquematerial;
            meshObject.GetComponent<MeshFilter>().sharedMesh = combinedmesh;

            meshObject.transform.SetParent(renderObject.transform);
        }
    }

    public static void BuildColliders(NativeList<float3> vertices, NativeList<int> collide, NativeList<SectorMeta> sectors, NativeList<PolygonMeta> polygons, List<MeshCollider> CollisionSectors, GameObject collisionObject)
    {
        float tinyNumber = 1e-6f;

        List<Vector3> collisionvertices = new List<Vector3>();

        List<int> collisiontriangles = new List<int>();

        for (int i = 0; i < sectors.Length; i++)
        {
            collisionvertices.Clear();

            collisiontriangles.Clear();

            int triangleCount = 0;

            for (int e = sectors[i].polygonStartIndex; e < sectors[i].polygonStartIndex + sectors[i].polygonCount; e++)
            {
                if (polygons[e].collideCount != -1)
                {
                    for (int f = polygons[e].collideStartIndex; f < polygons[e].collideStartIndex + polygons[e].collideCount; f += 3)
                    {
                        Vector3 v0 = vertices[collide[f]];
                        Vector3 v1 = vertices[collide[f + 1]];
                        Vector3 v2 = vertices[collide[f + 2]];

                        Vector3 e0 = v1 - v0;
                        Vector3 e1 = v2 - v1;
                        Vector3 e2 = v2 - v0;

                        if (e0.sqrMagnitude < tinyNumber || e1.sqrMagnitude < tinyNumber || e2.sqrMagnitude < tinyNumber)
                        {
                            continue;
                        }

                        collisionvertices.Add(v0);
                        collisionvertices.Add(v1);
                        collisionvertices.Add(v2);
                        collisiontriangles.Add(triangleCount);
                        collisiontriangles.Add(triangleCount + 1);
                        collisiontriangles.Add(triangleCount + 2);
                        triangleCount += 3;
                    }
                }
            }

            Mesh combinedmesh = new Mesh();

            combinedmesh.SetVertices(collisionvertices);

            combinedmesh.SetTriangles(collisiontriangles, 0);

            GameObject meshObject = new GameObject("Collision " + i);

            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();

            meshCollider.sharedMesh = combinedmesh;

            CollisionSectors.Add(meshCollider);

            meshObject.transform.SetParent(collisionObject.transform);
        }
    }

    public static float GetPlaneSignedDistanceToPoint(MathematicalPlane plane, float3 point)
    {
        return Vector3.Dot(plane.normal, point) + plane.distance;
    }

    public static void BuildTheLists(Level level, float Scale, NativeList<int> render, NativeList<int> collide, NativeList<int> edges, NativeList<float3> vertices, NativeList<float4> textures, NativeList<MathematicalPlane> planes, NativeList<PolygonMeta> polygons, NativeList<SectorMeta> sectors)
    {
        List<float3> floorvertices = new List<float3>();

        List<float4> floortextures = new List<float4>();

        List<int> floortriangles = new List<int>();

        List<float3> ceilingvertices = new List<float3>();

        List<float4> ceilingtextures = new List<float4>();

        List<int> ceilingtriangles = new List<int>();
             
        int polygonStart = 0;

        for (int i = 0; i < level.Polygons.Count; ++i)
        {
            int polygonCount = 0;

            var polygon = level.Polygons[i];

            var floorTexture = polygon.FloorTexture.Collection;

            var ceilingTexture = polygon.CeilingTexture.Collection;

            var floorBit = polygon.FloorTexture.Bitmap;

            var ceilingBit = polygon.CeilingTexture.Bitmap;

            var floorLight = polygon.FloorLight;

            var ceilingLight = polygon.CeilingLight;

            double V0 = (float)polygon.CeilingHeight / 1024 * Scale;
            double V1 = (float)polygon.FloorHeight / 1024 * Scale;

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
                double Z1 = (float)-level.Endpoints[vA].Y / 1024 * Scale;

                double X0 = (float)level.Endpoints[vB].X / 1024 * Scale;
                double Z0 = (float)-level.Endpoints[vB].Y / 1024 * Scale;

                double L0 = (float)line.LowestAdjacentCeiling / 1024 * Scale;
                double L1 = (float)line.HighestAdjacentFloor / 1024 * Scale;

                var renderStart = render.Length;

                var collideStart = collide.Length;

                var edgesStart = edges.Length;

                var vertexIndex = vertices.Length;

                if (sideIndex != -1)
                {
                    var type = level.Sides[sideIndex].Type;

                    if (ownerB == -1)
                    {
                        if (type == SideType.Full)
                        {
                            var primary = level.Sides[sideIndex].Primary.Texture.Collection;

                            var primaryX = (float)level.Sides[sideIndex].Primary.X / 1024;

                            var primaryY = (float)-level.Sides[sideIndex].Primary.Y / 1024;

                            var primaryBit = level.Sides[sideIndex].Primary.Texture.Bitmap;

                            var primaryLight = level.Sides[sideIndex].PrimaryLightsourceIndex;

                            var textureCount = 0;

                            if (primary == 17)
                            {
                                textureCount = 0;
                            }
                            if (primary == 18)
                            {
                                textureCount = 30;
                            }
                            if (primary == 19)
                            {
                                textureCount = 60;
                            }
                            if (primary == 20)
                            {
                                textureCount = 90;
                            }
                            if (primary == 21)
                            {
                                textureCount = 125;
                            }

                            vertices.Add(new float3((float)X1, (float)V1, (float)Z1));
                            vertices.Add(new float3((float)X1, (float)V0, (float)Z1));
                            vertices.Add(new float3((float)X0, (float)V0, (float)Z0));
                            vertices.Add(new float3((float)X0, (float)V1, (float)Z0));

                            float3 leftPlaneNormal1 = math.normalize(vertices[vertexIndex + 2] - vertices[vertexIndex + 1]);
                            float leftPlaneDistance1 = -math.dot(leftPlaneNormal1, vertices[vertexIndex + 1]);

                            float3 topPlaneNormal1 = math.normalize(vertices[vertexIndex + 1] - vertices[vertexIndex]);
                            float topPlaneDistance1 = -math.dot(topPlaneNormal1, vertices[vertexIndex + 1]);

                            MathematicalPlane LeftPlane1 = new MathematicalPlane { normal = leftPlaneNormal1, distance = leftPlaneDistance1 };
                            MathematicalPlane TopPlane1 = new MathematicalPlane { normal = topPlaneNormal1, distance = topPlaneDistance1 };

                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex]) / Scale + primaryY, primaryBit + textureCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 1]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 1]) / Scale + primaryY, primaryBit + textureCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 2]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 2]) / Scale + primaryY, primaryBit + textureCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 3]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 3]) / Scale + primaryY, primaryBit + textureCount, primaryLight));

                            PolygonMeta sm = new PolygonMeta();

                            sm.renderStartIndex = -1;
                            sm.renderCount = -1;

                            sm.collideStartIndex = -1;
                            sm.collideCount = -1;

                            if (V0 > V1)
                            {
                                if (primary == 17 || primary == 18 || primary == 19 || primary == 20 || primary == 21)
                                {
                                    render.Add(vertexIndex);
                                    render.Add(vertexIndex + 1);
                                    render.Add(vertexIndex + 2);
                                    render.Add(vertexIndex);
                                    render.Add(vertexIndex + 2);
                                    render.Add(vertexIndex + 3);

                                    sm.renderStartIndex = renderStart;
                                    sm.renderCount = 6;
                                }

                                collide.Add(vertexIndex);
                                collide.Add(vertexIndex + 1);
                                collide.Add(vertexIndex + 2);
                                collide.Add(vertexIndex);
                                collide.Add(vertexIndex + 2);
                                collide.Add(vertexIndex + 3);

                                sm.collideStartIndex = collideStart;
                                sm.collideCount = 6;
                            }

                            sm.edgeStartIndex = -1;
                            sm.edgeCount = -1;

                            sm.connectedSectorId = -1;
                            sm.sectorId = ownerA;

                            sm.plane = planes.Length;

                            polygons.Add(sm);

                            float3 v0 = vertices[vertexIndex];
                            float3 v1 = vertices[vertexIndex + 1];
                            float3 v2 = vertices[vertexIndex + 2];

                            float3 n = math.normalize(math.cross(v1 - v0, v2 - v0));

                            MathematicalPlane plane = new MathematicalPlane
                            {
                                normal = n,
                                distance = -math.dot(n, v0)
                            };

                            planes.Add(plane);

                            polygonCount += 1;
                        }
                    }
                    else
                    {
                        if (type == SideType.High)
                        {
                            if (polygon.FloorHeight > line.LowestAdjacentCeiling)
                            {
                                L0 = V1;
                            }

                            var primary = level.Sides[sideIndex].Primary.Texture.Collection;

                            var primaryX = (float)level.Sides[sideIndex].Primary.X / 1024;

                            var primaryY = (float)-level.Sides[sideIndex].Primary.Y / 1024;

                            var primaryBit = level.Sides[sideIndex].Primary.Texture.Bitmap;

                            var primaryLight = level.Sides[sideIndex].PrimaryLightsourceIndex;

                            var textureCount = 0;

                            if (primary == 17)
                            {
                                textureCount = 0;
                            }
                            if (primary == 18)
                            {
                                textureCount = 30;
                            }
                            if (primary == 19)
                            {
                                textureCount = 60;
                            }
                            if (primary == 20)
                            {
                                textureCount = 90;
                            }
                            if (primary == 21)
                            {
                                textureCount = 125;
                            }

                            vertices.Add(new float3((float)X1, (float)L1, (float)Z1));
                            vertices.Add(new float3((float)X1, (float)L0, (float)Z1));
                            vertices.Add(new float3((float)X1, (float)V0, (float)Z1));

                            vertices.Add(new float3((float)X0, (float)V0, (float)Z0));
                            vertices.Add(new float3((float)X0, (float)L0, (float)Z0));
                            vertices.Add(new float3((float)X0, (float)L1, (float)Z0));

                            float3 leftPlaneNormal1 = math.normalize(vertices[vertexIndex + 3] - vertices[vertexIndex + 2]);
                            float leftPlaneDistance1 = -math.dot(leftPlaneNormal1, vertices[vertexIndex + 2]);

                            float3 topPlaneNormal1 = math.normalize(vertices[vertexIndex + 2] - vertices[vertexIndex + 1]);
                            float topPlaneDistance1 = -math.dot(topPlaneNormal1, vertices[vertexIndex + 2]);

                            MathematicalPlane LeftPlane1 = new MathematicalPlane { normal = leftPlaneNormal1, distance = leftPlaneDistance1 };
                            MathematicalPlane TopPlane1 = new MathematicalPlane { normal = topPlaneNormal1, distance = topPlaneDistance1 };

                            textures.Add(float4.zero);
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 1]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 1]) / Scale + primaryY, primaryBit + textureCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 2]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 2]) / Scale + primaryY, primaryBit + textureCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 3]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 3]) / Scale + primaryY, primaryBit + textureCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 4]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 4]) / Scale + primaryY, primaryBit + textureCount, primaryLight));
                            textures.Add(float4.zero);

                            PolygonMeta sm = new PolygonMeta();

                            sm.renderStartIndex = -1;
                            sm.renderCount = -1;

                            sm.collideStartIndex = -1;
                            sm.collideCount = -1;

                            if (V0 > L0)
                            {
                                if (primary == 17 || primary == 18 || primary == 19 || primary == 20 || primary == 21)
                                {
                                    render.Add(vertexIndex + 1);
                                    render.Add(vertexIndex + 2);
                                    render.Add(vertexIndex + 3);
                                    render.Add(vertexIndex + 1);
                                    render.Add(vertexIndex + 3);
                                    render.Add(vertexIndex + 4);

                                    sm.renderStartIndex = renderStart;
                                    sm.renderCount = 6;
                                }

                                collide.Add(vertexIndex + 1);
                                collide.Add(vertexIndex + 2);
                                collide.Add(vertexIndex + 3);
                                collide.Add(vertexIndex + 1);
                                collide.Add(vertexIndex + 3);
                                collide.Add(vertexIndex + 4);

                                sm.collideStartIndex = collideStart;
                                sm.collideCount = 6;
                            }

                            sm.edgeStartIndex = -1;
                            sm.edgeCount = -1;

                            sm.connectedSectorId = -1;
                            sm.sectorId = ownerA;

                            if (L0 > L1)
                            {
                                edges.Add(vertexIndex);
                                edges.Add(vertexIndex + 1);
                                edges.Add(vertexIndex + 1);
                                edges.Add(vertexIndex + 4);
                                edges.Add(vertexIndex + 4);
                                edges.Add(vertexIndex + 5);
                                edges.Add(vertexIndex + 5);
                                edges.Add(vertexIndex);

                                sm.connectedSectorId = ownerB;

                                sm.edgeStartIndex = edgesStart;
                                sm.edgeCount = 8;
                            }

                            sm.plane = planes.Length;

                            if (line.Solid == true)
                            {
                                collide.Add(vertexIndex + 0);
                                collide.Add(vertexIndex + 1);
                                collide.Add(vertexIndex + 4);
                                collide.Add(vertexIndex + 0);
                                collide.Add(vertexIndex + 4);
                                collide.Add(vertexIndex + 5);

                                sm.collideStartIndex = collideStart;
                                sm.collideCount = 12;
                            }

                            polygons.Add(sm);

                            float3 v0 = vertices[vertexIndex];
                            float3 v1 = vertices[vertexIndex + 1];
                            float3 v2 = vertices[vertexIndex + 4];

                            float3 n = math.normalize(math.cross(v1 - v0, v2 - v0));

                            MathematicalPlane plane = new MathematicalPlane
                            {
                                normal = n,
                                distance = -math.dot(n, v0)
                            };

                            planes.Add(plane);

                            polygonCount += 1;
                        }
                        else if (type == SideType.Low)
                        {
                            if (polygon.CeilingHeight < line.HighestAdjacentFloor)
                            {
                                L1 = V0;
                            }

                            var primary = level.Sides[sideIndex].Primary.Texture.Collection;

                            var primaryX = (float)level.Sides[sideIndex].Primary.X / 1024;

                            var primaryY = (float)-level.Sides[sideIndex].Primary.Y / 1024;

                            var primaryBit = level.Sides[sideIndex].Primary.Texture.Bitmap;

                            var primaryLight = level.Sides[sideIndex].PrimaryLightsourceIndex;

                            var textureCount = 0;

                            if (primary == 17)
                            {
                                textureCount = 0;
                            }
                            if (primary == 18)
                            {
                                textureCount = 30;
                            }
                            if (primary == 19)
                            {
                                textureCount = 60;
                            }
                            if (primary == 20)
                            {
                                textureCount = 90;
                            }
                            if (primary == 21)
                            {
                                textureCount = 125;
                            }

                            vertices.Add(new float3((float)X1, (float)V1, (float)Z1));
                            vertices.Add(new float3((float)X1, (float)L1, (float)Z1));
                            vertices.Add(new float3((float)X1, (float)L0, (float)Z1));

                            vertices.Add(new float3((float)X0, (float)L0, (float)Z0));
                            vertices.Add(new float3((float)X0, (float)L1, (float)Z0));
                            vertices.Add(new float3((float)X0, (float)V1, (float)Z0));

                            float3 leftPlaneNormal1 = math.normalize(vertices[vertexIndex + 4] - vertices[vertexIndex + 1]);
                            float leftPlaneDistance1 = -math.dot(leftPlaneNormal1, vertices[vertexIndex + 1]);

                            float3 topPlaneNormal1 = math.normalize(vertices[vertexIndex + 1] - vertices[vertexIndex]);
                            float topPlaneDistance1 = -math.dot(topPlaneNormal1, vertices[vertexIndex + 1]);

                            MathematicalPlane LeftPlane1 = new MathematicalPlane { normal = leftPlaneNormal1, distance = leftPlaneDistance1 };
                            MathematicalPlane TopPlane1 = new MathematicalPlane { normal = topPlaneNormal1, distance = topPlaneDistance1 };

                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex]) / Scale + primaryY, primaryBit + textureCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 1]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 1]) / Scale + primaryY, primaryBit + textureCount, primaryLight));
                            textures.Add(float4.zero);
                            textures.Add(float4.zero);
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 4]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 4]) / Scale + primaryY, primaryBit + textureCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 5]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 5]) / Scale + primaryY, primaryBit + textureCount, primaryLight));

                            PolygonMeta sm = new PolygonMeta();

                            sm.renderStartIndex = -1;
                            sm.renderCount = -1;

                            sm.collideStartIndex = -1;
                            sm.collideCount = -1;

                            if (L1 > V1)
                            {
                                if (primary == 17 || primary == 18 || primary == 19 || primary == 20 || primary == 21)
                                {
                                    render.Add(vertexIndex);
                                    render.Add(vertexIndex + 1);
                                    render.Add(vertexIndex + 4);
                                    render.Add(vertexIndex);
                                    render.Add(vertexIndex + 4);
                                    render.Add(vertexIndex + 5);

                                    sm.renderStartIndex = renderStart;
                                    sm.renderCount = 6;
                                }

                                collide.Add(vertexIndex);
                                collide.Add(vertexIndex + 1);
                                collide.Add(vertexIndex + 4);
                                collide.Add(vertexIndex);
                                collide.Add(vertexIndex + 4);
                                collide.Add(vertexIndex + 5);

                                sm.collideStartIndex = collideStart;
                                sm.collideCount = 6;
                            }

                            sm.edgeStartIndex = -1;
                            sm.edgeCount = -1;

                            sm.connectedSectorId = -1;
                            sm.sectorId = ownerA;

                            if (L0 > L1)
                            {
                                edges.Add(vertexIndex + 1);
                                edges.Add(vertexIndex + 2);
                                edges.Add(vertexIndex + 2);
                                edges.Add(vertexIndex + 3);
                                edges.Add(vertexIndex + 3);
                                edges.Add(vertexIndex + 4);
                                edges.Add(vertexIndex + 4);
                                edges.Add(vertexIndex + 1);

                                sm.connectedSectorId = ownerB;

                                sm.edgeStartIndex = edgesStart;
                                sm.edgeCount = 8;
                            }

                            sm.plane = planes.Length;

                            if (line.Solid == true)
                            {
                                collide.Add(vertexIndex + 1);
                                collide.Add(vertexIndex + 2);
                                collide.Add(vertexIndex + 3);
                                collide.Add(vertexIndex + 1);
                                collide.Add(vertexIndex + 3);
                                collide.Add(vertexIndex + 4);

                                sm.collideStartIndex = collideStart;
                                sm.collideCount = 12;
                            }

                            polygons.Add(sm);

                            float3 v0 = vertices[vertexIndex + 1];
                            float3 v1 = vertices[vertexIndex + 2];
                            float3 v2 = vertices[vertexIndex + 3];

                            float3 n = math.normalize(math.cross(v1 - v0, v2 - v0));

                            MathematicalPlane plane = new MathematicalPlane
                            {
                                normal = n,
                                distance = -math.dot(n, v0)
                            };

                            planes.Add(plane);

                            polygonCount += 1;
                        }
                        else if (type == SideType.Split)
                        {
                            var primary = level.Sides[sideIndex].Primary.Texture.Collection;

                            var primaryX = (float)level.Sides[sideIndex].Primary.X / 1024;

                            var primaryY = (float)-level.Sides[sideIndex].Primary.Y / 1024;

                            var secondary = level.Sides[sideIndex].Secondary.Texture.Collection;

                            var secondaryX = (float)level.Sides[sideIndex].Secondary.X / 1024;

                            var secondaryY = (float)-level.Sides[sideIndex].Secondary.Y / 1024;

                            var primaryBit = level.Sides[sideIndex].Primary.Texture.Bitmap;

                            var secondaryBit = level.Sides[sideIndex].Secondary.Texture.Bitmap;

                            var primaryLight = level.Sides[sideIndex].PrimaryLightsourceIndex;

                            var secondaryLight = level.Sides[sideIndex].SecondaryLightsourceIndex;

                            var primaryCount = 0;

                            if (primary == 17)
                            {
                                primaryCount = 0;
                            }
                            if (primary == 18)
                            {
                                primaryCount = 30;
                            }
                            if (primary == 19)
                            {
                                primaryCount = 60;
                            }
                            if (primary == 20)
                            {
                                primaryCount = 90;
                            }
                            if (primary == 21)
                            {
                                primaryCount = 125;
                            }

                            var secondaryCount = 0;

                            if (secondary == 17)
                            {
                                secondaryCount = 0;
                            }
                            if (secondary == 18)
                            {
                                secondaryCount = 30;
                            }
                            if (secondary == 19)
                            {
                                secondaryCount = 60;
                            }
                            if (secondary == 20)
                            {
                                secondaryCount = 90;
                            }
                            if (secondary == 21)
                            {
                                secondaryCount = 125;
                            }

                            vertices.Add(new float3((float)X1, (float)V1, (float)Z1));
                            vertices.Add(new float3((float)X1, (float)L1, (float)Z1));
                            vertices.Add(new float3((float)X1, (float)L0, (float)Z1));
                            vertices.Add(new float3((float)X1, (float)V0, (float)Z1));

                            vertices.Add(new float3((float)X0, (float)V0, (float)Z0));
                            vertices.Add(new float3((float)X0, (float)L0, (float)Z0));
                            vertices.Add(new float3((float)X0, (float)L1, (float)Z0));
                            vertices.Add(new float3((float)X0, (float)V1, (float)Z0));

                            float3 leftPlaneNormal1 = math.normalize(vertices[vertexIndex + 6] - vertices[vertexIndex + 1]);
                            float leftPlaneDistance1 = -math.dot(leftPlaneNormal1, vertices[vertexIndex + 1]);

                            float3 topPlaneNormal1 = math.normalize(vertices[vertexIndex + 1] - vertices[vertexIndex]);
                            float topPlaneDistance1 = -math.dot(topPlaneNormal1, vertices[vertexIndex + 1]);

                            float3 leftPlaneNormal2 = math.normalize(vertices[vertexIndex + 4] - vertices[vertexIndex + 3]);
                            float leftPlaneDistance2 = -math.dot(leftPlaneNormal2, vertices[vertexIndex + 3]);

                            float3 topPlaneNormal2 = math.normalize(vertices[vertexIndex + 3] - vertices[vertexIndex + 2]);
                            float topPlaneDistance2 = -math.dot(topPlaneNormal2, vertices[vertexIndex + 3]);

                            MathematicalPlane LeftPlane1 = new MathematicalPlane { normal = leftPlaneNormal1, distance = leftPlaneDistance1 };
                            MathematicalPlane TopPlane1 = new MathematicalPlane { normal = topPlaneNormal1, distance = topPlaneDistance1 };

                            MathematicalPlane LeftPlane2 = new MathematicalPlane { normal = leftPlaneNormal2, distance = leftPlaneDistance2 };
                            MathematicalPlane TopPlane2 = new MathematicalPlane { normal = topPlaneNormal2, distance = topPlaneDistance2 };

                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex]) / Scale + secondaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex]) / Scale + secondaryY, secondaryBit + secondaryCount, secondaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 1]) / Scale + secondaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 1]) / Scale + secondaryY, secondaryBit + secondaryCount, secondaryLight));

                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane2, vertices[vertexIndex + 2]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane2, vertices[vertexIndex + 2]) / Scale + primaryY, primaryBit + primaryCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane2, vertices[vertexIndex + 3]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane2, vertices[vertexIndex + 3]) / Scale + primaryY, primaryBit + primaryCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane2, vertices[vertexIndex + 4]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane2, vertices[vertexIndex + 4]) / Scale + primaryY, primaryBit + primaryCount, primaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane2, vertices[vertexIndex + 5]) / Scale + primaryX, GetPlaneSignedDistanceToPoint(TopPlane2, vertices[vertexIndex + 5]) / Scale + primaryY, primaryBit + primaryCount, primaryLight));

                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 6]) / Scale + secondaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 6]) / Scale + secondaryY, secondaryBit + secondaryCount, secondaryLight));
                            textures.Add(new float4(GetPlaneSignedDistanceToPoint(LeftPlane1, vertices[vertexIndex + 7]) / Scale + secondaryX, GetPlaneSignedDistanceToPoint(TopPlane1, vertices[vertexIndex + 7]) / Scale + secondaryY, secondaryBit + secondaryCount, secondaryLight));

                            PolygonMeta sm = new PolygonMeta();

                            sm.renderStartIndex = -1;
                            sm.renderCount = -1;

                            sm.collideStartIndex = -1;
                            sm.collideCount = -1;

                            if (V0 > L0)
                            {
                                if (primary == 17 || primary == 18 || primary == 19 || primary == 20 || primary == 21)
                                {
                                    render.Add(vertexIndex + 2);
                                    render.Add(vertexIndex + 3);
                                    render.Add(vertexIndex + 4);
                                    render.Add(vertexIndex + 2);
                                    render.Add(vertexIndex + 4);
                                    render.Add(vertexIndex + 5);
                                }

                                collide.Add(vertexIndex + 2);
                                collide.Add(vertexIndex + 3);
                                collide.Add(vertexIndex + 4);
                                collide.Add(vertexIndex + 2);
                                collide.Add(vertexIndex + 4);
                                collide.Add(vertexIndex + 5);
                            }

                            if (L1 > V1)
                            {
                                if (secondary == 17 || secondary == 18 || secondary == 19 || secondary == 20 || secondary == 21)
                                {
                                    render.Add(vertexIndex);
                                    render.Add(vertexIndex + 1);
                                    render.Add(vertexIndex + 6);
                                    render.Add(vertexIndex);
                                    render.Add(vertexIndex + 6);
                                    render.Add(vertexIndex + 7);
                                }

                                collide.Add(vertexIndex);
                                collide.Add(vertexIndex + 1);
                                collide.Add(vertexIndex + 6);
                                collide.Add(vertexIndex);
                                collide.Add(vertexIndex + 6);
                                collide.Add(vertexIndex + 7);
                            }

                            if (V0 > L0 && L1 == V1)
                            {
                                sm.renderStartIndex = renderStart;
                                sm.renderCount = 6;

                                sm.collideStartIndex = collideStart;
                                sm.collideCount = 6;
                            }
                            else if (V0 == L0 && L1 > V1)
                            {
                                sm.renderStartIndex = renderStart;
                                sm.renderCount = 6;

                                sm.collideStartIndex = collideStart;
                                sm.collideCount = 6;
                            }
                            else if (L1 > V1 && V0 > L0)
                            {
                                sm.renderStartIndex = renderStart;
                                sm.renderCount = 12;

                                sm.collideStartIndex = collideStart;
                                sm.collideCount = 12;
                            }

                            sm.edgeStartIndex = -1;
                            sm.edgeCount = -1;

                            sm.connectedSectorId = -1;
                            sm.sectorId = ownerA;

                            if (L0 > L1)
                            {
                                if (line.Solid == true)
                                {
                                    collide.Add(vertexIndex + 1);
                                    collide.Add(vertexIndex + 2);
                                    collide.Add(vertexIndex + 5);
                                    collide.Add(vertexIndex + 1);
                                    collide.Add(vertexIndex + 5);
                                    collide.Add(vertexIndex + 6);

                                    if (V0 > L0 && L1 == V1)
                                    {
                                        sm.collideStartIndex = collideStart;
                                        sm.collideCount = 12;
                                    }
                                    else if (V0 == L0 && L1 > V1)
                                    {
                                        sm.collideStartIndex = collideStart;
                                        sm.collideCount = 12;
                                    }
                                    else if (L1 > V1 && V0 > L0)
                                    {
                                        sm.collideStartIndex = collideStart;
                                        sm.collideCount = 18;
                                    }
                                    else
                                    {
                                        sm.collideStartIndex = collideStart;
                                        sm.collideCount = 6;
                                    }
                                }

                                edges.Add(vertexIndex + 1);
                                edges.Add(vertexIndex + 2);
                                edges.Add(vertexIndex + 2);
                                edges.Add(vertexIndex + 5);
                                edges.Add(vertexIndex + 5);
                                edges.Add(vertexIndex + 6);
                                edges.Add(vertexIndex + 6);
                                edges.Add(vertexIndex + 1);

                                sm.connectedSectorId = ownerB;

                                sm.edgeStartIndex = edgesStart;
                                sm.edgeCount = 8;
                            }

                            sm.plane = planes.Length;

                            polygons.Add(sm);

                            float3 v0 = vertices[vertexIndex + 1];
                            float3 v1 = vertices[vertexIndex + 2];
                            float3 v2 = vertices[vertexIndex + 5];

                            float3 n = math.normalize(math.cross(v1 - v0, v2 - v0));

                            MathematicalPlane plane = new MathematicalPlane
                            {
                                normal = n,
                                distance = -math.dot(n, v0)
                            };

                            planes.Add(plane);

                            polygonCount += 1;
                        }
                        else
                        {
                            vertices.Add(new float3((float)X1, (float)L1, (float)Z1));
                            vertices.Add(new float3((float)X1, (float)L0, (float)Z1));
                            vertices.Add(new float3((float)X0, (float)L0, (float)Z0));
                            vertices.Add(new float3((float)X0, (float)L1, (float)Z0));

                            textures.Add(float4.zero);
                            textures.Add(float4.zero);
                            textures.Add(float4.zero);
                            textures.Add(float4.zero);

                            PolygonMeta sm = new PolygonMeta();

                            sm.renderStartIndex = -1;
                            sm.renderCount = -1;

                            sm.collideStartIndex = -1;
                            sm.collideCount = -1;

                            sm.connectedSectorId = -1;
                            sm.sectorId = ownerA;

                            sm.plane = planes.Length;

                            sm.edgeStartIndex = -1;
                            sm.edgeCount = -1;

                            if (L0 > L1)
                            {
                                if (line.Solid == true)
                                {
                                    collide.Add(vertexIndex);
                                    collide.Add(vertexIndex + 1);
                                    collide.Add(vertexIndex + 2);
                                    collide.Add(vertexIndex);
                                    collide.Add(vertexIndex + 2);
                                    collide.Add(vertexIndex + 3);

                                    sm.collideStartIndex = collideStart;
                                    sm.collideCount = 6;
                                }

                                edges.Add(vertexIndex);
                                edges.Add(vertexIndex + 1);
                                edges.Add(vertexIndex + 1);
                                edges.Add(vertexIndex + 2);
                                edges.Add(vertexIndex + 2);
                                edges.Add(vertexIndex + 3);
                                edges.Add(vertexIndex + 3);
                                edges.Add(vertexIndex);

                                sm.connectedSectorId = ownerB;

                                sm.edgeStartIndex = edgesStart;
                                sm.edgeCount = 8;
                            }

                            polygons.Add(sm);

                            float3 v0 = vertices[vertexIndex];
                            float3 v1 = vertices[vertexIndex + 1];
                            float3 v2 = vertices[vertexIndex + 2];

                            float3 n = math.normalize(math.cross(v1 - v0, v2 - v0));

                            MathematicalPlane plane = new MathematicalPlane
                            {
                                normal = n,
                                distance = -math.dot(n, v0)
                            };

                            planes.Add(plane);

                            polygonCount += 1;
                        }
                    }
                }
                else
                {
                    if (ownerB != -1)
                    {
                        vertices.Add(new float3((float)X1, (float)L1, (float)Z1));
                        vertices.Add(new float3((float)X1, (float)L0, (float)Z1));
                        vertices.Add(new float3((float)X0, (float)L0, (float)Z0));
                        vertices.Add(new float3((float)X0, (float)L1, (float)Z0));

                        textures.Add(float4.zero);
                        textures.Add(float4.zero);
                        textures.Add(float4.zero);
                        textures.Add(float4.zero);

                        PolygonMeta sm = new PolygonMeta();

                        sm.renderStartIndex = -1;
                        sm.renderCount = -1;

                        sm.collideStartIndex = -1;
                        sm.collideCount = -1;

                        sm.connectedSectorId = -1;
                        sm.sectorId = ownerA;

                        sm.plane = planes.Length;

                        sm.edgeStartIndex = -1;
                        sm.edgeCount = -1;

                        if (L0 > L1)
                        {
                            if (line.Solid == true)
                            {
                                collide.Add(vertexIndex);
                                collide.Add(vertexIndex + 1);
                                collide.Add(vertexIndex + 2);
                                collide.Add(vertexIndex);
                                collide.Add(vertexIndex + 2);
                                collide.Add(vertexIndex + 3);

                                sm.collideStartIndex = collideStart;
                                sm.collideCount = 6;
                            }

                            edges.Add(vertexIndex);
                            edges.Add(vertexIndex + 1);
                            edges.Add(vertexIndex + 1);
                            edges.Add(vertexIndex + 2);
                            edges.Add(vertexIndex + 2);
                            edges.Add(vertexIndex + 3);
                            edges.Add(vertexIndex + 3);
                            edges.Add(vertexIndex);

                            sm.connectedSectorId = ownerB;

                            sm.edgeStartIndex = edgesStart;
                            sm.edgeCount = 8;
                        }

                        polygons.Add(sm);

                        float3 v0 = vertices[vertexIndex];
                        float3 v1 = vertices[vertexIndex + 1];
                        float3 v2 = vertices[vertexIndex + 2];

                        float3 n = math.normalize(math.cross(v1 - v0, v2 - v0));

                        MathematicalPlane plane = new MathematicalPlane
                        {
                            normal = n,
                            distance = -math.dot(n, v0)
                        };

                        planes.Add(plane);

                        polygonCount += 1;
                    }
                }
            }

            if (polygon.FloorHeight != polygon.CeilingHeight)
            {
                floorvertices.Clear();
                floortextures.Clear();
                ceilingvertices.Clear();
                ceilingtextures.Clear();

                float tinyNumber = 1e-6f;

                var floorTextureCount = 0;

                if (floorTexture == 17)
                {
                    floorTextureCount = 0;
                }
                if (floorTexture == 18)
                {
                    floorTextureCount = 30;
                }
                if (floorTexture == 19)
                {
                    floorTextureCount = 60;
                }
                if (floorTexture == 20)
                {
                    floorTextureCount = 90;
                }
                if (floorTexture == 21)
                {
                    floorTextureCount = 125;
                }

                var ceilingTextureCount = 0;

                if (ceilingTexture == 17)
                {
                    ceilingTextureCount = 0;
                }
                if (ceilingTexture == 18)
                {
                    ceilingTextureCount = 30;
                }
                if (ceilingTexture == 19)
                {
                    ceilingTextureCount = 60;
                }
                if (ceilingTexture == 20)
                {
                    ceilingTextureCount = 90;
                }
                if (ceilingTexture == 21)
                {
                    ceilingTextureCount = 125;
                }

                for (int e = 0; e < polygon.VertexCount; ++e)
                {
                    double X = (float)level.Endpoints[polygon.EndpointIndexes[e]].X / 1024 * Scale;
                    double Z = (float)level.Endpoints[polygon.EndpointIndexes[e]].Y / 1024 * Scale * -1;

                    floorvertices.Add(new float3((float)X, (float)V1, (float)Z));
                    floortextures.Add(new float4((float)(level.Endpoints[polygon.EndpointIndexes[e]].Y + polygon.FloorOrigin.Y) / 1024, (float)(level.Endpoints[polygon.EndpointIndexes[e]].X + polygon.FloorOrigin.X) / 1024 * -1, floorBit + floorTextureCount, floorLight));
                    ceilingvertices.Add(new float3((float)X, (float)V0, (float)Z));
                    ceilingtextures.Add(new float4((float)(level.Endpoints[polygon.EndpointIndexes[e]].Y + polygon.CeilingOrigin.Y) / 1024, (float)(level.Endpoints[polygon.EndpointIndexes[e]].X + polygon.CeilingOrigin.X) / 1024 * -1, ceilingBit + ceilingTextureCount, ceilingLight));
                }

                if (floorvertices.Count > 2)
                {
                    floortriangles.Clear();

                    for (int e = 0; e < floorvertices.Count - 2; e++)
                    {
                        float3 v0 = floorvertices[0];
                        float3 v1 = floorvertices[e + 1];
                        float3 v2 = floorvertices[e + 2];

                        float3 e0 = v1 - v0;
                        float3 e1 = v2 - v1;
                        float3 e2 = v2 - v0;

                        if (math.lengthsq(e0) < tinyNumber || math.lengthsq(e1) < tinyNumber || math.lengthsq(e2) < tinyNumber)
                        {
                            continue;
                        }

                        float3 edge = math.cross(e0, e2);

                        if (math.lengthsq(edge) < tinyNumber)
                        {
                            continue;
                        }

                        floortriangles.Add(0);
                        floortriangles.Add(e + 1);
                        floortriangles.Add(e + 2);
                    }
                }

                int baseFloor = vertices.Length;

                int floorRenderIndex = render.Length;

                int floorCollideIndex = collide.Length;

                for (int e = 0; e < floorvertices.Count; e++)
                {
                    vertices.Add(floorvertices[e]);
                }

                for (int e = 0; e < floortriangles.Count; e++)
                {
                    collide.Add(baseFloor + floortriangles[e]);
                }

                PolygonMeta smFloor = new PolygonMeta();

                smFloor.renderStartIndex = -1;
                smFloor.renderCount = -1;

                if (floorTexture == 17 || floorTexture == 18 || floorTexture == 19 || floorTexture == 20 || floorTexture == 21)
                {
                    for (int e = 0; e < floortriangles.Count; e++)
                    {
                        render.Add(baseFloor + floortriangles[e]);
                    }

                    smFloor.renderStartIndex = floorRenderIndex;
                    smFloor.renderCount = floortriangles.Count;
                }

                for (int e = 0; e < floortextures.Count; e++)
                {
                    textures.Add(floortextures[e]);
                }

                smFloor.collideStartIndex = floorCollideIndex;
                smFloor.collideCount = floortriangles.Count;

                smFloor.edgeStartIndex = -1;
                smFloor.edgeCount = -1;

                smFloor.connectedSectorId = -1;
                smFloor.sectorId = i;

                smFloor.plane = planes.Length;

                polygons.Add(smFloor);

                polygonCount += 1;

                float3 f0 = floorvertices[floortriangles[0]];
                float3 f1 = floorvertices[floortriangles[1]];
                float3 f2 = floorvertices[floortriangles[2]];

                float3 f = math.normalize(math.cross(f1 - f0, f2 - f0));

                MathematicalPlane planeFloor = new MathematicalPlane
                {
                    normal = f,
                    distance = -math.dot(f, f0)
                };

                planes.Add(planeFloor);

                if (ceilingvertices.Count > 2)
                {
                    ceilingvertices.Reverse();

                    ceilingtextures.Reverse();

                    ceilingtriangles.Clear();

                    for (int e = 0; e < ceilingvertices.Count - 2; e++)
                    {
                        float3 v0 = ceilingvertices[0];
                        float3 v1 = ceilingvertices[e + 1];
                        float3 v2 = ceilingvertices[e + 2];

                        float3 e0 = v1 - v0;
                        float3 e1 = v2 - v1;
                        float3 e2 = v2 - v0;

                        if (math.lengthsq(e0) < tinyNumber || math.lengthsq(e1) < tinyNumber || math.lengthsq(e2) < tinyNumber)
                        {
                            continue;
                        }

                        float3 edge = math.cross(e0, e2);

                        if (math.lengthsq(edge) < tinyNumber)
                        {
                            continue;
                        }

                        ceilingtriangles.Add(0);
                        ceilingtriangles.Add(e + 1);
                        ceilingtriangles.Add(e + 2);
                    }
                }

                int baseCeiling = vertices.Length;

                int ceilingRenderIndex = render.Length;

                int ceilingCollideIndex = collide.Length;

                for (int e = 0; e < ceilingvertices.Count; e++)
                {
                    vertices.Add(ceilingvertices[e]);
                }

                for (int e = 0; e < ceilingtriangles.Count; e++)
                {
                    collide.Add(baseCeiling + ceilingtriangles[e]);
                }

                PolygonMeta smCeiling = new PolygonMeta();

                smCeiling.renderStartIndex = -1;
                smCeiling.renderCount = -1;

                if (ceilingTexture == 17 || ceilingTexture == 18 || ceilingTexture == 19 || ceilingTexture == 20 || ceilingTexture == 21)
                {
                    for (int e = 0; e < ceilingtriangles.Count; e++)
                    {
                        render.Add(baseCeiling + ceilingtriangles[e]);
                    }

                    smCeiling.renderStartIndex = ceilingRenderIndex;
                    smCeiling.renderCount = ceilingtriangles.Count;
                }

                for (int e = 0; e < ceilingtextures.Count; e++)
                {
                    textures.Add(ceilingtextures[e]);
                }

                smCeiling.collideStartIndex = ceilingCollideIndex;
                smCeiling.collideCount = ceilingtriangles.Count;

                smCeiling.edgeStartIndex = -1;
                smCeiling.edgeCount = -1;

                smCeiling.connectedSectorId = -1;
                smCeiling.sectorId = i;

                smCeiling.plane = planes.Length;

                polygons.Add(smCeiling);

                polygonCount += 1;

                float3 c0 = ceilingvertices[ceilingtriangles[0]];
                float3 c1 = ceilingvertices[ceilingtriangles[1]];
                float3 c2 = ceilingvertices[ceilingtriangles[2]];

                float3 c = math.normalize(math.cross(c1 - c0, c2 - c0));

                MathematicalPlane planeCeiling = new MathematicalPlane
                {
                    normal = c,
                    distance = -math.dot(c, c0)
                };

                planes.Add(planeCeiling);
            }

            SectorMeta sectorMeta = new SectorMeta
            {
                sectorId = i,
                polygonStartIndex = polygonStart,
                polygonCount = polygonCount,
                planeStartIndex = 0,
                planeCount = 4
            };

            sectors.Add(sectorMeta);

            polygonStart += polygonCount;
        }

        Debug.Log("Level built successfully!");
    }
}
