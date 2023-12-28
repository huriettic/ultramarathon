namespace Portals
{
    using System.Collections.Generic;
    using UnityEngine;

    public class GetLines : MonoBehaviour
    {
        public List<Vector3> CWTop = new List<Vector3>();

        public List<Vector3> CWMiddle = new List<Vector3>();

        public List<Vector3> CWBottom = new List<Vector3>();

        public List<Vector3> CCWTop = new List<Vector3>();

        public List<Vector3> CCWMiddle = new List<Vector3>();

        public List<Vector3> CCWBottom = new List<Vector3>();

        public List<int> triangles = new List<int>();

        public List<Vector3> verticesout = new List<Vector3>();
    }
}
