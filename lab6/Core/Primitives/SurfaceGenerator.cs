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

            // ШАГ 1: Генерация вершин (точек)
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