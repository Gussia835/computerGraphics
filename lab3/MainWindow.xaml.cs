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

        // ---- Состояние ----
        private enum Mode { Create, Select, Pivot }
        private Mode _mode = Mode.Create;

        private Point? _pivotPoint;                
        private Polygon2D _selectedPolygon;         

        // ---- Визуал ----
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

        // Смещение (трансляция)
        private void ApplyTranslation_Click(object sender, RoutedEventArgs e)
        {
            var poly = GetSelectedOrWarn();
            if (poly == null) return;

            double dx = ParseDouble(TbDx, "dx", 0);
            double dy = ParseDouble(TbDy, "dy", 0);

            // Матрица трансляции
            Matrix3x3 m = Matrix3x3.Translation(dx, dy);
            poly.ApplyTransform(m);

            SetStatus($"Сдвиг ({dx}, {dy}) применён к {poly.Name}");
            Redraw();
        }

        //Поворот вокруг центра масс
        private void ApplyRotationCenter_Click(object sender, RoutedEventArgs e)
        {
            var poly = GetSelectedOrWarn();
            if (poly == null) return;

            double angleDeg = ParseDouble(TbAngleCenter, "угол", 0);
            double angleRad = angleDeg * Math.PI / 180.0;

            Point c = poly.CenterOfMass;

            // M = T(c) × R(θ) × T(−c)
            Matrix3x3 m = Matrix3x3.RotationAroundPoint(angleRad, c.X, c.Y);
            poly.ApplyTransform(m);

            SetStatus($"Поворот {angleDeg}° вокруг центра ({c.X:F1},{c.Y:F1}) → {poly.Name}");
            Redraw();
        }

        //  Поворот вокруг произвольной точки
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

        //  Масштабирование вокруг центра масс
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

        //  Масштабирование вокруг произвольной точки
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


        private void Redraw()
        {
            MainCanvas.Children.Clear();

            // Сетка (лёгкая)
            DrawGrid();

            // Все завершённые полигоны
            for (int i = 0; i < _polygons.Count; i++)
            {
                var poly = _polygons[i];
                var color = _palette[i % _palette.Length];
                bool selected = (poly == _selectedPolygon);
                DrawPolygon(poly, color, selected);
            }

            // Текущий (создаваемый) полигон — пунктир
            if (_currentPolygon.Vertices.Count > 0)
                DrawPolygon(_currentPolygon, Brushes.Gray, false, dashed: true);

            // Опорная точка
            if (_pivotPoint.HasValue)
                DrawCross(_pivotPoint.Value, Brushes.Red, 8);
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

            // --- Точка (1 вершина) ---
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

            // --- Ребро / Полигон (≥2 вершин) ---
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
                // Замкнутый полигон — полупрозрачная заливка
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

            // Вершины — кружки
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

            // Центр масс (для выбранного)
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