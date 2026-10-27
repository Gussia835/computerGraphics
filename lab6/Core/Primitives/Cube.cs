using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab6.Core.Primitives
{
    public static class Cube
    {
        public static Polyhedron Create(double size = 1.0)
        {
            var poly = new Polyhedron();
            double s = size / 2;

            poly.Vertices.Add(new Point(-s, -s, -s));
            poly.Vertices.Add(new Point(s, -s, -s));
            poly.Vertices.Add(new Point(s, s, -s));
            poly.Vertices.Add(new Point(-s, s, -s));
            poly.Vertices.Add(new Point(-s, -s, s));
            poly.Vertices.Add(new Point(s, -s, s));
            poly.Vertices.Add(new Point(s, s, s));
            poly.Vertices.Add(new Point(-s, s, s));

            poly.Faces.Add(new Polygon(0, 1, 2, 3));
            poly.Faces.Add(new Polygon(5, 4, 7, 6));
            poly.Faces.Add(new Polygon(4, 0, 3, 7));
            poly.Faces.Add(new Polygon(1, 5, 6, 2));
            poly.Faces.Add(new Polygon(3, 2, 6, 7));
            poly.Faces.Add(new Polygon(4, 5, 1, 0));

            return poly;
        }
    }
}
