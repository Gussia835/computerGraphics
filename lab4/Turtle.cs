using System;
using System.Collections.Generic;
using System.Windows;

namespace lab4
{
    public class Turtle
    {
        public List<(Point A, Point B)> Segments { get; } = new List<(Point, Point)>();

        public void Interpret(string commands,
                              double angleDeg,
                              double initialDirectionDeg,
                              double stepLength,
                              double randomVariation,
                              Random rnd)
        {
            Segments.Clear();

            double x = 0, y = 0;
            double dirRad = initialDirectionDeg * Math.PI / 180.0;

            var stack = new Stack<(double x, double y, double dir)>();

            foreach (char c in commands)
            {
                switch (c)
                {
                    case 'F':
                    case 'G':
                        {
                            double nx = x + stepLength * Math.Cos(dirRad);
                            double ny = y + stepLength * Math.Sin(dirRad);
                            Segments.Add((new Point(x, y), new Point(nx, ny)));
                            x = nx;
                            y = ny;
                            break;
                        }

                    case 'f':
                        {
                            x += stepLength * Math.Cos(dirRad);
                            y += stepLength * Math.Sin(dirRad);
                            break;
                        }

                    case '+':
                        dirRad += GetDelta(angleDeg, randomVariation, rnd);
                        break;

                    case '-':
                        dirRad -= GetDelta(angleDeg, randomVariation, rnd);
                        break;

                    case '[':
                        stack.Push((x, y, dirRad));
                        break;

                    case ']':
                        if (stack.Count > 0)
                        {
                            var s = stack.Pop();
                            x = s.x;
                            y = s.y;
                            dirRad = s.dir;
                        }
                        break;
                }
            }
        }

        private static double GetDelta(double baseAngleDeg, double variation, Random rnd)
        {
            double deg = baseAngleDeg;
            if (variation > 0)
                deg += (rnd.NextDouble() * 2.0 - 1.0) * variation;
            return deg * Math.PI / 180.0;
        }
    }
}