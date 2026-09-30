using System;
using System.IO;
using System.Text;
using System.Threading;
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

namespace color_converter
{
    public partial class RGBtoHSVconverter : Window
    {
        private byte[]? origPixels;
        private int width, height, stride;
        private CancellationTokenSource? cts;

        public RGBtoHSVconverter()
        {
            InitializeComponent(); 

            string defaultPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "input.jpg");
            if (System.IO.File.Exists(defaultPath))
            {
                LoadTargetImage(defaultPath); 
            }
        }

        private void LoadTargetImage(string imagePath)
        {
            if (!System.IO.File.Exists(imagePath)) return;

            if (h != null) h.Value = 0;
            if (s != null) s.Value = 0;
            if (v != null) v.Value = 0;

            BitmapImage bitmapSource = new BitmapImage();
            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.OnLoad; 
            bitmapSource.UriSource = new Uri(imagePath);
            bitmapSource.EndInit();

            FormatConvertedBitmap converted = new FormatConvertedBitmap(bitmapSource, System.Windows.Media.PixelFormats.Bgra32, null, 0);

            width = converted.PixelWidth;
            height = converted.PixelHeight;
            stride = width * 4;

            origPixels = new byte[stride * height];
            converted.CopyPixels(origPixels, stride, 0);
            img.Source = converted;
        }

        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Все файлы (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                LoadTargetImage(openFileDialog.FileName);
            }
        }


        private async void Update(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (origPixels == null || img == null || h == null || s == null || v == null) return;

            cts?.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            try { await Task.Delay(1, token); }
            catch (TaskCanceledException) { return; }

            double shiftH = h.Value;
            double shiftS = s.Value / 100.0;
            double shiftV = v.Value / 100.0;
            byte[] localOrig = origPixels;

            try
            {
                byte[]? modifiedPixels = await Task.Run(() =>
                {
                    byte[] pix = (byte[])localOrig.Clone();

                    for (int i = 0; i < pix.Length; i += 4)
                    {
                        if (token.IsCancellationRequested) return null;

                        double b = pix[i] / 255.0;
                        double g = pix[i + 1] / 255.0;
                        double r = pix[i + 2] / 255.0;

                        double max = Math.Max(r, Math.Max(g, b));
                        double min = Math.Min(r, Math.Min(g, b));
                        double delta = max - min;

                        double hue = 0;
                        if (delta != 0)
                        {
                            if (max == r) hue = (g - b) / delta + (g < b ? 6 : 0);
                            else if (max == g) hue = (b - r) / delta + 2;
                            else hue = (r - g) / delta + 4;
                            hue *= 60;
                        }
                        double sat = max == 0 ? 0 : delta / max;
                        double val = max;

                        hue = (hue + shiftH + 360) % 360;
                        sat = Math.Clamp(sat + shiftS, 0.0, 1.0);
                        val = Math.Clamp(val + shiftV, 0.0, 1.0);

                        double newR = 0, newG = 0, newB = 0;
                        double sector = hue / 60.0;
                        int sectorIndex = (int)Math.Floor(sector);
                        double f = sector - sectorIndex;

                        double p = val * (1.0 - sat);
                        double q = val * (1.0 - sat * f);
                        double t = val * (1.0 - sat * (1.0 - f));

                        switch (sectorIndex % 6)
                        {
                            case 0: newR = val; newG = t; newB = p; break;
                            case 1: newR = q; newG = val; newB = p; break;
                            case 2: newR = p; newG = val; newB = t; break;
                            case 3: newR = p; newG = q; newB = val; break;
                            case 4: newR = t; newG = p; newB = val; break;
                            case 5: newR = val; newG = p; newB = q; break;
                        }

                        pix[i] = (byte)(newB * 255);
                        pix[i + 1] = (byte)(newG * 255);
                        pix[i + 2] = (byte)(newR * 255);
                    }
                    return pix;
                }, token);

                if (modifiedPixels != null && !token.IsCancellationRequested)
                {
                    var wBitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
                    wBitmap.WritePixels(new Int32Rect(0, 0, width, height), modifiedPixels, stride, 0);
                    img.Source = wBitmap;
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void Save(object sender, System.Windows.RoutedEventArgs e)
        {
            if (img.Source is BitmapSource currentSource)
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(currentSource));
                string outPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "out.jpg");
                using (var stream = new FileStream(outPath, FileMode.Create)) { encoder.Save(stream); }
                MessageBox.Show($"Сохранено в:\n{outPath}");
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
