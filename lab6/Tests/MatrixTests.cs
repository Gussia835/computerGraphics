using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using lab6.Core;
using lab6.Core.Primitives;

namespace lab6.Tests
{
    public static class MatrixTests
    {
        private const double Epsilon = 1e-6;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ЗАПУСК ТЕСТОВ МАТРИЦ ===\n");

            int passed = 0;
            int total = 0;

            passed += TestIdentity() ? 1 : 0; total++;
            passed += TestTranslation() ? 1 : 0; total++;
            passed += TestScale() ? 1 : 0; total++;
            passed += TestReflectXY() ? 1 : 0; total++;
            passed += TestDoubleReflect() ? 1 : 0; total++;
            passed += TestMatrixMultiplication() ? 1 : 0; total++;
            passed += TestTranslationComposition() ? 1 : 0; total++;
            passed += TestScaleComposition() ? 1 : 0; total++;

            Console.WriteLine($"\n=== РЕЗУЛЬТАТ: {passed}/{total} тестов пройдено ===");
        }

        private static bool TestIdentity()
        {
            var point = new Point(1, 2, 3);
            var result = point.Transform(Matrix4x4.Identity);

            bool passed = Math.Abs(result.X - 1) < Epsilon &&
                         Math.Abs(result.Y - 2) < Epsilon &&
                         Math.Abs(result.Z - 3) < Epsilon;

            Console.WriteLine($"[Identity] (1,2,3) -> {result} - {(passed ? "PASS" : "FAIL")}");
            return passed;
        }

        private static bool TestTranslation()
        {
            var point = new Point(0, 0, 0);
            var matrix = Matrix4x4.Translate(5, 3, -2);
            var result = point.Transform(matrix);

            bool passed = Math.Abs(result.X - 5) < Epsilon &&
                         Math.Abs(result.Y - 3) < Epsilon &&
                         Math.Abs(result.Z + 2) < Epsilon;

            Console.WriteLine($"[Translation] (0,0,0) + T(5,3,-2) -> {result} - {(passed ? "PASS" : "FAIL")}");
            return passed;
        }

        private static bool TestScale()
        {
            var point = new Point(1, 2, 3);
            var matrix = Matrix4x4.Scale(2, 3, 0.5);
            var result = point.Transform(matrix);

            bool passed = Math.Abs(result.X - 2) < Epsilon &&
                         Math.Abs(result.Y - 6) < Epsilon &&
                         Math.Abs(result.Z - 1.5) < Epsilon;

            Console.WriteLine($"[Scale] (1,2,3) * S(2,3,0.5) -> {result} - {(passed ? "PASS" : "FAIL")}");
            return passed;
        }

        private static bool TestReflectXY()
        {
            var point = new Point(1, 2, 3);
            var matrix = Matrix4x4.ReflectXY();
            var result = point.Transform(matrix);

            bool passed = Math.Abs(result.X - 1) < Epsilon &&
                         Math.Abs(result.Y - 2) < Epsilon &&
                         Math.Abs(result.Z + 3) < Epsilon;

            Console.WriteLine($"[ReflectXY] (1,2,3) -> {result} - {(passed ? "PASS" : "FAIL")}");
            return passed;
        }

        private static bool TestDoubleReflect()
        {
            var point = new Point(1, 2, 3);
            var matrix = Matrix4x4.ReflectXY() * Matrix4x4.ReflectXY();
            var result = point.Transform(matrix);

            bool passed = Math.Abs(result.X - 1) < Epsilon &&
                         Math.Abs(result.Y - 2) < Epsilon &&
                         Math.Abs(result.Z - 3) < Epsilon;

            Console.WriteLine($"[DoubleReflect] ReflectXY*ReflectXY -> {result} - {(passed ? "PASS" : "FAIL")}");
            return passed;
        }

        private static bool TestMatrixMultiplication()
        {
            var t1 = Matrix4x4.Translate(1, 0, 0);
            var t2 = Matrix4x4.Translate(0, 2, 0);
            var combined = t1 * t2;

            var point = new Point(0, 0, 0);
            var result = point.Transform(combined);

            bool passed = Math.Abs(result.X - 1) < Epsilon &&
                         Math.Abs(result.Y - 2) < Epsilon &&
                         Math.Abs(result.Z) < Epsilon;

            Console.WriteLine($"[Multiplication] T(1,0,0)*T(0,2,0) -> {result} - {(passed ? "PASS" : "FAIL")}");
            return passed;
        }

        private static bool TestTranslationComposition()
        {
            var point = new Point(1, 1, 1);
            var t1 = Matrix4x4.Translate(2, 3, 4);
            var t2 = Matrix4x4.Translate(-1, -2, -3);
            var combined = t1 * t2;
            var result = point.Transform(combined);

            bool passed = Math.Abs(result.X - 2) < Epsilon &&
                         Math.Abs(result.Y - 2) < Epsilon &&
                         Math.Abs(result.Z - 2) < Epsilon;

            Console.WriteLine($"[TranslationComposition] (1,1,1) + T(2,3,4) + T(-1,-2,-3) -> {result} - {(passed ? "PASS" : "FAIL")}");
            return passed;
        }

        private static bool TestScaleComposition()
        {
            var point = new Point(1, 2, 3);
            var s1 = Matrix4x4.Scale(2, 2, 2);
            var s2 = Matrix4x4.Scale(0.5, 0.5, 0.5);
            var combined = s1 * s2;
            var result = point.Transform(combined);

            bool passed = Math.Abs(result.X - 1) < Epsilon &&
                         Math.Abs(result.Y - 2) < Epsilon &&
                         Math.Abs(result.Z - 3) < Epsilon;

            Console.WriteLine($"[ScaleComposition] S(2,2,2)*S(0.5,0.5,0.5) -> {result} - {(passed ? "PASS" : "FAIL")}");
            return passed;
        }
    }
}
