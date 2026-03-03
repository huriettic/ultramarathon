using static BuildLevelFunctions;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System;
using Weland;

public class BuildLevel : MonoBehaviour
{
    public string Name = "Hyper Cube";

    public string Textures = "Textures";

    public Level level;

    public int LevelNumber;

    public TopLevelLists LevelLists;

    float Scale = 2.5f;

    Material opaquematerial;

    Material linematerial;

    List<MeshCollider> CollisionSectors = new List<MeshCollider>();

    GameObject collisionObject;

    GameObject renderObject;

    GameObject edgeObject;

    Color[] LightColor;

    [Serializable]
    public class TopLevelLists
    {
        public NativeList<float3> vertices;
        public NativeList<float4> textures;
        public NativeList<int> collide;
        public NativeList<int> render;
        public NativeList<int> edges;
        public NativeList<MathematicalPlane> planes;
        public NativeList<PolygonMeta> polygons;
        public NativeList<SectorMeta> sectors;
        public NativeList<StartPosition> positions;
        public NativeList<LevelLight> colors;
    }

    void Start()
    {
        LevelLists = new TopLevelLists();

        LevelLists.edges = new NativeList<int>(Allocator.Persistent);
        LevelLists.collide = new NativeList<int>(Allocator.Persistent);
        LevelLists.render = new NativeList<int>(Allocator.Persistent);
        LevelLists.vertices = new NativeList<float3>(Allocator.Persistent);
        LevelLists.textures = new NativeList<float4>(Allocator.Persistent);
        LevelLists.sectors = new NativeList<SectorMeta>(Allocator.Persistent);
        LevelLists.planes = new NativeList<MathematicalPlane>(Allocator.Persistent);
        LevelLists.polygons = new NativeList<PolygonMeta>(Allocator.Persistent);
        LevelLists.positions = new NativeList<StartPosition>(Allocator.Persistent);
        LevelLists.colors = new NativeList<LevelLight>(Allocator.Persistent);

        edgeObject = new GameObject("Edges");

        renderObject = new GameObject("Render");

        collisionObject = new GameObject("Collision");

        level = LoadLevel(Name, LevelNumber);

        BuildLights(LevelLists.colors, level);

        BuildObjects(LevelLists.positions, level, Scale);

        BuildTheLists(level, Scale, LevelLists.render, LevelLists.collide, LevelLists.edges, LevelLists.vertices, LevelLists.textures, LevelLists.planes, LevelLists.polygons, LevelLists.sectors);

        LightColor = new Color[LevelLists.colors.Length];

        Shader shader = Shader.Find("Custom/TexArray");

        opaquematerial = new Material(shader);

        linematerial = new Material(shader);

        for (int i = 0; i < LevelLists.colors.Length; i++)
        {
            LightColor[i] = new Color(LevelLists.colors[i].TriangleLight.r, LevelLists.colors[i].TriangleLight.g, LevelLists.colors[i].TriangleLight.b, 1.0f);
        }

        opaquematerial = new Material(shader);

        opaquematerial.mainTexture = Resources.Load<Texture2DArray>(Textures);

        opaquematerial.SetColorArray("_ColorArray", LightColor);

        BuildEdges(LevelLists.sectors, LevelLists.polygons, LevelLists.vertices, LevelLists.edges, linematerial, edgeObject);

        BuildOpaques(LevelLists.vertices, LevelLists.textures, LevelLists.render, LevelLists.sectors, LevelLists.polygons, opaquematerial, renderObject);

        BuildColliders(LevelLists.vertices, LevelLists.collide, LevelLists.sectors, LevelLists.polygons, CollisionSectors, collisionObject);
    }

    void OnDestroy()
    {
        if (LevelLists.edges.IsCreated)
        {
            LevelLists.edges.Dispose();
        }
        if (LevelLists.collide.IsCreated)
        {
            LevelLists.collide.Dispose();
        }
        if (LevelLists.render.IsCreated)
        {
            LevelLists.render.Dispose();
        }
        if (LevelLists.vertices.IsCreated)
        {
            LevelLists.vertices.Dispose();
        }
        if (LevelLists.textures.IsCreated)
        {
            LevelLists.textures.Dispose();
        }
        if (LevelLists.sectors.IsCreated)
        {
            LevelLists.sectors.Dispose();
        }
        if (LevelLists.planes.IsCreated)
        {
            LevelLists.planes.Dispose();
        }
        if (LevelLists.polygons.IsCreated)
        {
            LevelLists.polygons.Dispose();
        }
        if (LevelLists.positions.IsCreated)
        {
            LevelLists.positions.Dispose();
        }
        if (LevelLists.colors.IsCreated)
        {
            LevelLists.colors.Dispose();
        }
    }
}
