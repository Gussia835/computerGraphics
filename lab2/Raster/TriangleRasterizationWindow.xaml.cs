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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Raster
{
    /// <summary>
    /// Логика взаимодействия для TriangleRasterizationWindow.xaml
    /// </summary>
    public partial class TriangleRasterizationWindow : Window
    {
        private ImageMatrix currentImageMatrix;
        private Point[] selectedPoints = new Point[3];
        private int currentPointIndex = 0;
        private Ellipse[] pointMarkers = new Ellipse[3];
        public event EventHandler<PixelClickEventArgs> PixelClicked;
        public TriangleRasterizationWindow()
        {
            InitializeComponent();
            InitializePointMarkers();
        }
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();

            mainWindow.Show();
            this.Close();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int width = (int)ImageContainer.ActualWidth;
            int height = (int)ImageContainer.ActualHeight;

            currentImageMatrix = new ImageMatrix(width, height);
            DisplayImage.Source = currentImageMatrix.MatrixToImage();
        }
        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentImageMatrix == null) return;

            if (currentPointIndex >= 3)
            {
                MessageBox.Show("Все 3 точки уже установлены!");
                return;
            }

            Point clickPoint = e.GetPosition(DisplayImage);
            int pixelX = (int)clickPoint.X;
            int pixelY = (int)clickPoint.Y;

            if (pixelX >= 0 && pixelX < currentImageMatrix.Width &&
                pixelY >= 0 && pixelY < currentImageMatrix.Height)
            {
                selectedPoints[currentPointIndex] = new Point(pixelX, pixelY);

                pointMarkers[currentPointIndex].Visibility = Visibility.Visible;
                Canvas.SetLeft(pointMarkers[currentPointIndex], clickPoint.X - 5);
                Canvas.SetTop(pointMarkers[currentPointIndex], clickPoint.Y - 5);

                currentImageMatrix.SetSelectedPoint(currentPointIndex, pixelX, pixelY);

                currentPointIndex++;
            }
        }
        private void DrawTriangleButton_Click(object sender, RoutedEventArgs e)
        {
            bool PointsSelected = true;
            for (int i = 0; i < 3; i++)
            {
                Point p = currentImageMatrix.GetSelectedPoint(i);
                if (p.X == -1)
                {
                    PointsSelected = false;
                    break;
                }
            }

            if (!PointsSelected)
            {
                MessageBox.Show("Выберите все 3 точки на изображении");
                return;
            }

            Pixel[] targetPixels = GetCurrentPixels();
            currentImageMatrix.SetTargetPixels(targetPixels);

            currentImageMatrix.DrawTriangle();

            for (int i = 0; i < pointMarkers.Length; i++)
                pointMarkers[i].Visibility = Visibility.Collapsed;

            DisplayImage.Source = currentImageMatrix.MatrixToImage();
        }
        private void ClearSelectedPointsButton_Click(object sender, RoutedEventArgs e)
        {
            currentImageMatrix.ClearSelectedPoints();

            for (int i = 0; i < pointMarkers.Length; i++)
                pointMarkers[i].Visibility = Visibility.Collapsed;

            currentPointIndex = 0;

            currentImageMatrix.ClearMatrix();
            DisplayImage.Source = currentImageMatrix.MatrixToImage();
        }
        private void InitializePointMarkers()
        {
            for (int i = 0; i < 3; i++)
            {
                pointMarkers[i] = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Colors.Red),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2,
                    Visibility = Visibility.Collapsed
                };
                PointsCanvas.Children.Add(pointMarkers[i]);
            }
        }
        public class Pixel
        {
            public byte R { get; set; }
            public byte G { get; set; }
            public byte B { get; set; }
            public byte A { get; set; }
            public Pixel(byte r, byte g, byte b, byte a = 255)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }

            public bool Equals(Pixel other)
            {
                if (ReferenceEquals(other, null))
                    return false;

                return R == other.R && G == other.G && B == other.B && A == other.A;
            }
            public static bool operator ==(Pixel left, Pixel right)
            {
                if (ReferenceEquals(left, right))
                    return true;

                if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                    return false;

                return left.R == right.R && left.G == right.G && left.B == right.B && left.A == right.A;
            }
            public static bool operator !=(Pixel left, Pixel right)
            {
                return !(left == right);
            }
        }

        public class ImageMatrix
        {
            Pixel[,] matrix { get; set; }
            private Point[] selectedPoints = new Point[3];
            private Pixel[] targetPixels = new Pixel[3];
            public int Width { get; private set; }
            public int Height { get; private set; }
            public ImageMatrix(int width, int height)
            {
                Width = width;
                Height = height;

                ClearMatrix();

                ClearSelectedPoints();
            }

            private Pixel[,] ImageToMatrix(BitmapSource bitmap)
            {
                FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap();
                formattedBitmap.BeginInit();
                formattedBitmap.Source = bitmap;
                formattedBitmap.DestinationFormat = PixelFormats.Bgra32;
                formattedBitmap.EndInit();

                int width = formattedBitmap.PixelWidth;
                int height = formattedBitmap.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];
                formattedBitmap.CopyPixels(pixels, stride, 0);

                Pixel[,] matr = new Pixel[width, height];
                Pixel[] arr = new Pixel[height * width];
                int k = 0;

                for (int i = 0; i < height * stride; i += 4)
                {
                    arr[k] = new Pixel(pixels[i + 2], pixels[i + 1], pixels[i], pixels[i + 3]);
                    k++;
                }
                k = 0;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        matr[x, y] = arr[k];
                        k++;
                    }
                return matr;
            }

            public BitmapSource MatrixToImage()
            {
                int stride = Width * 4;

                byte[] pixels = new byte[Height * stride];

                int index = 0;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Pixel pixel = matrix[x, y];

                        pixels[index] = pixel.B;
                        pixels[index + 1] = pixel.G;
                        pixels[index + 2] = pixel.R;
                        pixels[index + 3] = pixel.A;
                        index += 4;
                    }
                }

                BitmapSource bitmap = BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
                return bitmap;
            }

            public void SetSelectedPoint(int index, int x, int y)
            {
                if (index >= 0 && index < 3)
                {
                    selectedPoints[index] = new Point(x, y);
                }
            }
            public void SetTargetPixels(Pixel[] pixels)
            {
                if (pixels != null && pixels.Length == 3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        targetPixels[i] = pixels[i];
                    }
                }
            }
            public Point GetSelectedPoint(int index)
            {
                if (index >= 0 && index < 3)
                    return selectedPoints[index];
                return new Point(-1, -1);
            }
            public Pixel GetTargetPixel(int index)
            {
                if (index >= 0 && index < 3 && targetPixels[index] != null)
                    return targetPixels[index];
                return new Pixel(255, 255, 255);
            }
            public void ClearSelectedPoints()
            {
                for (int i = 0; i < 3; i++)
                {
                    selectedPoints[i] = new Point(-1, -1);
                }
            }
            public void ClearMatrix()
            {
                matrix = new Pixel[Width, Height];
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        matrix[x, y] = new Pixel(255, 255, 255);
                    }
                }
            }
            public double Dist(int x1, int y1, int x2, int y2)
            {
                return Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            }
            public double AreaTr(int xa, int ya, int xb, int yb, int xc, int yc)
            {
                return ((xb - xa) * (yc - ya) - (xc - xa) * (yb - ya)) / 2.0;
            }
            public (double, double, double) barycentric(int x, int y, Point p1, Point p2, Point p3)
            {
                double areaABC = AreaTr((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, (int)p3.X, (int)p3.Y);
                double a = AreaTr(x, y, (int)p2.X, (int)p2.Y, (int)p3.X, (int)p3.Y) / areaABC;
                double b = AreaTr((int)p1.X, (int)p1.Y, x, y, (int)p3.X, (int)p3.Y) / areaABC;
                double c = AreaTr((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, x, y) / areaABC;
                return (a, b, c);
            }
            public void DrawTriangle()
            {
                Point p1 = selectedPoints[0];
                Point p2 = selectedPoints[1];
                Point p3 = selectedPoints[2];

                if (AreaTr((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, (int)p3.X, (int)p3.Y) < 1e-6)
                {
                    MessageBox.Show("Точки не образуют треугольник");
                    return;
                }

                Pixel pix1 = targetPixels[0];
                Pixel pix2 = targetPixels[1];
                Pixel pix3 = targetPixels[2];

                int xmin = (int)Math.Min(p1.X, Math.Min(p2.X, p3.X));
                int xmax = (int)Math.Max(p1.X, Math.Max(p2.X, p3.X));
                int ymin = (int)Math.Min(p1.Y, Math.Min(p2.Y, p3.Y));
                int ymax = (int)Math.Max(p1.Y, Math.Max(p2.Y, p3.Y));

                for (int y = ymin; y <= ymax; y++)
                    for (int x = xmin; x <= xmax; x++)
                    {
                        var (a, b, c) = barycentric(x, y, p1, p2, p3);
                        if (a >= 0 && b >= 0 && c >= 0)
                        {
                            byte r = (byte)(a * pix1.R + b * pix2.R + c * pix3.R);
                            byte g = (byte)(a * pix1.G + b * pix2.G + c * pix3.G);
                            byte b1 = (byte)(a * pix1.B + b * pix2.B + c * pix3.B);

                            matrix[x, y] = new Pixel(r, g, b1);
                        }
                    }
            }
        }
        public Pixel[] GetCurrentPixels()
        {
            byte r1 = 255, g1 = 255, b1 = 255;
            byte.TryParse(RedValue1.Text, out r1);
            byte.TryParse(GreenValue1.Text, out g1);
            byte.TryParse(BlueValue1.Text, out b1);

            byte r2 = 255, g2 = 255, b2 = 255;
            byte.TryParse(RedValue2.Text, out r2);
            byte.TryParse(GreenValue2.Text, out g2);
            byte.TryParse(BlueValue2.Text, out b2);

            byte r3 = 255, g3 = 255, b3 = 255;
            byte.TryParse(RedValue3.Text, out r3);
            byte.TryParse(GreenValue3.Text, out g3);
            byte.TryParse(BlueValue3.Text, out b3);

            Pixel[] pixels = new Pixel[3];
            pixels[0] = new Pixel(r1, g1, b1);
            pixels[1] = new Pixel(r2, g2, b2);
            pixels[2] = new Pixel(r3, g3, b3);

            return pixels;
        }
    }

    public class PixelClickEventArgs : EventArgs
    {
        public int X { get; }
        public int Y { get; }

        public PixelClickEventArgs(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
