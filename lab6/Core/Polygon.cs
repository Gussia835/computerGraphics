using System;

namespace lab6.Core
{
    public class Polygon
    {
        public int[] VertexIndices { get; set; }
        public (double u, double v)[]? TexCoords { get; set; }

        public Polygon(params int[] indices)
        {
            VertexIndices = indices;
            AssignDefaultUV(indices.Length);
        }

        public void AssignDefaultUV(int count)
        {
            if (count == 4)
            {
                TexCoords = new[] { (0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0) };
            }
            else if (count == 3)
            {
                TexCoords = new[] { (0.5, 0.0), (1.0, 1.0), (0.0, 1.0) };
            }
            else
            {
                TexCoords = new (double u, double v)[count];
                for (int i = 0; i < count; i++)
                {
                    double angle = 2.0 * Math.PI * i / count;
                    TexCoords[i] = (0.5 + 0.5 * Math.Cos(angle), 0.5 + 0.5 * Math.Sin(angle));
                }
            }
        }
    }
}
