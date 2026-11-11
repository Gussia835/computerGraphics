using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var poly = new Polyhedron();
            double angleStep = 2.0 * Math.PI / divisions;

            for (int i = 0; i < divisions; i++)
            {
                double angle = i * angleStep;

                Matrix4x4 rotationMatrix = Matrix4x4.Identity;
                if (axis == 0) rotationMatrix = Matrix4x4.RotateX(angle);
                else if (axis == 1) rotationMatrix = Matrix4x4.RotateY(angle);
                else if (axis == 2) rotationMatrix = Matrix4x4.RotateZ(angle);

                foreach (var pt in generatrix)
                {
                    poly.Vertices.Add(pt.Transform(rotationMatrix));
                }
            }

            int ptsPerRing = generatrix.Count;

            for (int i = 0; i < divisions; i++)
            {
                int nextRing = (i + 1) % divisions;

                for (int j = 0; j < ptsPerRing - 1; j++)
                {
                    int p1 = i * ptsPerRing + j;
                    int p2 = i * ptsPerRing + j + 1;
                    int p3 = nextRing * ptsPerRing + j + 1;
                    int p4 = nextRing * ptsPerRing + j;

                    poly.Faces.Add(new Polygon(p1, p2, p3, p4));
                }
            }

            return poly;
        }
    }
}
