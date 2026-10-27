using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace lab6.Core
{
    public struct Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }

        public Point(double x, double y, double z, double w = 1.0)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Point Transform(Matrix4x4 matrix)
        {
            return new Point(
                X * matrix.M11 + Y * matrix.M21 + Z * matrix.M31 + W * matrix.M41,
                X * matrix.M12 + Y * matrix.M22 + Z * matrix.M32 + W * matrix.M42,
                X * matrix.M13 + Y * matrix.M23 + Z * matrix.M33 + W * matrix.M43,
                X * matrix.M14 + Y * matrix.M24 + Z * matrix.M34 + W * matrix.M44
            );
        }

        public Point Normalize()
        {
            if (System.Math.Abs(W) < 1e-10) return this;
            return new Point(X / W, Y / W, Z / W, 1.0);
        }

        public double DistanceTo(Point other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";
    }
}

