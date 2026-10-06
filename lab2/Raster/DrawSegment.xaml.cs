using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Raster
{
    public partial class DrawSegment : Window
    {
        private WriteableBitmap wbitmap;
        private int width = 500;
        private int height = 350;

        public DrawSegment()
        {
            InitializeComponent();

            wbitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            MyImage.Source = wbitmap;

            RenderOptions.SetBitmapScalingMode(MyImage, BitmapScalingMode.NearestNeighbor);
        }

        private void DrawLines_Click(object sender, RoutedEventArgs e)
        {
            wbitmap.Lock();
            ClearBitmap();

            unsafe
            {
                int* pBackBuffer = (int*)wbitmap.BackBuffer;

                int cx1 = width / 4;
                int cy1 = height / 2;
                int len = 140;

                double a1 = 15.0 * Math.PI / 180.0;

                DrawBresenhamLine(pBackBuffer, cx1, cy1, cx1 + (int)(len * Math.Cos(a1)), cy1 + (int)(len * Math.Sin(a1)), 0xFF000000);
                DrawBresenhamLine(pBackBuffer, cx1, cy1, cx1 + (int)(len * Math.Cos(Math.PI / 2 - a1)), cy1 + (int)(len * Math.Sin(Math.PI / 2 - a1)), 0xFF000000);
                DrawBresenhamLine(pBackBuffer, cx1, cy1, cx1 - (int)(len * Math.Cos(Math.PI / 2 - a1)), cy1 + (int)(len * Math.Sin(Math.PI / 2 - a1)), 0xFF000000);
                DrawBresenhamLine(pBackBuffer, cx1, cy1, cx1 - (int)(len * Math.Cos(a1)), cy1 + (int)(len * Math.Sin(a1)), 0xFF000000);
                DrawBresenhamLine(pBackBuffer, cx1, cy1, cx1 - (int)(len * Math.Cos(a1)), cy1 - (int)(len * Math.Sin(a1)), 0xFF000000);
                DrawBresenhamLine(pBackBuffer, cx1, cy1, cx1 - (int)(len * Math.Cos(Math.PI / 2 - a1)), cy1 - (int)(len * Math.Sin(Math.PI / 2 - a1)), 0xFF000000);
                DrawBresenhamLine(pBackBuffer, cx1, cy1, cx1 + (int)(len * Math.Cos(Math.PI / 2 - a1)), cy1 - (int)(len * Math.Sin(Math.PI / 2 - a1)), 0xFF000000);
                DrawBresenhamLine(pBackBuffer, cx1, cy1, cx1 + (int)(len * Math.Cos(a1)), cy1 - (int)(len * Math.Sin(a1)), 0xFF000000);

                int cx2 = width * 3 / 4;
                int cy2 = height / 2;

                DrawWuLine(pBackBuffer, cx2, cy2, cx2 + (int)(len * Math.Cos(a1)), cy2 + (int)(len * Math.Sin(a1)));
                DrawWuLine(pBackBuffer, cx2, cy2, cx2 + (int)(len * Math.Cos(Math.PI / 2 - a1)), cy2 + (int)(len * Math.Sin(Math.PI / 2 - a1)));
                DrawWuLine(pBackBuffer, cx2, cy2, cx2 - (int)(len * Math.Cos(Math.PI / 2 - a1)), cy2 + (int)(len * Math.Sin(Math.PI / 2 - a1)));
                DrawWuLine(pBackBuffer, cx2, cy2, cx2 - (int)(len * Math.Cos(a1)), cy2 + (int)(len * Math.Sin(a1)));
                DrawWuLine(pBackBuffer, cx2, cy2, cx2 - (int)(len * Math.Cos(a1)), cy2 - (int)(len * Math.Sin(a1)));
                DrawWuLine(pBackBuffer, cx2, cy2, cx2 - (int)(len * Math.Cos(Math.PI / 2 - a1)), cy2 - (int)(len * Math.Sin(Math.PI / 2 - a1)));
                DrawWuLine(pBackBuffer, cx2, cy2, cx2 + (int)(len * Math.Cos(Math.PI / 2 - a1)), cy2 - (int)(len * Math.Sin(Math.PI / 2 - a1)));
                DrawWuLine(pBackBuffer, cx2, cy2, cx2 + (int)(len * Math.Cos(a1)), cy2 - (int)(len * Math.Sin(a1)));
            }

            wbitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            wbitmap.Unlock();
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;

            double newScaleX = ImageScale.ScaleX * zoomFactor;
            double newScaleY = ImageScale.ScaleY * zoomFactor;

            if (newScaleX >= 1.0 && newScaleX <= 20.0)
            {
                ImageScale.ScaleX = newScaleX;
                ImageScale.ScaleY = newScaleY;
            }

            e.Handled = true;
        }

        private void ClearBitmap()
        {
            unsafe
            {
                int* p = (int*)wbitmap.BackBuffer;
                int totalPixels = width * height;
                for (int i = 0; i < totalPixels; i++)
                {
                    p[i] = unchecked((int)0xFFFFFFFF);
                }
            }
        }

        private unsafe void PutPixel(int* buffer, int x, int y, uint color)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                buffer[y * width + x] = (int)color;
            }
        }

        private unsafe void DrawBresenhamLine(int* buffer, int x0, int y0, int x1, int y1, uint color)
        {
            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                PutPixel(buffer, x0, y0, color);

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * error;
                if (e2 >= dy)
                {
                    error += dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private unsafe void DrawWuLine(int* buffer, int x0, int y0, int x1, int y1)
        {
            void DrawPointAlpha(int x, int y, float intensity)
            {
                if (intensity < 0) intensity = 0;
                if (intensity > 1) intensity = 1;

                uint alpha = (uint)(intensity * 255);
                uint gray = 255 - alpha;
                uint finalColor = (255U << 24) | (gray << 16) | (gray << 8) | gray;
                PutPixel(buffer, x, y, finalColor);
            }

            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (steep)
            {
                int t; t = x0; x0 = y0; y0 = t;
                t = x1; x1 = y1; y1 = t;
            }
            if (x0 > x1)
            {
                int t; t = x0; x0 = x1; x1 = t;
                t = y0; y0 = y1; y1 = t;
            }

            float dx = x1 - x0;
            float dy = y1 - y0;
            float gradient = dx == 0 ? 1f : dy / dx;

            if (steep) DrawPointAlpha(y0, x0, 1f); else DrawPointAlpha(x0, y0, 1f);

            float y = y0 + gradient;

            for (int x = x0 + 1; x <= x1 - 1; x++)
            {
                int ipart = (int)Math.Floor(y);
                float fpart = y - ipart;

                if (steep)
                {
                    DrawPointAlpha(ipart, x, 1 - fpart);
                    DrawPointAlpha(ipart + 1, x, fpart);
                }
                else
                {
                    DrawPointAlpha(x, ipart, 1 - fpart);
                    DrawPointAlpha(x, ipart + 1, fpart);
                }
                y += gradient;
            }

            if (steep) DrawPointAlpha(y1, x1, 1f); else DrawPointAlpha(x1, y1, 1f);
        }
    }
}