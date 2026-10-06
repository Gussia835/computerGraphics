using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab3
{
    internal class Matrix3x3
    {
        public double[,] Data = new double[3, 3];

        public Matrix3x3() { }

        public Matrix3x3(double[,] data)
        {
            if (data.GetLength(0) != 3 || data.GetLength(1) != 3) {
                throw new ArgumentException("Матрица должна быть 3×3");

                Array.Copy(data, Data, 9);
            }
        }

        // Единичная матрица 
        public static Matrix3x3 Identity()
        {
            return new Matrix3x3(new double[,]
            {
                { 1, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, 1 }
            });
        }

        // Умножение матриц
        public static Matrix3x3 Multiply(Matrix3x3 a, Matrix3x3 b)
        {
            Matrix3x3 res = new Matrix3x3();

            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        res.Data[i, j] = a.Data[i, k] * b.Data[k, j];
                    }
                }
            }

            return res;
        }

        // сдвиг на(dx, dy) 
        public (double x, double y) TransformPoint(double x, double y)
        {
            double rx = Data[0, 0] * x + Data[0, 1] * y + Data[0, 2];
            double ry = Data[1, 0] * x + Data[1, 1] * y + Data[1, 2];
            double rw = Data[2, 0] * x + Data[2, 1] * y + Data[2, 2];

            return (rx / rw, ry / rw);
        }

        // Поворот вокруг НАЧАЛА КООРДИНАТ на угол (в радианах)
        public static Matrix3x3 Translation(double dx, double dy)
        {
            return new Matrix3x3(new double[,]
            {
                { 1, 0, dx },
                { 0, 1, dy },
                { 0, 0,  1 }
            });
        }

        // Масштабирование относительно начала координат
        public static Matrix3x3 Rotation(double angleRad)
        {
            double c = Math.Cos(angleRad);
            double s = Math.Sin(angleRad);
            return new Matrix3x3(new double[,]
            {
                { c, -s, 0 },
                { s,  c, 0 },
                { 0,  0, 1 }
            });
        }

        // Поворот вокруг произвольной точки(cx, cy).
        // M = T(cx,cy) × R(θ) × T(−cx,−cy)
        public static Matrix3x3 Scaling(double sx, double sy)
        {
            return new Matrix3x3(new double[,]
            {
                { sx,  0, 0 },
                {  0, sy, 0 },
                {  0,  0, 1 }
            });
        }

        // асштабирование относительно произвольной точки (cx, cy).
        // M = T(cx,cy) × S(sx,sy) × T(−cx,−cy)

        public static Matrix3x3 RotationAroundPoint(double angleRad, double cx, double cy)
        {
            var tBack = Translation(cx, cy);
            var rot = Rotation(angleRad);
            var tTo = Translation(-cx, -cy);
            return Multiply(tBack, Multiply(rot, tTo));
        }

        public static Matrix3x3 ScalingAroundPoint(double sx, double sy, double cx, double cy)
        {
            var tBack = Translation(cx, cy);
            var sc = Scaling(sx, sy);
            var tTo = Translation(-cx, -cy);
            return Multiply(tBack, Multiply(sc, tTo));
        }
    }
}
