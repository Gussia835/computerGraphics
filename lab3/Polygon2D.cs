using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace lab3
{
    internal class Polygon2D
    {
        public List<Point> Vertices { get; set; } = new List<Point>();
        public bool IsClosed => Vertices.Count >= 3;
        public string Name { get; set; }

        // Центр масс (среднее арифметическое вершин)
        public Point CenterOfMass
        {
            get
            {
                if (Vertices.Count == 0) return new Point(0, 0);
                double cx = Vertices.Average(v => v.X);
                double cy = Vertices.Average(v => v.Y);
                return new Point(cx, cy);
            }
        }

       
        // Применяет аффинное преобразование (матрицу 3×3) ко всем вершинам.
        public void ApplyTransform(Matrix3x3 matrix)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                var (nx, ny) = matrix.TransformPoint(Vertices[i].X, Vertices[i].Y);
                Vertices[i] = new Point(nx, ny);
            }
        }

        public Polygon2D Clone()
        {
            return new Polygon2D
            {
                Vertices = Vertices.Select(v => new Point(v.X, v.Y)).ToList(),
                Name = Name
            };
        }
    }
}
