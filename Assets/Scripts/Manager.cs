namespace Portals
{
    using System.Collections.Generic;
    using UnityEngine;
    using Weland;
    using System.Linq;
    using System;

    public class Manager : MonoBehaviour
    {
        private bool Ready = false;

        public string Directory;

        public int LevelNumber;

        public Camera Cam;

        public Level level;

        public Collider Player;

        public Vector4[] PlanePos;

        public RenderParams rp;

        public MaterialPropertyBlock BlockOne;

        public List<Plane> Planes = new List<Plane>(6);

        public List<GameObject> VisitedSector = new List<GameObject>();

        public List<GameObject> GetLines = new List<GameObject>();

        public List<GameObject> GetPolygons = new List<GameObject>();

        public List<GameObject> LevelObjects = new List<GameObject>();

        public List<GameObject> PlayerStarts = new List<GameObject>();

        public List<Vector3> PlayerPositions = new List<Vector3>();

        public List<int> list1 = new List<int>();

        public List<int> list2 = new List<int>();

        public GameObject CurrentSector;

        void Awake()
        {
            Cam = GameObject.Find("Main Camera").GetComponent<Camera>();

            Player = GameObject.Find("Player").GetComponent<CharacterController>();

            PlanePos = new Vector4[20];

            rp = new RenderParams();

            rp.matProps = new MaterialPropertyBlock();

            MapFile map = new MapFile();

            // Change name to load a different map
            map.Load(Application.dataPath + Directory);

            level = new Level();

            // Change the map directory number if the map has more than one level 
            level.Load(map.Directory[LevelNumber]);

            GameObject Polygons = new GameObject("Polygons");

            for (int i = 0; i < level.Polygons.Count; i++)
            {
                GameObject Polygon = new GameObject("Polygon__" + i);

                Polygon.transform.SetParent(Polygons.transform);
            }

            GameObject FPolygon = GameObject.Find("Polygons");

            for (int i = 0; i < FPolygon.gameObject.transform.childCount; ++i)
            {
                GetPolygons.Add(FPolygon.gameObject.transform.GetChild(i).gameObject);
            }
            for (int i = 0; i < GetPolygons.Count; ++i)
            {
                GetPolygons[i].AddComponent<GetPolygons>();
            }

            GameObject Lines = new GameObject("Lines");

            for (int i = 0; i < level.Lines.Count; i++)
            {
                GameObject Line = new GameObject("Line__" + i);

                Line.transform.SetParent(Lines.transform);
            }

            GameObject FLines = GameObject.Find("Lines");

            for (int i = 0; i < FLines.gameObject.transform.childCount; ++i)
            {
                GetLines.Add(FLines.gameObject.transform.GetChild(i).gameObject);
            }
            for (int i = 0; i < GetLines.Count; ++i)
            {
                GetLines[i].AddComponent<GetLines>();
            }

            GameObject MapObjects = new GameObject("MapObjects");

            for (int i = 0; i < level.Objects.Count; i++)
            {
                GameObject Objects = new GameObject("Object__" + i);

                Objects.transform.SetParent(MapObjects.transform);
            }

            GameObject FMapObjects = GameObject.Find("MapObjects");

            for (int i = 0; i < FMapObjects.gameObject.transform.childCount; ++i)
            {
                LevelObjects.Add(FMapObjects.gameObject.transform.GetChild(i).gameObject);
            }
        }

        System.Collections.IEnumerator BuildLines()
        {
            for (int i = 0; i < GetLines.Count; ++i)
            {
                if (level.Lines[i].ClockwisePolygonOwner != -1)
                {
                    if (level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight > level.Lines[i].LowestAdjacentCeiling)
                    {
                        if (level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight < level.Lines[i].LowestAdjacentCeiling)
                        {
                            double YC0 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight / 1024 * 2.5f;
                            double YC1 = (float)level.Lines[i].LowestAdjacentCeiling / 1024 * 2.5f;

                            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * 2.5f;
                            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * 2.5f * -1;

                            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * 2.5f;
                            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * 2.5f * -1;

                            GetLines[i].GetComponent<GetLines>().CWTop.Add(new Vector3((float)X1, (float)YC1, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CWTop.Add(new Vector3((float)X1, (float)YC0, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CWTop.Add(new Vector3((float)X0, (float)YC0, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CWTop.Add(new Vector3((float)X0, (float)YC1, (float)Z0));
                        }
                        else
                        {
                            double YC0 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight / 1024 * 2.5f;
                            double YC1 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight / 1024 * 2.5f;

                            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * 2.5f;
                            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * 2.5f * -1;

                            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * 2.5f;
                            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * 2.5f * -1;

                            GetLines[i].GetComponent<GetLines>().CWTop.Add(new Vector3((float)X1, (float)YC1, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CWTop.Add(new Vector3((float)X1, (float)YC0, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CWTop.Add(new Vector3((float)X0, (float)YC0, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CWTop.Add(new Vector3((float)X0, (float)YC1, (float)Z0));
                        }

                    }

                    if (level.Lines[i].LowestAdjacentCeiling != level.Lines[i].HighestAdjacentFloor)
                    {

                        if (level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight > level.Lines[i].HighestAdjacentFloor &&
                            level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight < level.Lines[i].LowestAdjacentCeiling)
                        {
                            double YC = (float)level.Lines[i].LowestAdjacentCeiling / 1024 * 2.5f;
                            double YF = (float)level.Lines[i].HighestAdjacentFloor / 1024 * 2.5f;

                            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * 2.5f;
                            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * 2.5f * -1;

                            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * 2.5f;
                            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * 2.5f * -1;

                            GetLines[i].GetComponent<GetLines>().CWMiddle.Add(new Vector3((float)X1, (float)YF, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CWMiddle.Add(new Vector3((float)X1, (float)YC, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CWMiddle.Add(new Vector3((float)X0, (float)YC, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CWMiddle.Add(new Vector3((float)X0, (float)YF, (float)Z0));
                        }

                    }

                    if (level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight < level.Lines[i].HighestAdjacentFloor)
                    {
                        if (level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight > level.Lines[i].HighestAdjacentFloor)
                        {
                            double YF0 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight / 1024 * 2.5f;
                            double YF1 = (float)level.Lines[i].HighestAdjacentFloor / 1024 * 2.5f;

                            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * 2.5f;
                            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * 2.5f * -1;

                            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * 2.5f;
                            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * 2.5f * -1;

                            GetLines[i].GetComponent<GetLines>().CWBottom.Add(new Vector3((float)X1, (float)YF0, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CWBottom.Add(new Vector3((float)X1, (float)YF1, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CWBottom.Add(new Vector3((float)X0, (float)YF1, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CWBottom.Add(new Vector3((float)X0, (float)YF0, (float)Z0));
                        }
                        else
                        {
                            double YF0 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].FloorHeight / 1024 * 2.5f;
                            double YF1 = (float)level.Polygons[level.Lines[i].ClockwisePolygonOwner].CeilingHeight / 1024 * 2.5f;

                            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * 2.5f;
                            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * 2.5f * -1;

                            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * 2.5f;
                            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * 2.5f * -1;

                            GetLines[i].GetComponent<GetLines>().CWBottom.Add(new Vector3((float)X1, (float)YF0, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CWBottom.Add(new Vector3((float)X1, (float)YF1, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CWBottom.Add(new Vector3((float)X0, (float)YF1, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CWBottom.Add(new Vector3((float)X0, (float)YF0, (float)Z0));
                        }
                    }
                }

                if (level.Lines[i].CounterclockwisePolygonOwner != -1)
                {
                    if (level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight > level.Lines[i].LowestAdjacentCeiling)
                    {
                        if (level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight < level.Lines[i].LowestAdjacentCeiling)
                        {
                            double YC0 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight / 1024 * 2.5f;
                            double YC1 = (float)level.Lines[i].LowestAdjacentCeiling / 1024 * 2.5f;

                            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * 2.5f;
                            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * 2.5f * -1;

                            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * 2.5f;
                            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * 2.5f * -1;

                            GetLines[i].GetComponent<GetLines>().CCWTop.Add(new Vector3((float)X0, (float)YC1, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CCWTop.Add(new Vector3((float)X0, (float)YC0, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CCWTop.Add(new Vector3((float)X1, (float)YC0, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CCWTop.Add(new Vector3((float)X1, (float)YC1, (float)Z1));
                        }
                        else
                        {
                            double YC0 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight / 1024 * 2.5f;
                            double YC1 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight / 1024 * 2.5f;

                            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * 2.5f;
                            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * 2.5f * -1;

                            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * 2.5f;
                            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * 2.5f * -1;

                            GetLines[i].GetComponent<GetLines>().CCWTop.Add(new Vector3((float)X0, (float)YC1, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CCWTop.Add(new Vector3((float)X0, (float)YC0, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CCWTop.Add(new Vector3((float)X1, (float)YC0, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CCWTop.Add(new Vector3((float)X1, (float)YC1, (float)Z1));
                        }

                    }

                    if (level.Lines[i].LowestAdjacentCeiling != level.Lines[i].HighestAdjacentFloor)
                    {
                        if (level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight > level.Lines[i].HighestAdjacentFloor &&
                            level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight < level.Lines[i].LowestAdjacentCeiling)
                        {
                            double YC = (float)level.Lines[i].LowestAdjacentCeiling / 1024 * 2.5f;
                            double YF = (float)level.Lines[i].HighestAdjacentFloor / 1024 * 2.5f;

                            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * 2.5f;
                            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * 2.5f * -1;

                            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * 2.5f;
                            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * 2.5f * -1;

                            GetLines[i].GetComponent<GetLines>().CCWMiddle.Add(new Vector3((float)X0, (float)YF, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CCWMiddle.Add(new Vector3((float)X0, (float)YC, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CCWMiddle.Add(new Vector3((float)X1, (float)YC, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CCWMiddle.Add(new Vector3((float)X1, (float)YF, (float)Z1));
                        }
                    }

                    if (level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight < level.Lines[i].HighestAdjacentFloor)
                    {
                        if (level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight > level.Lines[i].HighestAdjacentFloor)
                        {
                            double YF0 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight / 1024 * 2.5f;
                            double YF1 = (float)level.Lines[i].HighestAdjacentFloor / 1024 * 2.5f;

                            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * 2.5f;
                            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * 2.5f * -1;

                            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * 2.5f;
                            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * 2.5f * -1;

                            GetLines[i].GetComponent<GetLines>().CCWBottom.Add(new Vector3((float)X0, (float)YF0, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CCWBottom.Add(new Vector3((float)X0, (float)YF1, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CCWBottom.Add(new Vector3((float)X1, (float)YF1, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CCWBottom.Add(new Vector3((float)X1, (float)YF0, (float)Z1));
                        }
                        else
                        {
                            double YF0 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].FloorHeight / 1024 * 2.5f;
                            double YF1 = (float)level.Polygons[level.Lines[i].CounterclockwisePolygonOwner].CeilingHeight / 1024 * 2.5f;

                            double X1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].X / 1024 * 2.5f;
                            double Z1 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[0]].Y / 1024 * 2.5f * -1;

                            double X0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].X / 1024 * 2.5f;
                            double Z0 = (float)level.Endpoints[level.Lines[i].EndpointIndexes[1]].Y / 1024 * 2.5f * -1;

                            GetLines[i].GetComponent<GetLines>().CCWBottom.Add(new Vector3((float)X0, (float)YF0, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CCWBottom.Add(new Vector3((float)X0, (float)YF1, (float)Z0));
                            GetLines[i].GetComponent<GetLines>().CCWBottom.Add(new Vector3((float)X1, (float)YF1, (float)Z1));
                            GetLines[i].GetComponent<GetLines>().CCWBottom.Add(new Vector3((float)X1, (float)YF0, (float)Z1));
                        }

                    }
                }

                for (int e = 2; e < 4; e++)
                {
                    int a = 0;
                    int b = e - 1;
                    int c = e;

                    GetLines[i].GetComponent<GetLines>().triangles.Add(a);
                    GetLines[i].GetComponent<GetLines>().triangles.Add(b);
                    GetLines[i].GetComponent<GetLines>().triangles.Add(c);
                }

                if (level.Lines[i].ClockwisePolygonOwner != -1)
                {
                    if (GetLines[i].GetComponent<GetLines>().CWTop.Count > 2)
                    {
                        GameObject topclockwise = new GameObject("Clockwise Top");

                        topclockwise.AddComponent<MeshFilter>();
                        topclockwise.AddComponent<MeshRenderer>();

                        Renderer topclockwiseRend = topclockwise.GetComponent<Renderer>();
                        topclockwiseRend.sharedMaterial = new Material(Shader.Find("Custom/Clipping"));

                        Mesh topclockwisemesh = topclockwise.GetComponent<MeshFilter>().mesh;

                        topclockwisemesh.SetVertices(GetLines[i].GetComponent<GetLines>().CWTop);
                        topclockwisemesh.SetTriangles(GetLines[i].GetComponent<GetLines>().triangles, 0, true);
                        topclockwisemesh.RecalculateNormals();

                        topclockwise.AddComponent<MeshCollider>();

                        topclockwise.AddComponent<Side>();

                        topclockwise.GetComponent<Side>().vertices = GetLines[i].GetComponent<GetLines>().CWTop;

                        topclockwise.GetComponent<Side>().plane = new Plane(topclockwise.GetComponent<Side>().vertices[0],
                                                                            topclockwise.GetComponent<Side>().vertices[1],
                                                                            topclockwise.GetComponent<Side>().vertices[2]);

                        topclockwise.transform.SetParent(GetPolygons[level.Lines[i].ClockwisePolygonOwner].transform);
                    }

                    if (GetLines[i].GetComponent<GetLines>().CWMiddle.Count > 2)
                    {
                        GameObject middleclockwise = new GameObject("Clockwise Middle");

                        middleclockwise.AddComponent<MeshFilter>();
                        middleclockwise.AddComponent<MeshRenderer>();

                        Renderer middleclockwiseRend = middleclockwise.GetComponent<Renderer>();
                        middleclockwiseRend.sharedMaterial = new Material(Shader.Find("Custom/Clipping"));

                        Mesh middleclockwisemesh = middleclockwise.GetComponent<MeshFilter>().mesh;

                        middleclockwisemesh.SetVertices(GetLines[i].GetComponent<GetLines>().CWMiddle);
                        middleclockwisemesh.SetTriangles(GetLines[i].GetComponent<GetLines>().triangles, 0, true);
                        middleclockwisemesh.RecalculateNormals();

                        middleclockwise.AddComponent<MeshCollider>();

                        middleclockwise.AddComponent<Side>();

                        middleclockwise.GetComponent<Side>().vertices = GetLines[i].GetComponent<GetLines>().CWMiddle;

                        middleclockwise.GetComponent<Side>().plane = new Plane(middleclockwise.GetComponent<Side>().vertices[0],
                                                                               middleclockwise.GetComponent<Side>().vertices[1],
                                                                               middleclockwise.GetComponent<Side>().vertices[2]);

                        if (level.Lines[i].ClockwisePolygonOwner != -1 && level.Lines[i].CounterclockwisePolygonOwner != -1)
                        {
                            middleclockwise.GetComponent<Side>().TargetSector = GetPolygons[level.Lines[i].CounterclockwisePolygonOwner];
                        }

                        middleclockwise.transform.SetParent(GetPolygons[level.Lines[i].ClockwisePolygonOwner].transform);
                    }

                    if (GetLines[i].GetComponent<GetLines>().CWBottom.Count > 2)
                    {
                        GameObject bottomclockwise = new GameObject("Clockwise Bottom");

                        bottomclockwise.AddComponent<MeshFilter>();
                        bottomclockwise.AddComponent<MeshRenderer>();

                        Renderer bottomclockwiseRend = bottomclockwise.GetComponent<Renderer>();
                        bottomclockwiseRend.sharedMaterial = new Material(Shader.Find("Custom/Clipping"));

                        Mesh bottomclockwisemesh = bottomclockwise.GetComponent<MeshFilter>().mesh;

                        bottomclockwisemesh.SetVertices(GetLines[i].GetComponent<GetLines>().CWBottom);
                        bottomclockwisemesh.SetTriangles(GetLines[i].GetComponent<GetLines>().triangles, 0, true);
                        bottomclockwisemesh.RecalculateNormals();

                        bottomclockwise.AddComponent<MeshCollider>();

                        bottomclockwise.AddComponent<Side>();

                        bottomclockwise.GetComponent<Side>().vertices = GetLines[i].GetComponent<GetLines>().CWBottom;

                        bottomclockwise.GetComponent<Side>().plane = new Plane(bottomclockwise.GetComponent<Side>().vertices[0],
                                                                               bottomclockwise.GetComponent<Side>().vertices[1],
                                                                               bottomclockwise.GetComponent<Side>().vertices[2]);

                        bottomclockwise.transform.SetParent(GetPolygons[level.Lines[i].ClockwisePolygonOwner].transform);
                    }
                }

                if (level.Lines[i].CounterclockwisePolygonOwner != -1)
                {
                    if (GetLines[i].GetComponent<GetLines>().CCWTop.Count > 2)
                    {
                        GameObject topcounterclockwise = new GameObject("Counterclockwise Top");

                        topcounterclockwise.AddComponent<MeshFilter>();
                        topcounterclockwise.AddComponent<MeshRenderer>();

                        Renderer topcounterclockwiseRend = topcounterclockwise.GetComponent<Renderer>();
                        topcounterclockwiseRend.sharedMaterial = new Material(Shader.Find("Custom/Clipping"));

                        Mesh topcounterclockwisemesh = topcounterclockwise.GetComponent<MeshFilter>().mesh;

                        topcounterclockwisemesh.SetVertices(GetLines[i].GetComponent<GetLines>().CCWTop);
                        topcounterclockwisemesh.SetTriangles(GetLines[i].GetComponent<GetLines>().triangles, 0, true);
                        topcounterclockwisemesh.RecalculateNormals();

                        topcounterclockwise.AddComponent<MeshCollider>();

                        topcounterclockwise.AddComponent<Side>();

                        topcounterclockwise.GetComponent<Side>().vertices = GetLines[i].GetComponent<GetLines>().CCWTop;

                        topcounterclockwise.GetComponent<Side>().plane = new Plane(topcounterclockwise.GetComponent<Side>().vertices[0],
                                                                                   topcounterclockwise.GetComponent<Side>().vertices[1],
                                                                                   topcounterclockwise.GetComponent<Side>().vertices[2]);

                        topcounterclockwise.transform.SetParent(GetPolygons[level.Lines[i].CounterclockwisePolygonOwner].transform);
                    }

                    if (GetLines[i].GetComponent<GetLines>().CCWMiddle.Count > 2)
                    {
                        GameObject middlecounterclockwise = new GameObject("Counterclockwise Middle");

                        middlecounterclockwise.AddComponent<MeshFilter>();
                        middlecounterclockwise.AddComponent<MeshRenderer>();

                        Renderer middlecounterclockwiseRend = middlecounterclockwise.GetComponent<Renderer>();
                        middlecounterclockwiseRend.sharedMaterial = new Material(Shader.Find("Custom/Clipping"));

                        Mesh middlecounterclockwisemesh = middlecounterclockwise.GetComponent<MeshFilter>().mesh;

                        middlecounterclockwisemesh.SetVertices(GetLines[i].GetComponent<GetLines>().CCWMiddle);
                        middlecounterclockwisemesh.SetTriangles(GetLines[i].GetComponent<GetLines>().triangles, 0, true);
                        middlecounterclockwisemesh.RecalculateNormals();

                        middlecounterclockwise.AddComponent<MeshCollider>();

                        middlecounterclockwise.AddComponent<Side>();

                        middlecounterclockwise.GetComponent<Side>().vertices = GetLines[i].GetComponent<GetLines>().CCWMiddle;

                        middlecounterclockwise.GetComponent<Side>().plane = new Plane(middlecounterclockwise.GetComponent<Side>().vertices[0],
                                                                                      middlecounterclockwise.GetComponent<Side>().vertices[1],
                                                                                      middlecounterclockwise.GetComponent<Side>().vertices[2]);

                        if (level.Lines[i].ClockwisePolygonOwner != -1 && level.Lines[i].CounterclockwisePolygonOwner != -1)
                        {
                            middlecounterclockwise.GetComponent<Side>().TargetSector = GetPolygons[level.Lines[i].ClockwisePolygonOwner];
                        }

                        middlecounterclockwise.transform.SetParent(GetPolygons[level.Lines[i].CounterclockwisePolygonOwner].transform);
                    }

                    if (GetLines[i].GetComponent<GetLines>().CCWBottom.Count > 2)
                    {
                        GameObject bottomcounterclockwise = new GameObject("Counterclockwise Bottom");

                        bottomcounterclockwise.AddComponent<MeshFilter>();
                        bottomcounterclockwise.AddComponent<MeshRenderer>();

                        Renderer bottomcounterclockwiseRend = bottomcounterclockwise.GetComponent<Renderer>();
                        bottomcounterclockwiseRend.sharedMaterial = new Material(Shader.Find("Custom/Clipping"));

                        Mesh bottomcounterclockwisemesh = bottomcounterclockwise.GetComponent<MeshFilter>().mesh;

                        bottomcounterclockwisemesh.SetVertices(GetLines[i].GetComponent<GetLines>().CCWBottom);
                        bottomcounterclockwisemesh.SetTriangles(GetLines[i].GetComponent<GetLines>().triangles, 0, true);
                        bottomcounterclockwisemesh.RecalculateNormals();

                        bottomcounterclockwise.AddComponent<MeshCollider>();

                        bottomcounterclockwise.AddComponent<Side>();

                        bottomcounterclockwise.GetComponent<Side>().vertices = GetLines[i].GetComponent<GetLines>().CCWBottom;

                        bottomcounterclockwise.GetComponent<Side>().plane = new Plane(bottomcounterclockwise.GetComponent<Side>().vertices[0],
                                                                                      bottomcounterclockwise.GetComponent<Side>().vertices[1],
                                                                                      bottomcounterclockwise.GetComponent<Side>().vertices[2]);

                        bottomcounterclockwise.transform.SetParent(GetPolygons[level.Lines[i].CounterclockwisePolygonOwner].transform);
                    }
                }

                yield return null;
            }
        }

        System.Collections.IEnumerator BuildPolygons()
        {
            for (int i = 0; i < GetPolygons.Count; i++)
            {
                for (int e = 0; e < level.Polygons[i].VertexCount; ++e)
                {
                    GetPolygons[i].GetComponent<GetPolygons>().GetLines.Add(GetLines[level.Polygons[i].LineIndexes[e]]);

                    float YC = (float)level.Polygons[i].CeilingHeight / 1024 * 2.5f;
                    float YF = (float)level.Polygons[i].FloorHeight / 1024 * 2.5f;

                    float X = (float)level.Endpoints[level.Polygons[i].EndpointIndexes[e]].X / 1024 * 2.5f;
                    float Z = (float)level.Endpoints[level.Polygons[i].EndpointIndexes[e]].Y / 1024 * 2.5f * -1;

                    GetPolygons[i].GetComponent<GetPolygons>().X.Add(X);

                    GetPolygons[i].GetComponent<GetPolygons>().Y.Add(Z);

                    GetPolygons[i].GetComponent<GetPolygons>().ceilingverts.Add(new Vector3(X, YC, Z));

                    GetPolygons[i].GetComponent<GetPolygons>().floorverts.Add(new Vector3(X, YF, Z));
                }

                float XS0 = GetPolygons[i].GetComponent<GetPolygons>().X.Sum();

                float XS1 = XS0 / GetPolygons[i].GetComponent<GetPolygons>().X.Count;

                float ZS0 = GetPolygons[i].GetComponent<GetPolygons>().Y.Sum();

                float ZS1 = ZS0 / GetPolygons[i].GetComponent<GetPolygons>().Y.Count;

                float YP1 = level.Polygons[i].CeilingHeight + level.Polygons[i].FloorHeight;

                float YP2 = YP1 / 2 / 1024 * 2.5f;

                GetPolygons[i].GetComponent<GetPolygons>().CenterPoint = new Vector3(XS1, YP2, ZS1);

                for (int e = 0; e < level.Polygons.Count; ++e)
                {
                    list1.Clear();

                    for (int r = 0; r < level.Polygons[i].VertexCount; ++r)
                    {
                        list1.Add(level.Polygons[i].EndpointIndexes[r]);
                    }

                    for (int h = 0; h < level.Polygons.Count; ++h)
                    {
                        list2.Clear();

                        for (int x = 0; x < level.Polygons[h].VertexCount; ++x)
                        {
                            list2.Add(level.Polygons[h].EndpointIndexes[x]);
                        }

                        for (int x = 0; x < list2.Count; ++x)
                        {
                            if (list1.Contains(list2[x]))
                            {
                                if (!GetPolygons[i].GetComponent<GetPolygons>().CheckPolygons.Contains(GetPolygons[h]))
                                {
                                    GetPolygons[i].GetComponent<GetPolygons>().CheckPolygons.Add(GetPolygons[h]);
                                }
                            }
                        }
                    }
                }

                for (int e = 0; e < GetPolygons[i].GetComponent<GetPolygons>().Sides.Count; e++)
                {
                    if (GetPolygons[i].GetComponent<GetPolygons>().Sides[e].GetComponent<MeshCollider>() == null)
                    {
                        GetPolygons[i].GetComponent<GetPolygons>().Sides[e].AddComponent<MeshCollider>();
                    }
                }

                for (int e = 0; e < GetPolygons[i].GetComponent<GetPolygons>().Sides.Count; e++)
                {
                    for (int r = 0; r < GetPolygons[i].GetComponent<GetPolygons>().Sides[e].GetComponent<Renderer>().sharedMaterials.Length; r++)
                    {
                        GetPolygons[i].GetComponent<GetPolygons>().Sides[e].GetComponent<Renderer>().sharedMaterials[r].shader = Shader.Find("Custom/Clipping");
                    }
                }

                if (GetPolygons[i].GetComponent<GetPolygons>().floorverts.Count > 2)
                {
                    for (int e = 2; e < GetPolygons[i].GetComponent<GetPolygons>().floorverts.Count; e++)
                    {
                        int a = 0;
                        int b = e - 1;
                        int c = e;

                        GetPolygons[i].GetComponent<GetPolygons>().floortri.Add(a);
                        GetPolygons[i].GetComponent<GetPolygons>().floortri.Add(b);
                        GetPolygons[i].GetComponent<GetPolygons>().floortri.Add(c);
                    }

                    GameObject floorclockwise = new GameObject("Floor");

                    floorclockwise.AddComponent<MeshFilter>();
                    floorclockwise.AddComponent<MeshRenderer>();

                    Renderer floorclockwiseRend = floorclockwise.GetComponent<Renderer>();
                    floorclockwiseRend.sharedMaterial = new Material(Shader.Find("Custom/Clipping"));

                    Mesh floorclockwisemesh = floorclockwise.GetComponent<MeshFilter>().mesh;

                    floorclockwisemesh.SetVertices(GetPolygons[i].GetComponent<GetPolygons>().floorverts);
                    floorclockwisemesh.SetTriangles(GetPolygons[i].GetComponent<GetPolygons>().floortri, 0, true);
                    floorclockwisemesh.RecalculateNormals();

                    floorclockwise.AddComponent<MeshCollider>();

                    floorclockwise.AddComponent<Side>();

                    floorclockwise.GetComponent<Side>().vertices = GetPolygons[i].GetComponent<GetPolygons>().floorverts;

                    floorclockwise.GetComponent<Side>().plane = new Plane(floorclockwise.GetComponent<Side>().vertices[0],
                                                                          floorclockwise.GetComponent<Side>().vertices[1],
                                                                          floorclockwise.GetComponent<Side>().vertices[2]);

                    floorclockwise.transform.SetParent(GetPolygons[i].transform);
                }

                if (GetPolygons[i].GetComponent<GetPolygons>().ceilingverts.Count > 2)
                {
                    GetPolygons[i].GetComponent<GetPolygons>().ceilingverts.Reverse();

                    for (int e = 2; e < GetPolygons[i].GetComponent<GetPolygons>().ceilingverts.Count; e++)
                    {
                        int a = 0;
                        int b = e - 1;
                        int c = e;

                        GetPolygons[i].GetComponent<GetPolygons>().ceilingtri.Add(a);
                        GetPolygons[i].GetComponent<GetPolygons>().ceilingtri.Add(b);
                        GetPolygons[i].GetComponent<GetPolygons>().ceilingtri.Add(c);
                    }

                    GameObject ceilingcounterlockwise = new GameObject("Ceiling");

                    ceilingcounterlockwise.AddComponent<MeshFilter>();
                    ceilingcounterlockwise.AddComponent<MeshRenderer>();

                    Renderer ceilingcounterlockwiseRend = ceilingcounterlockwise.GetComponent<Renderer>();
                    ceilingcounterlockwiseRend.sharedMaterial = new Material(Shader.Find("Custom/Clipping"));

                    Mesh ceilingcounterlockwisemesh = ceilingcounterlockwise.GetComponent<MeshFilter>().mesh;

                    ceilingcounterlockwisemesh.SetVertices(GetPolygons[i].GetComponent<GetPolygons>().ceilingverts);
                    ceilingcounterlockwisemesh.SetTriangles(GetPolygons[i].GetComponent<GetPolygons>().ceilingtri, 0, true);
                    ceilingcounterlockwisemesh.RecalculateNormals();

                    ceilingcounterlockwise.AddComponent<MeshCollider>();

                    ceilingcounterlockwise.AddComponent<Side>();

                    ceilingcounterlockwise.GetComponent<Side>().vertices = GetPolygons[i].GetComponent<GetPolygons>().ceilingverts;

                    ceilingcounterlockwise.GetComponent<Side>().plane = new Plane(ceilingcounterlockwise.GetComponent<Side>().vertices[0],
                                                                                  ceilingcounterlockwise.GetComponent<Side>().vertices[1],
                                                                                  ceilingcounterlockwise.GetComponent<Side>().vertices[2]);

                    ceilingcounterlockwise.transform.SetParent(GetPolygons[i].transform);
                }

                yield return null;
            }
        }

        public void GetObjects()
        {
            for (int i = 0; i < level.Objects.Count; ++i)
            {
                if (level.Objects[i].Type == ObjectType.Player)
                {
                    GameObject PlayerStart = GetPolygons[level.Objects[i].PolygonIndex];

                    PlayerStarts.Add(PlayerStart);

                    PlayerPositions.Add(new Vector3((float)level.Objects[i].X / 1024 * 2.5f, (float)level.Polygons[level.Objects[i].PolygonIndex].FloorHeight / 1024 * 2.5f + 1, (float)level.Objects[i].Y / 1024 * 2.5f * -1));
                }
            }
            int random = UnityEngine.Random.Range(0, PlayerStarts.Count);

            CurrentSector = PlayerStarts[random];

            Player.transform.position = PlayerPositions[random];
        }

        // Start is called before the first frame update
        System.Collections.IEnumerator Start()
        {
            GetObjects();

            yield return StartCoroutine(BuildLines());

            yield return StartCoroutine(BuildPolygons());

            for (int i = 0; i < GetPolygons.Count; i++)
            {
                for (int e = 0; e < GetPolygons[i].gameObject.transform.childCount; ++e)
                {
                    GetPolygons[i].GetComponent<GetPolygons>().Sides.Add(GetPolygons[i].gameObject.transform.GetChild(e).gameObject);
                }

                for (int e = 0; e < GetPolygons[i].GetComponent<GetPolygons>().Sides.Count; e++)
                {
                    GetPolygons[i].GetComponent<GetPolygons>().Sides[e].GetComponent<MeshRenderer>().enabled = false;
                }

                for (int e = 0; e < GetPolygons[i].GetComponent<GetPolygons>().Sides.Count; e++)
                {
                    GetPolygons[i].GetComponent<GetPolygons>().Planes.Add(GetPolygons[i].GetComponent<GetPolygons>().Sides[e].GetComponent<Side>().plane);
                }
            }

            Ready = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (Ready == true)
            {
                Player.GetComponent<Move>().PlayerInput();

                CheckSector();

                Planes.Clear();

                Cam.ReadFrustumPlanes(Planes);

                Planes.RemoveAt(5);

                Planes.RemoveAt(4);

                VisitedSector.Clear();

                GetSector(Planes, CurrentSector);
            }
        }

        public void GetSector(List<Plane> APlanes, GameObject BSector)
        {
            Vector3 CamPoint = Cam.transform.position;

            rp.matProps.SetInt("_Int", APlanes.Count);

            Array.Clear(PlanePos, 0, APlanes.Count);

            for (int i = 0; i < APlanes.Count; i++)
            {
                PlanePos[i] = new Vector4(APlanes[i].normal.x, APlanes[i].normal.y, APlanes[i].normal.z, APlanes[i].distance);
            }

            rp.matProps.SetVectorArray("_Plane", PlanePos);

            for (int i = 0; i < BSector.GetComponent<GetPolygons>().Sides.Count; i++)
            {
                if (BSector.GetComponent<GetPolygons>().Sides[i].GetComponent<Side>().TargetSector == null)
                {
                    Matrix4x4 matrix = Matrix4x4.TRS(BSector.GetComponent<GetPolygons>().Sides[i].transform.position, BSector.GetComponent<GetPolygons>().Sides[i].transform.rotation, BSector.GetComponent<GetPolygons>().Sides[i].transform.lossyScale);

                    rp.material = BSector.GetComponent<GetPolygons>().Sides[i].GetComponent<Renderer>().sharedMaterial;

                    Graphics.RenderMesh(rp, BSector.GetComponent<GetPolygons>().Sides[i].GetComponent<MeshFilter>().mesh, 0, matrix);
                }
            }

            VisitedSector.Add(BSector);

            for (int i = 0; i < BSector.GetComponent<GetPolygons>().Sides.Count; ++i)
            {
                GameObject p = BSector.GetComponent<GetPolygons>().Sides[i];

                if (p.GetComponent<Side>().TargetSector == null)
                {
                    continue;
                }

                float d = p.GetComponent<Side>().plane.GetDistanceToPoint(CamPoint);

                bool t = CheckRadius(p.GetComponent<Side>().TargetSector);

                if (d < -0.1)
                {
                    continue;
                }

                if (VisitedSector.Contains(p.GetComponent<Side>().TargetSector) && d <= 0)
                {
                    continue;
                }

                if (t == true)
                {
                    p.GetComponent<Side>().Planes.Clear();

                    for (int n = 0; n < APlanes.Count; n++)
                    {
                        p.GetComponent<Side>().Planes.Add(APlanes[n]);
                    }

                    GetSector(p.GetComponent<Side>().Planes, p.GetComponent<Side>().TargetSector);

                    continue;
                }

                if (d != 0)
                {
                    p.GetComponent<Side>().verticesout = p.GetComponent<Side>().ClippingPlanes(p.GetComponent<Side>().vertices, APlanes);

                    if (p.GetComponent<Side>().verticesout.Count > 2)
                    {
                        p.GetComponent<Side>().Planes.Clear();

                        p.GetComponent<Side>().CreateClippingPlanes(p.GetComponent<Side>().verticesout, p.GetComponent<Side>().Planes, CamPoint);

                        GetSector(p.GetComponent<Side>().Planes, p.GetComponent<Side>().TargetSector);
                    }
                }
            }
        }

        public bool CheckRadius(GameObject PSector)
        {
            Vector3 CamPoint = Cam.transform.position;

            bool PointIn = true;

            for (int e = 0; e < PSector.GetComponent<GetPolygons>().Planes.Count; e++)
            {
                if (PSector.GetComponent<GetPolygons>().Planes[e].GetDistanceToPoint(CamPoint) < -0.5)
                {
                    PointIn = false;
                    break;
                }
            }
            return PointIn;
        }

        public void CheckSector()
        {
            Vector3 CamPoint = Cam.transform.position;

            for (int i = 0; i < CurrentSector.GetComponent<GetPolygons>().CheckPolygons.Count; i++)
            {
                bool PointIn = true;

                for (int e = 0; e < CurrentSector.GetComponent<GetPolygons>().CheckPolygons[i].GetComponent<GetPolygons>().Planes.Count; e++)
                {
                    if (CurrentSector.GetComponent<GetPolygons>().CheckPolygons[i].GetComponent<GetPolygons>().Planes[e].GetDistanceToPoint(CamPoint) < 0)
                    {
                        PointIn = false;
                        break;
                    }
                }

                if (PointIn == true)
                {
                    CurrentSector = CurrentSector.GetComponent<GetPolygons>().CheckPolygons[i];
                }
            }

            IEnumerable<GameObject> except = GetPolygons.Except(CurrentSector.GetComponent<GetPolygons>().CheckPolygons);

            foreach (GameObject sector in except)
            {
                foreach (GameObject side in sector.GetComponent<GetPolygons>().Sides)
                {
                    Physics.IgnoreCollision(Player, side.GetComponent<MeshCollider>(), true);
                }
            }

            foreach (GameObject sector in CurrentSector.GetComponent<GetPolygons>().CheckPolygons)
            {
                foreach (GameObject side in sector.GetComponent<GetPolygons>().Sides)
                {
                    if (side.GetComponent<Side>().TargetSector == null)
                    {
                        Physics.IgnoreCollision(Player, side.GetComponent<MeshCollider>(), false);
                    }
                    else
                    {
                        Physics.IgnoreCollision(Player, side.GetComponent<MeshCollider>(), true);
                    }
                }
            }
        }
    }
}

