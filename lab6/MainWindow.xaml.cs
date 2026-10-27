using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using lab6.Core;
using lab6.Core.Primitives;

namespace lab6
{
    public partial class MainWindow : Window
    {
        private Polyhedron _originalPoly;
        private Matrix4x4 _modelMatrix = Matrix4x4.Identity;

        public MainWindow()
        {
            InitializeComponent();
            // Создаем стартовый куб
            _originalPoly = PolyhedronFactory.CreateCube();
            UpdateStats();
            Render3D();
        }

        private void ShapeSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            switch (ShapeSelector.SelectedIndex)
            {
                case 0: _originalPoly = PolyhedronFactory.CreateTetrahedron(); break;
                case 1: _originalPoly = PolyhedronFactory.CreateCube(); break;
                case 2: _originalPoly = PolyhedronFactory.CreateOctahedron(); break;
                case 3: _originalPoly = PolyhedronFactory.CreateIcosahedron(); break;
            }
            _modelMatrix = Matrix4x4.Identity;
            UpdateStats();
            Render3D();
        }

        private void ApplyTranslate_Click(object sender, RoutedEventArgs e)
        {
            double dx = ReadDouble(TxBox, 0), dy = ReadDouble(TyBox, 0), dz = ReadDouble(TzBox, 0);
            _modelMatrix = _modelMatrix * Matrix4x4.Translate(dx, dy, dz);
            Render3D();
        }

        private void ApplyScale_Click(object sender, RoutedEventArgs e)
        {
            double k = ReadDouble(ScaleBox, 1);
            if (Math.Abs(k) < 1e-6) return;

            // Центр многогранника (в текущих координатах)
            var center = new lab6.Core.Point(0, 0, 0);
            foreach (var v in _originalPoly.Vertices) center = new lab6.Core.Point(center.X + v.X, center.Y + v.Y, center.Z + v.Z);
            center = new lab6.Core.Point(center.X / _originalPoly.Vertices.Count, center.Y / _originalPoly.Vertices.Count, center.Z / _originalPoly.Vertices.Count);
            center = center.Transform(_modelMatrix);

            // T(c) * S * T(-c)
            var m = Matrix4x4.Translate(-center.X, -center.Y, -center.Z)
                  * Matrix4x4.Scale(k, k, k)
                  * Matrix4x4.Translate(center.X, center.Y, center.Z);

            _modelMatrix = _modelMatrix * m;
            Render3D();
        }

        private void ApplyAxisRotation_Click(object sender, RoutedEventArgs e)
        {
            double rad = ReadDouble(AxisAngleBox, 0) * Math.PI / 180.0;
            Matrix4x4 m = Matrix4x4.Identity;

            double cos = Math.Cos(rad), sin = Math.Sin(rad);
            if (AxisCombo.SelectedIndex == 0) { m.M22 = cos; m.M23 = -sin; m.M32 = sin; m.M33 = cos; } // X
            else if (AxisCombo.SelectedIndex == 1) { m.M11 = cos; m.M13 = -sin; m.M31 = sin; m.M33 = cos; } // Y
            else { m.M11 = cos; m.M12 = -sin; m.M21 = sin; m.M22 = cos; } // Z

            _modelMatrix = _modelMatrix * m;
            Render3D();
        }

        private void ApplyReflect_Click(object sender, RoutedEventArgs e)
        {
            Matrix4x4 m = Matrix4x4.Identity;
            if (ReflectCombo.SelectedIndex == 0) m = Matrix4x4.ReflectXY();
            else if (ReflectCombo.SelectedIndex == 1) m = Matrix4x4.ReflectXZ();
            else m = Matrix4x4.ReflectYZ();

            _modelMatrix = _modelMatrix * m;
            Render3D();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _modelMatrix = Matrix4x4.Identity;
            Render3D();
        }

        private void ProjectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || Viewport == null) return;

            if (ProjectionCombo.SelectedIndex == 0)
            {
                // Переключение на Перспективную проекцию
                var pCamera = new PerspectiveCamera
                {
                    Position = new Point3D(0, 0, 4.5),
                    LookDirection = new Vector3D(0, 0, -1),
                    UpDirection = new Vector3D(0, 1, 0),
                    FieldOfView = 60
                };
                Viewport.Camera = pCamera;
                Camera3D = pCamera; // Обновляем ссылку, сохраненную в x:Name
            }
            else
            {
                // Переключение на Аксонометрическую (Ортографическую) проекцию
                var oCamera = new OrthographicCamera
                {
                    Position = new Point3D(0, 0, 4.5),
                    LookDirection = new Vector3D(0, 0, -1),
                    UpDirection = new Vector3D(0, 1, 0),
                    Width = 3.0 // Задает масштаб отображения в ортогональном пространстве
                };
                Viewport.Camera = oCamera;
            }
        }

        private void ApplyCenterAxisRotation_Click(object sender, RoutedEventArgs e)
        {
            if (_originalPoly == null) return;

            double rad = ReadDouble(AxisAngleBox, 0) * Math.PI / 180.0;

            var center = new lab6.Core.Point(0, 0, 0);
            foreach (var v in _originalPoly.Vertices)
            {
                center = new lab6.Core.Point(center.X + v.X, center.Y + v.Y, center.Z + v.Z);
            }
            center = new lab6.Core.Point(
                center.X / _originalPoly.Vertices.Count,
                center.Y / _originalPoly.Vertices.Count,
                center.Z / _originalPoly.Vertices.Count
            );
            center = center.Transform(_modelMatrix);

            Matrix4x4 R = Matrix4x4.Identity;
            if (AxisCombo.SelectedIndex == 0) R = Matrix4x4.RotateX(rad);
            else if (AxisCombo.SelectedIndex == 1) R = Matrix4x4.RotateY(rad);
            else R = Matrix4x4.RotateZ(rad);

            Matrix4x4 transformMatrix = Matrix4x4.Translate(-center.X, -center.Y, -center.Z)
                                        * R
                                        * Matrix4x4.Translate(center.X, center.Y, center.Z);

            _modelMatrix = _modelMatrix * transformMatrix;
            Render3D();
        }

        private double ReadDouble(TextBox tb, double def)
        {
            tb.Text = tb.Text.Replace('.', ',');
            return double.TryParse(tb.Text, out var v) ? v : def;
        }

        private void UpdateStats()
        {
            StatsText.Text = $"Вершин: {_originalPoly.Vertices.Count}\nГраней: {_originalPoly.Faces.Count}";
        }

        // --- НОВЫЙ РЕНДЕР (WPF 3D) ---
        private void Render3D()
        {
            if (_originalPoly == null) return;

            // Оставляем свет (первые 2 элемента ModelGroup)
            while (ModelGroup.Children.Count > 2)
                ModelGroup.Children.RemoveAt(2);

            var mesh = new MeshGeometry3D();

            // Применяем матрицу к каждой вершине
            foreach (var v in _originalPoly.Vertices)
            {
                var transformed = v.Transform(_modelMatrix);
                mesh.Positions.Add(new System.Windows.Media.Media3D.Point3D(transformed.X, transformed.Y, transformed.Z));
            }

            // Добавляем грани (триангуляция)
            var indices = new Int32Collection();
            foreach (var face in _originalPoly.Faces)
            {
                for (int i = 1; i < face.VertexIndices.Length - 1; i++)
                {
                    indices.Add(face.VertexIndices[0]);
                    indices.Add(face.VertexIndices[i]);
                    indices.Add(face.VertexIndices[i + 1]);
                }
            }
            mesh.TriangleIndices = indices;

            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(100, 150, 220)));
            var geoModel = new GeometryModel3D(mesh, material);
            geoModel.BackMaterial = material;

            ModelGroup.Children.Add(geoModel);
        }
    }
}