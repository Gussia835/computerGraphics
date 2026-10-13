using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace lab4
{
    public partial class MountainWindow : Window
    {
        private readonly MountainGenerator _generator = new MountainGenerator();
        private bool _isInitialized = false;

        public MountainWindow()
        {
            InitializeComponent();
            _isInitialized = true;
            UpdateLandscape();
        }

        private void UpdateLandscape()
        {
            if (!_isInitialized || MountainCanvas == null) return;

            MountainCanvas.Children.Clear();

            int step = (int)sliderSteps.Value;
            double roughness = sliderRoughness.Value;
            double leftHeight = sliderLeft.Value;
            double rightHeight = sliderRight.Value;

            if (!int.TryParse(txtSeed.Text, out int seed)) seed = 42;

            lblSteps.Text = step.ToString();
            lblRoughness.Text = roughness.ToString("F2");

            List<double> heights = _generator.Generate(step, leftHeight, rightHeight, roughness, seed);

            double canvasWidth = MountainCanvas.ActualWidth;
            double canvasHeight = MountainCanvas.ActualHeight;

            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            Polygon mountainPolygon = new Polygon
            {
                Fill = new LinearGradientBrush(Color.FromRgb(40, 50, 65), Color.FromRgb(20, 25, 35), 90),
                Stroke = Brushes.LightBlue,
                StrokeThickness = 2
            };

            mountainPolygon.Points.Add(new Point(0, canvasHeight));
            double stepX = canvasWidth / (heights.Count - 1);

            for (int i = 0; i < heights.Count; i++)
            {
                double x = i * stepX;
                double y = canvasHeight - (heights[i] * canvasHeight);
                mountainPolygon.Points.Add(new Point(x, y));

                if (step <= 5)
                {
                    Ellipse marker = new Ellipse { Width = 6, Height = 6, Fill = Brushes.OrangeRed, Margin = new Thickness(x - 3, y - 3, 0, 0) };
                    MountainCanvas.Children.Add(marker);
                }
            }

            mountainPolygon.Points.Add(new Point(canvasWidth, canvasHeight));
            MountainCanvas.Children.Insert(0, mountainPolygon);
        }

        private void Parameter_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateLandscape();
        private void MountainCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateLandscape();

        private void BtnNewSeed_Click(object sender, RoutedEventArgs e)
        {
            txtSeed.Text = new Random().Next(1, 99999).ToString();
            UpdateLandscape();
        }
    }

    public class MountainGenerator
    {
        public List<double> Generate(int step, double leftHeight, double rightHeight, double roughness, int seed)
        {
            Random random = new Random(seed);
            int pointsCount = (1 << step) + 1;
            double[] heights = new double[pointsCount];

            heights[0] = leftHeight;
            heights[heights.Length - 1] = rightHeight;

            for (int i = 1; i <= step; i++)
            {
                int segments = 1 << (i - 1);
                int stride = (heights.Length - 1) / segments;
                int halfStride = stride / 2;

                double segmentLength = 1.0 / segments;
                double maxDisplacement = roughness * segmentLength;

                for (int s = 0; s < segments; s++)
                {
                    int leftIdx = s * stride;
                    int rightIdx = (s + 1) * stride;
                    int midIdx = leftIdx + halfStride;

                    double avg = (heights[leftIdx] + heights[rightIdx]) / 2.0;
                    double displacement = (random.NextDouble() * 2.0 - 1.0) * maxDisplacement;

                    heights[midIdx] = avg + displacement;

                    if (heights[midIdx] < 0) heights[midIdx] = 0;
                    if (heights[midIdx] > 1) heights[midIdx] = 1;
                }
            }
            return new List<double>(heights);
        }
    }
}

