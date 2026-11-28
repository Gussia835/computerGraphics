using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using lab6.Core;
using lab6.Core.Primitives;

namespace lab6.Core
{
    public class Renderer
    {
        private byte[]? _texturePixels;
        private int _texWidth;
        private int _texHeight;
        public bool HasTexture => _texturePixels != null;

        public Point LightDir = new Point(0.5, 0.5, -1);

        public void LoadTexture(string filePath)
        {
            var bitmap = new BitmapImage(new Uri(filePath, UriKind.Absolute));
            var formattedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);

            _texWidth = formattedBitmap.PixelWidth;
            _texHeight = formattedBitmap.PixelHeight;
            int texStride = _texWidth * 4;
            _texturePixels = new byte[_texHeight * texStride];

            formattedBitmap.CopyPixels(_texturePixels, texStride, 0);
        }

        private WriteableBitmap _bitmap;
        private byte[] _pixels;
        private float[] _zBuffer;
        private int _width;
        private int _height;
        private int _stride;

        private bool _isPerspective;
        private float _farDepth;
        private const float Near = 0.1f;

        public bool EnableBackfaceCulling { get; set; } = false;

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

        private void DrawLine(int x0, int y0, float d0, int x1, int y1, float d1, byte r, byte g, byte b, int thickness = 1)
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

        // Текстурированная заливка с поддержкой Z-буфера и интерполяцией (u, v) (Лаба №9, Задание 2)
        public void FillTexturedPolygon(List<(int x, int y, float d, double u, double v)> points)
        {
            if (points.Count < 3 || _texturePixels == null) return;

            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var p in points)
            {
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            minY = Math.Max(0, minY);
            maxY = Math.Min(_height - 1, maxY);

            int texStride = _texWidth * 4;

            for (int y = minY; y <= maxY; y++)
            {
                var pairs = new List<(int x, float d, double u, double v)>();
                for (int i = 0; i < points.Count; i++)
                {
                    var p1 = points[i];
                    var p2 = points[(i + 1) % points.Count];
                    if ((p1.y <= y && p2.y > y) || (p2.y <= y && p1.y > y))
                    {
                        double t = (double)(y - p1.y) / (p2.y - p1.y);
                        int x = (int)(p1.x + t * (p2.x - p1.x));
                        float d = (float)(p1.d + t * (p2.d - p1.d));
                        double u = p1.u + t * (p2.u - p1.u);
                        double v = p1.v + t * (p2.v - p1.v);
                        pairs.Add((x, d, u, v));
                    }
                }

                pairs.Sort((a, c) => a.x.CompareTo(c.x));

                for (int i = 0; i < pairs.Count - 1; i += 2)
                {
                    int xa = pairs[i].x, xb = pairs[i + 1].x;
                    float da = pairs[i].d, db = pairs[i + 1].d;
                    double ua = pairs[i].u, ub = pairs[i + 1].u;
                    double va = pairs[i].v, vb = pairs[i + 1].v;

                    int span = Math.Max(1, xb - xa);
                    for (int x = Math.Max(0, xa); x < Math.Min(_width, xb); x++)
                    {
                        double t = (double)(x - xa) / span;
                        float d = (float)(da + (db - da) * t);
                        double u = ua + (ub - ua) * t;
                        double v = va + (vb - va) * t;

                        u = Math.Max(0.0, Math.Min(1.0, u));
                        v = Math.Max(0.0, Math.Min(1.0, v));

                        int tx = (int)(u * (_texWidth - 1));
                        int ty = (int)(v * (_texHeight - 1));
                        int texIndex = ty * texStride + tx * 4;

                        byte b = _texturePixels[texIndex];
                        byte g = _texturePixels[texIndex + 1];
                        byte r = _texturePixels[texIndex + 2];

                        // Рисуем тексель через ваш Z-буфер!
                        SetPixel(x, y, r, g, b, 255, d);
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

        // Отсечение ближней плоскостью с сохранением индекса вершины (для освещения) и интерполяцией UV-координат (для текстур)
        private List<(Point pt, int idx, double u, double v)> clipNear(List<(Point pt, int idx, double u, double v)> points)
        {
            var outPoints = new List<(Point pt, int idx, double u, double v)>();

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
                        Near),
                        -1, // Новая точка на срезе не имеет исходного индекса вершины
                        curr.u + t * (next.u - curr.u),
                        curr.v + t * (next.v - curr.v)));
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
            double cameraDistance = 10.0;

            Matrix4x4 projectionMatrix = isPerspective
                ? Matrix4x4.PerspectiveProjection(cameraDistance)
                : Matrix4x4.OrthographicProjection();

            var toCam = Matrix4x4.Translate(0, 0, cameraDistance + 2);

            // Только сбор и подготовка данных
            var vertexNormals = CalculateVertexNormals(poly);

            // Параметры освещения
            Point lightDir = new Point(LightDir.X, LightDir.Y, LightDir.Z);
            double lightLen = Math.Sqrt(lightDir.X * lightDir.X + lightDir.Y * lightDir.Y + lightDir.Z * lightDir.Z);
            lightDir = new Point(lightDir.X / lightLen, lightDir.Y / lightLen, lightDir.Z / lightLen);

            float ambient = 0.2f; // Фоновая освещённость
            byte baseR = 100, baseG = 150, baseB = 220; // базовый цвет объекта

            var preparedGouraud = new List<List<Vertex2D>>();
            var preparedTextured = new List<List<(int x, int y, float z, double u, double v)>>();
            var preparedWireframe = new List<List<(int x, int y, float z)>>();

            foreach (var face in poly.Faces)
            {
                if (face.VertexIndices.Length < 3) continue;

                // Подготовка UV для текстур
                if (face.TexCoords == null || face.TexCoords.Length != face.VertexIndices.Length)
                    face.AssignDefaultUV(face.VertexIndices.Length);

                // точки с исходными индексами и UV-координатами
                var worldItems = new List<(Point pt, int idx, double u, double v)>();
                for (int i = 0; i < face.VertexIndices.Length; i++)
                {
                    int idx = face.VertexIndices[i];
                    var wp = poly.Vertices[idx].Transform(modelMatrix);
                    if (viewMatrix.HasValue) wp = wp.Transform(viewMatrix.Value);
                    else wp = wp.Transform(toCam);

                    double u = face.TexCoords![i].u;
                    double v = face.TexCoords![i].v;

                    worldItems.Add((wp, idx, u, v));
                }

                // все что за камерой убираем
                worldItems = clipNear(worldItems);
                if (worldItems.Count < 3) continue;

                // Отсечение нелицевых граней
                if (enableBackfaceCulling || EnableBackfaceCulling)
                {
                    Point normal = CalculateFaceNormal(worldItems[0].pt, worldItems[1].pt, worldItems[2].pt);
                    Point v = new Point(-worldItems[0].pt.X, -worldItems[0].pt.Y, -worldItems[0].pt.Z);
                    double dotProduct = normal.X * v.X + normal.Y * v.Y + normal.Z * v.Z;
                    if (dotProduct <= 0) continue;

                    double nLen = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
                    double vLen = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
                    if (nLen > 1e-10 && vLen > 1e-10)
                    {
                        if (Math.Abs(dotProduct / (nLen * vLen)) < 0.03) continue;
                    }

                    /*Point v;
                    if (customViewVector.HasValue)
                    {
                        v = new Point(-customViewVector.Value.X, -customViewVector.Value.Y, -customViewVector.Value.Z);
                    }

                    else if (isPerspective)
                    {
                        v = new Point(-worldItems[0].pt.X, -worldItems[0].pt.Y, -worldItems[0].pt.Z);
                    }

                    else
                    {
                        v = new Point(0, 0, -1);
                    }

                    double dotProduct = normal.X * v.X + normal.Y * v.Y + normal.Z * v.Z;
                    if (dotProduct <= 0)
                    {
                        continue;
                    }*/
                }

                // экранные точки с рассчитанным цветом и UV
                var gouraudPts = new List<Vertex2D>();
                var texturedPts = new List<(int x, int y, float z, double u, double v)>();
                var wirePts = new List<(int x, int y, float z)>();

                foreach (var item in worldItems)
                {
                    var pt = item.pt;
                    //if (isPerspective && viewMatrix == null) pt = pt.Transform(Matrix4x4.Translate(0, 0, cameraDistance + 2));

                    var projected = pt.Transform(projectionMatrix);
                    var normalized = projected.Normalize();

                    if (double.IsNaN(normalized.X) || double.IsNaN(normalized.Y) || double.IsInfinity(normalized.X) || double.IsInfinity(normalized.Y)) continue;

                    int sx = (int)Math.Round(centerX + normalized.X * scale);
                    int sy = (int)Math.Round(centerY - normalized.Y * scale);
                    float sz = isPerspective ? (float)(1.0 / Math.Max(pt.Z, Near)) : (float)pt.Z;

                    // ламберт
                    Point normal;
                    if (item.idx >= 0)
                    {
                        // усреднённая нормаль для плавного Гуро
                        Point localN = vertexNormals[item.idx];
                        // трансформируем нормаль ротацией modelMatrix (без translate)
                        normal = new Point(
                            localN.X * modelMatrix.M11 + localN.Y * modelMatrix.M21 + localN.Z * modelMatrix.M31,
                            localN.X * modelMatrix.M12 + localN.Y * modelMatrix.M22 + localN.Z * modelMatrix.M32,
                            localN.X * modelMatrix.M13 + localN.Y * modelMatrix.M23 + localN.Z * modelMatrix.M33);
                        double nLen0 = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
                        if (nLen0 > 1e-10) normal = new Point(normal.X / nLen0, normal.Y / nLen0, normal.Z / nLen0);
                    }
                    else
                    {
                        // Для новых точек, берём нормаль грани для точек из clipNear
                        normal = CalculateFaceNormal(worldItems[0].pt, worldItems[1].pt, worldItems[2].pt);
                        double nLen = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
                        if (nLen > 1e-10) normal = new Point(normal.X / nLen, normal.Y / nLen, normal.Z / nLen);
                    }

                    // Скалярное произведение N · L
                    double intensity = normal.X * lightDir.X + normal.Y * lightDir.Y + normal.Z * lightDir.Z;
                    intensity = Math.Max(0, intensity); // max(0, N·L)

                    // Итоговая яркость - фоновая + диффузная
                    float brightness = ambient + (1 - ambient) * (float)intensity;

                    gouraudPts.Add(new Vertex2D
                    {
                        x = sx,
                        y = sy,
                        z = sz,
                        r = baseR * brightness,
                        g = baseG * brightness,
                        b = baseB * brightness
                    });

                    texturedPts.Add((sx, sy, sz, item.u, item.v));
                    wirePts.Add((sx, sy, sz));
                }

                if (gouraudPts.Count >= 3) preparedGouraud.Add(gouraudPts);
                if (texturedPts.Count >= 3) preparedTextured.Add(texturedPts);
                if (wirePts.Count >= 3) preparedWireframe.Add(wirePts);
            }

            // Заливаем грань в зависимости от режима
            if (renderMode == 1 || renderMode == 2)
            {
                foreach (var pts in preparedGouraud)
                {
                    FillPolygon(pts);
                }
            }
            else if (renderMode == 3)
            {
                if (HasTexture)
                {
                    foreach (var pts in preparedTextured)
                    {
                        FillTexturedPolygon(pts);
                    }
                }
                else
                {
                    // Фоллбэк на Гуро, если текстура не загружена, чтобы не было серых плоских граней
                    foreach (var pts in preparedGouraud)
                    {
                        FillPolygon(pts);
                    }
                }
            }

            // Отрисовка каркаса
            if (renderMode == 0 || renderMode == 2)
            {
                foreach (var pts in preparedWireframe)
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