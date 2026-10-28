using System;
using System.Collections.Generic;
using lab6.Core;

namespace lab6.Core.Primitives
{
    public static class PolyhedronFactory
    {
        // Базовый метод для создания Куба (Гексаэдра)
        public static Polyhedron CreateCube()
        {
            var v = new List<Point>();
            // Генерируем 8 стандартных вершин куба
            for (int i = 0; i < 8; i++)
                v.Add(new Point((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));

            NormalizeUnit(v);

            int[][] f = {
                new[] { 0, 2, 3, 1 }, new[] { 4, 5, 7, 6 }, new[] { 0, 1, 5, 4 },
                new[] { 2, 6, 7, 3 }, new[] { 0, 4, 6, 2 }, new[] { 1, 3, 7, 5 }
            };
            return Build(v, f);
        }

        // ЛЕКЦИЯ, СЛАЙД 15: Тетраэдр строится вложенным в куб (выбираем 4 вершины через одну)
        public static Polyhedron CreateTetrahedron()
        {
            // Берем вершины готового куба
            var cube = CreateCube();
            var v = new List<Point>
            {
                cube.Vertices[7], // ( 1,  1,  1)
                cube.Vertices[1], // ( 1, -1, -1)
                cube.Vertices[2], // (-1,  1, -1)
                cube.Vertices[4]  // (-1, -1,  1)
            };
            NormalizeUnit(v);

            int[][] f = {
                new[] { 0, 1, 2 }, new[] { 0, 3, 1 },
                new[] { 0, 2, 3 }, new[] { 1, 3, 2 }
            };
            return Build(v, f);
        }

        // ЛЕКЦИЯ, СЛАЙД 16: Октаэдр строится по центрам 6 граней куба
        public static Polyhedron CreateOctahedron()
        {
            var cube = CreateCube();
            var v = new List<Point>();

            // Для каждой из 6 граней куба находим центр тяжести
            foreach (var face in cube.Faces)
            {
                double cx = 0, cy = 0, cz = 0;
                foreach (int idx in face.VertexIndices)
                {
                    cx += cube.Vertices[idx].X;
                    cy += cube.Vertices[idx].Y;
                    cz += cube.Vertices[idx].Z;
                }
                v.Add(new Point(cx / 4.0, cy / 4.0, cz / 4.0));
            }
            NormalizeUnit(v);

            int[][] f = {
                new[] { 0, 2, 4 }, new[] { 2, 1, 4 }, new[] { 1, 3, 4 }, new[] { 3, 0, 4 },
                new[] { 2, 0, 5 }, new[] { 1, 2, 5 }, new[] { 3, 1, 5 }, new[] { 0, 3, 5 }
            };
            return Build(v, f);
        }

        // ЛЕКЦИЯ, СЛАЙД 17: Икосаэдр строится через вписанный цилиндр
        // Вершины лежат слоями на окружностях вдоль оси Z: две на полюсах и два кольца по 5 вершин
        public static Polyhedron CreateIcosahedron()
        {
            var v = new List<Point>();

            // Полюса на оси Z (0, 0, ±√5/2 по лекции)
            double zPole = Math.Sqrt(5) / 2.0;
            v.Add(new Point(0, 0, zPole));  // Северный полюс (индекс 0)
            v.Add(new Point(0, 0, -zPole)); // Южный полюс (индекс 1)

            // Радиус цилиндра R=1, высота слоев по Z = ±0.5
            double alpha = 2.0 * Math.PI / 5.0; // 72 градуса между вершинами в кольце

            // Верхнее кольцо (5 вершин)
            for (int i = 0; i < 5; i++)
            {
                double angle = i * alpha;
                v.Add(new Point(Math.Cos(angle), Math.Sin(angle), 0.5));
            }
            // Нижнее кольцо (5 вершин, сдвинуты на 36 градусов для чередования)
            for (int i = 0; i < 5; i++)
            {
                double angle = i * alpha + (alpha / 2.0);
                v.Add(new Point(Math.Cos(angle), Math.Sin(angle), -0.5));
            }

            NormalizeUnit(v);

            // Правильная топология граней для такой «цилиндрической» структуры
            int[][] f = {
                // Шапка вокруг северного полюса (0)
                new[] { 0, 2, 3 }, new[] { 0, 3, 4 }, new[] { 0, 4, 5 }, new[] { 0, 5, 6 }, new[] { 0, 6, 2 },
                // Дно вокруг южного полюса (1)
                new[] { 1, 8, 7 }, new[] { 1, 9, 8 }, new[] { 1, 10, 9 }, new[] { 1, 11, 10 }, new[] { 1, 7, 11 },
                // Средний пояс (зигзаг из треугольников)
                new[] { 2, 7, 3 }, new[] { 3, 7, 8 }, new[] { 3, 8, 4 }, new[] { 4, 8, 9 }, new[] { 4, 9, 5 },
                new[] { 5, 9, 10 }, new[] { 5, 10, 6 }, new[] { 6, 10, 11 }, new[] { 6, 11, 2 }, new[] { 2, 11, 7 }
            };
            return Build(v, f);
        }

        public static Polyhedron CreateDodecahedron()
        {
            var ico = CreateIcosahedron();
            var v = new List<Point>();

            // 1. Находим центры всех 20 граней икосаэдра — это 20 вершин додекаэдра
            foreach (var face in ico.Faces)
            {
                double cx = 0, cy = 0, cz = 0;
                foreach (int idx in face.VertexIndices)
                {
                    cx += ico.Vertices[idx].X;
                    cy += ico.Vertices[idx].Y;
                    cz += ico.Vertices[idx].Z;
                }
                v.Add(new Point(cx / 3.0, cy / 3.0, cz / 3.0));
            }
            NormalizeUnit(v);

            // 2. У икосаэдра 12 вершин = у додекаэдра 12 граней.
            // Собираем вокруг каждой вершины икосаэдра 5 смежных центров граней.
            var facesList = new List<int[]>();

            for (int icoVertIdx = 0; icoVertIdx < ico.Vertices.Count; icoVertIdx++)
            {
                var polygonVerts = new List<int>();
                for (int faceIdx = 0; faceIdx < ico.Faces.Count; faceIdx++)
                {
                    foreach (int vIdx in ico.Faces[faceIdx].VertexIndices)
                    {
                        if (vIdx == icoVertIdx)
                        {
                            polygonVerts.Add(faceIdx);
                            break;
                        }
                    }
                }

                // --- ЖЕЛЕЗОБЕТОННАЯ СОРТИРОВКА ПО УГЛУ (ГРАНЬ НЕ БУДЕТ "ЖМЫХАТЬ") ---
                // Находим среднюю точку (центр нашего пятиугольника)
                double gcx = 0, gcy = 0, gcz = 0;
                foreach (int idx in polygonVerts)
                {
                    gcx += v[idx].X;
                    gcy += v[idx].Y;
                    gcz += v[idx].Z;
                }
                var polyCenter = new Point(gcx / 5.0, gcy / 5.0, gcz / 5.0);

                // Задаем два перпендикулярных вектора в плоскости пятиугольника для локальных координат
                var baseV1 = new Point(v[polygonVerts[0]].X - polyCenter.X, v[polygonVerts[0]].Y - polyCenter.Y, v[polygonVerts[0]].Z - polyCenter.Z);

                // Вектор наружу (нормаль всей фигуры) для построения второго ортогонального вектора
                var normalOut = ico.Vertices[icoVertIdx];

                // Векторное произведение дает перпендикулярную ось baseV2 в плоскости грани
                double v2x = normalOut.Y * baseV1.Z - normalOut.Z * baseV1.Y;
                double v2y = normalOut.Z * baseV1.X - normalOut.X * baseV1.Z;
                double v2z = normalOut.X * baseV1.Y - normalOut.Y * baseV1.X;
                var baseV2 = new Point(v2x, v2y, v2z);

                // Сортируем вершины по полярному углу Math.Atan2(y, x) относительно центра грани
                polygonVerts.Sort((idxA, idxB) =>
                {
                    var vecA = new Point(v[idxA].X - polyCenter.X, v[idxA].Y - polyCenter.Y, v[idxA].Z - polyCenter.Z);
                    var vecB = new Point(v[idxB].X - polyCenter.X, v[idxB].Y - polyCenter.Y, v[idxB].Z - polyCenter.Z);

                    // Проекции на наши оси
                    double xA = vecA.X * baseV1.X + vecA.Y * baseV1.Y + vecA.Z * baseV1.Z;
                    double yA = vecA.X * baseV2.X + vecA.Y * baseV2.Y + vecA.Z * baseV2.Z;
                    double angleA = Math.Atan2(yA, xA);

                    double xB = vecB.X * baseV1.X + vecB.Y * baseV1.Y + vecB.Z * baseV1.Z;
                    double yB = vecB.X * baseV2.X + vecB.Y * baseV2.Y + vecB.Z * baseV2.Z;
                    double angleB = Math.Atan2(yB, xB);

                    return angleA.CompareTo(angleB);
                });

                // Финальная проверка направления обхода: считаем нормаль получившегося пятиугольника
                var p0 = v[polygonVerts[0]];
                var p1 = v[polygonVerts[1]];
                var p2 = v[polygonVerts[2]];

                double nx = (p1.Y - p0.Y) * (p2.Z - p0.Z) - (p1.Z - p0.Z) * (p2.Y - p0.Y);
                double ny = (p1.Z - p0.Z) * (p2.X - p0.X) - (p1.X - p0.X) * (p2.Z - p0.Z);
                double nz = (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);

                // Если нормаль смотрит внутрь центра координат, разворачиваем пятиугольник наружу
                if (nx * polyCenter.X + ny * polyCenter.Y + nz * polyCenter.Z < 0)
                {
                    polygonVerts.Reverse();
                }

                facesList.Add(polygonVerts.ToArray());
            }

            return Build(v, facesList.ToArray());
        }


        private static void NormalizeUnit(List<Point> v)
        {
            double r = 0;
            foreach (var p in v)
            {
                double len = Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
                r = Math.Max(r, len);
            }
            if (r < 1e-10) return;
            for (int i = 0; i < v.Count; i++)
            {
                v[i] = new Point(v[i].X / r, v[i].Y / r, v[i].Z / r);
            }
        }

        private static Polyhedron Build(List<Point> verts, int[][] faces)
        {
            var p = new Polyhedron { Vertices = verts };
            foreach (var fi in faces)
            {
                p.Faces.Add(new Polygon(fi));
            }
            return p;
        }
    }
}
