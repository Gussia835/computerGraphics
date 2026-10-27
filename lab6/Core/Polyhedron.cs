using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab6.Core
{
    public class Polyhedron
    {
        public List<Point> Vertices { get; set; }
        public List<Polygon> Faces { get; set; }

        public Polyhedron()
        {
            Vertices = new List<Point>();
            Faces = new List<Polygon>();
        }

        public void ApplyTransform(Matrix4x4 matrix)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i] = Vertices[i].Transform(matrix);
            }
        }

        public Polyhedron Clone()
        {
            var clone = new Polyhedron();
            clone.Vertices.AddRange(Vertices);
            clone.Faces.AddRange(Faces);
            return clone;
        }
    }
}
