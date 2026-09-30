using System;
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

                DrawThickBresenhamLine(pBackBuffer, 70, 40, 220, 310, 0xFF000000);

                DrawThickWuLine(pBackBuffer, 270, 40, 420, 310);
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

        private unsafe void DrawThickBresenhamLine(int* buffer, int x0, int y0, int x1, int y1, uint color)
        {
            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;

            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);

            while (true)
            {
                PutPixel(buffer, x0, y0, color);
                if (steep)
                {
                    PutPixel(buffer, x0 - 1, y0, color);
                    PutPixel(buffer, x0 + 1, y0, color);
                }
                else
                {
                    PutPixel(buffer, x0, y0 - 1, color);
                    PutPixel(buffer, x0, y0 + 1, color);
                }

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

        private unsafe void DrawThickWuLine(int* buffer, int x0, int y0, int x1, int y1)
        {
            void DrawPointAlpha(int x, int y, float intensity)
            {
                if (intensity < 0) intensity = 0;
                if (intensity > 1) intensity = 1;

                intensity = (float)Math.Sqrt(intensity);
                uint alpha = (uint)(intensity * 255);


                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    uint bg = 255 - alpha;
                    uint gray = bg;
                    uint finalColor = (255U << 24) | (gray << 16) | (gray << 8) | gray;
                    PutPixel(buffer, x, y, finalColor);
                }
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
                    DrawPointAlpha(ipart - 1, x, (1 - fpart) * 0.5f);
                    DrawPointAlpha(ipart, x, 1 - fpart + fpart * 0.5f);
                    DrawPointAlpha(ipart + 1, x, fpart + (1 - fpart) * 0.5f);
                    DrawPointAlpha(ipart + 2, x, fpart * 0.5f);
                }
                else
                {
                    DrawPointAlpha(x, ipart - 1, (1 - fpart) * 0.5f);
                    DrawPointAlpha(x, ipart, 1 - fpart + fpart * 0.5f);
                    DrawPointAlpha(x, ipart + 1, fpart + (1 - fpart) * 0.5f);
                    DrawPointAlpha(x, ipart + 2, fpart * 0.5f);
                }
                y += gradient;
            }

            if (steep) DrawPointAlpha(y1, x1, 1f); else DrawPointAlpha(x1, y1, 1f);
        }
    }
}

