/*
 * This file is a modified version of OBJExporter from Weland.
 * Modifications made by huriettic on 2026-03-15.
 * Original code licensed under the GPL.
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Weland;

public class MarathonMeshBuilder : MonoBehaviour
{
    [Serializable]
    public class Face
    {
        public int[] indices;
    }

    [Serializable]
    public class EndpointIndex
    {
        public short height;
        public int vertexIndex;
    }

    [Serializable]
    public class EndpointLists
    {
        public List<EndpointIndex> listOfEndpoints = new List<EndpointIndex>();
    }

    const double Scale = 2.5;

    public string LevelName = "Hyper Cube";
    public int LevelNumber = 0;

    Level level;

    public List<Vector3> coplanarVertices = new List<Vector3>();
    public List<int> coplanarTriangles = new List<int>();
    public List<EndpointLists> endpointVertices = new List<EndpointLists>();
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();   
    public List<Face> faces = new List<Face>();

    void Start()
    {
        level = LoadLevel(LevelName, LevelNumber);
        Mesh mesh = BuildMeshFromLevel();

        var mgo = new GameObject("Marathon Mesh");
        var mgof = mgo.AddComponent<MeshFilter>();
        var mgor = mgo.AddComponent<MeshRenderer>();

        mgof.sharedMesh = mesh;
        mgor.sharedMaterial = new Material(Shader.Find("Standard"));
        mgor.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public Mesh BuildMeshFromLevel()
    {
        faces.Clear();
        vertices.Clear();
        triangles.Clear();
        coplanarVertices.Clear();
        coplanarTriangles.Clear();
        endpointVertices.Clear();

        for (int i = 0; i < level.Endpoints.Count; i++)
        {
            endpointVertices.Add(new EndpointLists());
        }

        foreach (Polygon p in level.Polygons)
        {
            if (p.CeilingHeight > p.FloorHeight)
            {
                if (p.FloorTransferMode != 9)
                {
                    faces.Add(new Face { indices = FloorFace(p) });
                } 

                if (p.CeilingTransferMode != 9)
                {
                    faces.Add(new Face { indices = CeilingFace(p) });
                }    

                for (int i = 0; i < p.VertexCount; ++i)
                {
                    InsertLineFaces(level.Lines[p.LineIndexes[i]], p);
                }   
            }
        }

        var mesh = new Mesh();
        
        foreach (Face f in faces)
        {
            if (f.indices.Length == 3)
            {
                triangles.Add(f.indices[0]);
                triangles.Add(f.indices[1]);
                triangles.Add(f.indices[2]);
            }
            else if (f.indices.Length == 4)
            {
                triangles.Add(f.indices[0]);
                triangles.Add(f.indices[1]);
                triangles.Add(f.indices[2]);

                triangles.Add(f.indices[0]);
                triangles.Add(f.indices[2]);
                triangles.Add(f.indices[3]);
            }
            else if (f.indices.Length > 4)
            {
                for (int i = 1; i < f.indices.Length - 1; i++)
                {
                    triangles.Add(f.indices[0]);
                    triangles.Add(f.indices[i]);
                    triangles.Add(f.indices[i + 1]);
                }
            }
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            coplanarVertices.Add(vertices[triangles[i]]);
            coplanarTriangles.Add(i);
        }

        mesh.SetVertices(coplanarVertices);
        mesh.SetTriangles(coplanarTriangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    int GetVertexIndex(int endpointIndex, short height)
    {
        List<EndpointIndex> list = endpointVertices[endpointIndex].listOfEndpoints;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].height == height)
            {
                return list[i].vertexIndex;
            } 
        }

        Point p = level.Endpoints[endpointIndex];

        Vector3 v = new Vector3((float)(World.ToDouble(p.X) * Scale), (float)(World.ToDouble(height) * Scale), (float)-(World.ToDouble(p.Y) * Scale));

        int newIndex = vertices.Count;
        vertices.Add(v);

        list.Add(new EndpointIndex
        {
            height = height,
            vertexIndex = newIndex
        });

        return newIndex;
    }

    int[] FloorFace(Polygon p)
    {
        int[] result = new int[p.VertexCount];
        for (int i = 0; i < p.VertexCount; ++i)
        {
            result[i] = GetVertexIndex(p.EndpointIndexes[i], p.FloorHeight);
        }
            
        return result;
    }

    int[] CeilingFace(Polygon p)
    {
        int[] result = new int[p.VertexCount];
        for (int i = 0; i < p.VertexCount; ++i)
        {
            result[i] = GetVertexIndex(p.EndpointIndexes[i], p.CeilingHeight);
        } 

        Array.Reverse(result);

        return result;
    }

    int[] BuildFace(int left, int right, short ceiling, short floor)
    {
        int[] result = new int[4];
        result[0] = GetVertexIndex(left, floor);
        result[1] = GetVertexIndex(right, floor);
        result[2] = GetVertexIndex(right, ceiling);
        result[3] = GetVertexIndex(left, ceiling);
        return result;
    }

    void InsertLineFaces(Line line, Polygon p)
    {
        int left;
        int right;
        Polygon opposite = null;
        Side side = null;

        if (line.ClockwisePolygonOwner != -1 && level.Polygons[line.ClockwisePolygonOwner] == p)
        {
            left = line.EndpointIndexes[0];
            right = line.EndpointIndexes[1];

            if (line.CounterclockwisePolygonOwner != -1)
            {
                opposite = level.Polygons[line.CounterclockwisePolygonOwner];
            }  

            if (line.ClockwisePolygonSideIndex != -1)
            {
                side = level.Sides[line.ClockwisePolygonSideIndex];
            }  
        }
        else
        {
            left = line.EndpointIndexes[1];
            right = line.EndpointIndexes[0];

            if (line.ClockwisePolygonOwner != -1)
            {
                opposite = level.Polygons[line.ClockwisePolygonOwner];
            }
                
            if (line.CounterclockwisePolygonSideIndex != -1)
            {
                side = level.Sides[line.CounterclockwisePolygonSideIndex];
            }   
        }

        bool landscapeTop = false;
        bool landscapeBottom = false;

        if (side != null)
        {
            if (side.Type == SideType.Low)
            {
                if (side.PrimaryTransferMode == 9)
                {
                    landscapeBottom = true;
                } 
            }
            else
            {
                if (side.PrimaryTransferMode == 9)
                {
                    landscapeTop = true;
                }
                    
                if (side.SecondaryTransferMode == 9)
                {
                    landscapeBottom = true;
                }   
            }
        }

        if (opposite == null || (opposite.FloorHeight > p.CeilingHeight || opposite.CeilingHeight < p.FloorHeight))
        {
            if (!landscapeTop)
            {
                faces.Add(new Face { indices = BuildFace(left, right, p.FloorHeight, p.CeilingHeight) } );
            }  
        }
        else
        {
            if (opposite.FloorHeight > p.FloorHeight)
            {
                if (!landscapeBottom)
                {
                    faces.Add(new Face { indices = BuildFace(left, right, p.FloorHeight, opposite.FloorHeight) } );
                }  
            }
            if (opposite.CeilingHeight < p.CeilingHeight)
            {
                if (!landscapeTop)
                {
                    faces.Add(new Face { indices = BuildFace(left, right, opposite.CeilingHeight, p.CeilingHeight) } );
                }  
            }
        }
    }

    public static Level LoadLevel(string name, int levelNumber)
    {
        MapFile map = new MapFile();
        Level level = new Level();

        try
        {
            map.Load(Path.Combine(Application.streamingAssetsPath, name + ".sceA"));
            Debug.Log("Map loaded successfully!");
        }
        catch (Exception exit)
        {
            Debug.LogError("Failed to load Map: " + exit.Message);
        }

        try
        {
            level.Load(map.Directory[levelNumber]);
            Debug.Log("Level loaded successfully!");
        }
        catch (Exception exit)
        {
            Debug.LogError("Failed to load level: " + exit.Message);
        }

        return level;
    }
}
