using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using lab6.Core;
using lab6.Core.Primitives;

namespace lab6
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Polyhedron originalPolyhedron;
        private Polyhedron transformedPolyhedron;

        public MainWindow()
        {
            InitializeComponent();
            LoadShape();
        }

        private void LoadShape()
        {
            if (ShapeSelector.SelectedIndex == 0)
                originalPolyhedron = Tetrahedron.Create(1.0);
            else
                originalPolyhedron = Cube.Create(2.0);

            transformedPolyhedron = originalPolyhedron.Clone();
            UpdateStats();
            Render();
        }

        private void ShapeSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadShape();
        }

        private void Transform_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            transformedPolyhedron = originalPolyhedron.Clone();

            var transform = Matrix4x4.Identity;
            transform = transform * Matrix4x4.Scale(ScaleSlider.Value, ScaleSlider.Value, ScaleSlider.Value);
            transform = transform * Matrix4x4.Translate(TranslateX.Value, TranslateY.Value, TranslateZ.Value);

            if (ReflectXYCheck.IsChecked == true)
                transform = transform * Matrix4x4.ReflectXY();

            if (ReflectXZCheck.IsChecked == true)
                transform = transform * Matrix4x4.ReflectXZ();

            if (ReflectYZCheck.IsChecked == true)
                transform = transform * Matrix4x4.ReflectYZ();

            transformedPolyhedron.ApplyTransform(transform);
            Render();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            TranslateX.Value = 0;
            TranslateY.Value = 0;
            TranslateZ.Value = 0;
            ScaleSlider.Value = 1;

           
            ReflectXYCheck.IsChecked = false;
            ReflectXZCheck.IsChecked = false; 
            ReflectYZCheck.IsChecked = false; 

            Transform_Changed(null, null);
        }

        private void Render()
        {
            RenderCanvas.Children.Clear();

            double centerX = RenderCanvas.ActualWidth / 2;
            double centerY = RenderCanvas.ActualHeight / 2;
            double scale = 80;

            double cameraDistance = 2.0;

            Console.WriteLine($"\n--- Отрисовка --- Текущий TranslateZ = {TranslateZ.Value}");

            foreach (var face in transformedPolyhedron.Faces)
            {
                var points = new PointCollection();

                
                double debugZ = transformedPolyhedron.Vertices[face.VertexIndices[0]].Z;
                double debugFactor = cameraDistance / (cameraDistance + debugZ);
                Console.WriteLine($"  Грань: Z={debugZ:F2} -> perspectiveFactor={debugFactor:F2} (размер x{debugFactor:F2})");

                foreach (var vertexIndex in face.VertexIndices)
                {
                    var vertex = transformedPolyhedron.Vertices[vertexIndex];

                    double perspectiveFactor = cameraDistance / (cameraDistance + vertex.Z);

                    double screenX = centerX + (vertex.X * perspectiveFactor) * scale;
                    double screenY = centerY - (vertex.Y * perspectiveFactor) * scale;

                    points.Add(new System.Windows.Point(screenX, screenY));
                }

             
                byte alpha = (byte)(255 * debugFactor);
                if (alpha > 255) alpha = 255;
                if (alpha < 30) alpha = 30; 

                var polygon = new System.Windows.Shapes.Polygon
                {
                    Points = points,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(alpha, 173, 216, 230))
                };
                RenderCanvas.Children.Add(polygon);
            }
        }

        private void UpdateStats()
        {
            StatsText.Text = $"Вершин: {originalPolyhedron.Vertices.Count}\n" +
                             $"Граней: {originalPolyhedron.Faces.Count}";
        }
    }
}