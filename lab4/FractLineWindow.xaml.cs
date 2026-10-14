using lab4;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace lab4
{
    public partial class FractLineWindow : Window
    {
        private LSystem _currentLSystem;

        public FractLineWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "*.txt|*.txt|Все файлы (*.*)|*.*",
                Title = "Выберите файл с описанием L-системы",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            _currentLSystem = LSystem.LoadFromFile(openFileDialog.FileName);

        }

        private void DrawButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLSystem == null)
            {
                MessageBox.Show("Сначала загрузите файл с описанием L-системы.",
                                "Нет данных",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(IterationsTextBox.Text, out int iterations) ||
                iterations < 0 || iterations > 10)
            {
                MessageBox.Show("Введите корректное число итераций (0–10).",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            try
            {
                string commands = _currentLSystem.Generate(iterations);

                var turtle = new Turtle();
                turtle.Interpret(
                    commands,
                    _currentLSystem.Angle,
                    _currentLSystem.InitialDirection,
                    _currentLSystem.StepLength,
                    randomVariation: 0.0,
                    rnd: new Random());

                DrawSegments(turtle.Segments);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при построении фрактала:\n{ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void DrawSegments(List<(Point A, Point B)> segments)
        {
            DrawCanvas.Children.Clear();

            if (segments == null || segments.Count == 0)
            {
                DrawCanvas.Width = 0;
                DrawCanvas.Height = 0;
                return;
            }

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var seg in segments)
            {
                minX = Math.Min(minX, Math.Min(seg.A.X, seg.B.X));
                minY = Math.Min(minY, Math.Min(seg.A.Y, seg.B.Y));
                maxX = Math.Max(maxX, Math.Max(seg.A.X, seg.B.X));
                maxY = Math.Max(maxY, Math.Max(seg.A.Y, seg.B.Y));
            }

            double srcWidth = maxX - minX;
            double srcHeight = maxY - minY;
            if (srcWidth <= 0) srcWidth = 1;
            if (srcHeight <= 0) srcHeight = 1;

            double margin = 20;
            double availW = Math.Max(50, ActualWidth - margin * 2);
            double availH = Math.Max(50, ActualHeight - margin * 2 - 80);

            double scale = Math.Min(availW / srcWidth, availH / srcHeight);
            if (scale > 20) scale = 20;

            Point Transform(Point p)
            {
                double px = (p.X - minX) * scale + margin;
                double py = (p.Y - minY) * scale + margin;
                return new Point(px, py);
            }

            var brush = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50));
            brush.Freeze();

            foreach (var seg in segments)
            {
                var a = Transform(seg.A);
                var b = Transform(seg.B);
                var line = new Line
                {
                    X1 = a.X,
                    Y1 = a.Y,
                    X2 = b.X,
                    Y2 = b.Y,
                    Stroke = brush,
                    StrokeThickness = 1.0,
                    SnapsToDevicePixels = true
                };
                DrawCanvas.Children.Add(line);
            }

            DrawCanvas.Width = srcWidth * scale + margin * 2;
            DrawCanvas.Height = srcHeight * scale + margin * 2;
        }
    }
}