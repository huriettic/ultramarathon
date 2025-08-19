using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weland;

[Serializable]
public struct Edge
{
    public Vector3 start;
    public Vector3 end;

    public int portalID;
    public int sectorID;
};

[Serializable]
public struct Triangle
{
    public Vector3 v1, v2, v3;
    public Vector4 uv1, uv2, uv3;

    public int sectorID;
};

[Serializable]
public struct Collisions
{
    public Vector3 v1, v2, v3;
    public int sectorID;
};

[Serializable]
public struct SectorPlane
{
    public Vector3 normal;
    public Vector3 point;

    public int sectorID;
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
public struct FrustumMeta
{
    public int planeStartIndex;
    public int planeCount;

    public int frustumID;
};

[Serializable]
public struct PortalMeta
{
    public int lineStartIndex;
    public int lineCount;

    public int portalPlane;
    public int portalID;

    public int sectorID;
    public int connectedSectorID;
};

[Serializable]
public struct SectorMeta
{
    public int planeStartIndex;
    public int planeCount;

    public int opaqueStartIndex;
    public int opaqueCount;

    public int transparentStartIndex;
    public int transparentCount;

    public int collisionStartIndex;
    public int collisionCount;

    public int portalStartIndex;
    public int portalCount;

    public int sectorID;
};

public class BuildAndRunLevel : MonoBehaviour
{
    public string Name = "Tutorial";

    public string Textures = "Textures";

    // Default scale is 2.5, but Unreal tournament is 128
    private float Scale = 2.5f;

    public Level level;

    public int LevelNumber;

    private float d;

    private int h;

    private int y;

    private bool t;

    private int MaxDepth;

    private float[] planeDist;

    private bool[] InSide;

    private float[] lineDist;

    private bool[] lineInSide;

    private Vector3[] intersectionPoints;

    private Vector3[] lineSegment;

    private Mesh opaquemesh;

    private Mesh transparentmesh;

    private Vector3 p1;

    private Vector3 p2;

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

    private CharacterController Player;

    private Color[] LightColor;

    private int[] OneTriangle;

    private Camera Cam;

    private Vector3 CamPoint;

    private RenderParams rp;

    private List<Plane> CamPlanes = new List<Plane>();

    private List<Plane> Planes = new List<Plane>();

    private SectorMeta CurrentSector;

    private GameObject CollisionObjects;

    private List<Vector3> CombinedVertices = new List<Vector3>();

    private List<Vector4> CombinedTextures = new List<Vector4>();

    private List<int> CombinedTriangles = new List<int>();

    private List<Vector3> ClippedVertices = new List<Vector3>();

    private List<Vector4> ClippedTextures = new List<Vector4>();

    private List<Vector3> OpaqueVertices = new List<Vector3>();

    private List<int> OpaqueTriangles = new List<int>();

    private List<Vector4> OpaqueTextures = new List<Vector4>();

    private List<Vector3> TransparentVertices = new List<Vector3>();

    private List<Vector4> TransparentTextures = new List<Vector4>();

    private List<int> TransparentTriangles = new List<int>();

    private List<SectorMeta> Sectors = new List<SectorMeta>();

    private List<SectorMeta> OldSectors = new List<SectorMeta>();

    private List<GameObject> CollisionSectors = new List<GameObject>();

    private List<Vector3> OutVertices = new List<Vector3>();

    private List<Vector4> OutTextures = new List<Vector4>();

    private Matrix4x4 matrix;

    private Material opaquematerial;

    private Material transparentmaterial;

    private List<Mesh> CollisionMesh = new List<Mesh>();

    private TopLevelLists LevelLists;

    [Serializable]
    public class TopLevelLists
    {
        public List<SectorMeta> sectors = new List<SectorMeta>();
        public List<PortalMeta> portals = new List<PortalMeta>();
        public List<StartPos> positions = new List<StartPos>();
        public List<LevelLight> colors = new List<LevelLight>();
        public List<Triangle> opaques = new List<Triangle>();
        public List<Triangle> transparents = new List<Triangle>();
        public List<Collisions> collisions = new List<Collisions>();
        public List<FrustumMeta> frustums = new List<FrustumMeta>();
        public List<SectorPlane> planes = new List<SectorPlane>();
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

        BuildLines();

        BuildPolygons();

        BuildObjects();

        BuildLights();

        BuildTheLists();

        LightColor = new Color[LevelLists.colors.Count];

        OneTriangle = new int[3];

        planeDist = new float[3];

        InSide = new bool[3];

        lineDist = new float[2];

        lineInSide = new bool[2];

        lineSegment = new Vector3[2];

        intersectionPoints = new Vector3[2];

        CreateMaterial();

        opaquemesh = new Mesh();

        opaquemesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        transparentmesh = new Mesh();

        transparentmesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        rp = new RenderParams();

        matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

        CollisionObjects = new GameObject("Collision Meshes");

        BuildCollsionSectors();

        CreatePolygonPlane();

        Cursor.lockState = CursorLockMode.Locked;

        Playerstart();

        FrustumMeta temp = LevelLists.frustums[LevelLists.frustums.Count - 1];

        temp.planeStartIndex = 0;

        temp.planeCount = 4;

        LevelLists.frustums[LevelLists.frustums.Count - 1] = temp;

        Player.GetComponent<CharacterController>().enabled = true;

        foreach (SectorMeta sector in LevelLists.sectors)
        {
            Physics.IgnoreCollision(Player, CollisionSectors[sector.sectorID].GetComponent<MeshCollider>(), true);
        }
    }

    void Update()
    {
        PlayerInput();

        if (Cam.transform.hasChanged)
        {
            CamPoint = Cam.transform.position;

            Sectors.Clear();

            GetPolyhedrons(CurrentSector);

            CamPlanes.Clear();

            ReadFrustumPlanes(Cam, CamPlanes);

            CamPlanes.RemoveAt(5);

            CamPlanes.RemoveAt(4);

            OpaqueVertices.Clear();

            OpaqueTextures.Clear();

            OpaqueTriangles.Clear();

            TransparentVertices.Clear();

            TransparentTextures.Clear();

            TransparentTriangles.Clear();

            h = 0;

            y = 0;

            MaxDepth = 0;

            GetPolygons(LevelLists.frustums[LevelLists.frustums.Count - 1], CurrentSector);

            SetRenderMeshes();

            Cam.transform.hasChanged = false;
        }

        Renderit();
    }

    void Awake()
    {
        Player = GameObject.Find("Player").GetComponent<CharacterController>();

        Cam = Camera.main;
    }

    void FixedUpdate()
    {
        if (!Player.isGrounded)
        {
            currentForce.y -= gravity * Time.deltaTime;
        }
    }

    public void LoadLevel()
    {
        MapFile map = new MapFile();

        level = new Level();

        try
        {
            // Change name to load a different map
            map.Load(Application.streamingAssetsPath + "/" + Name + ".sceA");
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
    }

    public void CreateMaterial()
    {
        Shader shader = Resources.Load<Shader>("TexArray");

        Shader shaderT = Resources.Load<Shader>("TexArrayT");

        for (int i = 0; i < LevelLists.colors.Count; i++)
        {
            LightColor[i] = new Color(LevelLists.colors[i].TriangleLight.r, LevelLists.colors[i].TriangleLight.g, LevelLists.colors[i].TriangleLight.b, 1.0f);
        }

        opaquematerial = new Material(shader);

        opaquematerial.mainTexture = Resources.Load<Texture2DArray>(Textures);

        opaquematerial.SetColorArray("_ColorArray", LightColor);

        transparentmaterial = new Material(shaderT);

        transparentmaterial.mainTexture = Resources.Load<Texture2DArray>(Textures);

        transparentmaterial.SetColorArray("_ColorArray", LightColor);
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

    private Plane FromVec4(Vector4 aVec)
    {
        Vector3 n = aVec;
        float l = n.magnitude;
        return new Plane(n / l, aVec.w / l);
    }

    public void SetFrustumPlanes(List<Plane> planes, Matrix4x4 m)
    {
        if (planes == null)
            return;
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

    public void ReadFrustumPlanes(Camera cam, List<Plane> planes)
    {
        SetFrustumPlanes(planes, cam.projectionMatrix * cam.worldToCameraMatrix);
    }

    public void CreatePolygonPlane()
    {
        for (int i = 0; i < LevelLists.planes.Count; i++)
        {
            p1 = LevelLists.planes[i].normal;
            p2 = LevelLists.planes[i].point;

            Planes.Add(new Plane(p1, p2));
        }
    }

    public void SetClippingPlanes(List<Vector3> vertices, int portalnumber, Vector3 viewPos)
    {
        int StartIndex = CamPlanes.Count;

        int IndexCount = 0;

        int count = vertices.Count;
        for (int i = 0; i < count; i += 2)
        {
            Vector3 p1 = vertices[i];
            Vector3 p2 = vertices[i + 1];
            Vector3 normal = Vector3.Cross(p1 - p2, viewPos - p2);
            float magnitude = normal.magnitude;

            if (magnitude > 0.01f)
            {
                CamPlanes.Add(new Plane(normal / magnitude, p1));
                IndexCount += 1;
            }
        }

        FrustumMeta temp = LevelLists.frustums[portalnumber];

        temp.planeStartIndex = StartIndex;
        temp.planeCount = IndexCount;

        LevelLists.frustums[portalnumber] = temp;
    }

    public void BuildCollsionSectors()
    {
        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            CombinedVertices.Clear();

            CombinedTriangles.Clear();

            List<Collisions> colliders = LevelLists.collisions.GetRange(LevelLists.sectors[i].collisionStartIndex, LevelLists.sectors[i].collisionCount);

            for (int e = 0; e < colliders.Count; e++)
            {
                CombinedVertices.Add(colliders[e].v1);
                CombinedVertices.Add(colliders[e].v2);
                CombinedVertices.Add(colliders[e].v3);
            }

            for (int e = 0; e < CombinedVertices.Count; e++)
            {
                CombinedTriangles.Add(e);
            }

            Mesh combinedmesh = new Mesh();

            CollisionMesh.Add(combinedmesh);

            combinedmesh.SetVertices(CombinedVertices);

            combinedmesh.SetTriangles(CombinedTriangles, 0);

            GameObject meshObject = new GameObject("Collision " + i);

            CollisionSectors.Add(meshObject);

            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();

            meshCollider.sharedMesh = combinedmesh;

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

    public (List<Vector3>, List<Vector4>) ClipTriangles((List<Vector3>, List<Vector4>) verttex, Plane plane)
    {
        OutVertices.Clear();
        OutTextures.Clear();

        int inIndex = 0;
        int outIndex1 = 0;
        int outIndex2 = 0;
        int outIndex = 0;
        int inIndex1 = 0;
        int inIndex2 = 0;

        int count = verttex.Item1.Count;

        if (count < 3)
        {
            return (OutVertices, OutTextures);
        }

        List<Vector3> vertices = verttex.Item1;

        List<Vector4> textures = verttex.Item2;

        for (int i = 0; i < count; i += 3)
        {
            planeDist[0] = plane.GetDistanceToPoint(vertices[i]);
            planeDist[1] = plane.GetDistanceToPoint(vertices[i + 1]);
            planeDist[2] = plane.GetDistanceToPoint(vertices[i + 2]);
            InSide[0] = planeDist[0] >= 0;
            InSide[1] = planeDist[1] >= 0;
            InSide[2] = planeDist[2] >= 0;

            int inCount = 0;

            if (InSide[0])
            {
                inCount++;
            }

            if (InSide[1])
            {
                inCount++;
            }

            if (InSide[2])
            {
                inCount++;
            }

            if (inCount == 3)
            {
                OutVertices.Add(vertices[i]);
                OutVertices.Add(vertices[i + 1]);
                OutVertices.Add(vertices[i + 2]);
                OutTextures.Add(textures[i]);
                OutTextures.Add(textures[i + 1]);
                OutTextures.Add(textures[i + 2]);
            }
            else if (inCount == 1)
            {
                if (InSide[0] && !InSide[1] && !InSide[2])
                {
                    inIndex = 0;
                    outIndex1 = 1;
                    outIndex2 = 2;
                }
                else if (!InSide[0] && InSide[1] && !InSide[2])
                {
                    outIndex1 = 2;
                    inIndex = 1;
                    outIndex2 = 0;
                }
                else if (!InSide[0] && !InSide[1] && InSide[2])
                {
                    outIndex1 = 0;
                    outIndex2 = 1;
                    inIndex = 2;
                }

                float t1 = planeDist[inIndex] / (planeDist[inIndex] - planeDist[outIndex1]);
                float t2 = planeDist[inIndex] / (planeDist[inIndex] - planeDist[outIndex2]);

                OutVertices.Add(vertices[i + inIndex]);
                OutTextures.Add(textures[i + inIndex]);
                OutVertices.Add(Vector3.Lerp(vertices[i + inIndex], vertices[i + outIndex1], t1));
                OutTextures.Add(Vector4.Lerp(textures[i + inIndex], textures[i + outIndex1], t1));
                OutVertices.Add(Vector3.Lerp(vertices[i + inIndex], vertices[i + outIndex2], t2));
                OutTextures.Add(Vector4.Lerp(textures[i + inIndex], textures[i + outIndex2], t2));
            }
            else if (inCount == 2)
            {
                if (!InSide[0] && InSide[1] && InSide[2])
                {
                    outIndex = 0;
                    inIndex1 = 1;
                    inIndex2 = 2;
                }
                else if (InSide[0] && !InSide[1] && InSide[2])
                {
                    inIndex1 = 2;
                    outIndex = 1;
                    inIndex2 = 0;
                }
                else if (InSide[0] && InSide[1] && !InSide[2])
                {
                    inIndex1 = 0;
                    inIndex2 = 1;
                    outIndex = 2;
                }

                float t1 = planeDist[inIndex1] / (planeDist[inIndex1] - planeDist[outIndex]);
                float t2 = planeDist[inIndex2] / (planeDist[inIndex2] - planeDist[outIndex]);

                OutVertices.Add(vertices[i + inIndex1]);
                OutTextures.Add(textures[i + inIndex1]);
                OutVertices.Add(vertices[i + inIndex2]);
                OutTextures.Add(textures[i + inIndex2]);
                OutVertices.Add(Vector3.Lerp(vertices[i + inIndex1], vertices[i + outIndex], t1));
                OutTextures.Add(Vector4.Lerp(textures[i + inIndex1], textures[i + outIndex], t1));
                OutVertices.Add(Vector3.Lerp(vertices[i + inIndex1], vertices[i + outIndex], t1));
                OutTextures.Add(Vector4.Lerp(textures[i + inIndex1], textures[i + outIndex], t1));
                OutVertices.Add(vertices[i + inIndex2]);
                OutTextures.Add(textures[i + inIndex2]);
                OutVertices.Add(Vector3.Lerp(vertices[i + inIndex2], vertices[i + outIndex], t2));
                OutTextures.Add(Vector4.Lerp(textures[i + inIndex2], textures[i + outIndex], t2));
            }
        }

        return (OutVertices, OutTextures);
    }

    public (List<Vector3>, List<Vector4>) ClippingPlanesForTriangles((List<Vector3>, List<Vector4>) verttex, FrustumMeta planes)
    {
        for (int i = planes.planeStartIndex; i < planes.planeStartIndex + planes.planeCount; i++)
        {
            if (verttex.Item1.Count < 3)
            {
                return verttex;
            }

            ClippedVertices.Clear();

            ClippedVertices.AddRange(verttex.Item1);

            ClippedTextures.Clear();

            ClippedTextures.AddRange(verttex.Item2);

            verttex = ClipTriangles((ClippedVertices, ClippedTextures), CamPlanes[i]);
        }

        return verttex;
    }

    public List<Vector3> ClippingPlanesLines(List<Vector3> lines, FrustumMeta planes)
    {
        for (int i = planes.planeStartIndex; i < planes.planeStartIndex + planes.planeCount; i++)
        {
            if (lines.Count < 6 || lines.Count % 2 == 1)
            {
                return lines;
            }

            ClippedVertices.Clear();

            ClippedVertices.AddRange(lines);

            lines = ClipEdges(ClippedVertices, CamPlanes[i]);
        }

        return lines;
    }

    public List<Vector3> ClipEdges(List<Vector3> lines, Plane plane)
    {
        OutVertices.Clear();

        int count = lines.Count;

        if (count < 6 || count % 2 == 1)
        {
            return OutVertices;
        }

        int intersection = 0;
        int inIndex = 0;
        int outIndex = 0;

        for (int i = 0; i < count; i += 2)
        {
            lineDist[0] = plane.GetDistanceToPoint(lines[i]);
            lineDist[1] = plane.GetDistanceToPoint(lines[i + 1]);
            lineInSide[0] = lineDist[0] >= 0;
            lineInSide[1] = lineDist[1] >= 0;

            int inCount = 0;

            if (lineInSide[0])
            {
                inCount++;
            }

            if (lineInSide[1])
            {
                inCount++;
            }

            if (inCount == 2)
            {
                OutVertices.Add(lines[i]);
                OutVertices.Add(lines[i + 1]);
            }
            else if (inCount == 1)
            {
                if (lineInSide[0] && !lineInSide[1])
                {
                    inIndex = 0;
                    outIndex = 1;
                }
                else if (!lineInSide[0] && lineInSide[1])
                {
                   inIndex = 1; 
                   outIndex = 0;
                }

                float t = lineDist[0] / (lineDist[0] - lineDist[1]);

                intersectionPoints[outIndex] = Vector3.Lerp(lines[i], lines[i + 1], t);

                lineSegment[inIndex] = lines[i + inIndex];
                lineSegment[outIndex] = intersectionPoints[outIndex];

                OutVertices.Add(lineSegment[0]);
                OutVertices.Add(lineSegment[1]);

                intersection++;
            }
        }
        if (intersection == 2)
        {
            OutVertices.Add(intersectionPoints[1]);
            OutVertices.Add(intersectionPoints[0]);
        }

        return OutVertices;
    }

    public bool CheckRadius(SectorMeta asector, Vector3 campoint)
    {
        for (int i = asector.planeStartIndex; i < asector.planeStartIndex + asector.planeCount; i++)
        {
            if (Planes[i].GetDistanceToPoint(campoint) < -0.6f)
            {
                return false;
            }
        }
        return true;
    }

    public bool CheckPolyhedron(SectorMeta asector, Vector3 campoint)
    {
        for (int i = asector.planeStartIndex; i < asector.planeStartIndex + asector.planeCount; i++)
        {
            if (Planes[i].GetDistanceToPoint(campoint) < 0)
            {
                return false;
            }
        }
        return true;
    }

    public void GetPolyhedrons(SectorMeta ASector)
    {
        Sectors.Add(ASector);

        for (int i = ASector.portalStartIndex; i < ASector.portalStartIndex + ASector.portalCount; i++)
        {
            int portalnumber = LevelLists.portals[i].connectedSectorID;

            if (Sectors.Contains(LevelLists.sectors[portalnumber]))
            {
                continue;
            }

            t = CheckRadius(LevelLists.sectors[portalnumber], CamPoint);

            if (t == true)
            {
                GetPolyhedrons(LevelLists.sectors[portalnumber]);

                continue;
            }
        }

        t = CheckPolyhedron(ASector, CamPoint);

        if (t == true)
        {
            CurrentSector = ASector;

            if (!OldSectors.SequenceEqual(Sectors))
            {
                foreach (SectorMeta sector in OldSectors)
                {
                    Physics.IgnoreCollision(Player, CollisionSectors[sector.sectorID].GetComponent<MeshCollider>(), true);
                }

                foreach (SectorMeta sector in Sectors)
                {
                    Physics.IgnoreCollision(Player, CollisionSectors[sector.sectorID].GetComponent<MeshCollider>(), false);
                }

                OldSectors.Clear();

                OldSectors.AddRange(Sectors);
            }
        }
    }

    public void SetRenderMeshes()
    {
        opaquemesh.Clear();

        opaquemesh.SetVertices(OpaqueVertices);

        opaquemesh.SetUVs(0, OpaqueTextures);

        opaquemesh.SetTriangles(OpaqueTriangles, 0);

        transparentmesh.Clear();

        transparentmesh.subMeshCount = TransparentTriangles.Count / 3;

        transparentmesh.SetVertices(TransparentVertices);

        transparentmesh.SetUVs(0, TransparentTextures);

        for (int i = 0; i < TransparentTriangles.Count; i += 3)
        {
            OneTriangle[0] = TransparentTriangles[i];
            OneTriangle[1] = TransparentTriangles[i + 1];
            OneTriangle[2] = TransparentTriangles[i + 2];

            transparentmesh.SetTriangles(OneTriangle, i / 3);
        }
    }

    public void Renderit()
    {
        rp.material = opaquematerial;

        Graphics.RenderMesh(rp, opaquemesh, 0, matrix);

        rp.material = transparentmaterial;

        for (int i = TransparentTriangles.Count - 1; i >= 0; i -= 3)
        {
            Graphics.RenderMesh(rp, transparentmesh, i / 3, matrix);
        }
    }

    public void GetPolygons(FrustumMeta APlanes, SectorMeta BSector)
    {
        CombinedVertices.Clear();

        CombinedTextures.Clear();

        for (int e = BSector.opaqueStartIndex; e < BSector.opaqueStartIndex + BSector.opaqueCount; e++)
        {
            CombinedVertices.Add(LevelLists.opaques[e].v1);
            CombinedVertices.Add(LevelLists.opaques[e].v2);
            CombinedVertices.Add(LevelLists.opaques[e].v3);
            CombinedTextures.Add(LevelLists.opaques[e].uv1);
            CombinedTextures.Add(LevelLists.opaques[e].uv2);
            CombinedTextures.Add(LevelLists.opaques[e].uv3);
        }

        (List<Vector3>, List<Vector4>) oclippedData = ClippingPlanesForTriangles((CombinedVertices, CombinedTextures), APlanes);

        List<Vector3> overtices = oclippedData.Item1;

        List<Vector4> otextures = oclippedData.Item2;

        for (int e = 0; e < oclippedData.Item1.Count; e++)
        {
            OpaqueVertices.Add(overtices[e]);
            OpaqueTextures.Add(otextures[e]);
            OpaqueTriangles.Add(e + h);
        }

        h += oclippedData.Item1.Count;

        CombinedVertices.Clear();

        CombinedTextures.Clear();

        for (int e = BSector.transparentStartIndex; e < BSector.transparentStartIndex + BSector.transparentCount; e++)
        {
            CombinedVertices.Add(LevelLists.transparents[e].v1);
            CombinedVertices.Add(LevelLists.transparents[e].v2);
            CombinedVertices.Add(LevelLists.transparents[e].v3);
            CombinedTextures.Add(LevelLists.transparents[e].uv1);
            CombinedTextures.Add(LevelLists.transparents[e].uv2);
            CombinedTextures.Add(LevelLists.transparents[e].uv3);
        }

        (List<Vector3>, List<Vector4>) tclippedData = ClippingPlanesForTriangles((CombinedVertices, CombinedTextures), APlanes);

        List<Vector3> tvertices = tclippedData.Item1;

        List<Vector4> ttextures = tclippedData.Item2;

        for (int e = 0; e < tclippedData.Item1.Count; e++)
        {
            TransparentVertices.Add(tvertices[e]);
            TransparentTextures.Add(ttextures[e]);
            TransparentTriangles.Add(e + y);
        }

        y += tclippedData.Item1.Count;

        for (int i = BSector.portalStartIndex; i < BSector.portalStartIndex + BSector.portalCount; i++)
        {
            if (MaxDepth > 4096)
            {
                continue;
            }

            d = Planes[LevelLists.portals[i].portalPlane].GetDistanceToPoint(CamPoint);

            if (d < -0.1f || d <= 0 || d == 0)
            {
                continue;
            }

            int sectornumber = LevelLists.portals[i].connectedSectorID;

            int portalnumber = LevelLists.portals[i].portalID;

            if (Sectors.Contains(LevelLists.sectors[sectornumber]))
            {
                MaxDepth += 1;

                GetPolygons(APlanes, LevelLists.sectors[sectornumber]);

                continue;
            }

            CombinedVertices.Clear();

            for (int f = LevelLists.portals[i].lineStartIndex; f < LevelLists.portals[i].lineStartIndex + LevelLists.portals[i].lineCount; f++)
            {
                CombinedVertices.Add(LevelLists.edges[f].start);
                CombinedVertices.Add(LevelLists.edges[f].end);
            }

            List<Vector3> clippedLines = ClippingPlanesLines(CombinedVertices, APlanes);

            if (clippedLines.Count < 6 || clippedLines.Count % 2 == 1)
            {
                continue;
            }

            SetClippingPlanes(clippedLines, portalnumber, CamPoint);

            MaxDepth += 1;

            GetPolygons(LevelLists.frustums[portalnumber], LevelLists.sectors[sectornumber]);
        }
    }

    public void BuildTheLists()
    {
        int planeStart = 0;

        int opaqueStart = 0;

        int collisionStart = 0;

        int portalStart = 0;

        int transparentStart = 0;

        int edgeStart = 0;

        int portalnumber = 0;

        int portalPlaneCount = 0;

        for (int h = 0; h < level.Polygons.Count; h++)
        {
            int planeCount = 0;

            int portalCount = 0;

            int rendersCount = 0;

            int collideCount = 0;

            int transparentCount = 0;

            for (int e = 0; e < Plane.Count; e++)
            {
                if (Plane[e] == h)
                {
                    Mesh mesh = meshes[e];

                    SectorPlane sectorplane = new SectorPlane();

                    sectorplane.normal = mesh.normals[0];
                    sectorplane.point = mesh.vertices[0];

                    sectorplane.sectorID = h;

                    LevelLists.planes.Add(sectorplane);

                    planeCount += 1;
                }

                if (Plane[e] == h && Portal[e] != -1)
                {
                    int edgeCount = 0;

                    Mesh mesh = meshes[e];

                    PortalMeta portalMeta = new PortalMeta();

                    FrustumMeta portalfrustum = new FrustumMeta();

                    for (int x = 0; x < mesh.vertices.Length; x++)
                    {
                        int y = (x + 1) % mesh.vertices.Length;

                        Edge line = new Edge();

                        line.start = mesh.vertices[x];
                        line.end = mesh.vertices[y];

                        line.portalID = portalnumber;

                        line.sectorID = h;

                        LevelLists.edges.Add(line);

                        edgeCount += 1;
                    }

                    portalMeta.lineStartIndex = edgeStart;

                    portalMeta.lineCount = edgeCount;

                    portalMeta.portalPlane = portalPlaneCount;

                    portalMeta.sectorID = h;

                    portalMeta.connectedSectorID = Portal[e];

                    portalMeta.portalID = portalnumber;

                    LevelLists.portals.Add(portalMeta);

                    portalfrustum.planeStartIndex = 0;

                    portalfrustum.planeCount = 0;

                    portalfrustum.frustumID = portalnumber;

                    LevelLists.frustums.Add(portalfrustum);

                    edgeStart += edgeCount;

                    portalCount += 1;

                    portalnumber += 1;
                }

                if (Plane[e] == h)
                {
                    portalPlaneCount += 1;
                }

                if (Render[e] == h)
                {
                    Mesh mesh = meshes[e];

                    uvVector4.Clear();

                    mesh.GetUVs(0, uvVector4);

                    for (int i = 0; i < mesh.triangles.Length; i += 3)
                    {
                        Triangle otriangle = new Triangle();

                        otriangle.v1 = mesh.vertices[mesh.triangles[i]];
                        otriangle.v2 = mesh.vertices[mesh.triangles[i + 1]];
                        otriangle.v3 = mesh.vertices[mesh.triangles[i + 2]];
                        otriangle.uv1 = uvVector4[mesh.triangles[i]];
                        otriangle.uv2 = uvVector4[mesh.triangles[i + 1]];
                        otriangle.uv3 = uvVector4[mesh.triangles[i + 2]];

                        otriangle.sectorID = h;

                        LevelLists.opaques.Add(otriangle);

                        rendersCount += 1;
                    }
                }

                if (Collision[e] == h)
                {
                    Mesh mesh = meshes[e];

                    for (int i = 0; i < mesh.triangles.Length; i += 3)
                    {
                        Collisions ctriangle = new Collisions();

                        ctriangle.v1 = mesh.vertices[mesh.triangles[i]];
                        ctriangle.v2 = mesh.vertices[mesh.triangles[i + 1]];
                        ctriangle.v3 = mesh.vertices[mesh.triangles[i + 2]];

                        ctriangle.sectorID = h;

                        LevelLists.collisions.Add(ctriangle);

                        collideCount += 1;
                    }
                }

                if (Transparent[e] == h)
                {
                    Mesh mesh = meshes[e];

                    uvVector4.Clear();

                    mesh.GetUVs(0, uvVector4);

                    for (int i = 0; i < mesh.triangles.Length; i += 3)
                    {
                        Triangle ttriangle = new Triangle();

                        ttriangle.v1 = mesh.vertices[mesh.triangles[i]];
                        ttriangle.v2 = mesh.vertices[mesh.triangles[i + 1]];
                        ttriangle.v3 = mesh.vertices[mesh.triangles[i + 2]];
                        ttriangle.uv1 = uvVector4[mesh.triangles[i]];
                        ttriangle.uv2 = uvVector4[mesh.triangles[i + 1]];
                        ttriangle.uv3 = uvVector4[mesh.triangles[i + 2]];

                        ttriangle.sectorID = h;

                        LevelLists.transparents.Add(ttriangle);

                        transparentCount += 1;
                    }
                }
            }

            SectorMeta sectorMeta = new SectorMeta();

            sectorMeta.planeStartIndex = planeStart;
            sectorMeta.planeCount = planeCount;

            sectorMeta.opaqueStartIndex = opaqueStart;
            sectorMeta.opaqueCount = rendersCount;

            sectorMeta.transparentStartIndex = transparentStart;
            sectorMeta.transparentCount = transparentCount;

            sectorMeta.collisionStartIndex = collisionStart;
            sectorMeta.collisionCount = collideCount;

            sectorMeta.portalStartIndex = portalStart;
            sectorMeta.portalCount = portalCount;

            sectorMeta.sectorID = h;

            LevelLists.sectors.Add(sectorMeta);

            planeStart += planeCount;

            opaqueStart += rendersCount;

            transparentStart += transparentCount;

            portalStart += portalCount;

            collisionStart += collideCount;
        }

        FrustumMeta camfrustum = new FrustumMeta();

        camfrustum.planeStartIndex = 0;

        camfrustum.planeCount = 0;

        camfrustum.frustumID = portalnumber + 1;

        LevelLists.frustums.Add(camfrustum);

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
