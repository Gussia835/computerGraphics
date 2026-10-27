using System;
using System.Collections.Generic;
using lab6.Core;

namespace lab6.Core.Primitives
{
    public static class PolyhedronFactory
    {
        private const double Phi = 1.6180339887498949;

        public static Polyhedron CreateTetrahedron() => CreateFromFaces(GetTetrahedronVerts(), new[] { new[] { 0, 1, 2 }, new[] { 0, 3, 1 }, new[] { 0, 2, 3 }, new[] { 1, 3, 2 } });
        public static Polyhedron CreateCube() => CreateFromFaces(GetCubeVerts(), new[] { new[] { 0, 2, 3, 1 }, new[] { 4, 5, 7, 6 }, new[] { 0, 1, 5, 4 }, new[] { 2, 6, 7, 3 }, new[] { 0, 4, 6, 2 }, new[] { 1, 3, 7, 5 } });
        public static Polyhedron CreateOctahedron() => CreateFromFaces(GetOctahedronVerts(), new[] { new[] { 0, 2, 4 }, new[] { 2, 1, 4 }, new[] { 1, 3, 4 }, new[] { 3, 0, 4 }, new[] { 2, 0, 5 }, new[] { 1, 2, 5 }, new[] { 3, 1, 5 }, new[] { 0, 3, 5 } });

        public static Polyhedron CreateIcosahedron()
        {
            var v = new List<Point>
            {
                new Point(-1,  Phi,  0), 
                new Point( 1,  Phi,  0), 
                new Point(-1, -Phi,  0), 
                new Point( 1, -Phi,  0), 

                new Point( 0, -1,  Phi), 
                new Point( 0,  1,  Phi), 
                new Point( 0, -1, -Phi), 
                new Point( 0,  1, -Phi), 

                new Point( Phi,  0, -1), 
                new Point( Phi,  0,  1), 
                new Point(-Phi,  0, -1), 
                new Point(-Phi,  0,  1)  
            };
            NormalizeUnit(v);

            int[][] f = {
                new[]{0, 11, 5}, new[]{0, 5, 1}, new[]{0, 1, 7}, new[]{0, 7, 10}, new[]{0, 10, 11},
                new[]{1, 5, 9}, new[]{5, 11, 4}, new[]{11, 10, 2}, new[]{10, 7, 6}, new[]{7, 1, 8},
                new[]{3, 9, 4}, new[]{3, 4, 2}, new[]{3, 2, 6}, new[]{3, 6, 8}, new[]{3, 8, 9},
                new[]{4, 9, 5}, new[]{2, 4, 11}, new[]{6, 2, 10}, new[]{8, 6, 7}, new[]{9, 8, 1}
            };
            return Build(v, f);
        }

       

        private static List<Point> GetTetrahedronVerts()
        {
            var v = new List<Point> { new Point(1, 1, 1), new Point(1, -1, -1), new Point(-1, 1, -1), new Point(-1, -1, 1) };
            NormalizeUnit(v);
            return v;
        }

        private static List<Point> GetCubeVerts()
        {
            var v = new List<Point>();
            for (int i = 0; i < 8; i++) v.Add(new Point((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            NormalizeUnit(v);
            return v;
        }

        private static List<Point> GetOctahedronVerts()
        {
            var v = new List<Point> { new Point(1, 0, 0), new Point(-1, 0, 0), new Point(0, 1, 0), new Point(0, -1, 0), new Point(0, 0, 1), new Point(0, 0, -1) };
            NormalizeUnit(v);
            return v;
        }

        private static Polyhedron CreateFromFaces(List<Point> v, int[][] faces) => Build(v, faces);

        private static void NormalizeUnit(List<Point> v)
        {
            double r = 0;
            foreach (var p in v)
            {
                double len = Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
                r = Math.Max(r, len);
            }
            if (r < 1e-10) return;
            for (int i = 0; i < v.Count; i++)
            {
                v[i] = new Point(v[i].X / r, v[i].Y / r, v[i].Z / r);
            }
        }

        private static Polyhedron Build(List<Point> verts, int[][] faces)
        {
            var p = new Polyhedron { Vertices = verts };
            foreach (var fi in faces)
            {
                p.Faces.Add(new Polygon(fi));
            }
            return p;
        }
    }
}
