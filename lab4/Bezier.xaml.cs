using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierSpline
{
    public partial class Bezier : Window
    {
        private List<Point> controlPoints = new List<Point>();
        private List<Ellipse> pointMarkers = new List<Ellipse>();

        private const double nearestRadius = 15.0;

        private double currentScale = 1.0;
        private double offsetX = 0;
        private double offsetY = 0;
        private bool isPanning = false;
        private Point lastPanPosition;

        private bool isDragging = false;
        private int dragIndex = -1;

        public Bezier()
        {
            InitializeComponent();
        }

        private Point ScreenToCanvas(Point screenPoint)
        {
            return new Point(
                (screenPoint.X - offsetX) / currentScale,
                (screenPoint.Y - offsetY) / currentScale
            );
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point mousePos = e.GetPosition(outerCanvas);

            double newScale = currentScale;
            if (e.Delta > 0)
                newScale *= 1.15;
            else
                newScale /= 1.15;

            newScale = Math.Max(0.1, Math.Min(20.0, newScale));

            double scaleChange = newScale / currentScale;
            double newOffsetX = mousePos.X - (mousePos.X - offsetX) * scaleChange;
            double newOffsetY = mousePos.Y - (mousePos.Y - offsetY) * scaleChange;

            currentScale = newScale;
            offsetX = newOffsetX;
            offsetY = newOffsetY;

            ApplyTransform();
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            isPanning = true;
            lastPanPosition = e.GetPosition(outerCanvas);
            outerCanvas.Cursor = Cursors.SizeAll;
            outerCanvas.CaptureMouse();
        }

        private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isPanning)
            {
                isPanning = false;
                outerCanvas.Cursor = Cursors.Arrow;
                outerCanvas.ReleaseMouseCapture();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point screenPos = e.GetPosition(outerCanvas);

            if (isPanning)
            {
                double deltaX = screenPos.X - lastPanPosition.X;
                double deltaY = screenPos.Y - lastPanPosition.Y;

                offsetX += deltaX;
                offsetY += deltaY;

                lastPanPosition = screenPos;
                ApplyTransform();
                return;
            }

            if (isDragging && dragIndex >= 0 && dragIndex < controlPoints.Count)
            {
                Point canvasPos = ScreenToCanvas(screenPos);

                controlPoints[dragIndex] = canvasPos;

                Canvas.SetLeft(pointMarkers[dragIndex], canvasPos.X - 5);
                Canvas.SetTop(pointMarkers[dragIndex], canvasPos.Y - 5);

                Redraw();
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point screenPos = e.GetPosition(outerCanvas);
            Point canvasPos = ScreenToCanvas(screenPos);

            dragIndex = FindNearestPoint(canvasPos);

            if (dragIndex != -1)
            {
                isDragging = true;
                outerCanvas.CaptureMouse();
            }
            else
            {
                AddNewPoint(canvasPos);
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                dragIndex = -1;
                outerCanvas.ReleaseMouseCapture();
            }
        }

        private int FindNearestPoint(Point canvasPos)
        {
            double adjustedRadius = nearestRadius / currentScale;

            for (int i = 0; i < controlPoints.Count; i++)
            {
                Point p = controlPoints[i];
                double dx = p.X - canvasPos.X;
                double dy = p.Y - canvasPos.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= adjustedRadius)
                {
                    return i;
                }
            }
            return -1;
        }

        private void AddNewPoint(Point position)
        {
            foreach (Point existingPoint in controlPoints)
            {
                double dx = existingPoint.X - position.X;
                double dy = existingPoint.Y - position.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < 5 / currentScale)
                {
                    return;
                }
            }

            controlPoints.Add(position);

            Ellipse marker = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Blue,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };

            Canvas.SetLeft(marker, position.X - 5);
            Canvas.SetTop(marker, position.Y - 5);

            innerCanvas.Children.Add(marker);
            pointMarkers.Add(marker);

            Redraw();
        }

        private void Redraw()
        {
            controlPolyline.Points = new PointCollection(controlPoints);
            controlPolyline.Visibility = controlPoints.Count >= 2 ? Visibility.Visible : Visibility.Collapsed;

            int segmentsCount = (controlPoints.Count - 1) / 3;

            if (segmentsCount >= 1)
            {
                List<Point> curvePoints = new List<Point>();

                for (int seg = 0; seg < segmentsCount; seg++)
                {
                    int startIndex = seg * 3;
                    Point p0 = controlPoints[startIndex];
                    Point p1 = controlPoints[startIndex + 1];
                    Point p2 = controlPoints[startIndex + 2];
                    Point p3 = controlPoints[startIndex + 3];

                    for (int i = 0; i <= 50; i++)
                    {
                        double t = (double)i / 50.0;
                        Point curvePoint = CalculateCubicBezier(p0, p1, p2, p3, t);
                        curvePoints.Add(curvePoint);
                    }
                }

                curvePolyline.Points = new PointCollection(curvePoints);
                curvePolyline.Visibility = Visibility.Visible;
            }
            else
            {
                curvePolyline.Points = new PointCollection();
                curvePolyline.Visibility = Visibility.Collapsed;
            }
        }

        private Point CalculateCubicBezier(Point p0, Point p1, Point p2, Point p3, double t)
        {
            double u = 1 - t;
            double u2 = u * u;
            double u3 = u2 * u;
            double t2 = t * t;
            double t3 = t2 * t;

            double x = (p0.X * u3) + (3 * p1.X * u2 * t) + (3 * p2.X * u * t2) + (p3.X * t3);
            double y = (p0.Y * u3) + (3 * p1.Y * u2 * t) + (3 * p2.Y * u * t2) + (p3.Y * t3);

            return new Point(x, y);
        }

        private void ApplyTransform()
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(currentScale, currentScale));
            transformGroup.Children.Add(new TranslateTransform(offsetX, offsetY));
            innerCanvas.RenderTransform = transformGroup;
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            controlPoints.Clear();

            foreach (var marker in pointMarkers)
            {
                innerCanvas.Children.Remove(marker);
            }
            pointMarkers.Clear();

            Redraw();
        }

        private void ResetScale_Click(object sender, RoutedEventArgs e)
        {
            currentScale = 1.0;
            offsetX = 0;
            offsetY = 0;
            ApplyTransform();
        }
    }
}