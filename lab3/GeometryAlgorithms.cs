using System;
using System.Collections.Generic;
using System.Windows;

namespace lab3
{
    internal static class GeometryAlgorithms
    {
        public static Point? FindEdgeIntersection(Point a, Point b, Point c, Point d)
        {
            double dcX = d.X - c.X;
            double dcY = d.Y - c.Y;
            double nX = -dcY;
            double nY = dcX;

            double baX = b.X - a.X;
            double baY = b.Y - a.Y;
            double denominator = nX * baX + nY * baY;

            if (Math.Abs(denominator) < 1e-9) return null;

            double acX = a.X - c.X;
            double acY = a.Y - c.Y;
            double t = -(nX * acX + nY * acY) / denominator;

            double n1X = -baY;
            double n1Y = baX;
            double caX = c.X - a.X;
            double caY = c.Y - a.Y;
            double denominatorU = n1X * dcX + n1Y * dcY;

            if (Math.Abs(denominatorU) < 1e-9) return null;
            double u = -(n1X * caX + n1Y * caY) / denominatorU;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                return new Point(a.X + t * baX, a.Y + t * baY);
            }
            return null;
        }


        public static string ClassifyPointRelativeToEdge(Point p, Point edgeStart, Point edgeEnd)
        {
            double xa = edgeEnd.X - edgeStart.X;
            double ya = edgeEnd.Y - edgeStart.Y;
            double xb = p.X - edgeStart.X;
            double yb = p.Y - edgeStart.Y;

            double crossProduct = yb * xa - xb * ya;

            if (crossProduct > 1e-5) return "Слева";
            if (crossProduct < -1e-5) return "Справа";
            return "На ребре";
        }

        public static bool IsPointInsidePolygon(Point p, List<Point> vertices)
        {
            if (vertices.Count < 3) return false;

            bool inside = false;
            int j = vertices.Count - 1;

            for (int i = 0; i < vertices.Count; i++)
            {
                if ((vertices[i].Y > p.Y) != (vertices[j].Y > p.Y))
                {
                    double intersectX = (vertices[j].X - vertices[i].X) * (p.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) + vertices[i].X;
                    if (p.X < intersectX)
                    {
                        inside = !inside;
                    }
                }
                j = i;
            }
            return inside;
        }
    }
}
