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
            if (currentColor == borderColor) return;

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
                if (GetPixel(x, y) != borderColor)
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

            uint currentColor = GetPixel(x, y);

   
            if (currentColor == borderColor || currentColor == 0xFF808080)
                return;

            int xLeft = x;
            while (xLeft >= 0)
            {
                uint c = GetPixel(xLeft, y);
                if (c == borderColor || c == 0xFF808080) break;
                --xLeft;
            }
            ++xLeft;

            int xRight = x;
            while (xRight < width)
            {
                uint c = GetPixel(xRight, y);
                if (c == borderColor || c == 0xFF808080) break;
                ++xRight;
            }
            --xRight;

            //временный маркер
            for (int i = xLeft; i <= xRight; ++i)
            {
                SetPixel(i, y, 0xFF808080); // Серый маркер
            }

           
            FillNextScanlinePattern(xLeft, xRight, y - 1, borderColor, isCyclic);
            FillNextScanlinePattern(xLeft, xRight, y + 1, borderColor, isCyclic);

            // ПОТОМ заменяем серый на цвета паттерна
            for (int i = xLeft; i <= xRight; ++i)
            {
                int sampleX = isCyclic ? ((i % patWidth) + patWidth) % patWidth : i;
                int sampleY = isCyclic ? ((y % patHeight) + patHeight) % patHeight : y;

                if (!isCyclic && (i >= patWidth || y >= patHeight))
                {
                    SetPixel(i, y, 0xFFAAAAAA); // Светло-серый для выхода за пределы
                    continue;
                }

                int patternIndex = sampleY * patWidth + sampleX;
                SetPixel(i, y, (uint)patternPixels[patternIndex]);
            }
        }

        private void FillNextScanlinePattern(int prevLeft, int prevRight, int y, uint borderColor, bool isCyclic)
        {
            if (y < 0 || y >= height) return;

            int x = prevLeft;
            bool spanStarted = false;

            while (x <= prevRight)
            {
                uint c = GetPixel(x, y);

                
                if (c != borderColor && c != 0xFF808080)
                {
                    if (!spanStarted)
                    {
                        spanStarted = true;
                        FillScanlinePattern(x, y, borderColor, isCyclic);
                    }
                }
                else
                {
                    spanStarted = false;
                }
                ++x;
            }
        }

        // ЗАДАНИЕ 1в 
        private void TraceBoundary_Click(object sender, RoutedEventArgs e)
        {
            if (clickX == -1 || clickY == -1)
            {
                StatusText.Text = "Сначала кликните на или внутри фигуры!";
                return;
            }

            StatusText.Text = "Поиск границы...";

            int startX = -1, startY = -1;

            for (int radius = 0; radius <= 200 && startX == -1; radius++)
            {
                for (int dy = -radius; dy <= radius && startX == -1; dy++)
                {
                    for (int dx = -radius; dx <= radius && startX == -1; dx++)
                    {
                        int x = clickX + dx;
                        int y = clickY + dy;

                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            if (GetPixel(x, y) == 0xFF000000) // Чёрный пиксель
                            {
                                startX = x;
                                startY = y;
                                break;
                            }
                        }
                    }
                }
            }

            if (startX == -1)
            {
                StatusText.Text = "Граница не найдена рядом с кликом!";
                return;
            }

                
            List<Point> contour = TraceBoundaryFromPoint(startX, startY, 0xFF000000);

            if (contour.Count > 0)
            {
              
                foreach (Point p in contour)
                {
                    int px = (int)p.X;
                    int py = (int)p.Y;

                    
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            SetPixel(px + dx, py + dy, 0xFF00FF00); // Зелёный
                        }
                    }
                }
                UpdateScreen();
                StatusText.Text = $"Граница области обведена! Найдено {contour.Count} точек.";
            }
            else
            {
                StatusText.Text = "Не удалось обойти границу!";
            }
        }

        
        private List<Point> TraceBoundaryFromPoint(int startX, int startY, uint borderColor)
        {
            List<Point> boundaryPoints = new List<Point>();
            boundaryPoints.Add(new Point(startX, startY));

            int currX = startX, currY = startY;
            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
            int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
            int enterDir = 4; 

            int maxIterations = 10000; 
            int iterations = 0;

            do
            {
                iterations++;
                if (iterations > maxIterations)
                {
                    StatusText.Text = "Превышено максимальное число итераций!";
                    break;
                }

                int bgDir = -1;

       
                for (int i = 0; i < 8; i++)
                {
                    int checkDir = (enterDir + 5 + i) % 8;
                    int nx = currX + dx[checkDir];
                    int ny = currY + dy[checkDir];

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (GetPixel(nx, ny) != borderColor)
                        {
                            bgDir = checkDir;
                            break;
                        }
                    }
                }

                if (bgDir == -1) break; 

        
                int nextX = -1, nextY = -1, nextDir = -1;

                for (int i = 0; i < 8; i++)
                {
                    int checkDir = (bgDir + i) % 8;
                    int nx = currX + dx[checkDir];
                    int ny = currY + dy[checkDir];

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (GetPixel(nx, ny) == borderColor)
                        {
                            nextX = nx;
                            nextY = ny;
                            nextDir = checkDir;
                            break;
                        }
                    }
                }

                if (nextX == -1) break; 

    
                currX = nextX;
                currY = nextY;
                enterDir = (nextDir + 4) % 8; 

         
                if (currX == startX && currY == startY)
                    break;

                boundaryPoints.Add(new Point(currX, currY));

            } while (true);

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
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point pos = e.GetPosition(MyImage);

                if (pos.X < 0 || pos.X >= MyImage.ActualWidth ||
                    pos.Y < 0 || pos.Y >= MyImage.ActualHeight)
                    return;

                double scaleX = width / MyImage.ActualWidth;
                double scaleY = height / MyImage.ActualHeight;
                int x = (int)(pos.X * scaleX);
                int y = (int)(pos.Y * scaleY);

                if (lastX == -1 || lastY == -1)
                {
                    lastX = x;
                    lastY = y;
                }

                int steps = Math.Max(Math.Abs(x - lastX), Math.Abs(y - lastY));

                steps = Math.Max(steps, 10);

                for (int i = 0; i <= steps; i++)
                {
                    int px = lastX + (x - lastX) * i / steps;
                    int py = lastY + (y - lastY) * i / steps;

                    for (int dx = -2; dx <= 2; dx++)
                        for (int dy = -2; dy <= 2; dy++)
                            SetPixel(px + dx, py + dy, 0xFF000000);
                }

                lastX = x;
                lastY = y;
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
            StatusText.Text = "Заливка завершена!";
        }

        private void FillPattern_Click(object sender, RoutedEventArgs e)
        {
            if (patternPixels == null) { StatusText.Text = "Загрузите паттерн!"; return; }
            if (clickX == -1) { StatusText.Text = "Кликните внутри фигуры!"; return; }
            StatusText.Text = "Заливка паттерном...";
            FillScanlinePattern(clickX, clickY, 0xFF000000, CbCyclic.IsChecked == true);
            UpdateScreen();
            StatusText.Text = "Готово";
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

      
        private void TestCircle_Click(object sender, RoutedEventArgs e)
        {
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
            clickX = cx; clickY = cy; 
            StatusText.Text = "Тестовый круг нарисован";
        }

        private void CreateTestPatternChess_Click(object sender, RoutedEventArgs e)
        {
            patWidth = 32;
            patHeight = 32;
            patternPixels = new int[patWidth * patHeight];

            for (int y = 0; y < patHeight; y++)
            {
                for (int x = 0; x < patWidth; x++)
                {
                
                    int blockX = x / 8;
                    int blockY = y / 8;

                    if ((blockX + blockY) % 2 == 0)
                        patternPixels[y * patWidth + x] = unchecked((int)0xFFFFFFFF); // Белый
                    else
                        patternPixels[y * patWidth + x] = unchecked((int)0xFF0000FF); // Красный (BGRA!)
                }
            }
            StatusText.Text = "красно-белый паттерн 32x32 создан!";
        }

        private void CreateTestPatternLines_Click(object sender, RoutedEventArgs e)
        {
            patWidth = 16;
            patHeight = 16;
            patternPixels = new int[patWidth * patHeight];

            for (int y = 0; y < patHeight; y++)
            {
                for (int x = 0; x < patWidth; x++)
                {
                    if (y % 4 < 2)
                        patternPixels[y * patWidth + x] = unchecked((int)0xFFFFFFFF); // Белый
                    else
                        patternPixels[y * patWidth + x] = unchecked((int)0xFF0000FF); // Красный
                }
            }
            StatusText.Text = "Паттерн 'Полоски' создан!";
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {

            MainWindow mainWindow = new MainWindow();

            mainWindow.Show();
            this.Close();
        }
    }
}
