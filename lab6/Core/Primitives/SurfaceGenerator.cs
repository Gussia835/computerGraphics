using System;
using System.Collections.Generic;
using lab6.Core;

namespace lab6.Core.Primitives
{

    // Генератор математических поверхностей z = f(x,y)
    public static class SurfaceGenerator
    {

        // Создает многогранник в виде поверхности по заданной функции
        public static Polyhedron CreateSurface(
            Func<double, double, double> function, 
            double xMin, double xMax,              
            double yMin, double yMax,            
            int stepsX, int stepsY)                
        {
            var poly = new Polyhedron();

            stepsX = Math.Max(1, stepsX);
            stepsY = Math.Max(1, stepsY);

            double stepX = (xMax - xMin) / stepsX;
            double stepY = (yMax - yMin) / stepsY;

            int pointsPerRow = stepsX + 1; 

            // Генерация вершин (точек)
            for (int j = 0; j <= stepsY; j++) 
            {
                double y = yMin + j * stepY;

                for (int i = 0; i <= stepsX; i++) 
                {
                    double x = xMin + i * stepX;
                    double z = function(x, y); 

                    poly.Vertices.Add(new Point(x, y, z));
                }
            }

            double cx = 0;
            double cy = 0;
            double cz = 0;

            foreach (Point v in poly.Vertices) {
                cx += v.X;
                cy += v.Y;
                cz += v.Z;
            }

            int n = poly.Vertices.Count;
            cx /= n;
            cy /= n;
            cz /= n;

            double max_radius = 0;
            for (int i =0; i < n; ++i)
            {
                Point v = poly.Vertices[i];
                Point shifted = new Point(v.X - cx, v.Y - cy, v.Z - cz);
                poly.Vertices[i] = shifted;

                double radius = Math.Sqrt(shifted.X * shifted.X + shifted.Y*shifted.Y + shifted.Z * shifted.Z);

                max_radius = Math.Max(max_radius, radius);
            }

            double scale = max_radius > 0.001 ? 1.5 / max_radius : 1.0;

            for (int i = 0; i < n; ++i)
            {
                Point currPoint = poly.Vertices[i];
                poly.Vertices[i] = new Point(currPoint.X * scale, currPoint.Y * scale, currPoint.Z * scale);
            }

            //Создание граней (полигонов)
            for (int j = 0; j < stepsY; j++)
            {
                for (int i = 0; i < stepsX; i++)
                {
                    int topLeft = j * pointsPerRow + i;
                    int topRight = topLeft + 1;
                    int bottomLeft = (j + 1) * pointsPerRow + i;
                    int bottomRight = bottomLeft + 1;

                    poly.Faces.Add(new Polygon(topLeft, topRight, bottomRight, bottomLeft));
                }
            }

            return poly;
        }
    }
}