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
        private int _width;
        private int _height;
        private int _stride;

        public Renderer(int width, int height)
        {
            _width = width;
            _height = height;
            _stride = _width * 4;
            _pixels = new byte[_height * _stride];
            _bitmap = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgra32, null);
        }

        // БЫСТРАЯ установка пикселя (без проверки границ - делаем её снаружи)
        private void SetPixelFast(int x, int y, byte r, byte g, byte b, byte a = 255)
        {
            int index = y * _stride + x * 4;
            _pixels[index] = b;
            _pixels[index + 1] = g;
            _pixels[index + 2] = r;
            _pixels[index + 3] = a;
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 255)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            SetPixelFast(x, y, r, g, b, a);
        }


        public void Clear(byte r, byte g, byte b)
        {

            for (int i = 0; i < _pixels.Length; i += 4)
            {
                _pixels[i] = b;     // B
                _pixels[i + 1] = g; // G
                _pixels[i + 2] = r; // R
                _pixels[i + 3] = 255; // A
            }
        }

        // Алгоритм Брезенхэма
        public void DrawLine(int x0, int y0, int x1, int y1, byte r, byte g, byte b)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                SetPixel(x0, y0, r, g, b);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        // Заполнение полигона
        public void FillPolygon(List<(int x, int y)> points, byte r, byte g, byte b, byte a)
        {
            if (points.Count < 3) return;

            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var p in points)
            {
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            // Ограничиваем范围 экрана
            minY = Math.Max(0, minY);
            maxY = Math.Min(_height - 1, maxY);

            for (int y = minY; y <= maxY; y++)
            {
                List<int> intersections = new List<int>();
                for (int i = 0; i < points.Count; i++)
                {
                    var p1 = points[i];
                    var p2 = points[(i + 1) % points.Count];

                    if ((p1.y <= y && p2.y > y) || (p2.y <= y && p1.y > y))
                    {
                        double t = (double)(y - p1.y) / (p2.y - p1.y);
                        int xIntersect = (int)(p1.x + t * (p2.x - p1.x));
                        if (xIntersect >= 0 && xIntersect < _width)
                            intersections.Add(xIntersect);
                    }
                }

                intersections.Sort();
                for (int i = 0; i < intersections.Count - 1; i += 2)
                {
                    int xStart = Math.Max(0, intersections[i]);
                    int xEnd = Math.Min(_width - 1, intersections[i + 1]);

                    for (int x = xStart; x < xEnd; x++)
                    {
                        SetPixelFast(x, y, r, g, b, a);
                    }
                }
            }
        }

        public WriteableBitmap RenderPolyhedron(Polyhedron poly, Matrix4x4 modelMatrix, bool isPerspective, int renderMode)
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
                var screenPoints = new List<(int x, int y)>();

                foreach (int idx in face.VertexIndices)
                {
                    var pt = poly.Vertices[idx].Transform(modelMatrix);

                    if (isPerspective)
                    {
                        pt = pt.Transform(Matrix4x4.Translate(0, 0, cameraDistance + 2));
                    }


                    var projected = pt.Transform(projectionMatrix);
                    var normalized = projected.Normalize();


                    if (double.IsNaN(normalized.X) || double.IsNaN(normalized.Y) ||
                        double.IsInfinity(normalized.X) || double.IsInfinity(normalized.Y))
                    {
                        continue; 
                    }

                    int sx = (int)(centerX + normalized.X * scale);
                    int sy = (int)(centerY - normalized.Y * scale);

                    screenPoints.Add((sx, sy));
                }

                if (screenPoints.Count < 3) continue; 

                if (renderMode == 0 || renderMode == 2)
                {
                    for (int i = 0; i < screenPoints.Count; i++)
                    {
                        var p1 = screenPoints[i];
                        var p2 = screenPoints[(i + 1) % screenPoints.Count];
                        DrawLine(p1.x, p1.y, p2.x, p2.y, 0, 0, 0);
                    }
                }

                if (renderMode == 1 || renderMode == 2)
                {
                    FillPolygon(screenPoints, 100, 150, 220, 255);
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