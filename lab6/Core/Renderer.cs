using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using lab6.Core;

namespace lab6.Core
{
    public class Renderer
    {
        private WriteableBitmap _bitmap;
        private byte[] _pixels;
        private float[] _zBuffer;
        private int _width;
        private int _height;
        private int _stride;

        private bool _isPerspective;
        private float _farDepth;

        public bool EnableBackfaceCulling { get; set; } = false;

        private const float Near = 0.1f;
        private const float Far = 100.0f;

        public Renderer(int width, int height)
        {
            _width = width;
            _height = height;
            _stride = _width * 4;
            _pixels = new byte[_height * _stride];
            _zBuffer = new float[_width * _height];
            _bitmap = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgra32, null);
        }

        public void Clear(byte r, byte g, byte b)
        {
            for (int i = 0; i < _pixels.Length; i += 4)
            {
                _pixels[i] = b;
                _pixels[i + 1] = g;
                _pixels[i + 2] = r;
                _pixels[i + 3] = 255;
            }
            for (int i = 0; i < _zBuffer.Length; i++)
                _zBuffer[i] = _farDepth;
        }

        private float NormalizeDepth(double zWorld)
        {
            double d = (zWorld - Near) / (Far - Near);
            if (d < 0) d = 0;
            if (d > 1) d = 1;
            return (float)d;
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b, byte a, float depth)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            int pixelIndex = y * _width + x;


            bool isCloser = _isPerspective ? (depth > _zBuffer[pixelIndex]) : (depth < _zBuffer[pixelIndex]);

            if (isCloser)
            {
                int byteIndex = y * _stride + x * 4;
                _pixels[byteIndex] = b;
                _pixels[byteIndex + 1] = g;
                _pixels[byteIndex + 2] = r;
                _pixels[byteIndex + 3] = a;
                _zBuffer[pixelIndex] = depth;
            }
        }

        // Базовый алгоритм Брезенхэма (толщина 1 пиксель)
        private void DrawLine(int x0, int y0, float d0, int x1, int y1, float d1, byte r, byte g, byte b, int thickness=1)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int total = Math.Max(dx, dy);
            int step = 0;

            while (true)
            {
                float t = total == 0 ? 0f : (float)step / total;
                float d = d0 + (d1 - d0) * t;

                for (int oy = 0; oy < thickness; oy++)
                    for (int ox = 0; ox < thickness; ox++)
                        SetPixel(x0 + ox, y0 + oy, r, g, b, 255, d);

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
                step++;
            }
        }

        public void FillPolygon(
            List<(int x, int y, float d)> points,
            byte r, byte g, byte b, byte a)
        {
            if (points.Count < 3) return;

            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var p in points)
            {
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            minY = Math.Max(0, minY);
            maxY = Math.Min(_height - 1, maxY);

            for (int y = minY; y <= maxY; y++)
            {
                var xs = new List<int>();
                var ds = new List<float>();
                for (int i = 0; i < points.Count; i++)
                {
                    var p1 = points[i];
                    var p2 = points[(i + 1) % points.Count];
                    if ((p1.y <= y && p2.y > y) || (p2.y <= y && p1.y > y))
                    {
                        double t = (double)(y - p1.y) / (p2.y - p1.y);
                        xs.Add((int)(p1.x + t * (p2.x - p1.x)));
                        ds.Add((float)(p1.d + t * (p2.d - p1.d)));
                    }
                }

                var pairs = new List<(int x, float d)>();
                for (int i = 0; i < xs.Count; i++) pairs.Add((xs[i], ds[i]));
                pairs.Sort((a, c) => a.x.CompareTo(c.x));

                for (int i = 0; i < pairs.Count - 1; i += 2)
                {
                    int xa = pairs[i].x, xb = pairs[i + 1].x;
                    float da = pairs[i].d, db = pairs[i + 1].d;
                    int span = Math.Max(1, xb - xa);
                    for (int x = Math.Max(0, xa); x < Math.Min(_width, xb); x++)
                    {
                        float t = (float)(x - xa) / span;
                        float d = da + (db - da) * t;
                        SetPixel(x, y, r, g, b, a, d);
                    }
                }
            }
        }

        private Point CalculateFaceNormal(Point p0, Point p1, Point p2)
        {
            double ux = p1.X - p0.X;
            double uy = p1.Y - p0.Y;
            double uz = p1.Z - p0.Z;

            double vx = p2.X - p0.X;
            double vy = p2.Y - p0.Y;
            double vz = p2.Z - p0.Z;

            double nx = uy * vz - uz * vy;
            double ny = uz * vx - ux * vz;
            double nz = ux * vy - uy * vx;

            double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len > 1e-10)
            {
                nx /= len;
                ny /= len;
                nz /= len;
            }

            return new Point(nx, ny, nz);
        }

        private List<Point> clipNear(List<Point> points)
        {
            List <Point> outPoints = new List<Point>();

            for (int i = 0; i < points.Count; ++i) {

                Point curr = points[i];
                Point next = points[(i + 1) % points.Count];

                bool currIn = curr.Z >= 0.1f;
                bool nextIn = next.Z >= 0.1f;

                if (currIn)
                {
                    outPoints.Add(curr);
                }

                if (currIn != nextIn)
                {
                    double t = (0.1f - curr.Z) / (next.Z - curr.Z);
                    outPoints.Add(new Point( curr.X+t*(next.X - curr.X),
                                             curr.Y+t*(next.Y - curr.Y),
                                             0.1f));
                }
            }

            return outPoints;
        }

        public WriteableBitmap RenderPolyhedron(
            Polyhedron poly,
            Matrix4x4 modelMatrix,
            bool isPerspective,
            int renderMode,
            bool enableBackfaceCulling = false,
            Point? customViewVector = null,
                     Matrix4x4? viewMatrix = null)
        {
            _isPerspective = isPerspective;
            _farDepth = isPerspective ? 0f : float.MaxValue;
            Clear(255, 255, 255);

            double centerX = _width / 2.0;
            double centerY = _height / 2.0;
            double scale = Math.Min(_width, _height) / 4.0;
            double cameraDistance = 5.0; // 5.0 для лучшего обзора

            Matrix4x4 projectionMatrix = isPerspective
                                        ? Matrix4x4.PerspectiveProjection(cameraDistance)
                                        : Matrix4x4.OrthographicProjection();

            Point viewDir = customViewVector ?? new Point(0, 0, 1);
            var toCam = Matrix4x4.Translate(0, 0, cameraDistance + 2);

            // Только сбор и подготовка данных
            var preparedFaces = new List<List<(int x, int y, float z)>>();

            foreach (var face in poly.Faces)
            {
                if (face.VertexIndices.Length < 3) continue;

                var worldPoints = new List<Point>();
                foreach (int idx in face.VertexIndices)
                {
                    var wp = poly.Vertices[idx].Transform(modelMatrix);
                    if (viewMatrix.HasValue) wp = wp.Transform(viewMatrix.Value);
                    else wp = wp.Transform(toCam);
                    worldPoints.Add(wp);
                }

                worldPoints = clipNear(worldPoints);
                if (worldPoints.Count < 3) continue;

                if (enableBackfaceCulling)
                {
                    Point normal = CalculateFaceNormal(worldPoints[0], worldPoints[1], worldPoints[2]);

                    // Вектор ОТ грани К камере (камера всегда в 0,0,0 после применения viewMatrix или toCam)
                    Point v = new Point(-worldPoints[0].X, -worldPoints[0].Y, -worldPoints[0].Z);

                    double dotProduct = normal.X * v.X + normal.Y * v.Y + normal.Z * v.Z;

                    // Если скалярное произведение <= 0, нормаль смотрит от камеры или перпендикулярна (грань невидима)
                    if (dotProduct <= 0)
                    {
                        continue;
                    }

                    // Защита от артефактов на гранях, стоящих ровно ребром к камере
                    double nLen = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
                    double vLen = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
                    if (nLen > 1e-10 && vLen > 1e-10)
                    {
                        double cosAngle = dotProduct / (nLen * vLen);
                        if (Math.Abs(cosAngle) < 0.03) continue;
                    }
                }

                var screenPoints = new List<(int x, int y, float z)>();
                foreach (var worldPt in worldPoints)
                {
                    var pt = worldPt;
                    if (isPerspective && viewMatrix == null) pt = pt.Transform(Matrix4x4.Translate(0, 0, cameraDistance + 2));

                    var projected = pt.Transform(projectionMatrix);
                    var normalized = projected.Normalize();

                    if (double.IsNaN(normalized.X) || double.IsNaN(normalized.Y) || double.IsInfinity(normalized.X) || double.IsInfinity(normalized.Y)) continue;

                    int sx = (int)(centerX + normalized.X * scale);
                    int sy = (int)(centerY - normalized.Y * scale);
                    float sz = isPerspective ? (float)(1.0 / Math.Max(pt.Z, 0.1f)) : (float)pt.Z;

                    screenPoints.Add((sx, sy, sz));
                }

                // Добавляем грань в список
                if (screenPoints.Count >= 3)
                {
                    preparedFaces.Add(screenPoints);
                }

                // Заливаем грань сразу 
                if (renderMode == 1 || renderMode == 2)
                {
                    FillPolygon(screenPoints, 100, 150, 220, 255);
                }

            } 

            // Отрисовка каркаса
            if (renderMode == 0 || renderMode == 2)
            {
                foreach (var pts in preparedFaces)
                {
                    for (int i = 0; i < pts.Count; i++)
                    {
                        var p1 = pts[i];
                        var p2 = pts[(i + 1) % pts.Count];

                        float d1 = isPerspective ? p1.z * 1.02f : p1.z - 0.05f;
                        float d2 = isPerspective ? p2.z * 1.02f : p2.z - 0.05f;

                        DrawLine(p1.x, p1.y, d1, p2.x, p2.y, d2, 0, 0, 0);
                    }
                }
            }

            _bitmap.Lock();
            System.Runtime.InteropServices.Marshal.Copy(_pixels, 0, _bitmap.BackBuffer, _pixels.Length);
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
            _bitmap.Unlock();

            return _bitmap;
        }
    }
}