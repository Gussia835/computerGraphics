using System;
using System.Collections.Generic;

namespace lab6.Core.Primitives
{
    public static class SurfaceOfRevolutionGenerator
    {
        public static Polyhedron CreateRevolution(
            List<Point> generatrix,
            int axis,
            int divisions)
        {
            if (generatrix == null || generatrix.Count < 2)
                throw new ArgumentException("Образующая должна содержать минимум 2 точки.");

            if (divisions < 3)
                throw new ArgumentException("Количество разбиений должно быть не менее 3.");

            var pts = new List<Point>(generatrix);
            if (pts.Count > 1)
            {
                var first = pts[0];
                var last = pts[pts.Count - 1];
                if (Math.Abs(first.X - last.X) < 1e-6 &&
                    Math.Abs(first.Y - last.Y) < 1e-6 &&
                    Math.Abs(first.Z - last.Z) < 1e-6)
                {
                    pts.RemoveAt(pts.Count - 1);
                }
            }

            var poly = new Polyhedron();
            double angleStep = 2.0 * Math.PI / divisions;

            for (int i = 0; i < divisions; i++)
            {
                double angle = i * angleStep;

                Matrix4x4 rotationMatrix = Matrix4x4.Identity;
                if (axis == 0) rotationMatrix = Matrix4x4.RotateX(angle);
                else if (axis == 1) rotationMatrix = Matrix4x4.RotateY(angle);
                else if (axis == 2) rotationMatrix = Matrix4x4.RotateZ(angle);

                foreach (var pt in pts)
                {
                    poly.Vertices.Add(pt.Transform(rotationMatrix));
                }
            }

            int ptsPerRing = pts.Count;

            for (int i = 0; i < divisions; i++)
            {
                int nextRing = (i + 1) % divisions;

                for (int j = 0; j < ptsPerRing - 1; j++)
                {
                    int p1 = i * ptsPerRing + j;
                    int p2 = i * ptsPerRing + j + 1;
                    int p3 = nextRing * ptsPerRing + j + 1;
                    int p4 = nextRing * ptsPerRing + j;

                    poly.Faces.Add(new Polygon(p1, p4, p3, p2));
                }
            }

            return poly;
        }
    }
}