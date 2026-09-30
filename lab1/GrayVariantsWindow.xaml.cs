using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Linq;


namespace color_converter
{
    public partial class GrayVariantsWindow : Window
    {
        private BitmapImage originalBitmap;
        private int[] histogram1;
        private int[] histogram2;

        public GrayVariantsWindow()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp;*.gif)|*.png;*.jpeg;*.jpg;*.bmp;*.gif|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == true)
                {
                    originalBitmap = new BitmapImage();
                    originalBitmap.BeginInit();
                    originalBitmap.UriSource = new Uri(openFileDialog.FileName);
                    originalBitmap.EndInit();

                    OriginalImage.Source = originalBitmap;

                    GrayscaleImage1.Source = null;
                    GrayscaleImage2.Source = null;
                    HistogramCanvas1.Children.Clear();
                    HistogramCanvas2.Children.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (OriginalImage.Source == null)
            {
                MessageBox.Show("Сначала загрузите изображение!", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap();
                formattedBitmap.BeginInit();
                formattedBitmap.Source = originalBitmap;
                formattedBitmap.DestinationFormat = PixelFormats.Bgra32;
                formattedBitmap.EndInit();

                int width = formattedBitmap.PixelWidth;
                int height = formattedBitmap.PixelHeight;
                (BitmapSource grayscale1, byte[] gray_pixels1) = ConvertToGrayscale(formattedBitmap, width, height, "NTSC");
                GrayscaleImage1.Source = grayscale1;

                (BitmapSource grayscale2, byte[] gray_pixels2) = ConvertToGrayscale(formattedBitmap, width, height, "BT709");
                GrayscaleImage2.Source = grayscale2;

                BitmapSource gray_dif = DifferenceImage(gray_pixels1, gray_pixels2, width, height, formattedBitmap.DpiX, formattedBitmap.DpiY);
                GrayDifferenceImage.Source = gray_dif;

                histogram1 = CalculateHistogram(grayscale1);
                histogram2 = CalculateHistogram(grayscale2);

                DrawHistogram(HistogramCanvas1, histogram1, Colors.Blue);
                DrawHistogram(HistogramCanvas2, histogram2, Colors.Red);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка преобразования: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (BitmapSource, byte[]) ConvertToGrayscale(FormatConvertedBitmap formattedBitmap, int width, int height, string algorithm)
        {
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            byte[] gray_pixels = new byte[height * width];

            formattedBitmap.CopyPixels(pixels, stride, 0);

            int j = 0;

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte blue = pixels[i];
                byte green = pixels[i + 1];
                byte red = pixels[i + 2];
                byte alpha = pixels[i + 3];

                byte gray = CalculateGrayValue(red, green, blue, algorithm);

                gray_pixels[j] = gray;
                j += 1;
            }

            return (BitmapSource.Create(
                width, height,
                formattedBitmap.DpiX, formattedBitmap.DpiY,
                PixelFormats.Gray8,
                null,
                gray_pixels,
                width), gray_pixels);
        }

        private BitmapSource DifferenceImage(byte[] gray_pixels1, byte[] gray_pixels2, int width, int height, double DpiX, double DpiY)
        {
            byte[] dif_pixels = new byte[gray_pixels1.Length];

            byte max = 0;
            byte min = 255;

            for (int i = 0; i < gray_pixels1.Length; i++)
            {
                dif_pixels[i] = (byte)Math.Abs(gray_pixels1[i] - gray_pixels2[i]);
                if (dif_pixels[i] < min)
                    min = dif_pixels[i];
                if (dif_pixels[i] > max)
                    max = dif_pixels[i];
            }

            for (int i = 0; i < gray_pixels1.Length; i++)
            {
                dif_pixels[i] = (byte)((dif_pixels[i] - min) * 255 / (max - min));
            }

            return BitmapSource.Create(
                width, height,
                DpiX, DpiY,
                PixelFormats.Gray8,
                null,
                dif_pixels,
                width);
        }

        private byte CalculateGrayValue(byte red, byte green, byte blue, string algorithm)
        {
            double gray = 0;

            switch (algorithm)
            {
                case "NTSC":
                    gray = 0.299 * red + 0.587 * green + 0.114 * blue;
                    break;
                case "BT709":
                    gray = 0.2126 * red + 0.7152 * green + 0.0722 * blue;
                    break;
                default:
                    gray = 0.299 * red + 0.587 * green + 0.114 * blue;
                    break;
            }

            return (byte)Math.Max(0, Math.Min(255, gray));
        }

        private int[] CalculateHistogram(BitmapSource image)
        {
            int[] histogram = new int[256];

            FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap();
            formattedBitmap.BeginInit();
            formattedBitmap.Source = image;
            formattedBitmap.DestinationFormat = PixelFormats.Gray8;
            formattedBitmap.EndInit();

            int width = formattedBitmap.PixelWidth;
            int height = formattedBitmap.PixelHeight;
            int stride = width;
            byte[] pixels = new byte[height * stride];

            formattedBitmap.CopyPixels(pixels, stride, 0);

            foreach (byte intensity in pixels)
            {
                histogram[intensity]++;
            }

            return histogram;
        }

        private void DrawHistogram(Canvas canvas, int[] histogram, Color color)
        {
            canvas.Children.Clear();

            if (histogram == null) return;

            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;

            int maxCount = histogram.Max();
            if (maxCount == 0) return;

            Brush brush = new SolidColorBrush(color);

            for (int i = 0; i < histogram.Length; i++)
            {
                if (histogram[i] > 0)
                {
                    double columnWidth = canvasWidth / histogram.Length;
                    double columnHeight = (double)histogram[i] / maxCount * canvasHeight * 0.9;

                    Rectangle rect = new Rectangle
                    {
                        Width = Math.Max(1, columnWidth),
                        Height = columnHeight,
                        Fill = brush,
                        Stroke = Brushes.Black,
                        StrokeThickness = 0.5
                    };

                    Canvas.SetLeft(rect, i * columnWidth);
                    Canvas.SetBottom(rect, 0);

                    canvas.Children.Add(rect);
                }
            }

            AddAxisLabels(canvas, maxCount);
        }

        private void AddAxisLabels(Canvas canvas, int maxCount)
        {
            TextBlock yLabel = new TextBlock
            {
                Text = $"Max: {maxCount}",
                FontSize = 10,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(yLabel, 5);
            Canvas.SetTop(yLabel, 5);
            canvas.Children.Add(yLabel);

            for (int i = 0; i <= 255; i += 64)
            {
                TextBlock xLabel = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(xLabel, (i / 255.0) * canvas.ActualWidth);
                Canvas.SetBottom(xLabel, -15);
                canvas.Children.Add(xLabel);
            }
        }
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {

            MainWindow mainWindow = new MainWindow();

            mainWindow.Show();
            this.Close();
        }
    }
}