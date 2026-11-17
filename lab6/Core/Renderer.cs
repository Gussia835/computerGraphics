using System;
using System.Collections.Generic;
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

        public bool EnableBackfaceCulling { get; set; } = false;

        // near/far для нормализации глубины в [0,1]
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
                _zBuffer[i] = float.MaxValue;
        }

        // Глубина в [0,1]: 0 = у камеры, 1 = на far. Меньше = ближе.
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

            if (depth < _zBuffer[pixelIndex])
            {
                int byteIndex = y * _stride + x * 4;
                _pixels[byteIndex] = b;
                _pixels[byteIndex + 1] = g;
                _pixels[byteIndex + 2] = r;
                _pixels[byteIndex + 3] = a;
                _zBuffer[pixelIndex] = depth;
            }
        }

        // Линия с интерполяцией глубины между концами
        public void DrawLine(int x0, int y0, float d0, int x1, int y1, float d1, byte r, byte g, byte b)
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
                SetPixel(x0, y0, r, g, b, 255, d);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
                step++;
            }
        }

        // Заливка с интерполяцией глубины по рёбрам scanline
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

                // сортируем пары (x, d) вместе
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

        // Проецирует точку и возвращает (screenX, screenY, depth)
        private (int sx, int sy, float d) Project(
            Point pt, Matrix4x4 projectionMatrix,
            double centerX, double centerY, double scale)
        {
            var projected = pt.Transform(projectionMatrix);
            var normalized = projected.Normalize();
            if (double.IsNaN(normalized.X) || double.IsNaN(normalized.Y) ||
                double.IsInfinity(normalized.X) || double.IsInfinity(normalized.Y))
                return (-1, -1, float.MaxValue);

            int sx = (int)(centerX + normalized.X * scale);
            int sy = (int)(centerY - normalized.Y * scale);
            float d = NormalizeDepth(pt.Z); // мировой Z после сдвига камеры
            return (sx, sy, d);
        }

        public WriteableBitmap RenderPolyhedron(
            Polyhedron poly, Matrix4x4 modelMatrix, bool isPerspective, int renderMode)
        {
            Clear(255, 255, 255);

            double centerX = _width / 2.0;
            double centerY = _height / 2.0;
            double scale = Math.Min(_width, _height) / 4.0;
            double cameraDistance = 5.0;

            Matrix4x4 projectionMatrix = isPerspective
                ? Matrix4x4.PerspectiveProjection(cameraDistance)
                : Matrix4x4.OrthographicProjection();

            foreach (var face in poly.Faces)
            {
                var screenPts = new List<(int x, int y, float d)>();
                var worldPts = new List<Point>();

                foreach (int idx in face.VertexIndices)
                {
                    var pt = poly.Vertices[idx].Transform(modelMatrix);
                    // в обеих проекциях сдвигаем сцену вперёд камеры,
                    // чтобы Z был положительным и сравнение было честным
                    pt = pt.Transform(Matrix4x4.Translate(0, 0, cameraDistance + 2));

                    var proj = Project(pt, projectionMatrix, centerX, centerY, scale);
                    if (proj.sx < 0) continue;

                    screenPts.Add(proj);
                    worldPts.Add(pt);
                }

                if (screenPts.Count < 3) continue;

                if (EnableBackfaceCulling)
                {
                    var v0 = worldPts[0]; var v1 = worldPts[1]; var v2 = worldPts[2];
                    var e1 = new Point(v1.X - v0.X, v1.Y - v0.Y, v1.Z - v0.Z);
                    var e2 = new Point(v2.X - v0.X, v2.Y - v0.Y, v2.Z - v0.Z);
                    double nx = e1.Y * e2.Z - e1.Z * e2.Y;
                    double ny = e1.Z * e2.X - e1.X * e2.Z;
                    double nz = e1.X * e2.Y - e1.Y * e2.X;
                    var toCam = new Point(-v0.X, -v0.Y, -v0.Z);
                    if (nx * toCam.X + ny * toCam.Y + nz * toCam.Z < 0) continue;
                }

                // 1. сначала заливка
                if (renderMode == 1 || renderMode == 2)
                    FillPolygon(screenPts, 100, 150, 220, 255);

                // 2. потом линии поверх, с tiny bias чтобы прошли Z-тест
                if (renderMode == 0 || renderMode == 2)
                {
                    const float lineBias = 0.0005f;
                    for (int i = 0; i < screenPts.Count; i++)
                    {
                        var a = screenPts[i];
                        var c = screenPts[(i + 1) % screenPts.Count];
                        DrawLine(a.x, a.y, a.d - lineBias,
                                 c.x, c.y, c.d - lineBias, 0, 0, 0);
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