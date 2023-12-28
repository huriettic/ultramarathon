namespace Portals
{
    using System.Collections.Generic;
    using UnityEngine;

    public class GetPolygons : MonoBehaviour
    {
        public List<Vector3> ceilingverts = new List<Vector3>();

        public List<int> ceilingtri = new List<int>();

        public List<Vector3> floorverts = new List<Vector3>();

        public List<int> floortri = new List<int>();

        public Vector3 CenterPoint;

        public List<float> X = new List<float>();

        public List<float> Y = new List<float>();

        public List<GameObject> GetLines = new List<GameObject>();

        public List<Plane> Planes = new List<Plane>();

        public List<GameObject> CheckPolygons = new List<GameObject>();

        public List<GameObject> Sides = new List<GameObject>();
    }
}
