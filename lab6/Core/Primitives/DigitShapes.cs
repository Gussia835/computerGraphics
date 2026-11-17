using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab6.Core.Primitives
{
    public static class DigitShapes
    {
        // Каждая цифра — список линий (x1, y1, x2, y2) в локальных координатах [-1, 1]
        public static List<(double x1, double y1, double x2, double y2)> GetDigit(int digit)
        {
            switch (digit)
            {
                case 0:
                    return new List<(double, double, double, double)>
            {
                (-0.5, -0.8, 0.5, -0.8),   // Верх
                (0.5, -0.8, 0.5, 0.8),     // Право
                (0.5, 0.8, -0.5, 0.8),     // Низ
                (-0.5, 0.8, -0.5, -0.8),   // Лево
            };
                case 1:
                    return new List<(double, double, double, double)>
            {
                (0, -0.8, 0, 0.8),         // Вертикальная линия
            };
                case 2:
                    return new List<(double, double, double, double)>
            {
                (-0.5, -0.8, 0.5, -0.8),   // Верх
                (0.5, -0.8, 0.5, 0),       // Право верх
                (0.5, 0, -0.5, 0),         // Середина
                (-0.5, 0, -0.5, 0.8),      // Лево низ
                (-0.5, 0.8, 0.5, 0.8),     // Низ
            };
                case 3:
                    return new List<(double, double, double, double)>
            {
                (-0.5, -0.8, 0.5, -0.8),   // Верх
                (0.5, -0.8, 0.5, 0.8),     // Право
                (0.5, 0, -0.5, 0),         // Середина
                (-0.5, 0.8, 0.5, 0.8),     // Низ
            };
                case 4:
                    return new List<(double, double, double, double)>
            {
                (-0.5, -0.8, -0.5, 0),     // Лево верх
                (-0.5, 0, 0.5, 0),         // Середина
                (0.5, -0.8, 0.5, 0.8),     // Право
            };
                case 5:
                    return new List<(double, double, double, double)>
            {
                (-0.5, -0.8, 0.5, -0.8),   // Верх
                (-0.5, -0.8, -0.5, 0),     // Лево верх
                (-0.5, 0, 0.5, 0),         // Середина
                (0.5, 0, 0.5, 0.8),        // Право низ
                (0.5, 0.8, -0.5, 0.8),     // Низ
            };
                case 6:
                    return new List<(double, double, double, double)>
            {
                (-0.5, -0.8, 0.5, -0.8),   // Верх
                (-0.5, -0.8, -0.5, 0.8),   // Лево
                (-0.5, 0.8, 0.5, 0.8),     // Низ
                (0.5, 0.8, 0.5, 0),        // Право низ
                (0.5, 0, -0.5, 0),         // Середина
            };
                case 7:
                    return new List<(double, double, double, double)>
            {
                (-0.5, -0.8, 0.5, -0.8),   // Верх
                (0.5, -0.8, 0, 0.8),       // Диагональ
            };
                case 8:
                    return new List<(double, double, double, double)>
            {
                (-0.5, -0.8, 0.5, -0.8),   // Верх
                (0.5, -0.8, 0.5, 0.8),     // Право
                (0.5, 0.8, -0.5, 0.8),     // Низ
                (-0.5, 0.8, -0.5, -0.8),   // Лево
                (-0.5, 0, 0.5, 0),         // Середина
            };
                case 9:
                    return new List<(double, double, double, double)>
            {
                (-0.5, -0.8, 0.5, -0.8),   // Верх
                (0.5, -0.8, 0.5, 0.8),     // Право
                (0.5, 0.8, -0.5, 0.8),     // Низ
                (-0.5, 0.8, -0.5, 0),      // Лево низ
                (-0.5, 0, 0.5, 0),         // Середина
            };
                default: return new List<(double, double, double, double)>();
            }
        }
    }
}
