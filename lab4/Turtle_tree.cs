using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace lab4
{
    public class Segment
    {
        public Point A;
        public Point B;
        public double Thickness;
        public Color Color;
    }

    public class Turtle_tree
    {
        public List<Segment> Segments { get; } = new List<Segment>();

        public Color BaseColor { get; set; } = Color.FromRgb(0x5D, 0x40, 0x37);
        public Color LeafColor { get; set; } = Color.FromRgb(0x27, 0xAE, 0x60);
        public double BaseThickness { get; set; } = 12.0;
        public double ThicknessRatio { get; set; } = 0.75;
        public int MaxDepth { get; set; } = 10;

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
            double thickness = BaseThickness;
            int depth = 0;

            var stack = new Stack<(double x, double y, double dir, double th, int d)>();

            foreach (char c in commands)
            {
                switch (c)
                {
                    case 'F':
                    case 'G':
                        {
                            double nx = x + stepLength * Math.Cos(dirRad);
                            double ny = y - stepLength * Math.Sin(dirRad);

                            double t = MaxDepth <= 0 ? 0 : (double)depth / MaxDepth;
                            if (t > 1) t = 1;

                            Segments.Add(new Segment
                            {
                                A = new Point(x, y),
                                B = new Point(nx, ny),
                                Thickness = Math.Max(0.5, thickness),
                                Color = Lerp(BaseColor, LeafColor, t)
                            });

                            x = nx;
                            y = ny;
                            break;
                        }

                    case 'f':
                        x += stepLength * Math.Cos(dirRad);
                        y -= stepLength * Math.Sin(dirRad);
                        break;

                    case '+':
                        dirRad += GetDelta(angleDeg, randomVariation, rnd);
                        break;

                    case '-':
                        dirRad -= GetDelta(angleDeg, randomVariation, rnd);
                        break;

                    case '[':
                        stack.Push((x, y, dirRad, thickness, depth));
                        depth++;
                        thickness *= ThicknessRatio;
                        break;

                    case ']':
                        if (stack.Count > 0)
                        {
                            var s = stack.Pop();
                            x = s.x;
                            y = s.y;
                            dirRad = s.dir;
                            thickness = s.th;
                            depth = s.d;
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

        private static Color Lerp(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            byte r = (byte)(a.R + (b.R - a.R) * t);
            byte g = (byte)(a.G + (b.G - a.G) * t);
            byte bl = (byte)(a.B + (b.B - a.B) * t);
            return Color.FromRgb(r, g, bl);
        }
    }
}