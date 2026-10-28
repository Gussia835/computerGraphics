using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab6.Core
{
    public struct Matrix4x4
    {
        public double M11, M12, M13, M14;
        public double M21, M22, M23, M24;
        public double M31, M32, M33, M34;
        public double M41, M42, M43, M44;

        public static Matrix4x4 Identity => new Matrix4x4
        {
            M11 = 1,
            M22 = 1,
            M33 = 1,
            M44 = 1
        };

        public static Matrix4x4 Translate(double tx, double ty, double tz)
        {
            return new Matrix4x4
            {
                M11 = 1,
                M22 = 1,
                M33 = 1,
                M44 = 1,
                M41 = tx,
                M42 = ty,
                M43 = tz
            };
        }

        public static Matrix4x4 Scale(double sx, double sy, double sz)
        {
            return new Matrix4x4
            {
                M11 = sx,
                M22 = sy,
                M33 = sz,
                M44 = 1
            };
        }

        public static Matrix4x4 ReflectXY()
        {
            return new Matrix4x4
            {
                M11 = 1,
                M22 = 1,
                M33 = -1,
                M44 = 1
            };
        }

        public static Matrix4x4 ReflectXZ()
        {
            return new Matrix4x4
            {
                M11 = 1,
                M22 = -1,
                M33 = 1,
                M44 = 1
            };
        }

        public static Matrix4x4 ReflectYZ()
        {
            return new Matrix4x4
            {
                M11 = -1,
                M22 = 1,
                M33 = 1,
                M44 = 1
            };
        }

        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            return new Matrix4x4
            {
                M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
                M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
                M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
                M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,

                M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
                M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
                M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
                M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,

                M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
                M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
                M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
                M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,

                M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
                M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
                M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
                M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44
            };
        }

        public bool Equals(Matrix4x4 other, double epsilon = 1e-6)
        {
            return Math.Abs(M11 - other.M11) < epsilon &&
                   Math.Abs(M12 - other.M12) < epsilon &&
                   Math.Abs(M13 - other.M13) < epsilon &&
                   Math.Abs(M14 - other.M14) < epsilon &&
                   Math.Abs(M21 - other.M21) < epsilon &&
                   Math.Abs(M22 - other.M22) < epsilon &&
                   Math.Abs(M23 - other.M23) < epsilon &&
                   Math.Abs(M24 - other.M24) < epsilon &&
                   Math.Abs(M31 - other.M31) < epsilon &&
                   Math.Abs(M32 - other.M32) < epsilon &&
                   Math.Abs(M33 - other.M33) < epsilon &&
                   Math.Abs(M34 - other.M34) < epsilon &&
                   Math.Abs(M41 - other.M41) < epsilon &&
                   Math.Abs(M42 - other.M42) < epsilon &&
                   Math.Abs(M43 - other.M43) < epsilon &&
                   Math.Abs(M44 - other.M44) < epsilon;
        }

        public static Matrix4x4 RotateX(double angleRad)
        {
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            return new Matrix4x4
            {
                M11 = 1,
                M22 = cos,
                M23 = -sin,
                M32 = sin,
                M33 = cos,
                M44 = 1
            };
        }

        public static Matrix4x4 RotateY(double angleRad)
        {
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            return new Matrix4x4
            {
                M11 = cos,
                M13 = -sin,
                M22 = 1,
                M31 = sin,
                M33 = cos,
                M44 = 1
            };
        }

        public static Matrix4x4 RotateZ(double angleRad)
        {
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            return new Matrix4x4
            {
                M11 = cos,
                M12 = -sin,
                M21 = sin,
                M22 = cos,
                M33 = 1,
                M44 = 1
            };
        }

        public static Matrix4x4 RotateAroundAxis(Point axisPoint1, Point axisPoint2, double angleRad)
        {
            double dx = axisPoint2.X - axisPoint1.X;
            double dy = axisPoint2.Y - axisPoint1.Y;
            double dz = axisPoint2.Z - axisPoint1.Z;
            double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 1e-10) return Identity;

            double ux = dx / len, uy = dy / len, uz = dz / len;
            double c = Math.Cos(angleRad), s = Math.Sin(angleRad);
            double t = 1 - c;

            var r = new Matrix4x4
            {
                M11 = t * ux * ux + c,
                M12 = t * ux * uy + s * uz,
                M13 = t * ux * uz - s * uy,
                M21 = t * ux * uy - s * uz,
                M22 = t * uy * uy + c,
                M23 = t * uy * uz + s * ux,
                M31 = t * ux * uz + s * uy,
                M32 = t * uy * uz - s * ux,
                M33 = t * uz * uz + c,
                M44 = 1
            };

            return Translate(-axisPoint1.X, -axisPoint1.Y, -axisPoint1.Z)
                 * r
                 * Translate(axisPoint1.X, axisPoint1.Y, axisPoint1.Z);
        }
    }
}