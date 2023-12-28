/******************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright (c) 2016 Bunny83
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 *****************************************************************************/
namespace Portals
{

    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Provides some Camera extension methods. ReadFrustomPlanes does the same as Unity's
    /// "GeometryUtility.CalculateFrustumPlanes" but instead of creating a new array each time
    /// you can pass in either an array or a List of Planes. The array has to have at least 6 elements.
    /// The List overload does not clear the list, it simply adds the 6 planes to the list.
    /// The order is:
    /// 0 - Right
    /// 1 - Left
    /// 2 - Top
    /// 3 - Bottom
    /// 4 - Far
    /// 5 - Near
    /// </summary>
    public static class CameraExtension
    {
        private static Plane FromVec4(Vector4 aVec)
        {
            Vector3 n = aVec;
            float l = n.magnitude;
            return new Plane(n / l, aVec.w / l);
        }

        public static void ReadFrustumPlanes(Plane[] planes, Matrix4x4 m)
        {
            if (planes == null || planes.Length < 6)
                return;
            var r0 = m.GetRow(0);
            var r1 = m.GetRow(1);
            var r2 = m.GetRow(2);
            var r3 = m.GetRow(3);
            planes[0] = FromVec4(r3 - r0); // Right
            planes[1] = FromVec4(r3 + r0); // Left
            planes[2] = FromVec4(r3 - r1); // Top
            planes[3] = FromVec4(r3 + r1); // Bottom
            planes[4] = FromVec4(r3 - r2); // Far
            planes[5] = FromVec4(r3 + r2); // Near
        }
        public static void ReadFrustumPlanes(List<Plane> planes, Matrix4x4 m)
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

        public static void ReadFrustumPlanes(this Camera cam, Plane[] planes)
        {
            ReadFrustumPlanes(planes, cam.projectionMatrix * cam.worldToCameraMatrix);
        }
        public static void ReadFrustumPlanes(this Camera cam, List<Plane> planes)
        {
            ReadFrustumPlanes(planes, cam.projectionMatrix * cam.worldToCameraMatrix);
        }
    }
}