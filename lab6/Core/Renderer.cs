using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using lab6.Core;
using lab6.Core.Primitives;

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

        public void FillPolygon(List<Vertex2D> points) 
        { 

            if (points.Count < 3) return;

            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (var p in points)
            {
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            minY = Math.Max(0, minY);
            maxY = Math.Min(_height - 1, maxY);

            for (int y = minY; y <= maxY; y++)
            {
                List<Vertex2D> intersections = new List<Vertex2D>();

                for (int i = 0; i < points.Count; i++)
                {
                    var p1 = points[i];
                    var p2 = points[(i + 1) % points.Count];
                    if ((p1.y <= y && p2.y > y) || (p2.y <= y && p1.y > y))
                    {
                        double t = (double)(y - p1.y) / (p2.y - p1.y);
                        intersections.Add(new Vertex2D
                        {
                            x = (int)(p1.x + t * (p2.x - p1.x)),
                            z = p1.z + (float)(t * (p2.z - p1.z)),
                            r = p1.r + (float)(t * (p2.r - p1.r)),
                            g = p1.g + (float)(t * (p2.g - p1.g)),
                            b = p1.b + (float)(t * (p2.b - p1.b))
                        });
                    }
                }

                intersections.Sort((a, c) => a.x.CompareTo(c.x));

                for (int i = 0; i < intersections.Count - 1; i += 2)
                {
                    var left = intersections[i];
                    var right = intersections[i + 1];
                    int span = Math.Max(1, right.x - left.x);

                    for (int x = Math.Max(0, left.x); x < Math.Min(_width, right.x); x++)
                    {
                        float t = (float)(x - left.x) / span;

                        float z = left.z + (right.z - left.z) * t;
                        float r = left.r + (right.r - left.r) * t;
                        float g = left.g + (right.g - left.g) * t;
                        float b = left.b + (right.b - left.b) * t;

                        
                        SetPixel(x, y, (byte)r, (byte)g, (byte)b, 255, z);
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

        private List<(Point pt, int idx)> clipNear(List<(Point pt, int idx)> points)
        {
            var outPoints = new List<(Point pt, int idx)>();

            for (int i = 0; i < points.Count; ++i)
            {
                var curr = points[i];
                var next = points[(i + 1) % points.Count];

                bool currIn = curr.pt.Z >= Near;
                bool nextIn = next.pt.Z >= Near;

                if (currIn)
                {
                    outPoints.Add(curr);
                }

                if (currIn != nextIn)
                {
                    double t = (Near - curr.pt.Z) / (next.pt.Z - curr.pt.Z);
                    outPoints.Add((new Point(
                        curr.pt.X + t * (next.pt.X - curr.pt.X),
                        curr.pt.Y + t * (next.pt.Y - curr.pt.Y),
                        Near), -1)); 
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
            double cameraDistance = 10.0; // 5.0 для лучшего обзора

            Matrix4x4 projectionMatrix = isPerspective
                                        ? Matrix4x4.PerspectiveProjection(cameraDistance)
                                        : Matrix4x4.OrthographicProjection();

            Point viewDir = customViewVector ?? new Point(0, 0, 1);
            var toCam = Matrix4x4.Translate(0, 0, cameraDistance + 2);

            // Только сбор и подготовка данных
            var vertexNormals = CalculateVertexNormals(poly);

            // Параметры освещения
            Point lightDir = new Point(0.5, 0.5, -1); // Свет сверху-справа-спереди
            double lightLen = Math.Sqrt(lightDir.X * lightDir.X + lightDir.Y * lightDir.Y + lightDir.Z * lightDir.Z);
            lightDir = new Point(lightDir.X / lightLen, lightDir.Y / lightLen, lightDir.Z / lightLen);

            float ambient = 0.2f; // Фоновая освещённость
            byte baseR = 100, baseG = 150, baseB = 220; // синий цвет объекта

            var preparedFaces = new List<List<Vertex2D>>();

            foreach (var face in poly.Faces)
            {
                if (face.VertexIndices.Length < 3) continue;

                // точки с исходными индексами
                var worldPointsWithIdx = new List<(Point pt, int idx)>();
                foreach (int idx in face.VertexIndices)
                {
                    var wp = poly.Vertices[idx].Transform(modelMatrix);
                    if (viewMatrix.HasValue) wp = wp.Transform(viewMatrix.Value);
                    else wp = wp.Transform(toCam);
                    worldPointsWithIdx.Add((wp, idx));
                }

                // все что за камерой убираем
                worldPointsWithIdx = clipNear(worldPointsWithIdx);
                if (worldPointsWithIdx.Count < 3) continue;

                // Отсечение нелицевых граней
                if (enableBackfaceCulling)
                {
                    Point normal = CalculateFaceNormal(worldPointsWithIdx[0].pt, worldPointsWithIdx[1].pt, worldPointsWithIdx[2].pt);
                    Point v = new Point(-worldPointsWithIdx[0].pt.X, -worldPointsWithIdx[0].pt.Y, -worldPointsWithIdx[0].pt.Z);
                    double dotProduct = normal.X * v.X + normal.Y * v.Y + normal.Z * v.Z;
                    if (dotProduct <= 0) continue;

                    double nLen = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
                    double vLen = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
                    if (nLen > 1e-10 && vLen > 1e-10)
                    {
                        if (Math.Abs(dotProduct / (nLen * vLen)) < 0.03) continue;
                    }
                }

                // экранные точки с рассчитанным цветом
                var screenPoints = new List<Vertex2D>();
                foreach (var item in worldPointsWithIdx)
                {
                    var pt = item.pt;
                    if (isPerspective && viewMatrix == null) pt = pt.Transform(Matrix4x4.Translate(0, 0, cameraDistance + 2));

                    var projected = pt.Transform(projectionMatrix);
                    var normalized = projected.Normalize();

                    if (double.IsNaN(normalized.X) || double.IsNaN(normalized.Y) || double.IsInfinity(normalized.X) || double.IsInfinity(normalized.Y)) continue;

                    int sx = (int)(centerX + normalized.X * scale);
                    int sy = (int)(centerY - normalized.Y * scale);
                    float sz = isPerspective ? (float)(1.0 / Math.Max(pt.Z, Near)) : (float)pt.Z;

                    // ламберт
                    Point normal;
                    if (item.idx >= 0)
                    {
                        // усреднённая нормаль для плавного Гуро
                        normal = vertexNormals[item.idx];
                    }
                    else
                    {
                        // Для новых точек, берём нормаль грани для точек из clipNear
                        normal = CalculateFaceNormal(worldPointsWithIdx[0].pt, worldPointsWithIdx[1].pt, worldPointsWithIdx[2].pt);
                        double nLen = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
                        if (nLen > 1e-10) normal = new Point(normal.X / nLen, normal.Y / nLen, normal.Z / nLen);
                    }

                    // Скалярное произведение N · L
                    double intensity = normal.X * lightDir.X + normal.Y * lightDir.Y + normal.Z * lightDir.Z;
                    intensity = Math.Max(0, intensity); // max(0, N·L)

                    // Итоговая яркость - фоновая + диффузная
                    float brightness = ambient + (1 - ambient) * (float)intensity;

                    screenPoints.Add(new Vertex2D
                    {
                        x = sx,
                        y = sy,
                        z = sz,
                        r = baseR * brightness,
                        g = baseG * brightness,
                        b = baseB * brightness
                    });
                }

                if (screenPoints.Count >= 3)
                {
                    preparedFaces.Add(screenPoints);

                    // Заливаем грань
                    if (renderMode == 1 || renderMode == 2)
                    {
                        FillPolygon(screenPoints);
                    }
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

                        DrawLine(p1.x, p1.y, d1, p2.x, p2.y, d2, 0, 0, 0, 2);
                    }
                }
            }

            _bitmap.Lock();
            System.Runtime.InteropServices.Marshal.Copy(_pixels, 0, _bitmap.BackBuffer, _pixels.Length);
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
            _bitmap.Unlock();

            return _bitmap;
        }

        private List<Point> CalculateVertexNormals(Polyhedron poly)
        {
            int n = poly.Vertices.Count;

            double[] sumX = new double[n];
            double[] sumY = new double[n];
            double[] sumZ = new double[n];

            int[] count = new int[n];

            foreach (var face in poly.Faces)
            {
                if (face.VertexIndices.Length < 3)
                {
                    continue;
                }

                Point p0 = poly.Vertices[face.VertexIndices[0]];
                Point p1 = poly.Vertices[face.VertexIndices[1]];
                Point p2 = poly.Vertices[face.VertexIndices[2]];

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
                if (len < 1e-10)
                {
                    continue;
                }
                nx /= len; ny /= len; nz /= len;

                foreach (int idx in face.VertexIndices)
                {
                    sumX[idx] += nx;
                    sumY[idx] += ny;
                    sumZ[idx] += nz;
                    count[idx]++;
                }
            }

            var vertexNormals = new List<Point>(n);
            for (int i = 0; i < n; i++)
            {
                if (count[i] == 0)
                {
                    vertexNormals.Add(new Point(0, 0, 1));
                }

                else
                {
                    double nx = sumX[i] / count[i];
                    double ny = sumY[i] / count[i];
                    double nz = sumZ[i] / count[i];

                    double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                    if (len > 1e-10)
                    {
                        nx /= len; ny /= len; nz /= len;
                    }

                    vertexNormals.Add(new Point(nx, ny, nz));
                }
            }
            
            return vertexNormals;
        
        }
    }
}