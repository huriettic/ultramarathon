using static BuildLevelFunctions;
using static RunLevelFunctions;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Weland;

public class BuildAndRunLevel : MonoBehaviour
{
    public string levelName = "Hyper Cube";

    public string texturesName = "Textures";

    public string shapesName = "Shapes";

    public int levelNumber;

    public float speed = 7f;
    public float jumpHeight = 2f;
    public float gravity = 5f;
    public float sensitivity = 10f;
    public float clampAngle = 90f;
    public float smoothFactor = 25f;

    float2 targetRotation;
    float3 currentForce;
    float2 currentRotation;
    float3 targetMovement;

    Level level;

    Texture2DArray textureArray;

    GraphicsBuffer triBuffer;

    // Default scale is 2.5, but Unreal tournament is 128
    float Scale = 2.5f;

    CharacterController Player;

    Color[] LightColor;

    Camera Cam;

    float3 CamPoint;

    SectorMeta CurrentSector;

    GameObject CollisionObjects;

    NativeArray<bool> processbool;
    NativeArray<float3> processvertices;
    NativeArray<float4> processtextures;
    NativeArray<float3> temporaryvertices;
    NativeArray<float4> temporarytextures;
    NativeArray<float3> outEdges;
    NativeArray<float3> processedgevertices;
    NativeArray<bool> processedgebool;
    NativeArray<float3> temporaryedgevertices;
    NativeArray<MathematicalPlane> planeA;
    NativeArray<MathematicalPlane> planeB;
    NativeList<SectorMeta> sideA;
    NativeList<SectorMeta> sideB;
    NativeList<Triangle> outTriangles;
    NativeList<TrianglesMeta> rawTriangles;
    NativeList<PortalMeta> rawPortals;
    NativeList<SectorMeta> contains;
    NativeList<SectorMeta> oldContains;
    NativeList<MathematicalPlane> OriginalFrustum;

    List<List<SectorMeta>> ListOfSectorLists = new List<List<SectorMeta>>();

    List<MeshCollider> CollisionSectors = new List<MeshCollider>();

    Material opaquematerial;

    TopLevelLists LevelLists;

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
        PlayerInput(Player, Cam, ref targetMovement, ref targetRotation, ref currentRotation, ref currentForce, speed, jumpHeight, gravity, sensitivity, clampAngle, smoothFactor);

        if (Cam.transform.hasChanged)
        {
            CamPoint = Cam.transform.position;

            GetSectors(ref CurrentSector, contains, oldContains, Player, CollisionSectors, LevelLists.sectors, LevelLists.polygons, LevelLists.planes, CamPoint, ListOfSectorLists);

            OriginalFrustum.Clear();

            ReadFrustumPlanes(Cam, OriginalFrustum);

            OriginalFrustum.RemoveAt(5);

            OriginalFrustum.RemoveAt(4);

            GetPolygons(CamPoint, CurrentSector, sideA, sideB, planeA, planeB, rawTriangles, rawPortals, outTriangles, OriginalFrustum, contains, LevelLists.vertices, LevelLists.textures, LevelLists.planes, LevelLists.sectors, LevelLists.polygons, LevelLists.render, LevelLists.edges, outEdges, processvertices, processtextures, temporaryvertices, temporarytextures, processbool, processedgevertices, temporaryedgevertices, processedgebool);

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
        if (LevelLists.render.IsCreated)
        {
            LevelLists.render.Dispose();
        }
        if (LevelLists.collide.IsCreated)
        {
            LevelLists.collide.Dispose();
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
        Player = GameObject.Find("Player").GetComponent<CharacterController>();

        Player.GetComponent<CharacterController>().enabled = true;

        Cursor.lockState = CursorLockMode.Locked;

        Cam = Camera.main;

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

        CollisionObjects = new GameObject("Collision Meshes");

        textureArray = BuildTextureArray(shapesName);

        level = LoadLevel(levelName, levelNumber);

        BuildLights(LevelLists.colors, level);

        BuildObjects(LevelLists.positions, level, Scale);

        BuildTheLists(level, Scale, LevelLists.render, LevelLists.collide, LevelLists.edges, LevelLists.vertices, LevelLists.textures, LevelLists.planes, LevelLists.polygons, LevelLists.sectors);

        LightColor = new Color[LevelLists.colors.Length];

        opaquematerial = CreateMaterial(LevelLists.colors, LightColor, texturesName, textureArray);

        BuildColliders(LevelLists.vertices, LevelLists.collide, LevelLists.sectors, LevelLists.polygons, CollisionSectors, CollisionObjects);

        CurrentSector = PlayerStart(Player, LevelLists.positions, LevelLists.sectors);

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
}
