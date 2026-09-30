using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace Raster
{
    /// <summary>
    /// Логика взаимодействия для FillImage.xaml
    /// </summary>
    public partial class FillImage : Window
    {
        private WriteableBitmap canvas;
        private int width, height;
        private int[] pixels;
        private int[] patternPixels;
        private int patWidth, patHeight;
        private int lastX = -1, lastY = -1;
        private int clickX = -1, clickY = -1;

        public FillImage()
        {
            InitializeComponent();
            InitializeCanvas();
        }

        private void InitializeCanvas()
        {
            width = 800;
            height = 600;
            pixels = new int[width * height];
            canvas = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            MyImage.Source = canvas;
            ClearCanvas();
        }

        private void ClearCanvas()
        {
            uint white = 0xFFFFFFFF;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = (int)white;
            UpdateScreen();
            StatusText.Text = "Холст очищен. Нарисуйте фигуру или нажмите 'Тестовый круг'";
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            ClearCanvas();
        }

        private void SetPixel(int x, int y, uint color)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            pixels[y * width + x] = (int)color;
        }

        private uint GetPixel(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return 0;
            return (uint)pixels[y * width + x];
        }

        private void UpdateScreen()
        {
            canvas.Lock();
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, canvas.BackBuffer, pixels.Length);
            canvas.AddDirtyRect(new Int32Rect(0, 0, width, height));
            canvas.Unlock();
        }

        // ЗАДАНИЕ 1а
        private void FillScanlineColor(int x, int y, uint borderColor, uint fillColor)
        {
            if (y < 0 || y >= height) return;
            uint currentColor = GetPixel(x, y);
            if (currentColor == borderColor || currentColor == fillColor) return;

            int xLeft = x;
            while (xLeft >= 0 && GetPixel(xLeft, y) != borderColor && GetPixel(xLeft, y) != fillColor)
                --xLeft;
            ++xLeft;

            int xRight = x;
            while (xRight < width && GetPixel(xRight, y) != borderColor && GetPixel(xRight, y) != fillColor)
                ++xRight;
            --xRight;

            for (int i = xLeft; i <= xRight; ++i)
                SetPixel(i, y, fillColor);

            FillNextScanline(xLeft, xRight, y - 1, borderColor, fillColor);
            FillNextScanline(xLeft, xRight, y + 1, borderColor, fillColor);
        }

        private void FillNextScanline(int prevLeft, int prevRight, int y, uint borderColor, uint fillColor)
        {
            if (y < 0 || y >= height) return;
            int x = prevLeft;
            bool spanStarted = false;

            while (x <= prevRight)
            {
                if (GetPixel(x, y) != borderColor && GetPixel(x, y) != fillColor)
                {
                    if (!spanStarted)
                    {
                        spanStarted = true;
                        FillScanlineColor(x, y, borderColor, fillColor);
                    }
                }
                else
                {
                    spanStarted = false;
                }
                ++x;
            }
        }

        //ЗАДАНИЕ 1б 
        private void FillScanlinePattern(int x, int y, uint borderColor, bool isCyclic)
        {
            if (y < 0 || y >= height) return;
            if (GetPixel(x, y) == borderColor) return;

            int xLeft = x;
            while (xLeft >= 0 && GetPixel(xLeft, y) != borderColor && !IsPatterned(GetPixel(xLeft, y)))
                --xLeft;
            ++xLeft;

            int xRight = x;
            while (xRight < width && GetPixel(xRight, y) != borderColor && !IsPatterned(GetPixel(xRight, y)))
                ++xRight;
            --xRight;

            for (int i = xLeft; i <= xRight; ++i)
            {
                int sampleX = isCyclic ? ((i % patWidth) + patWidth) % patWidth : i;
                int sampleY = isCyclic ? ((y % patHeight) + patHeight) % patHeight : y;

                if (!isCyclic && (i >= patWidth || y >= patHeight))
                {
                    SetPixel(i, y, 0xFF808080);
                    continue;
                }

                int patternIndex = sampleY * patWidth + sampleX;
                SetPixel(i, y, (uint)patternPixels[patternIndex]);
            }

            FillNextScanlinePattern(xLeft, xRight, y - 1, borderColor, isCyclic);
            FillNextScanlinePattern(xLeft, xRight, y + 1, borderColor, isCyclic);
        }

        private void FillNextScanlinePattern(int prevLeft, int prevRight, int y, uint borderColor, bool isCyclic)
        {
            if (y < 0 || y >= height) return;
            int x = prevLeft;
            bool spanStarted = false;

            while (x <= prevRight)
            {
                uint c = GetPixel(x, y);
                if (c != borderColor && !IsPatterned(c))
                {
                    if (!spanStarted)
                    {
                        spanStarted = true;
                        FillScanlinePattern(x, y, borderColor, isCyclic);
                    }
                }
                else spanStarted = false;
                ++x;
            }
        }

        private bool IsPatterned(uint color)
        {
            return color != 0xFFFFFFFF && color != 0xFF000000 && color != 0xFF808080;
        }

        // ЗАДАНИЕ 1в 
        private List<Point> TraceBoundary(uint borderColor)
        {
            List<Point> boundaryPoints = new List<Point>();
            int startX = -1, startY = -1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (GetPixel(x, y) == borderColor)
                    {
                        startX = x; startY = y;
                        break;
                    }
                }
                if (startX != -1) break;
            }

            if (startX == -1) return boundaryPoints;
            boundaryPoints.Add(new Point(startX, startY));

            int currX = startX, currY = startY;
            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
            int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
            int enterDir = 4;

            do
            {
                int bgDir = -1;
                for (int i = 0; i < 8; i++)
                {
                    int checkDir = (enterDir + 5 + i) % 8;
                    int nx = currX + dx[checkDir], ny = currY + dy[checkDir];
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && GetPixel(nx, ny) != borderColor)
                    {
                        bgDir = checkDir;
                        break;
                    }
                }
                if (bgDir == -1) break;

                int nextX = -1, nextY = -1, nextDir = -1;
                for (int i = 0; i < 8; i++)
                {
                    int checkDir = (bgDir + i) % 8;
                    int nx = currX + dx[checkDir], ny = currY + dy[checkDir];
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && GetPixel(nx, ny) == borderColor)
                    {
                        nextX = nx; nextY = ny; nextDir = checkDir;
                        break;
                    }
                }
                if (nextX == -1) break;

                currX = nextX; currY = nextY;
                enterDir = (nextDir + 4) % 8;
                boundaryPoints.Add(new Point(currX, currY));

            } while (currX != startX || currY != startY);

            return boundaryPoints;
        }

        // ОБРАБОТЧИКИ
        private void MyImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Point pos = e.GetPosition(MyImage);
                // Масштабируем координаты мыши в координаты пикселей
                double scaleX = width / MyImage.ActualWidth;
                double scaleY = height / MyImage.ActualHeight;
                lastX = (int)(pos.X * scaleX);
                lastY = (int)(pos.Y * scaleY);
                clickX = lastX;
                clickY = lastY;
                StatusText.Text = $"Точка: ({clickX}, {clickY})";
            }
        }

        private void MyImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && lastX != -1)
            {
                Point pos = e.GetPosition(MyImage);
                double scaleX = width / MyImage.ActualWidth;
                double scaleY = height / MyImage.ActualHeight;
                int x = (int)(pos.X * scaleX);
                int y = (int)(pos.Y * scaleY);

                int steps = Math.Max(Math.Abs(x - lastX), Math.Abs(y - lastY));
                for (int i = 0; i <= steps; i++)
                {
                    int px = lastX + (x - lastX) * i / (steps == 0 ? 1 : steps);
                    int py = lastY + (y - lastY) * i / (steps == 0 ? 1 : steps);
                    // Толстая линия 3 пикселя
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            SetPixel(px + dx, py + dy, 0xFF000000);
                }
                lastX = x; lastY = y;
                UpdateScreen();
            }
        }

        private void MyImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            lastX = -1; lastY = -1;
        }

        private void FillColor_Click(object sender, RoutedEventArgs e)
        {
            if (clickX == -1) { StatusText.Text = "Кликните внутри фигуры!"; return; }
            StatusText.Text = "Заливка...";
            FillScanlineColor(clickX, clickY, 0xFF000000, 0xFF0000FF); // Красный в BGRA
            UpdateScreen();
            StatusText.Text = "✓ Заливка завершена!";
        }

        private void FillPattern_Click(object sender, RoutedEventArgs e)
        {
            if (patternPixels == null) { StatusText.Text = "Загрузите паттерн!"; return; }
            if (clickX == -1) { StatusText.Text = "Кликните внутри фигуры!"; return; }
            StatusText.Text = "Заливка паттерном...";
            FillScanlinePattern(clickX, clickY, 0xFF000000, CbCyclic.IsChecked == true);
            UpdateScreen();
            StatusText.Text = "✓ Готово!";
        }

        private void TraceBoundary_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Поиск границы...";
            List<Point> contour = TraceBoundary(0xFF000000);
            if (contour.Count > 0)
            {
                foreach (Point p in contour)
                    SetPixel((int)p.X, (int)p.Y, 0xFF00FF00); // Зеленый
                UpdateScreen();
                StatusText.Text = $"✓ Найдено {contour.Count} точек!";
            }
            else StatusText.Text = "Граница не найдена!";
        }

        private void LoadPattern_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Images|*.png;*.jpg|All|*.*";
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bmp = new BitmapImage(new Uri(dlg.FileName));
                    patWidth = bmp.PixelWidth;
                    patHeight = bmp.PixelHeight;
                    patternPixels = new int[patWidth * patHeight];
                    bmp.CopyPixels(patternPixels, patWidth * 4, 0);
                    StatusText.Text = $"Паттерн {patWidth}x{patHeight} загружен!";
                }
                catch (Exception ex) { StatusText.Text = "Ошибка: " + ex.Message; }
            }
        }

        // ДОБАВЬТЕ ЭТУ КНОПКУ В XAML И ЭТОТ МЕТОД - для теста!
        private void TestCircle_Click(object sender, RoutedEventArgs e)
        {
            // Рисуем идеальный круг для тестирования
            int cx = 400, cy = 300, r = 150;
            for (int angle = 0; angle < 360; angle++)
            {
                double rad = angle * Math.PI / 180.0;
                int x = cx + (int)(r * Math.Cos(rad));
                int y = cy + (int)(r * Math.Sin(rad));
                for (int dx = -2; dx <= 2; dx++)
                    for (int dy = -2; dy <= 2; dy++)
                        SetPixel(x + dx, y + dy, 0xFF000000);
            }
            UpdateScreen();
            clickX = cx; clickY = cy; // Центр для заливки
            StatusText.Text = "Тестовый круг нарисован! Кликните 'Залить цветом'";
        }

        private void CreateTestPattern_Click(object sender, RoutedEventArgs e)
        {
            // Создаем шахматный паттерн 16x16
            patWidth = 16;
            patHeight = 16;
            patternPixels = new int[patWidth * patHeight];

            for (int y = 0; y < patHeight; y++)
            {
                for (int x = 0; x < patWidth; x++)
                {
                    if ((x + y) % 2 == 0)
                        patternPixels[y * patWidth + x] = (int)0xFF0000FF;
                    else
                        patternPixels[y * patWidth + x] = (int)0xFFFF0000;
                }
            }

            StatusText.Text = "✓ Шахматный паттерн 16x16 создан!";
        }
    }
}
