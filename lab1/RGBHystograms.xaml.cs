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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace color_converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class RGBHystograms : Window
    {

        private BitmapSource originalBitmap;
        private byte[] originalPixels;

        private int width;
        private int height;
        private int stride;

        private int[] histR;
        private int[] histB;
        private int[] histG;

        public RGBHystograms()
        {
            InitializeComponent();
        }

        private void ButtonLoad_Click(object seneder, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "Image files (*.jpg;*jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(openFileDialog.FileName);
                    bitmapImage.EndInit();

                    originalBitmap = new FormatConvertedBitmap(bitmapImage, PixelFormats.Bgra32, null, 0);

                    width = originalBitmap.PixelWidth;
                    height = originalBitmap.PixelHeight;
                    stride = width * 4;

                    originalPixels = new byte[height * stride];
                    originalBitmap.CopyPixels(originalPixels, stride, 0);

                    OriginalImage.Source = originalBitmap; 


                    buttonProcess.IsEnabled = true;

                } catch (Exception ex)
                {

                    MessageBox.Show("Ошибка загрузки изображения " + ex.Message);
                }
            }
        }

        private void ButtonProcess_Click(object sender, RoutedEventArgs e) {

            if (originalBitmap == null) {

                return;
            }


            byte[] pixelsR = new byte[originalPixels.Length];
            byte[] pixelsG = new byte[originalPixels.Length];
            byte[] pixelsB = new byte[originalPixels.Length];

            histR = new int[256];
            histB = new int[256];
            histG = new int[256];

            for (int i = 0; i < originalPixels.Length; i += 4) { 

                byte b = originalPixels[i];
                byte g = originalPixels[i + 1];
                byte r = originalPixels[i + 2];
                byte a = originalPixels[i + 3];



                pixelsR[i] = 0;
                pixelsR[i + 1] = 0;
                pixelsR[i + 2] = r;
                pixelsR[i + 3] = 255;

                pixelsG[i] = 0;
                pixelsG[i + 1] = g;
                pixelsG[i + 2] = 0;
                pixelsG[i + 3] = 255;

                pixelsB[i] = b;
                pixelsB[i + 1] = 0;
                pixelsB[i + 2] = 0;
                pixelsB[i + 3] = 255;

                
                histR[r]++;
                histG[g]++;
                histB[b]++;
            }

            imageRed.Source = createBitmapSource(pixelsR);
            imageGreen.Source = createBitmapSource(pixelsG);
            imageBlue.Source = createBitmapSource(pixelsB);


            drawHystogram(canvasHistR, histR, System.Windows.Media.Brushes.Red);
            drawHystogram(canvasHistG, histG, System.Windows.Media.Brushes.Green);
            drawHystogram(canvasHistB, histB, System.Windows.Media.Brushes.Blue);
        }

        private void drawHystogram(Canvas canvas, int[] hystogram, System.Windows.Media.Brush color) {
            canvas.Children.Clear();

            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;

            if (w == 0 || h == 0) {

                return;
            }

            int maxVal = 0;
            for (int i = 0; i < 256; ++i) {

                if (hystogram[i] > maxVal) {
                    maxVal = hystogram[i];
                }
            }

            if (maxVal == 0) {
                maxVal = 1;
            }

            double barWidth = w / 256.0;

            for (int i = 0; i < 256; ++i) {

                double barHeight = ((double)hystogram[i] / maxVal) * h;

                Line line = new Line
                {
                    X1 = i * barWidth,
                    Y1 = h,
                    X2 = i * barWidth,
                    Y2 = h - barHeight,
                    Stroke = color,
                    StrokeThickness = barWidth
                };

                canvas.Children.Add(line);

            }

            
        }

        private BitmapSource createBitmapSource(byte[] pixels)
        {
            return BitmapSource.Create(
                width,           
                height,          
                96, 96,          
                PixelFormats.Bgra32, 
                null,            
                pixels,       
                stride         
            );
        }



        private void ButtonBack_Click(object sender, RoutedEventArgs e) {

            MainWindow mainWindow = new MainWindow();

            mainWindow.Show();
            this.Close();
        }
    }
}
