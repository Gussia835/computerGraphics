using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace lab3
{
    public partial class MainWindow : Window
    {
        private readonly List<Polygon2D> _polygons = new List<Polygon2D>();
        private Polygon2D _currentPolygon;
        private int _polygonCounter;

        private enum Mode { Create, Select, Pivot, PointInPolygon, PointVsEdge }
        private Mode _mode = Mode.Create;

        private Point? _pivotPoint;
        private Polygon2D _selectedPolygon;

        private Point? _userPoint;
        private (Polygon2D poly, int edgeIndex)? _selectedEdge;

        private readonly SolidColorBrush[] _palette =
        {
            Brushes.SteelBlue, Brushes.Crimson, Brushes.ForestGreen,
            Brushes.DarkOrange, Brushes.MediumPurple, Brushes.Teal,
            Brushes.Brown, Brushes.DarkMagenta
        };

        public MainWindow()
        {
            InitializeComponent();
            RbCreate.IsChecked = true;
            _currentPolygon = new Polygon2D();
            RefreshTask3Selectors();
        }

        private void ModeChanged(object sender, RoutedEventArgs e)
        {
            if (RbCreate?.IsChecked == true)
            {
                _mode = Mode.Create;
                SetStatus("Режим создания: кликайте по холсту, добавляя вершины.");
            }
            else if (RbSelect?.IsChecked == true)
            {
                _mode = Mode.Select;
                SetStatus("Режим выбора: кликните рядом с вершиной полигона для выбора.");
            }
            else if (RbPivot?.IsChecked == true)
            {
                _mode = Mode.Pivot;
                SetStatus("Режим задания точки: кликните на холст, чтобы задать опорную точку.");
            }
            else if (RbPointInPolygon?.IsChecked == true)
            {
                _mode = Mode.PointInPolygon;
                SetStatus("3а: кликните точку — проверю принадлежность выбранному полигону.");
            }
            else if (RbPointVsEdge?.IsChecked == true)
            {
                _mode = Mode.PointVsEdge;
                SetStatus("3б: кликните точку — определю, слева или справа от ребра.");
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pt = e.GetPosition(MainCanvas);

            switch (_mode)
            {
                case Mode.Create:
                    _currentPolygon.Vertices.Add(pt);
                    SetStatus($"Вершин: {_currentPolygon.Vertices.Count}. " +
                              "Кликайте ещё или «Завершить полигон».");
                    Redraw();
                    break;

                case Mode.Select:
                    TrySelectPolygon(pt);
                    break;

                case Mode.Pivot:
                    _pivotPoint = pt;
                    TbPivotInfo.Text = $"Точка: ({pt.X:F1}, {pt.Y:F1})";
                    TbPivotInfo.Foreground = Brushes.DarkGreen;
                    SetStatus($"Опорная точка задана: ({pt.X:F1}, {pt.Y:F1})");
                    Redraw();
                    break;

                case Mode.PointInPolygon:
                    HandlePointInPolygonClick(pt);
                    break;

                case Mode.PointVsEdge:
                    HandlePointVsEdgeClick(pt);
                    break;
            }
        }

        private void FinishPolygon_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPolygon.Vertices.Count == 0)
            {
                MessageBox.Show("Нет вершин для завершения.", "Внимание");
                return;
            }

            _polygonCounter++;
            _currentPolygon.Name = $"P{_polygonCounter} ({_currentPolygon.Vertices.Count} верш.)";
            _polygons.Add(_currentPolygon);
            LbPolygons.Items.Add(_currentPolygon.Name);
            LbPolygons.SelectedIndex = LbPolygons.Items.Count - 1;

            _currentPolygon = new Polygon2D();
            RefreshTask3Selectors();
            SetStatus("Полигон добавлен. Создавайте следующий или выберите для преобразований.");
            Redraw();
        }

        private void ClearScene_Click(object sender, RoutedEventArgs e)
        {
            _polygons.Clear();
            _currentPolygon = new Polygon2D();
            _selectedPolygon = null;
            _pivotPoint = null;
            _polygonCounter = 0;
            LbPolygons.Items.Clear();
            TbPivotInfo.Text = "Точка не задана";
            TbPivotInfo.Foreground = Brushes.Red;

            _userPoint = null;
            _selectedEdge = null;
            TbTask3Result.Text = "—";
            TbTask3PointInfo.Text = "Точка не задана";
            RefreshTask3Selectors();

            SetStatus("Сцена очищена.");
            Redraw();
        }

        private void TrySelectPolygon(Point clickPt)
        {
            const double threshold = 15;
            Polygon2D best = null;
            double bestDist = double.MaxValue;

            foreach (var poly in _polygons)
            {
                foreach (var v in poly.Vertices)
                {
                    double d = (v - clickPt).Length;
                    if (d < threshold && d < bestDist)
                    {
                        bestDist = d;
                        best = poly;
                    }
                }
            }

            if (best != null)
            {
                _selectedPolygon = best;
                int idx = _polygons.IndexOf(best);
                LbPolygons.SelectedIndex = idx;
                SetStatus($"Выбран: {best.Name}");
            }
            else
            {
                SetStatus("Полигон не найден рядом с кликом.");
            }
            Redraw();
        }

        private void PolygonSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = LbPolygons.SelectedIndex;
            if (idx >= 0 && idx < _polygons.Count)
            {
                _selectedPolygon = _polygons[idx];
                SetStatus($"Выбран: {_selectedPolygon.Name}");
                Redraw();
            }
        }

        private Polygon2D GetSelectedOrWarn()
        {
            if (_selectedPolygon == null)
            {
                MessageBox.Show("Сначала выберите полигон!", "Нет выбора");
                return null;
            }
            return _selectedPolygon;
        }

        private double ParseDouble(TextBox tb, string name, double fallback)
        {
            if (double.TryParse(tb.Text, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out double v))
                return v;
            MessageBox.Show($"Некорректное значение {name}", "Ошибка ввода");
            return fallback;
        }

        private void ApplyTranslation_Click(object sender, RoutedEventArgs e)
        {
            var poly = GetSelectedOrWarn();
            if (poly == null) return;

            double dx = ParseDouble(TbDx, "dx", 0);
            double dy = ParseDouble(TbDy, "dy", 0);

            Matrix3x3 m = Matrix3x3.Translation(dx, dy);
            poly.ApplyTransform(m);

            SetStatus($"Сдвиг ({dx}, {dy}) применён к {poly.Name}");
            Redraw();
        }

        private void ApplyRotationCenter_Click(object sender, RoutedEventArgs e)
        {
            var poly = GetSelectedOrWarn();
            if (poly == null) return;

            double angleDeg = ParseDouble(TbAngleCenter, "угол", 0);
            double angleRad = angleDeg * Math.PI / 180.0;

            Point c = poly.CenterOfMass;
            Matrix3x3 m = Matrix3x3.RotationAroundPoint(angleRad, c.X, c.Y);
            poly.ApplyTransform(m);

            SetStatus($"Поворот {angleDeg}° вокруг центра ({c.X:F1},{c.Y:F1}) → {poly.Name}");
            Redraw();
        }

        private void ApplyRotationPivot_Click(object sender, RoutedEventArgs e)
        {
            var poly = GetSelectedOrWarn();
            if (poly == null) return;
            if (_pivotPoint == null)
            {
                MessageBox.Show("Сначала задайте опорную точку (режим «Задать точку»)!",
                                "Нет точки");
                return;
            }

            double angleDeg = ParseDouble(TbAnglePivot, "угол", 0);
            double angleRad = angleDeg * Math.PI / 180.0;

            Point p = _pivotPoint.Value;
            Matrix3x3 m = Matrix3x3.RotationAroundPoint(angleRad, p.X, p.Y);
            poly.ApplyTransform(m);

            SetStatus($"Поворот {angleDeg}° вокруг ({p.X:F1},{p.Y:F1}) → {poly.Name}");
            Redraw();
        }

        private void ApplyScaleCenter_Click(object sender, RoutedEventArgs e)
        {
            var poly = GetSelectedOrWarn();
            if (poly == null) return;

            double sx = ParseDouble(TbSxCenter, "Sx", 1);
            double sy = ParseDouble(TbSyCenter, "Sy", 1);

            Point c = poly.CenterOfMass;
            Matrix3x3 m = Matrix3x3.ScalingAroundPoint(sx, sy, c.X, c.Y);
            poly.ApplyTransform(m);

            SetStatus($"Масштаб ({sx},{sy}) вокруг центра → {poly.Name}");
            Redraw();
        }

        private void ApplyScalePivot_Click(object sender, RoutedEventArgs e)
        {
            var poly = GetSelectedOrWarn();
            if (poly == null) return;
            if (_pivotPoint == null)
            {
                MessageBox.Show("Сначала задайте опорную точку!", "Нет точки");
                return;
            }

            double sx = ParseDouble(TbSxPivot, "Sx", 1);
            double sy = ParseDouble(TbSyPivot, "Sy", 1);

            Point p = _pivotPoint.Value;
            Matrix3x3 m = Matrix3x3.ScalingAroundPoint(sx, sy, p.X, p.Y);
            poly.ApplyTransform(m);

            SetStatus($"Масштаб ({sx},{sy}) вокруг ({p.X:F1},{p.Y:F1}) → {poly.Name}");
            Redraw();
        }

        private void HandlePointInPolygonClick(Point p)
        {
            _userPoint = p;

            var polygon = CbPolygonForPointIn.SelectedItem as Polygon2D
                          ?? _selectedPolygon;

            if (polygon == null)
            {
                SetStatus("3а: полигон не выбран.");
                Redraw();
                return;
            }

            bool inside = GeometryAlgorithms.IsPointInsidePolygon(p, polygon.Vertices);

            TbTask3Result.Text = inside
                ? $"Точка ВНУТРИ полигона «{polygon.Name}»"
                : $"Точка СНАРУЖИ полигона «{polygon.Name}»";
            TbTask3PointInfo.Text = $"Точка: ({p.X:F1}, {p.Y:F1})";
            SetStatus(TbTask3Result.Text);
            Redraw();
        }

        private void HandlePointVsEdgeClick(Point p)
        {
            _userPoint = p;

            if (_selectedEdge == null)
            {
                SetStatus("3б: ребро не выбрано.");
                Redraw();
                return;
            }

            var (poly, idx) = _selectedEdge.Value;
            if (poly.Vertices.Count < 2) return;

            Point a = poly.Vertices[idx];
            Point b = poly.Vertices[(idx + 1) % poly.Vertices.Count];

            string side = GeometryAlgorithms.ClassifyPointRelativeToEdge(p, a, b);

            TbTask3Result.Text = $"Точка {side} от ребра {idx}→{(idx + 1) % poly.Vertices.Count}";
            TbTask3PointInfo.Text = $"Точка: ({p.X:F1}, {p.Y:F1})";
            SetStatus(TbTask3Result.Text);
            Redraw();
        }

        private void PolygonForPointIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_userPoint.HasValue && _mode == Mode.PointInPolygon)
                HandlePointInPolygonClick(_userPoint.Value);
        }

        private void EdgeForClassification_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbEdgeForClassification.SelectedItem is EdgeItem item)
            {
                _selectedEdge = (item.Polygon, item.EdgeIndex);
                if (_userPoint.HasValue && _mode == Mode.PointVsEdge)
                    HandlePointVsEdgeClick(_userPoint.Value);
                else
                    Redraw();
            }
        }

        private void RefreshTask3Selectors()
        {
            var prevIn = CbPolygonForPointIn.SelectedItem as Polygon2D;
            CbPolygonForPointIn.ItemsSource = null;
            CbPolygonForPointIn.ItemsSource = _polygons;
            if (prevIn != null && _polygons.Contains(prevIn))
                CbPolygonForPointIn.SelectedItem = prevIn;
            else if (_polygons.Count > 0)
                CbPolygonForPointIn.SelectedIndex = 0;

            var edges = new List<EdgeItem>();
            foreach (var poly in _polygons)
            {
                if (poly.Vertices.Count < 2) continue;
                int limit = poly.Vertices.Count == 2 ? 1 : poly.Vertices.Count;
                for (int i = 0; i < limit; i++)
                {
                    int j = (i + 1) % poly.Vertices.Count;
                    edges.Add(new EdgeItem
                    {
                        Polygon = poly,
                        EdgeIndex = i,
                        Display = $"{poly.Name}: {i}→{j}"
                    });
                }
            }
            CbEdgeForClassification.ItemsSource = edges;
            if (edges.Count > 0) CbEdgeForClassification.SelectedIndex = 0;
            else _selectedEdge = null;
        }

        internal class EdgeItem
        {
            public Polygon2D Polygon { get; set; }
            public int EdgeIndex { get; set; }
            public string Display { get; set; }
        }

        private void Redraw()
        {
            MainCanvas.Children.Clear();

            DrawGrid();

            for (int i = 0; i < _polygons.Count; i++)
            {
                var poly = _polygons[i];
                var color = _palette[i % _palette.Length];
                bool selected = (poly == _selectedPolygon);
                DrawPolygon(poly, color, selected);
            }

            if (_currentPolygon.Vertices.Count > 0)
                DrawPolygon(_currentPolygon, Brushes.Gray, false, dashed: true);

            if (_pivotPoint.HasValue)
                DrawCross(_pivotPoint.Value, Brushes.Red, 8);

            if (_userPoint.HasValue)
            {
                var marker = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Red,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(marker, _userPoint.Value.X - 5);
                Canvas.SetTop(marker, _userPoint.Value.Y - 5);
                MainCanvas.Children.Add(marker);
            }

            if (_selectedEdge != null)
            {
                var (poly, idx) = _selectedEdge.Value;
                if (poly.Vertices.Count >= 2)
                {
                    Point a = poly.Vertices[idx];
                    Point b = poly.Vertices[(idx + 1) % poly.Vertices.Count];
                    var hl = new Line
                    {
                        X1 = a.X,
                        Y1 = a.Y,
                        X2 = b.X,
                        Y2 = b.Y,
                        Stroke = Brushes.Orange,
                        StrokeThickness = 4,
                        Opacity = 0.7
                    };
                    MainCanvas.Children.Add(hl);
                }
            }
        }

        private void DrawGrid()
        {
            double w = MainCanvas.ActualWidth > 0 ? MainCanvas.ActualWidth : 800;
            double h = MainCanvas.ActualHeight > 0 ? MainCanvas.ActualHeight : 600;
            var pen = new Pen(Brushes.LightGray, 0.5);

            for (double x = 0; x < w; x += 50)
            {
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = h,
                    Stroke = pen.Brush,
                    StrokeThickness = pen.Thickness
                };
                MainCanvas.Children.Add(line);
            }
            for (double y = 0; y < h; y += 50)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = w,
                    Y2 = y,
                    Stroke = pen.Brush,
                    StrokeThickness = pen.Thickness
                };
                MainCanvas.Children.Add(line);
            }
        }

        private void DrawPolygon(Polygon2D poly, Brush color, bool selected,
                                  bool dashed = false)
        {
            var pts = poly.Vertices;
            if (pts.Count == 0) return;

            if (pts.Count == 1)
            {
                var ell = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = color,
                    Stroke = selected ? Brushes.Gold : Brushes.Black,
                    StrokeThickness = selected ? 2.5 : 1
                };
                Canvas.SetLeft(ell, pts[0].X - 4);
                Canvas.SetTop(ell, pts[0].Y - 4);
                MainCanvas.Children.Add(ell);
                return;
            }

            var polyline = new Polyline
            {
                Points = new PointCollection(pts),
                Stroke = color,
                StrokeThickness = selected ? 3 : 2,
                StrokeLineJoin = PenLineJoin.Round
            };

            if (dashed)
                polyline.StrokeDashArray = new DoubleCollection { 4, 3 };

            if (poly.IsClosed && !dashed)
            {
                var polygon = new System.Windows.Shapes.Polygon
                {
                    Points = new PointCollection(pts),
                    Fill = new SolidColorBrush(((SolidColorBrush)color).Color)
                    {
                        Opacity = 0.15
                    },
                    Stroke = color,
                    StrokeThickness = selected ? 3 : 2,
                    StrokeLineJoin = PenLineJoin.Round
                };
                MainCanvas.Children.Add(polygon);
            }
            else
            {
                MainCanvas.Children.Add(polyline);
            }

            foreach (var v in pts)
            {
                var ell = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.White,
                    Stroke = color,
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(ell, v.X - 3);
                Canvas.SetTop(ell, v.Y - 3);
                MainCanvas.Children.Add(ell);
            }

            if (selected && pts.Count >= 2)
            {
                Point c = poly.CenterOfMass;
                DrawCross(c, Brushes.Gold, 6);
            }
        }

        private void DrawCross(Point pt, Brush color, double size)
        {
            var l1 = new Line
            {
                X1 = pt.X - size,
                Y1 = pt.Y,
                X2 = pt.X + size,
                Y2 = pt.Y,
                Stroke = color,
                StrokeThickness = 2
            };
            var l2 = new Line
            {
                X1 = pt.X,
                Y1 = pt.Y - size,
                X2 = pt.X,
                Y2 = pt.Y + size,
                Stroke = color,
                StrokeThickness = 2
            };
            MainCanvas.Children.Add(l1);
            MainCanvas.Children.Add(l2);
        }

        private void SetStatus(string text)
        {
            if (TbStatus != null)
                TbStatus.Text = text;
        }
    }
}