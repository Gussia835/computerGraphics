using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab6.Core.Primitives
{
    public static class Tetrahedron
    {
        public static Polyhedron Create(double size = 1.0)
        {
            var poly = new Polyhedron();

            poly.Vertices.Add(new Point(0, size, 0));
            poly.Vertices.Add(new Point(-size, -size, -size));
            poly.Vertices.Add(new Point(size, -size, -size));
            poly.Vertices.Add(new Point(0, -size, size));

            poly.Faces.Add(new Polygon(0, 1, 2));
            poly.Faces.Add(new Polygon(0, 2, 3));
            poly.Faces.Add(new Polygon(0, 3, 1));
            poly.Faces.Add(new Polygon(1, 3, 2));

            return poly;
        }
    }
}
