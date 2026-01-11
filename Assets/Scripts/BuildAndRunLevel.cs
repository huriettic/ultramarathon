using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Weland;

[Serializable]
public struct Edge
{
    public Vector3 start;
    public Vector3 end;
};

[Serializable]
public struct Triangle
{
    public Vector3 v0, v1, v2;
    public Vector4 uv0, uv1, uv2;
};

[Serializable]
public struct MathematicalPlane
{
    public Vector3 normal;
    public float distance;
};

[Serializable]
public struct StartPos
{
    public Vector3 Position;
    public int SectorID;
};

[Serializable]
public struct LevelLight
{
    public Color TriangleLight;
};

[Serializable]
public struct PolygonMeta
{
    public int lineStartIndex;
    public int lineCount;

    public int opaqueStartIndex;
    public int opaqueCount;

    public int collisionStartIndex;
    public int collisionCount;

    public int connectedSectorID;
    public int sectorID;

    public int planeStartIndex;
    public int planeCount;

    public int plane;
};

[Serializable]
public struct SectorMeta
{
    public int polygonStartIndex;
    public int polygonCount;

    public int planeStartIndex;
    public int planeCount;

    public int sectorID;
};

public class BuildAndRunLevel : MonoBehaviour
{
    public bool SaveTheLevel = false;

    public bool LevelIsSaved = false;

    public string Name = "Hyper Cube";

    public string Textures = "Textures";

    public ComputeShader computeShader;

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

    private int kernel;
    private ComputeBuffer processVertices;
    private ComputeBuffer processTextures;
    private ComputeBuffer processBool;
    private ComputeBuffer temporaryVertices;
    private ComputeBuffer temporaryTextures;
    private ComputeBuffer inputTriangleBuffer;
    private ComputeBuffer inputPolygonBuffer;
    private ComputeBuffer planeBuffer;
    private ComputeBuffer outputTriangleBuffer;
    private ComputeBuffer argsBuffer;

    // Default scale is 2.5, but Unreal tournament is 128
    private float Scale = 2.5f;

    private CharacterController Player;

    private float planeDistance;

    private bool radius;

    private bool check;

    private int MaxDepth;

    private Mesh opaquemesh;

    private Mesh transparentmesh;

    private Color[] LightColor;

    private Camera Cam;

    private Vector3 CamPoint;

    private SectorMeta CurrentSector;

    private SectorMeta NextSector;

    private GameObject CollisionObjects;

    private bool[] processbool;

    private Vector3[] processvertices;

    private Vector3[] temporaryvertices;

    private Vector3[] outputvertices;

    private PolygonMeta[] combinedpolygons;

    private MathematicalPlane[] combinedplanes;

    private SectorMeta[] oldsectors;

    private SectorMeta[] sectors;

    private uint[] uintArgs;

    private int sectorscount;

    private int oldsectorscount;

    private int combinedpolygonscount;

    private int combinedplanescount;

    private int outputverticescount;

    private Queue<SectorMeta> PortalQueue = new Queue<SectorMeta>();

    private Queue<SectorMeta> SectorQueue = new Queue<SectorMeta>();

    private List<Vector3> OpaqueVertices = new List<Vector3>();

    private List<int> OpaqueTriangles = new List<int>();

    private List<MeshCollider> CollisionSectors = new List<MeshCollider>();

    private Material opaquematerial;

    private List<Mesh> CollisionMesh = new List<Mesh>();

    private TopLevelLists LevelLists;

    [Serializable]
    public class TopLevelLists
    {
        public List<SectorMeta> sectors = new List<SectorMeta>();
        public List<StartPos> positions = new List<StartPos>();
        public List<LevelLight> colors = new List<LevelLight>();
        public List<Triangle> opaques = new List<Triangle>();
        public List<Triangle> collisions = new List<Triangle>();
        public List<MathematicalPlane> planes = new List<MathematicalPlane>();
        public List<PolygonMeta> polygons = new List<PolygonMeta>();
        public List<Edge> edges = new List<Edge>();
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

    private List<Vector3> CCW = new List<Vector3>();

    private List<Vector2> CCWUV = new List<Vector2>();

    private List<Vector2> CCWUVOffset = new List<Vector2>();

    private List<Vector4> CCWUVOffsetZ = new List<Vector4>();

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
        walltri = new List<int>()
        {
            0, 1, 2, 0, 2, 3
        };

        LevelLists = new TopLevelLists();

        LoadLevel();

        LightColor = new Color[LevelLists.colors.Count];

        combinedpolygons = new PolygonMeta[LevelLists.polygons.Count * 10];

        combinedplanes = new MathematicalPlane[LevelLists.polygons.Count * 10];

        processbool = new bool[128];

        processvertices = new Vector3[128];

        temporaryvertices = new Vector3[128];

        outputvertices = new Vector3[128];

        oldsectors = new SectorMeta[128];

        sectors = new SectorMeta[128];

        uintArgs = new uint[] { 0, 1, 0, 0 };

        CreateMaterial();

        int strideTriangle = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle));
        int stridePolygon = System.Runtime.InteropServices.Marshal.SizeOf(typeof(PolygonMeta));
        int stridePlane = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MathematicalPlane));
        int strideVertex = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
        int strideTexture = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4));
        int strideUint = System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint));
        int scratchSize = (LevelLists.polygons.Count * 10) * 256;

        processVertices = new ComputeBuffer(scratchSize, strideVertex);
        processTextures = new ComputeBuffer(scratchSize, strideTexture);
        processBool = new ComputeBuffer(scratchSize, strideUint);
        temporaryVertices = new ComputeBuffer(scratchSize, strideVertex);
        temporaryTextures = new ComputeBuffer(scratchSize, strideTexture);
        planeBuffer = new ComputeBuffer(LevelLists.polygons.Count * 10, stridePlane, ComputeBufferType.Structured);
        inputPolygonBuffer = new ComputeBuffer(LevelLists.polygons.Count * 10, stridePolygon, ComputeBufferType.Structured);
        inputTriangleBuffer = new ComputeBuffer(LevelLists.opaques.Count, strideTriangle, ComputeBufferType.Structured);
        outputTriangleBuffer = new ComputeBuffer(LevelLists.opaques.Count * 10, strideTriangle, ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, strideUint * 4, ComputeBufferType.IndirectArguments);

        inputTriangleBuffer.SetData(LevelLists.opaques);

        computeShader.SetBuffer(kernel, "processVertices", processVertices);
        computeShader.SetBuffer(kernel, "processTextures", processTextures);
        computeShader.SetBuffer(kernel, "processBool", processBool);
        computeShader.SetBuffer(kernel, "temporaryVertices", temporaryVertices);
        computeShader.SetBuffer(kernel, "temporaryTextures", temporaryTextures);
        computeShader.SetBuffer(kernel, "planeBuffer", planeBuffer);
        computeShader.SetBuffer(kernel, "inputTriangleBuffer", inputTriangleBuffer);
        computeShader.SetBuffer(kernel, "inputPolygonBuffer", inputPolygonBuffer);
        computeShader.SetBuffer(kernel, "outputTriangleBuffer", outputTriangleBuffer);
        computeShader.SetBuffer(kernel, "argsBuffer", argsBuffer);

        opaquematerial.SetBuffer("outputTriangleBuffer", outputTriangleBuffer);

        opaquemesh = new Mesh();

        opaquemesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        transparentmesh = new Mesh();

        transparentmesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        CollisionObjects = new GameObject("Collision Meshes");

        BuildColliders();

        Cursor.lockState = CursorLockMode.Locked;

        Playerstart();

        Player.GetComponent<CharacterController>().enabled = true;

        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            Physics.IgnoreCollision(Player, CollisionSectors[LevelLists.sectors[i].sectorID], true);
        }
    }

    void Update()
    {
        PlayerInput();

        if (Cam.transform.hasChanged)
        {
            CamPoint = Cam.transform.position;

            sectorscount = 0;

            GetSectors(CurrentSector);

            combinedplanescount = 0;

            ReadFrustumPlanes(Cam, combinedplanes);

            MaxDepth = 0;

            combinedpolygonscount = 0;

            GetPortals(CurrentSector);

            GetTriangles();

            Cam.transform.hasChanged = false;
        }
    }

    void OnDestroy()
    {
        processVertices?.Release();
        processTextures?.Release();
        processBool?.Release();
        temporaryVertices?.Release();
        temporaryTextures?.Release();
        planeBuffer?.Release();
        inputTriangleBuffer?.Release();
        inputPolygonBuffer?.Release();
        outputTriangleBuffer?.Release();
        argsBuffer?.Release();
    }

    void OnRenderObject()
    {
        opaquematerial.SetPass(0);
        Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, argsBuffer);
    }

    void Awake()
    {
        Player = GameObject.Find("Player").GetComponent<CharacterController>();

        kernel = computeShader.FindKernel("CSMain");

        Cam = Camera.main;
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

            BuildLines();

            BuildPolygons();

            BuildObjects();

            BuildLights();

            BuildTheLists();

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

        for (int i = 0; i < LevelLists.colors.Count; i++)
        {
            LightColor[i] = new Color(LevelLists.colors[i].TriangleLight.r, LevelLists.colors[i].TriangleLight.g, LevelLists.colors[i].TriangleLight.b, 1.0f);
        }

        opaquematerial = new Material(shader);

        opaquematerial.mainTexture = Resources.Load<Texture2DArray>(Textures);

        opaquematerial.SetColorArray("_ColorArray", LightColor);
    }

    public void Playerstart()
    {
        if (LevelLists.positions.Count == 0)
        {
            Debug.LogError("No player starts available.");

            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, LevelLists.positions.Count);

        StartPos selectedPosition = LevelLists.positions[randomIndex];

        CurrentSector = LevelLists.sectors[selectedPosition.SectorID];

        Player.transform.position = new Vector3(selectedPosition.Position.x, selectedPosition.Position.y + 1.10f, selectedPosition.Position.z);
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

    public void SetFrustumPlanes(MathematicalPlane[] planes, Matrix4x4 m)
    {
        if (planes == null)
        {
            return;
        }
           
        var r0 = m.GetRow(0);
        var r1 = m.GetRow(1);
        var r2 = m.GetRow(2);
        var r3 = m.GetRow(3);

        planes[combinedplanescount] = FromVec4(r3 - r0); // Right
        planes[combinedplanescount + 1] = FromVec4(r3 + r0); // Left
        planes[combinedplanescount + 2] = FromVec4(r3 - r1); // Top
        planes[combinedplanescount + 3] = FromVec4(r3 + r1); // Bottom
        planes[combinedplanescount + 4] = FromVec4(r3 - r2); // Far
        planes[combinedplanescount + 5] = FromVec4(r3 + r2); // Near
        combinedplanescount += 4;
    }

    public void ReadFrustumPlanes(Camera cam, MathematicalPlane[] planes)
    {
        SetFrustumPlanes(planes, cam.projectionMatrix * cam.worldToCameraMatrix);
    }

    public void SetClippingPlanes(Vector3[] vertices, int portalnumber, int polygonStart, int polygonCount, Vector3 viewPos)
    {
        int StartIndex = combinedplanescount;

        int IndexCount = 0;

        int count = outputverticescount;
        for (int i = 0; i < count; i += 2)
        {
            Vector3 p1 = vertices[i];
            Vector3 p2 = vertices[i + 1];
            Vector3 normal = Vector3.Cross(p1 - p2, viewPos - p2);
            float magnitude = normal.magnitude;
            Vector3 normalized = normal / magnitude;

            if (magnitude > 0.01f)
            {
                combinedplanes[combinedplanescount] = new MathematicalPlane { normal = normalized, distance = -Vector3.Dot(normalized, p1) };
                combinedplanescount += 1;
                IndexCount += 1;
            }
        }

        NextSector.polygonStartIndex = polygonStart;
        NextSector.polygonCount = polygonCount;

        NextSector.planeStartIndex = StartIndex;
        NextSector.planeCount = IndexCount;

        NextSector.sectorID = portalnumber;
    }

    public void BuildColliders()
    {
        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            OpaqueVertices.Clear();

            OpaqueTriangles.Clear();

            int triangleCount = 0;

            for (int e = LevelLists.sectors[i].polygonStartIndex; e < LevelLists.sectors[i].polygonStartIndex + LevelLists.sectors[i].polygonCount; e++)
            {
                if (LevelLists.polygons[e].collisionCount != -1)
                {
                    for (int f = LevelLists.polygons[e].collisionStartIndex; f < LevelLists.polygons[e].collisionStartIndex + LevelLists.polygons[e].collisionCount; f++)
                    {
                        OpaqueVertices.Add(LevelLists.collisions[f].v0);
                        OpaqueVertices.Add(LevelLists.collisions[f].v1);
                        OpaqueVertices.Add(LevelLists.collisions[f].v2);
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

    public void ClipEdgesWithPlanes(SectorMeta planes, PolygonMeta portal)
    {
        outputverticescount = 0;

        int processverticescount = 0;
        int processboolcount = 0;

        for (int a = portal.lineStartIndex; a < portal.lineStartIndex + portal.lineCount; a++)
        {
            Edge line = LevelLists.edges[a];
            processvertices[processverticescount] = line.start;
            processvertices[processverticescount + 1] = line.end;
            processverticescount += 2;
            processbool[processboolcount] = true;
            processbool[processboolcount + 1] = true;
            processboolcount += 2;
        }

        for (int b = planes.planeStartIndex; b < planes.planeStartIndex + planes.planeCount; b++)
        {
            int intersection = 0;
            int temporaryverticescount = 0;

            Vector3 intersectionPoint1 = Vector3.zero;
            Vector3 intersectionPoint2 = Vector3.zero;

            for (int c = 0; c < processverticescount; c += 2)
            {
                if (processbool[c] == false && processbool[c + 1] == false)
                {
                    continue;
                }

                Vector3 p1 = processvertices[c];
                Vector3 p2 = processvertices[c + 1];

                float d1 = GetPlaneSignedDistanceToPoint(combinedplanes[b], processvertices[c]);
                float d2 = GetPlaneSignedDistanceToPoint(combinedplanes[b], processvertices[c + 1]);

                bool b0 = d1 >= 0;
                bool b1 = d2 >= 0;

                if (b0 && b1)
                {
                    continue;
                }
                else if ((b0 && !b1) || (!b0 && b1))
                {
                    Vector3 point1;
                    Vector3 point2;

                    float t = d1 / (d1 - d2);

                    Vector3 intersectionPoint = Vector3.Lerp(p1, p2, t);

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

                    temporaryvertices[temporaryverticescount] = point1;
                    temporaryvertices[temporaryverticescount + 1] = point2;
                    temporaryverticescount += 2;

                    processbool[c] = false;
                    processbool[c + 1] = false;

                    intersection += 1;
                }
                else
                {
                    processbool[c] = false;
                    processbool[c + 1] = false;
                }
            }

            if (intersection == 2)
            {
                for (int d = 0; d < temporaryverticescount; d += 2)
                {
                    processvertices[processverticescount] = temporaryvertices[d];
                    processvertices[processverticescount + 1] = temporaryvertices[d + 1];
                    processverticescount += 2;
                    processbool[processboolcount] = true;
                    processbool[processboolcount + 1] = true;
                    processboolcount += 2;
                }

                processvertices[processverticescount] = intersectionPoint1;
                processvertices[processverticescount + 1] = intersectionPoint2;
                processverticescount += 2;
                processbool[processboolcount] = true;
                processbool[processboolcount + 1] = true;
                processboolcount += 2;
            }
        }

        for (int e = 0; e < processboolcount; e += 2)
        {
            if (processbool[e] == true && processbool[e + 1] == true)
            {
                outputvertices[outputverticescount] = processvertices[e];
                outputvertices[outputverticescount + 1] = processvertices[e + 1];
                outputverticescount += 2;
            }
        }
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
        for (int i = 0; i < sectorscount; i++)
        {
            if (sectors[i].sectorID == sectorID) 
            {
                return true;
            }    
        }
        return false;
    }

    public bool SectorsDoNotEqual()
    {
        if (sectorscount != oldsectorscount) 
        {
            return true;
        }

        for (int i = 0; i < sectorscount; i++)
        {
            if (sectors[i].sectorID != oldsectors[i].sectorID)
            {
                return true;
            }
        }
        return false;
    }

    public void GetSectors(SectorMeta ASector)
    {
        for (int i = 0; i < oldsectorscount; i++)
        {
            Physics.IgnoreCollision(Player, CollisionSectors[oldsectors[i].sectorID], true);
        }

        SectorQueue.Enqueue(ASector);

        while (SectorQueue.Count > 0)
        {
            SectorMeta sector = SectorQueue.Dequeue();

            sectors[sectorscount] = sector;
            sectorscount += 1;

            Physics.IgnoreCollision(Player, CollisionSectors[sector.sectorID], false);

            for (int i = sector.polygonStartIndex; i < sector.polygonStartIndex + sector.polygonCount; i++)
            {
                int connectedsector = LevelLists.polygons[i].connectedSectorID;

                if (connectedsector == -1)
                {
                    continue;
                }

                SectorMeta portalsector = LevelLists.sectors[connectedsector];

                if (SectorsContains(portalsector.sectorID))
                {
                    continue;
                }

                radius = CheckRadius(portalsector, CamPoint);

                if (radius)
                {
                    SectorQueue.Enqueue(portalsector);
                }
            }

            check = CheckSector(sector, CamPoint);

            if (check)
            {
                CurrentSector = sector;
            }
        }

        if (SectorsDoNotEqual())
        {
            oldsectorscount = 0;

            for (int i = 0; i < sectorscount; i++)
            {
                oldsectors[oldsectorscount] = sectors[i];
                oldsectorscount += 1;
            }
        }
    }

    public void GetTriangles()
    {
        inputPolygonBuffer.SetData(combinedpolygons, 0, 0, combinedpolygonscount);

        planeBuffer.SetData(combinedplanes, 0, 0, combinedplanescount);

        argsBuffer.SetData(uintArgs);

        outputTriangleBuffer.SetCounterValue(0);

        computeShader.Dispatch(kernel, combinedpolygonscount, 1, 1);
    }

    public void GetPortals(SectorMeta ASector)
    {
        PortalQueue.Enqueue(ASector);

        while (PortalQueue.Count > 0)
        {
            SectorMeta sector = PortalQueue.Dequeue();

            for (int i = sector.polygonStartIndex; i < sector.polygonStartIndex + sector.polygonCount; i++)
            {
                if (MaxDepth > 4096)
                {
                    continue;
                }

                PolygonMeta polygon = LevelLists.polygons[i];

                planeDistance = GetPlaneSignedDistanceToPoint(LevelLists.planes[polygon.plane], CamPoint);

                if (planeDistance <= 0)
                {
                    continue;
                }

                int connectedsector = polygon.connectedSectorID;

                if (connectedsector == -1)
                {
                    polygon.planeStartIndex = sector.planeStartIndex;
                    polygon.planeCount = sector.planeCount;

                    combinedpolygons[combinedpolygonscount] = polygon;
                    combinedpolygonscount += 1;
                }
                else
                {
                    int connectedstart = LevelLists.sectors[connectedsector].polygonStartIndex;

                    int connectedcount = LevelLists.sectors[connectedsector].polygonCount;

                    if (SectorsContains(LevelLists.sectors[connectedsector].sectorID))
                    {
                        MaxDepth += 1;

                        NextSector.polygonStartIndex = connectedstart;
                        NextSector.polygonCount = connectedcount;

                        NextSector.planeStartIndex = sector.planeStartIndex;
                        NextSector.planeCount = sector.planeCount;

                        NextSector.sectorID = connectedsector;

                        PortalQueue.Enqueue(NextSector);

                        continue;
                    }

                    ClipEdgesWithPlanes(sector, LevelLists.polygons[i]);

                    if (outputverticescount < 6 || outputverticescount % 2 == 1)
                    {
                        continue;
                    }

                    SetClippingPlanes(outputvertices, connectedsector, connectedstart, connectedcount, CamPoint);

                    MaxDepth += 1;

                    PortalQueue.Enqueue(NextSector);
                }
            }
        }
    }

    public void BuildTheLists()
    {
        int opaqueStart = 0;
        int collisionStart = 0;
        int edgeStart = 0;
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

                if (Portal[b] != -1)
                {
                    int edgeCount = 0;

                    for (int c = 0; c < mesh.vertexCount; c++)
                    {
                        int d = (c + 1) % mesh.vertexCount;

                        Edge line = new Edge
                        {
                            start = mesh.vertices[c],
                            end = mesh.vertices[d]
                        };

                        LevelLists.edges.Add(line);

                        edgeCount += 1;
                    }

                    meta.lineStartIndex = edgeStart;
                    meta.lineCount = edgeCount;
                    edgeStart += edgeCount;
                }
                else
                {
                    meta.lineStartIndex = -1;
                    meta.lineCount = -1;
                }

                MathematicalPlane plane = new MathematicalPlane
                {
                    normal = mesh.normals[0],
                    distance = -Vector3.Dot(mesh.normals[0], mesh.vertices[0])
                };

                LevelLists.planes.Add(plane);
                meta.plane = planeStart;
                planeStart += 1;

                if (Render[b] == a)
                {
                    int count = 0;
                    uvVector4.Clear();
                    mesh.GetUVs(0, uvVector4);

                    for (int c = 0; c < mesh.triangles.Length; c += 3)
                    {
                        Triangle t = new Triangle
                        {
                            v0 = mesh.vertices[mesh.triangles[c]],
                            v1 = mesh.vertices[mesh.triangles[c + 1]],
                            v2 = mesh.vertices[mesh.triangles[c + 2]],
                            uv0 = uvVector4[mesh.triangles[c]],
                            uv1 = uvVector4[mesh.triangles[c + 1]],
                            uv2 = uvVector4[mesh.triangles[c + 2]],
                        };

                        LevelLists.opaques.Add(t);
                        count += 1;
                    }

                    meta.opaqueStartIndex = opaqueStart;
                    meta.opaqueCount = count;
                    opaqueStart += count;
                }
                else
                {
                    meta.opaqueStartIndex = -1;
                    meta.opaqueCount = -1;
                }

                if (Collision[b] == a)
                {
                    int count = 0;

                    for (int c = 0; c < mesh.triangles.Length; c += 3)
                    {
                        Triangle t = new Triangle
                        {
                            v0 = mesh.vertices[mesh.triangles[c]],
                            v1 = mesh.vertices[mesh.triangles[c + 1]],
                            v2 = mesh.vertices[mesh.triangles[c + 2]],
                            uv0 = Vector4.zero,
                            uv1 = Vector4.zero,
                            uv2 = Vector4.zero,
                        };

                        LevelLists.collisions.Add(t);
                        count += 1;
                    }

                    meta.collisionStartIndex = collisionStart;
                    meta.collisionCount = count;
                    collisionStart += count;
                }
                else
                {
                    meta.collisionStartIndex = -1;
                    meta.collisionCount = -1;
                }

                meta.sectorID = a;
                meta.connectedSectorID = Portal[b];
                meta.planeStartIndex = -1;
                meta.planeCount = -1;

                LevelLists.polygons.Add(meta);
                polygonCount += 1;
            }

            SectorMeta sectorMeta = new SectorMeta
            {
                sectorID = a,
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
                StartPos Start = new StartPos();

                Start.Position = new Vector3((float)level.Objects[i].X / 1024 * Scale, (float)level.Polygons[level.Objects[i].PolygonIndex].FloorHeight / 1024 * Scale, (float)level.Objects[i].Y / 1024 * Scale * -1);

                Start.SectorID = level.Objects[i].PolygonIndex;

                LevelLists.positions.Add(Start);
            }
        }
    }

    public void BuildLines()
    {
        for (int i = 0; i < level.Lines.Count; ++i)
        {
            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * Scale;
            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * Scale * -1;

            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * Scale;
            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * Scale * -1;

            if (level.Lines[i].ClockwisePolygonOwner != -1)
            {
                if (level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight > level.Lines[i].LowestAdjacentCeiling)
                {
                    if (level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight < level.Lines[i].LowestAdjacentCeiling)
                    {
                        double YC0 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight / 1024 * Scale;
                        double YC1 = (float)level.Lines[i].LowestAdjacentCeiling / 1024 * Scale;

                        CW.Clear();
                        CWUV.Clear();
                        CWUVOffset.Clear();
                        CWUVOffsetZ.Clear();

                        GetVertsCW(X0, X1, YC1, YC0, Z0, Z1);

                        if (level.Lines[i].ClockwisePolygonSideIndex != -1)
                        {
                            MakeSidesCW(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary, level.Sides[level.Lines[i].ClockwisePolygonSideIndex].PrimaryLightsourceIndex);

                            if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 27 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 28 ||
                                level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 29 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 30)
                            {
                                Render.Add(-1);

                                MeshTexture.Add(-1);

                                MeshTextureCollection.Add(-1);
                            }
                            else
                            {
                                Render.Add(level.Lines[i].ClockwisePolygonOwner);

                                MeshTexture.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection);
                            }
                        }
                        else
                        {
                            Render.Add(-1);

                            MeshTexture.Add(-1);

                            MeshTextureCollection.Add(-1);
                        }

                        Plane.Add(level.Lines[i].ClockwisePolygonOwner);

                        Portal.Add(-1);



                        Collision.Add(level.Lines[i].ClockwisePolygonOwner);

                        Transparent.Add(-1);

                        Mesh mesh = new Mesh();

                        mesh.SetVertices(CW);

                        if (level.Lines[i].ClockwisePolygonSideIndex == -1)
                        {
                            mesh.SetUVs(0, CWUV);
                        }
                        else
                        {
                            if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.IsEmpty())
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
                        double YC0 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight / 1024 * Scale;
                        double YC1 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight / 1024 * Scale;

                        CW.Clear();
                        CWUV.Clear();
                        CWUVOffset.Clear();
                        CWUVOffsetZ.Clear();

                        GetVertsCW(X0, X1, YC1, YC0, Z0, Z1);

                        if (level.Lines[i].ClockwisePolygonSideIndex != -1)
                        {
                            MakeSidesCW(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary, level.Sides[level.Lines[i].ClockwisePolygonSideIndex].PrimaryLightsourceIndex);

                            if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 27 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 28 ||
                                level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 29 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 30)
                            {
                                Render.Add(-1);

                                MeshTexture.Add(-1);

                                MeshTextureCollection.Add(-1);
                            }
                            else
                            {
                                Render.Add(level.Lines[i].ClockwisePolygonOwner);

                                MeshTexture.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection);
                            }
                        }
                        else
                        {
                            Render.Add(-1);

                            MeshTexture.Add(-1);

                            MeshTextureCollection.Add(-1);
                        }

                        Plane.Add(level.Lines[i].ClockwisePolygonOwner);

                        Portal.Add(-1);

                        Collision.Add(level.Lines[i].ClockwisePolygonOwner);

                        Transparent.Add(-1);

                        Mesh mesh = new Mesh();

                        mesh.SetVertices(CW);

                        if (level.Lines[i].ClockwisePolygonSideIndex == -1)
                        {
                            mesh.SetUVs(0, CWUV);
                        }
                        else
                        {
                            if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.IsEmpty())
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
                if (level.Lines[i].LowestAdjacentCeiling != level.Lines[i].HighestAdjacentFloor)
                {
                    if (level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight > level.Lines[i].HighestAdjacentFloor &&
                        level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight < level.Lines[i].LowestAdjacentCeiling)
                    {
                        double YC = (float)level.Lines[i].LowestAdjacentCeiling / 1024 * Scale;
                        double YF = (float)level.Lines[i].HighestAdjacentFloor / 1024 * Scale;

                        CW.Clear();
                        CWUV.Clear();
                        CWUVOffset.Clear();
                        CWUVOffsetZ.Clear();

                        GetVertsCW(X0, X1, YF, YC, Z0, Z1);

                        Plane.Add(level.Lines[i].ClockwisePolygonOwner);

                        if (level.Lines[i].CounterclockwisePolygonOwner != -1)
                        {
                            Portal.Add(level.Lines[i].CounterclockwisePolygonOwner);
                        }
                        else
                        {
                            Portal.Add(-1);
                        }

                        if (level.Lines[i].CounterclockwisePolygonOwner == -1)
                        {
                            if (level.Lines[i].ClockwisePolygonSideIndex != -1)
                            {
                                if (!level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.IsEmpty())
                                {
                                    if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 27 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 28 ||
                                        level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 29 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 30)
                                    {
                                        Render.Add(-1);

                                        Transparent.Add(-1);
                                    }
                                    else
                                    {
                                        Render.Add(level.Lines[i].ClockwisePolygonOwner);

                                        Transparent.Add(-1);
                                    }

                                    MakeSidesCW(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary, level.Sides[level.Lines[i].ClockwisePolygonSideIndex].PrimaryLightsourceIndex);

                                    MeshTexture.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection);
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
                        }
                        else
                        {
                            if (level.Lines[i].ClockwisePolygonSideIndex != -1)
                            {
                                if (!level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Transparent.Texture.IsEmpty())
                                {
                                    if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Transparent.Texture.Collection == 27 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Transparent.Texture.Collection == 28 ||
                                        level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Transparent.Texture.Collection == 29 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Transparent.Texture.Collection == 30)
                                    {
                                        Render.Add(-1);

                                        Transparent.Add(-1);
                                    }
                                    else
                                    {
                                        Render.Add(-1);

                                        Transparent.Add(level.Lines[i].ClockwisePolygonOwner);
                                    }

                                    MakeSidesCW(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Transparent, level.Sides[level.Lines[i].ClockwisePolygonSideIndex].TransparentLightsourceIndex);

                                    MeshTexture.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Transparent.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Transparent.Texture.Collection);
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
                        }

                        if (level.Lines[i].Solid == true)
                        {
                            Collision.Add(level.Lines[i].ClockwisePolygonOwner);
                        }
                        else
                        {
                            Collision.Add(-1);
                        }

                        Mesh mesh = new Mesh();

                        mesh.SetVertices(CW);

                        if (level.Lines[i].CounterclockwisePolygonOwner == -1)
                        {
                            if (level.Lines[i].ClockwisePolygonSideIndex != -1)
                            {
                                if (!level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.IsEmpty())
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
                        }
                        else
                        {
                            if (level.Lines[i].ClockwisePolygonSideIndex != -1)
                            {
                                if (!level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Transparent.Texture.IsEmpty())
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
                        }

                        mesh.SetTriangles(walltri, 0);
                        mesh.RecalculateNormals();

                        meshes.Add(mesh);
                    }
                }

                if (level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight < level.Lines[i].HighestAdjacentFloor)
                {
                    if (level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight > level.Lines[i].HighestAdjacentFloor)
                    {
                        double YF0 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight / 1024 * Scale;
                        double YF1 = (float)level.Lines[i].HighestAdjacentFloor / 1024 * Scale;

                        CW.Clear();
                        CWUV.Clear();
                        CWUVOffset.Clear();
                        CWUVOffsetZ.Clear();

                        GetVertsCW(X0, X1, YF0, YF1, Z0, Z1);

                        if (level.Lines[i].ClockwisePolygonSideIndex != -1)
                        {
                            if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Type == SideType.Low)
                            {
                                MakeSidesCW(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary, level.Sides[level.Lines[i].ClockwisePolygonSideIndex].PrimaryLightsourceIndex);

                                if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 27 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 28 ||
                                    level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 29 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 30)
                                {
                                    Render.Add(-1);
                                }
                                else
                                {
                                    Render.Add(level.Lines[i].ClockwisePolygonOwner);
                                }

                                MeshTexture.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection);
                            }
                            else if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Type == SideType.Split)
                            {
                                MakeSidesCW(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary, level.Sides[level.Lines[i].ClockwisePolygonSideIndex].SecondaryLightsourceIndex);

                                if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Collection == 27 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Collection == 28 ||
                                    level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Collection == 29 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Collection == 30)
                                {
                                    Render.Add(-1);
                                }
                                else
                                {
                                    Render.Add(level.Lines[i].ClockwisePolygonOwner);
                                }

                                MeshTexture.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Collection);
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

                        Plane.Add(level.Lines[i].ClockwisePolygonOwner);

                        Portal.Add(-1);

                        Collision.Add(level.Lines[i].ClockwisePolygonOwner);

                        Transparent.Add(-1);

                        Mesh mesh = new Mesh();

                        mesh.SetVertices(CW);

                        if (level.Lines[i].ClockwisePolygonSideIndex != -1)
                        {
                            if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Type == SideType.Low)
                            {
                                if (level.Lines[i].ClockwisePolygonSideIndex == -1)
                                {
                                    mesh.SetUVs(0, CWUV);
                                }
                                else
                                {
                                    if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.IsEmpty())
                                    {
                                        mesh.SetUVs(0, CWUV);
                                    }
                                    else
                                    {
                                        mesh.SetUVs(0, CWUVOffsetZ);
                                    }
                                }
                            }
                            else if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Type == SideType.Split)
                            {
                                if (level.Lines[i].ClockwisePolygonSideIndex == -1)
                                {
                                    mesh.SetUVs(0, CWUV);
                                }
                                else
                                {
                                    if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.IsEmpty())
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
                        double YF0 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight / 1024 * Scale;
                        double YF1 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight / 1024 * Scale;

                        CW.Clear();
                        CWUV.Clear();
                        CWUVOffset.Clear();
                        CWUVOffsetZ.Clear();

                        GetVertsCW(X0, X1, YF0, YF1, Z0, Z1);

                        if (level.Lines[i].ClockwisePolygonSideIndex != -1)
                        {
                            if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Type == SideType.Low)
                            {
                                MakeSidesCW(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary, level.Sides[level.Lines[i].ClockwisePolygonSideIndex].PrimaryLightsourceIndex);

                                if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 27 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 28 ||
                                    level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 29 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection == 30)
                                {
                                    Render.Add(-1);
                                }
                                else
                                {
                                    Render.Add(level.Lines[i].ClockwisePolygonOwner);
                                }

                                MeshTexture.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.Collection);
                            }
                            else if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Type == SideType.Split)
                            {
                                MakeSidesCW(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary, level.Sides[level.Lines[i].ClockwisePolygonSideIndex].SecondaryLightsourceIndex);

                                if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Collection == 27 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Collection == 28 ||
                                    level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Collection == 29 || level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Collection == 30)
                                {
                                    Render.Add(-1);
                                }
                                else
                                {
                                    Render.Add(level.Lines[i].ClockwisePolygonOwner);
                                }

                                MeshTexture.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.Collection);
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

                        Plane.Add(level.Lines[i].ClockwisePolygonOwner);

                        Portal.Add(-1);

                        Collision.Add(level.Lines[i].ClockwisePolygonOwner);

                        Transparent.Add(-1);

                        Mesh mesh = new Mesh();

                        mesh.SetVertices(CW);

                        if (level.Lines[i].ClockwisePolygonSideIndex != -1)
                        {
                            if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Type == SideType.Low)
                            {
                                if (level.Lines[i].ClockwisePolygonSideIndex == -1)
                                {
                                    mesh.SetUVs(0, CWUV);
                                }
                                else
                                {
                                    if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Primary.Texture.IsEmpty())
                                    {
                                        mesh.SetUVs(0, CWUV);
                                    }
                                    else
                                    {
                                        mesh.SetUVs(0, CWUVOffsetZ);
                                    }
                                }
                            }
                            else if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Type == SideType.Split)
                            {
                                if (level.Lines[i].ClockwisePolygonSideIndex == -1)
                                {
                                    mesh.SetUVs(0, CWUV);
                                }
                                else
                                {
                                    if (level.Sides[level.Lines[i].ClockwisePolygonSideIndex].Secondary.Texture.IsEmpty())
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

            if (level.Lines[i].CounterclockwisePolygonOwner != -1)
            {
                if (level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight > level.Lines[i].LowestAdjacentCeiling)
                {
                    if (level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight < level.Lines[i].LowestAdjacentCeiling)
                    {
                        double YC0 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight / 1024 * Scale;
                        double YC1 = (float)level.Lines[i].LowestAdjacentCeiling / 1024 * Scale;

                        CCW.Clear();
                        CCWUV.Clear();
                        CCWUVOffset.Clear();
                        CCWUVOffsetZ.Clear();

                        GetVertsCCW(X0, X1, YC1, YC0, Z0, Z1);

                        if (level.Lines[i].CounterclockwisePolygonSideIndex != -1)
                        {
                            MakeSidesCCW(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary, level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].PrimaryLightsourceIndex);

                            if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 27 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 28 ||
                                level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 29 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 30)
                            {
                                Render.Add(-1);

                                MeshTexture.Add(-1);

                                MeshTextureCollection.Add(-1);
                            }
                            else
                            {
                                Render.Add(level.Lines[i].CounterclockwisePolygonOwner);

                                MeshTexture.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection);
                            }
                        }
                        else
                        {
                            Render.Add(-1);

                            MeshTexture.Add(-1);

                            MeshTextureCollection.Add(-1);
                        }

                        Plane.Add(level.Lines[i].CounterclockwisePolygonOwner);

                        Portal.Add(-1);

                        Collision.Add(level.Lines[i].CounterclockwisePolygonOwner);

                        Transparent.Add(-1);

                        Mesh mesh = new Mesh();

                        mesh.SetVertices(CCW);

                        if (level.Lines[i].CounterclockwisePolygonSideIndex == -1)
                        {
                            mesh.SetUVs(0, CCWUV);
                        }
                        else
                        {
                            if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.IsEmpty())
                            {
                                mesh.SetUVs(0, CCWUV);
                            }
                            else
                            {
                                mesh.SetUVs(0, CCWUVOffsetZ);
                            }
                        }

                        mesh.SetTriangles(walltri, 0);
                        mesh.RecalculateNormals();

                        meshes.Add(mesh);
                    }
                    else
                    {
                        double YC0 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight / 1024 * Scale;
                        double YC1 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight / 1024 * Scale;

                        CCW.Clear();
                        CCWUV.Clear();
                        CCWUVOffset.Clear();
                        CCWUVOffsetZ.Clear();

                        GetVertsCCW(X0, X1, YC1, YC0, Z0, Z1);

                        if (level.Lines[i].CounterclockwisePolygonSideIndex != -1)
                        {
                            MakeSidesCCW(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary, level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].PrimaryLightsourceIndex);

                            if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 27 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 28 ||
                                level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 29 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 30)
                            {
                                Render.Add(-1);

                                MeshTexture.Add(-1);

                                MeshTextureCollection.Add(-1);
                            }
                            else
                            {
                                Render.Add(level.Lines[i].CounterclockwisePolygonOwner);

                                MeshTexture.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection);
                            }
                        }
                        else
                        {
                            Render.Add(-1);

                            MeshTexture.Add(-1);

                            MeshTextureCollection.Add(-1);
                        }

                        Plane.Add(level.Lines[i].CounterclockwisePolygonOwner);

                        Portal.Add(-1);

                        Collision.Add(level.Lines[i].CounterclockwisePolygonOwner);

                        Transparent.Add(-1);

                        Mesh mesh = new Mesh();

                        mesh.SetVertices(CCW);

                        if (level.Lines[i].CounterclockwisePolygonSideIndex == -1)
                        {
                            mesh.SetUVs(0, CCWUV);
                        }
                        else
                        {
                            if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.IsEmpty())
                            {
                                mesh.SetUVs(0, CCWUV);
                            }
                            else
                            {
                                mesh.SetUVs(0, CCWUVOffsetZ);
                            }
                        }

                        mesh.SetTriangles(walltri, 0);
                        mesh.RecalculateNormals();

                        meshes.Add(mesh);
                    }
                }

                if (level.Lines[i].LowestAdjacentCeiling != level.Lines[i].HighestAdjacentFloor)
                {
                    if (level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight > level.Lines[i].HighestAdjacentFloor &&
                        level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight < level.Lines[i].LowestAdjacentCeiling)
                    {
                        double YC = (float)level.Lines[i].LowestAdjacentCeiling / 1024 * Scale;
                        double YF = (float)level.Lines[i].HighestAdjacentFloor / 1024 * Scale;

                        CCW.Clear();
                        CCWUV.Clear();
                        CCWUVOffset.Clear();
                        CCWUVOffsetZ.Clear();

                        GetVertsCCW(X0, X1, YF, YC, Z0, Z1);

                        Plane.Add(level.Lines[i].CounterclockwisePolygonOwner);

                        if (level.Lines[i].ClockwisePolygonOwner != -1)
                        {
                            Portal.Add(level.Lines[i].ClockwisePolygonOwner);
                        }
                        else
                        {
                            Portal.Add(-1);
                        }

                        if (level.Lines[i].ClockwisePolygonOwner == -1)
                        {
                            if (level.Lines[i].CounterclockwisePolygonSideIndex != -1)
                            {
                                if (!level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.IsEmpty())
                                {
                                    if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 27 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 28 ||
                                        level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 29 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 30)
                                    {
                                        Render.Add(-1);

                                        Transparent.Add(-1);
                                    }
                                    else
                                    {
                                        Render.Add(level.Lines[i].CounterclockwisePolygonOwner);

                                        Transparent.Add(-1);
                                    }

                                    MakeSidesCCW(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary, level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].PrimaryLightsourceIndex);

                                    MeshTexture.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection);
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
                        }
                        else
                        {
                            if (level.Lines[i].CounterclockwisePolygonSideIndex != -1)
                            {
                                if (!level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Transparent.Texture.IsEmpty())
                                {
                                    if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Transparent.Texture.Collection == 27 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Transparent.Texture.Collection == 28 ||
                                        level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Transparent.Texture.Collection == 29 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Transparent.Texture.Collection == 30)
                                    {
                                        Render.Add(-1);

                                        Transparent.Add(-1);
                                    }
                                    else
                                    {
                                        Render.Add(-1);

                                        Transparent.Add(level.Lines[i].CounterclockwisePolygonOwner);
                                    }

                                    MakeSidesCCW(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Transparent, level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].TransparentLightsourceIndex);

                                    MeshTexture.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Transparent.Texture.Bitmap);

                                    MeshTextureCollection.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Transparent.Texture.Collection);
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
                        }

                        if (level.Lines[i].Solid == true)
                        {
                            Collision.Add(level.Lines[i].CounterclockwisePolygonOwner);
                        }
                        else
                        {
                            Collision.Add(-1);
                        }

                        Mesh mesh = new Mesh();

                        mesh.SetVertices(CCW);

                        if (level.Lines[i].ClockwisePolygonOwner == -1)
                        {
                            if (level.Lines[i].CounterclockwisePolygonSideIndex != -1)
                            {
                                if (!level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.IsEmpty())
                                {
                                    mesh.SetUVs(0, CCWUVOffsetZ);
                                }
                                else
                                {
                                    mesh.SetUVs(0, CCWUV);
                                }
                            }
                            else
                            {
                                mesh.SetUVs(0, CCWUV);
                            }
                        }
                        else
                        {
                            if (level.Lines[i].CounterclockwisePolygonSideIndex != -1)
                            {
                                if (!level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Transparent.Texture.IsEmpty())
                                {
                                    mesh.SetUVs(0, CCWUVOffsetZ);
                                }
                                else
                                {
                                    mesh.SetUVs(0, CCWUV);
                                }
                            }
                            else
                            {
                                mesh.SetUVs(0, CCWUV);
                            }
                        }

                        mesh.SetTriangles(walltri, 0);
                        mesh.RecalculateNormals();

                        meshes.Add(mesh);
                    }
                }

                if (level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight < level.Lines[i].HighestAdjacentFloor)
                {
                    if (level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight > level.Lines[i].HighestAdjacentFloor)
                    {
                        double YF0 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight / 1024 * Scale;
                        double YF1 = (float)level.Lines[i].HighestAdjacentFloor / 1024 * Scale;

                        CCW.Clear();
                        CCWUV.Clear();
                        CCWUVOffset.Clear();
                        CCWUVOffsetZ.Clear();

                        GetVertsCCW(X0, X1, YF0, YF1, Z0, Z1);

                        if (level.Lines[i].CounterclockwisePolygonSideIndex != -1)
                        {
                            if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Type == SideType.Low)
                            {
                                MakeSidesCCW(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary, level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].PrimaryLightsourceIndex);

                                if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 27 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 28 ||
                                    level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 29 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 30)
                                {
                                    Render.Add(-1);
                                }
                                else
                                {
                                    Render.Add(level.Lines[i].CounterclockwisePolygonOwner);
                                }

                                MeshTexture.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection);
                            }
                            else if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Type == SideType.Split)
                            {
                                MakeSidesCCW(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary, level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].SecondaryLightsourceIndex);

                                if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Collection == 27 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Collection == 28 ||
                                    level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Collection == 29 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Collection == 30)
                                {
                                    Render.Add(-1);
                                }
                                else
                                {
                                    Render.Add(level.Lines[i].CounterclockwisePolygonOwner);
                                }

                                MeshTexture.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Collection);
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

                        Plane.Add(level.Lines[i].CounterclockwisePolygonOwner);

                        Portal.Add(-1);

                        Collision.Add(level.Lines[i].CounterclockwisePolygonOwner);

                        Mesh mesh = new Mesh();

                        Transparent.Add(-1);

                        mesh.SetVertices(CCW);

                        if (level.Lines[i].CounterclockwisePolygonSideIndex != -1)
                        {
                            if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Type == SideType.Low)
                            {
                                if (level.Lines[i].CounterclockwisePolygonSideIndex == -1)
                                {
                                    mesh.SetUVs(0, CCWUV);
                                }
                                else
                                {
                                    if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.IsEmpty())
                                    {
                                        mesh.SetUVs(0, CCWUV);
                                    }
                                    else
                                    {
                                        mesh.SetUVs(0, CCWUVOffsetZ);
                                    }
                                }
                            }
                            else if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Type == SideType.Split)
                            {
                                if (level.Lines[i].CounterclockwisePolygonSideIndex == -1)
                                {
                                    mesh.SetUVs(0, CCWUV);
                                }
                                else
                                {
                                    if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.IsEmpty())
                                    {
                                        mesh.SetUVs(0, CCWUV);
                                    }
                                    else
                                    {
                                        mesh.SetUVs(0, CCWUVOffsetZ);
                                    }
                                }
                            }
                            else
                            {
                                mesh.SetUVs(0, CCWUV);
                            }
                        }
                        else
                        {
                            mesh.SetUVs(0, CCWUV);
                        }

                        mesh.SetTriangles(walltri, 0);
                        mesh.RecalculateNormals();

                        meshes.Add(mesh);
                    }
                    else
                    {
                        double YF0 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight / 1024 * Scale;
                        double YF1 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight / 1024 * Scale;

                        CCW.Clear();
                        CCWUV.Clear();
                        CCWUVOffset.Clear();
                        CCWUVOffsetZ.Clear();

                        GetVertsCCW(X0, X1, YF0, YF1, Z0, Z1);

                        if (level.Lines[i].CounterclockwisePolygonSideIndex != -1)
                        {
                            if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Type == SideType.Low)
                            {
                                MakeSidesCCW(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary, level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].PrimaryLightsourceIndex);

                                if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 27 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 28 ||
                                    level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 29 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection == 30)
                                {
                                    Render.Add(-1);
                                }
                                else
                                {
                                    Render.Add(level.Lines[i].CounterclockwisePolygonOwner);
                                }

                                MeshTexture.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.Collection);
                            }
                            else if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Type == SideType.Split)
                            {
                                MakeSidesCCW(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary, level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].SecondaryLightsourceIndex);

                                if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Collection == 27 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Collection == 28 ||
                                    level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Collection == 29 || level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Collection == 30)
                                {
                                    Render.Add(-1);
                                }
                                else
                                {
                                    Render.Add(level.Lines[i].CounterclockwisePolygonOwner);
                                }

                                MeshTexture.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Bitmap);

                                MeshTextureCollection.Add(level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.Collection);
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

                        Plane.Add(level.Lines[i].CounterclockwisePolygonOwner);

                        Portal.Add(-1);

                        Collision.Add(level.Lines[i].CounterclockwisePolygonOwner);

                        Transparent.Add(-1);

                        Mesh mesh = new Mesh();

                        mesh.SetVertices(CCW);

                        if (level.Lines[i].CounterclockwisePolygonSideIndex != -1)
                        {
                            if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Type == SideType.Low)
                            {
                                if (level.Lines[i].CounterclockwisePolygonSideIndex == -1)
                                {
                                    mesh.SetUVs(0, CCWUV);
                                }
                                else
                                {
                                    if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Primary.Texture.IsEmpty())
                                    {
                                        mesh.SetUVs(0, CCWUV);
                                    }
                                    else
                                    {
                                        mesh.SetUVs(0, CCWUVOffsetZ);
                                    }
                                }
                            }
                            else if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Type == SideType.Split)
                            {
                                if (level.Lines[i].CounterclockwisePolygonSideIndex == -1)
                                {
                                    mesh.SetUVs(0, CCWUV);
                                }
                                else
                                {
                                    if (level.Sides[level.Lines[i].CounterclockwisePolygonSideIndex].Secondary.Texture.IsEmpty())
                                    {
                                        mesh.SetUVs(0, CCWUV);
                                    }
                                    else
                                    {
                                        mesh.SetUVs(0, CCWUVOffsetZ);
                                    }
                                }
                            }
                            else
                            {
                                mesh.SetUVs(0, CCWUV);
                            }
                        }
                        else
                        {
                            mesh.SetUVs(0, CCWUV);
                        }

                        mesh.SetTriangles(walltri, 0);
                        mesh.RecalculateNormals();

                        meshes.Add(mesh);
                    }
                }
            }
        }
    }

    public void BuildPolygons()
    {
        for (int i = 0; i < level.Polygons.Count; i++)
        {
            if (level.Polygons[i].FloorHeight != level.Polygons[i].CeilingHeight)
            {
                floorverts.Clear();
                flooruvs.Clear();
                flooruvsz.Clear();
                ceilingverts.Clear();
                ceilinguvs.Clear();
                ceilinguvsz.Clear();

                for (int e = 0; e < level.Polygons[i].VertexCount; ++e)
                {
                    float YF = (float)level.Polygons[i].FloorHeight / 1024 * Scale;
                    float YC = (float)level.Polygons[i].CeilingHeight / 1024 * Scale;
                    float X = (float)level.Endpoints[level.Polygons[i].EndpointIndexes[e]].X / 1024 * Scale;
                    float Z = (float)level.Endpoints[level.Polygons[i].EndpointIndexes[e]].Y / 1024 * Scale * -1;

                    float YFOX = (float)(level.Endpoints[level.Polygons[i].EndpointIndexes[e]].X + level.Polygons[i].FloorOrigin.X) / 1024 * -1;
                    float YFOY = (float)(level.Endpoints[level.Polygons[i].EndpointIndexes[e]].Y + level.Polygons[i].FloorOrigin.Y) / 1024;
                    float YCOX = (float)(level.Endpoints[level.Polygons[i].EndpointIndexes[e]].X + level.Polygons[i].CeilingOrigin.X) / 1024 * -1;
                    float YCOY = (float)(level.Endpoints[level.Polygons[i].EndpointIndexes[e]].Y + level.Polygons[i].CeilingOrigin.Y) / 1024;

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
                        floortri.Add(0);
                        floortri.Add(e + 1);
                        floortri.Add(e + 2);
                    }

                    Plane.Add(i);

                    Portal.Add(-1);

                    if (level.Polygons[i].FloorTexture.Collection == 27 || level.Polygons[i].FloorTexture.Collection == 28 ||
                        level.Polygons[i].FloorTexture.Collection == 29 || level.Polygons[i].FloorTexture.Collection == 30)
                    {
                        Render.Add(-1);
                    }
                    else
                    {
                        Render.Add(i);
                    }

                    if (level.Polygons[i].FloorTexture.Collection == 17)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, level.Polygons[i].FloorTexture.Bitmap, level.Polygons[i].FloorLight));
                        }
                    }
                    if (level.Polygons[i].FloorTexture.Collection == 18)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, level.Polygons[i].FloorTexture.Bitmap + 30, level.Polygons[i].FloorLight));
                        }
                    }
                    if (level.Polygons[i].FloorTexture.Collection == 19)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, level.Polygons[i].FloorTexture.Bitmap + 60, level.Polygons[i].FloorLight));
                        }
                    }
                    if (level.Polygons[i].FloorTexture.Collection == 20)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, level.Polygons[i].FloorTexture.Bitmap + 90, level.Polygons[i].FloorLight));
                        }
                    }
                    if (level.Polygons[i].FloorTexture.Collection == 21)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, level.Polygons[i].FloorTexture.Bitmap + 125, level.Polygons[i].FloorLight));
                        }
                    }
                    if (level.Polygons[i].FloorTexture.Collection == 27 || level.Polygons[i].FloorTexture.Collection == 28 ||
                        level.Polygons[i].FloorTexture.Collection == 29 || level.Polygons[i].FloorTexture.Collection == 30)
                    {
                        for (int e = 0; e < flooruvs.Count; e++)
                        {
                            flooruvsz.Add(new Vector4(flooruvs[e].x, flooruvs[e].y, level.Polygons[i].FloorTexture.Bitmap, level.Polygons[i].FloorLight));
                        }
                    }

                    MeshTexture.Add(level.Polygons[i].FloorTexture.Bitmap);

                    MeshTextureCollection.Add(level.Polygons[i].FloorTexture.Collection);

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
                        ceilingtri.Add(0);
                        ceilingtri.Add(e + 1);
                        ceilingtri.Add(e + 2);
                    }

                    Plane.Add(i);

                    Portal.Add(-1);

                    if (level.Polygons[i].CeilingTexture.Collection == 27 || level.Polygons[i].CeilingTexture.Collection == 28 ||
                        level.Polygons[i].CeilingTexture.Collection == 29 || level.Polygons[i].CeilingTexture.Collection == 30)
                    {
                        Render.Add(-1);
                    }
                    else
                    {
                        Render.Add(i);
                    }

                    if (level.Polygons[i].CeilingTexture.Collection == 17)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, level.Polygons[i].CeilingTexture.Bitmap, level.Polygons[i].CeilingLight));
                        }
                    }
                    if (level.Polygons[i].CeilingTexture.Collection == 18)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, level.Polygons[i].CeilingTexture.Bitmap + 30, level.Polygons[i].CeilingLight));
                        }
                    }
                    if (level.Polygons[i].CeilingTexture.Collection == 19)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, level.Polygons[i].CeilingTexture.Bitmap + 60, level.Polygons[i].CeilingLight));
                        }
                    }
                    if (level.Polygons[i].CeilingTexture.Collection == 20)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, level.Polygons[i].CeilingTexture.Bitmap + 90, level.Polygons[i].CeilingLight));
                        }
                    }
                    if (level.Polygons[i].CeilingTexture.Collection == 21)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, level.Polygons[i].CeilingTexture.Bitmap + 125, level.Polygons[i].CeilingLight));
                        }
                    }
                    if (level.Polygons[i].CeilingTexture.Collection == 27 || level.Polygons[i].CeilingTexture.Collection == 28 ||
                        level.Polygons[i].CeilingTexture.Collection == 29 || level.Polygons[i].CeilingTexture.Collection == 30)
                    {
                        for (int e = 0; e < ceilinguvs.Count; e++)
                        {
                            ceilinguvsz.Add(new Vector4(ceilinguvs[e].x, ceilinguvs[e].y, level.Polygons[i].CeilingTexture.Bitmap, level.Polygons[i].CeilingLight));
                        }
                    }

                    MeshTexture.Add(level.Polygons[i].CeilingTexture.Bitmap);

                    MeshTextureCollection.Add(level.Polygons[i].CeilingTexture.Collection);

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

    public void GetVertsCW(double X0, double X1, double V0, double V1, double Z0, double Z1)
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

    public void MakeSidesCW(Side.TextureDefinition sideDef, int Light)
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

    public void GetVertsCCW(double X0, double X1, double V0, double V1, double Z0, double Z1)
    {
        CCW.Add(new Vector3((float)X0, (float)V0, (float)Z0));
        CCW.Add(new Vector3((float)X0, (float)V1, (float)Z0));
        CCW.Add(new Vector3((float)X1, (float)V1, (float)Z1));
        CCW.Add(new Vector3((float)X1, (float)V0, (float)Z1));

        LeftPlane = new Plane((CCW[2] - CCW[1]).normalized, CCW[1]);
        TopPlane = new Plane((CCW[1] - CCW[0]).normalized, CCW[1]);

        CCWUV.Add(new Vector2(LeftPlane.GetDistanceToPoint(CCW[0]) / Scale, TopPlane.GetDistanceToPoint(CCW[0]) / Scale));
        CCWUV.Add(new Vector2(LeftPlane.GetDistanceToPoint(CCW[1]) / Scale, TopPlane.GetDistanceToPoint(CCW[1]) / Scale));
        CCWUV.Add(new Vector2(LeftPlane.GetDistanceToPoint(CCW[2]) / Scale, TopPlane.GetDistanceToPoint(CCW[2]) / Scale));
        CCWUV.Add(new Vector2(LeftPlane.GetDistanceToPoint(CCW[3]) / Scale, TopPlane.GetDistanceToPoint(CCW[3]) / Scale));
    }

    public void MakeSidesCCW(Side.TextureDefinition sideDef, int Light)
    {
        for (int e = 0; e < CCWUV.Count; e++)
        {
            CCWUVOffset.Add(new Vector2(CCWUV[e].x + (float)sideDef.X / 1024,
            CCWUV[e].y + (float)sideDef.Y / 1024 * -1));
        }

        if (sideDef.Texture.Collection == 17)
        {
            for (int e = 0; e < CCWUVOffset.Count; e++)
            {
                CCWUVOffsetZ.Add(new Vector4(CCWUVOffset[e].x, CCWUVOffset[e].y, sideDef.Texture.Bitmap, Light));
            }
        }
        if (sideDef.Texture.Collection == 18)
        {
            for (int e = 0; e < CCWUVOffset.Count; e++)
            {
                CCWUVOffsetZ.Add(new Vector4(CCWUVOffset[e].x, CCWUVOffset[e].y, sideDef.Texture.Bitmap + 30, Light));
            }
        }
        if (sideDef.Texture.Collection == 19)
        {
            for (int e = 0; e < CCWUVOffset.Count; e++)
            {
                CCWUVOffsetZ.Add(new Vector4(CCWUVOffset[e].x, CCWUVOffset[e].y, sideDef.Texture.Bitmap + 60, Light));
            }
        }
        if (sideDef.Texture.Collection == 20)
        {
            for (int e = 0; e < CCWUVOffset.Count; e++)
            {
                CCWUVOffsetZ.Add(new Vector4(CCWUVOffset[e].x, CCWUVOffset[e].y, sideDef.Texture.Bitmap + 90, Light));
            }
        }
        if (sideDef.Texture.Collection == 21)
        {
            for (int e = 0; e < CCWUVOffset.Count; e++)
            {
                CCWUVOffsetZ.Add(new Vector4(CCWUVOffset[e].x, CCWUVOffset[e].y, sideDef.Texture.Bitmap + 125, Light));
            }
        }

        if (sideDef.Texture.Collection == 27 || sideDef.Texture.Collection == 28 ||
            sideDef.Texture.Collection == 29 || sideDef.Texture.Collection == 30)
        {
            for (int e = 0; e < CCWUVOffset.Count; e++)
            {
                CCWUVOffsetZ.Add(new Vector4(CCWUVOffset[e].x, CCWUVOffset[e].y, sideDef.Texture.Bitmap, Light));
            }
        }
    }
}
