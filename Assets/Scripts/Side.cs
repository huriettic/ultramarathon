namespace Portals
{
    using System.Collections.Generic;
    using UnityEngine;

    public class Side : MonoBehaviour
    {
        public GameObject TargetSector;

        public List<Vector3> vertices = new List<Vector3>();

        public List<Vector3> verticesout = new List<Vector3>();

        public List<int> triangles = new List<int>();

        public List<Plane> Planes = new List<Plane>();

        public Plane plane;

        public void CreateClippingPlanes(List<Vector3> aVertices, List<Plane> aList, Vector3 aViewPos)
        {
            int count = aVertices.Count;
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                var p1 = aVertices[i];
                var p2 = aVertices[j];
                var n = Vector3.Cross(p1 - p2, aViewPos - p2);
                var l = n.magnitude;
                if (l < 0.01f)
                    continue;
                aList.Add(new Plane(n / l, aViewPos));
            }
        }

        public List<Vector3> ClippingPlanes(List<Vector3> invertices, List<Plane> aPlanes, float aEpsilon = 0.001f)
        {
            for (int e = 0; e < aPlanes.Count; e++)
            {
                List<float> m_Dists = new List<float>();
                List<Vector3> outvertices = new List<Vector3>();
                int count = invertices.Count;
                if (m_Dists.Capacity < count)
                    m_Dists.Capacity = count;
                for (int i = 0; i < count; i++)
                {
                    Vector3 p = invertices[i];
                    m_Dists.Add(aPlanes[e].GetDistanceToPoint(p));
                }
                for (int i = 0; i < count; i++)
                {
                    int j = (i + 1) % count;
                    float d1 = m_Dists[i];
                    float d2 = m_Dists[j];
                    Vector3 p1 = invertices[i];
                    Vector3 p2 = invertices[j];
                    bool split = d1 > aEpsilon;
                    if (split)
                    {
                        outvertices.Add(p1);
                    }
                    else if (d1 > -aEpsilon)
                    {
                        // point on clipping plane so just keep it
                        outvertices.Add(p1);
                        continue;
                    }
                    // both points are on the same side of the plane
                    if ((d2 > -aEpsilon && split) || (d2 < aEpsilon && !split))
                    {
                        continue;
                    }
                    float d = d1 / (d1 - d2);
                    outvertices.Add(p1 + (p2 - p1) * d);
                }
                invertices = outvertices;
            }
            return invertices;
        }
    }
}

