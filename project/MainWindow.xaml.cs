using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DelaunayVoronoi
{
    /// <summary>
    /// Interactively add points, perform Delaunay Triangulation and create a Voronoi Diagram.
    /// 
    /// Credits: Original code of the Delaunay Triangulation / Voronoi Diagram creation by Rafael Kuebler.
    /// Please visit https://github.com/RafaelKuebler/DelaunayVoronoi
    /// 
    /// This is an interactive wrapper which uses the framework created by Rafael Kuebler. 
    /// This wrapper is intended for learning purposes, performance was neglected.
    /// If you need performance, please use Rafael's framework directly, it's very fast.
    /// </summary>
    public partial class MainWindow : Window
    {
        private DelaunayTriangulator delaunay = new DelaunayTriangulator();
        private Voronoi voronoi = new Voronoi();

        IEnumerable<Point> points;
        int initialPointCount = 5;

        bool mousePressedDuringMove = false;
        int movementPointIndex = -1;

        Random random = new Random();

        enum FillPattern
        {
            Diagonal,
            Horizontal,
            Vertical,
            Cross,
            Circle,
            Ellipse
        }

        /// <summary>
        /// A map of <point index, color> which will be used to fill the Voronoi polygons
        /// </summary>
        Dictionary<int, Color> colorMap = new Dictionary<int, Color>();

        public MainWindow()
        {
            InitializeComponent();

            cbFillPattern.ItemsSource = Enum.GetValues(typeof(FillPattern));
            cbFillPattern.SelectedItem = FillPattern.Diagonal;

            // ensure the graph isn't painted outside of the canvas
            Graph.ClipToBounds = true;

            // mouse event handlers
            Graph.MouseLeftButtonDown += MouseLeftButtonDownHandler; // TODO find out why the click handler doesn't work on mouse move
            Graph.MouseMove += MouseMoveHandler;

            #region checkbox handlers
            // checkbox visualization changes
            cbDrawCircumCenters.Checked += GraphUpdateHandler;
            cbDrawCircumCenters.Unchecked += GraphUpdateHandler;

            cbDrawCircumCircles.Checked += GraphUpdateHandler;
            cbDrawCircumCircles.Unchecked += GraphUpdateHandler;

            cbDrawPoints.Checked += GraphUpdateHandler;
            cbDrawPoints.Unchecked += GraphUpdateHandler;

            cbDrawDelaunay.Checked += GraphUpdateHandler;
            cbDrawDelaunay.Unchecked += GraphUpdateHandler;

            cbDrawVoronoi.Checked += GraphUpdateHandler;
            cbDrawVoronoi.Unchecked += GraphUpdateHandler;

            cbDrawVoronoiFillCurrent.Checked += GraphUpdateHandler;
            cbDrawVoronoiFillCurrent.Unchecked += GraphUpdateHandler;

            cbDrawVoronoiFillAll.Checked += GraphUpdateHandler;
            cbDrawVoronoiFillAll.Unchecked += GraphUpdateHandler;

            #endregion checkbox handlers

            // create the graph with initial settings
            InitGraph();
        }

        /// <summary>
        /// Clear the graph
        /// </summary>
        private void ClearGraph()
        {
            this.points = delaunay.GeneratePoints(0, 800, 400);

            CreateGraph();
        }

        /// <summary>
        /// Clear the graph and add a number of points.
        /// </summary>
        private void InitGraph()
        {
            int pointCount = initialPointCount;
            if (!Int32.TryParse(tfPointCount.Text, out pointCount))
            {
                pointCount = initialPointCount;
            }

            if( pointCount  < 0)
            {
                pointCount = 0;
            }

            this.points = delaunay.GeneratePoints(pointCount, 800, 400);

            CreateGraph();
        }

 
        /// <summary>
        /// Add new point at mouse click position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            // skip if continuous update during mouse move is enabled
            if (cbContinuousUpdate.IsChecked.GetValueOrDefault())
                return;

            // get clicked position
            Point point = new Point(e.GetPosition(Graph).X, e.GetPosition(Graph).Y);

            // add new point to points list
            ((List<Point>)points).Add(point);

            CreateGraph();
        }


        /// <summary>
        /// Constantly modify a point and redraw the diagram.
        /// Note: Canvas needs to have a background color in order to detect events. Even transparent works, just don't let it be null.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            // skip if continuous update during mouse move isn't enabled
            if (!cbContinuousUpdate.IsChecked.GetValueOrDefault())
                return;

            #region Move Click Handler
            // for some reason (probably because Canvas.Children.Clear the click handler doesn't work when move handler is active
            // => we handle the clicking manually
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                mousePressedDuringMove = true;
            }
            if (e.LeftButton == MouseButtonState.Released)
            {
                if (mousePressedDuringMove)
                {
                    // get clicked position
                    Point newPoint = new Point(e.GetPosition(Graph).X, e.GetPosition(Graph).Y);

                    // add new point to points list
                    ((List<Point>)points).Add(newPoint);

                    CreateGraph();

                    mousePressedDuringMove = false;

                    return;
                }
            }
            #endregion Move Click Handler


            // get clicked position
            Point point = new Point(e.GetPosition(Graph).X, e.GetPosition(Graph).Y);

            // add new point to points list
            int count = ((List<Point>)points).Count;
            if (count > 2)
            {
                movementPointIndex = count - 1;
                ((List<Point>)points)[movementPointIndex] = point;
            }

            CreateGraph();
        }


        /// <summary>
        /// Add points of the given pattern.
        /// </summary>
        /// <param name="pattern"></param>
        private void AddPoints(FillPattern pattern)
        {
            switch (pattern)
            {
                case FillPattern.Horizontal:

                    for (int i = 0; i < 20; i++)
                    {
                        Point point = new Point(100 + i * 30, 100);
                        ((List<Point>)points).Add(point);
                    }
                    break;

                case FillPattern.Vertical:

                    for (int i = 0; i < 20; i++)
                    {
                        Point point = new Point(100, 100 + i * 30);
                        ((List<Point>)points).Add(point);
                    }
                    break;

                case FillPattern.Diagonal:
                    for (int i = 0; i < 20; i++)
                    {
                        Point point = new Point(100 + i * 30, 100 + i * 30);
                        ((List<Point>)points).Add(point);
                    }
                    break;

                case FillPattern.Cross:

                    ((List<Point>)points).Add(new Point(200, 100));
                    ((List<Point>)points).Add(new Point(200, 200));
                    ((List<Point>)points).Add(new Point(200, 300));
                    ((List<Point>)points).Add(new Point(100, 200));
                    ((List<Point>)points).Add(new Point(300, 200));

                    break;

                case FillPattern.Circle:

                    int angleStepCircle = 30;

                    for (int i = 0; i < 360; i += angleStepCircle)
                    {
                        float radius = 120f;
                        int x = (int)(Math.Cos(Math.PI / 180 * i) * radius);
                        int y = (int)(Math.Sin(Math.PI / 180 * i) * radius);

                        Point point = new Point(400 + x, 150 + y);
                        ((List<Point>)points).Add(point);
                    }
                    break;

                case FillPattern.Ellipse:

                    int angleStepEllipse = 20;

                    for (int i = 0; i < 360; i += angleStepEllipse)
                    {
                        float radiusX = 180f;
                        float radiusY = 100;

                        int x = (int)(Math.Cos(Math.PI / 180 * i) * radiusX);
                        int y = (int)(Math.Sin(Math.PI / 180 * i) * radiusY);

                        Point point = new Point(400 + x, 200 + y);
                        ((List<Point>)points).Add(point);
                    }
                    break;
            }
        }

        /// <summary>
        /// Clear any calculated data out of the points list so that we can recreate the graph using the same points
        /// </summary>
        private void ResetData()
        {

            foreach (Point point in points)
            {
                point.AdjacentTriangles.Clear();

            }

        }

        /// <summary>
        /// Create the graph using the points list
        /// </summary>
        private void CreateGraph()
        {
            // null-check required because of checkbox events during startup
            if (points == null)
                return;

            // clear the canvas
            Graph.Children.Clear();

            // reset data
            ResetData();

            // delaunay
            var delaunayTimer = Stopwatch.StartNew();
            var triangulation = delaunay.BowyerWatson(points);
            delaunayTimer.Stop();

            // voronoi
            var voronoiTimer = Stopwatch.StartNew();
            var vornoiEdges = voronoi.GenerateEdgesFromDelaunay(triangulation);
            voronoiTimer.Stop();

            #region visualization

            if (cbDrawDelaunay.IsChecked.GetValueOrDefault())
            {
                DrawTriangulation(triangulation);

            }

            if (cbDrawPoints.IsChecked.GetValueOrDefault())
            {
                DrawPoints(points);

            }

            if (cbDrawCircumCenters.IsChecked.GetValueOrDefault())
            {
                DrawCircumCenters(triangulation);
            }

            if (cbDrawCircumCircles.IsChecked.GetValueOrDefault())
            {
                DrawCircumCircles(triangulation);
            }

            if (cbDrawVoronoiFillCurrent.IsChecked.GetValueOrDefault())
            {
                DrawVoronoiFillCurrent();
            }

            if (cbDrawVoronoiFillAll.IsChecked.GetValueOrDefault())
            {
                DrawVoronoiFillAll();
            }

            if (cbDrawVoronoi.IsChecked.GetValueOrDefault())
            {
                DrawVoronoi(vornoiEdges);

            }
            #endregion visualization

        }

        /// <summary>
        /// Draw the point list
        /// </summary>
        /// <param name="points"></param>
        private void DrawPoints(IEnumerable<Point> points)
        {
            foreach (var point in points)
            {
                var myEllipse = new Ellipse
                {
                    Fill = System.Windows.Media.Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 10,
                    Height = 10
                };

                var ellipseX = point.X - 0.5 * myEllipse.Height;
                var ellipseY = point.Y - 0.5 * myEllipse.Width;
                myEllipse.Margin = new Thickness(ellipseX, ellipseY, 0, 0);

                Graph.Children.Add(myEllipse);
            }
        }

        /// <summary>
        /// Draw the circumcenter points of the triangulation
        /// </summary>
        /// <param name="triangulation"></param>
        private void DrawCircumCenters(IEnumerable<Triangle> triangulation)
        {
            foreach (var triangle in triangulation)
            {
                var myEllipse = new Ellipse
                {
                    Stroke = System.Windows.Media.Brushes.Blue,
                    StrokeThickness = 1,
                    Fill = System.Windows.Media.Brushes.LightBlue,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 10,
                    Height = 10
                };

                var ellipseX = triangle.Circumcenter.X - myEllipse.Height * 0.5;
                var ellipseY = triangle.Circumcenter.Y - myEllipse.Width * 0.5;

                myEllipse.Margin = new Thickness(ellipseX, ellipseY, 0, 0);

                Graph.Children.Add(myEllipse);

            }
        }

        /// <summary>
        /// Draw the circumcircles of the triangulation
        /// </summary>
        /// <param name="triangulation"></param>
        private void DrawCircumCircles(IEnumerable<Triangle> triangulation)
        {
            foreach (Triangle triangle in triangulation)
            {
                var myEllipse = new Ellipse
                {
                    Stroke = System.Windows.Media.Brushes.LightBlue,
                    StrokeThickness = 1,
                    //Fill = System.Windows.Media.Brushes.AliceBlue,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = System.Math.Sqrt(triangle.RadiusSquared) * 2,
                    Height = System.Math.Sqrt(triangle.RadiusSquared) * 2
                };

                var ellipseX = triangle.Circumcenter.X - myEllipse.Height * 0.5;
                var ellipseY = triangle.Circumcenter.Y - myEllipse.Width * 0.5;

                myEllipse.Margin = new Thickness(ellipseX, ellipseY, 0, 0);

                Graph.Children.Add(myEllipse);

            }
        }

        /// <summary>
        /// Draw the delaunay graph
        /// </summary>
        /// <param name="triangulation"></param>
        private void DrawTriangulation(IEnumerable<Triangle> triangulation)
        {
            var edges = new List<Edge>();
            foreach (var triangle in triangulation)
            {
                edges.Add(new Edge(triangle.Vertices[0], triangle.Vertices[1]));
                edges.Add(new Edge(triangle.Vertices[1], triangle.Vertices[2]));
                edges.Add(new Edge(triangle.Vertices[2], triangle.Vertices[0]));
            }

            foreach (var edge in edges)
            {
                var line = new Line
                {
                    Stroke = System.Windows.Media.Brushes.SteelBlue,
                    StrokeThickness = 1,

                    X1 = edge.Point1.X,
                    X2 = edge.Point2.X,
                    Y1 = edge.Point1.Y,
                    Y2 = edge.Point2.Y
                };

                Graph.Children.Add(line);
            }
        }

        /// <summary>
        /// Draw the voronoi graph
        /// </summary>
        /// <param name="voronoiEdges"></param>
        private void DrawVoronoi(IEnumerable<Edge> voronoiEdges)
        {
            foreach (var edge in voronoiEdges)
            {
                var line = new Line
                {
                    Stroke = System.Windows.Media.Brushes.DarkViolet,
                    StrokeThickness = 1,

                    X1 = edge.Point1.X,
                    X2 = edge.Point2.X,
                    Y1 = edge.Point1.Y,
                    Y2 = edge.Point2.Y
                };

                Graph.Children.Add(line);
            }
        }

        /// <summary>
        /// Get the circumcenter points for the given point in a clocwise order
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private List<Point> GetCircumCenterPoints(Point point)
        {
            List<Point> circumCenterPoints = new List<Point>();

            foreach (Triangle triangle in point.AdjacentTriangles)
            {
                Point circumCenterPoint = new Point(triangle.Circumcenter.X, triangle.Circumcenter.Y);
                circumCenterPoints.Add(circumCenterPoint);

            }

            // ensure the points are in clockwise order
            circumCenterPoints.Sort(new ClockwiseComparer(point));

            return circumCenterPoints;
        }

        /// <summary>
        /// Fill the voronoi section of the current point during move interaction
        /// </summary>
        private void DrawVoronoiFillCurrent()
        {
            // ensure it's the last one
            if (movementPointIndex < 0 || movementPointIndex != ((List<Point>)points).Count - 1)
                return;

            Point point = ((List<Point>)points)[movementPointIndex];

            List<Point> circumCenterPoints = GetCircumCenterPoints(point);

            foreach (Point circumCenterPoint in circumCenterPoints)
            {

                DrawPoint(circumCenterPoint, Brushes.DarkSalmon);

            }

            // current point
            DrawPoint(point, Brushes.Red);

            // fill polygon
            Color color = GetRandomColor(movementPointIndex);

            DrawPolygon(circumCenterPoints, color);

        }

        /// <summary>
        /// Fill all voronoi segments
        /// </summary>
        private void DrawVoronoiFillAll()
        {
            int index = -1;
            foreach (Point point in points)
            {
                index++;

                List<Point> circumCenterPoints = GetCircumCenterPoints(point);

                // fill polygon
                Color color = GetRandomColor(index);

                DrawPolygon(circumCenterPoints, color);

            }

        }

        /// <summary>
        /// Draw the polygon of the given points using the given color
        /// </summary>
        /// <param name="circumCenterPoints"></param>
        /// <param name="color"></param>
        private void DrawPolygon(List<Point> circumCenterPoints, Color color)
        {
            // draw circumcenter polygon
            PointCollection pointCollection = new PointCollection();
            foreach (Point circumCenterPoint in circumCenterPoints)
            {
                pointCollection.Add(new System.Windows.Point(circumCenterPoint.X, circumCenterPoint.Y));
            }

            SolidColorBrush strokeBrush = new SolidColorBrush(color);
            SolidColorBrush fillBrush = new SolidColorBrush(color);
            fillBrush.Opacity = 0.4f;

            var polygon = new Polygon
            {
                //Stroke = strokeBrush,
                Fill = fillBrush,
                StrokeThickness = 2,
                Points = pointCollection
            };


            Graph.Children.Add(polygon);
        }

        /// <summary>
        /// Draw a single point using the given brush
        /// </summary>
        /// <param name="point"></param>
        /// <param name="brush"></param>
        private void DrawPoint(Point point, SolidColorBrush brush)
        {
            // draw point
            var myEllipse = new Ellipse
            {
                Fill = brush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 10,
                Height = 10
            };

            var ellipseX = point.X - 0.5 * myEllipse.Height;
            var ellipseY = point.Y - 0.5 * myEllipse.Width;
            myEllipse.Margin = new Thickness(ellipseX, ellipseY, 0, 0);

            Graph.Children.Add(myEllipse);
        }

        /// <summary>
        /// Re-Create the graph e. g. when the value of a checkbox changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GraphUpdateHandler(object sender, RoutedEventArgs e)
        {
            CreateGraph();
        }

        /// <summary>
        /// Re-initialize the graph
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtReset_Click(object sender, RoutedEventArgs e)
        {
            InitGraph();
        }

        /// <summary>
        /// Remove all points and clear the graph
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtClear_Click(object sender, RoutedEventArgs e)
        {
            ClearGraph();

        }

        /// <summary>
        /// Get a random ARGB color
        /// </summary>
        /// <returns></returns>
        public Color GetRandomColor()
        {
            return Color.FromArgb(255, (byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256));
        }

        /// <summary>
        /// Get a random color for the given point index. Point index is just an incrementally created index. 
        /// Ensures the same color is always returned for the same index. If no color is found for an index, a new random color is created for it and stored.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Color GetRandomColor( int index)
        {
            if (!colorMap.ContainsKey(index))
            {
                Color color = GetRandomColor();

                colorMap.Add(index, color);
            }

            return colorMap[index];
        }

        /// <summary>
        /// Ensure the point count textfield supports only numbers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TfPointCount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        /// <summary>
        /// Add a pattern of points
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtFillPattern_Click(object sender, RoutedEventArgs e)
        {
            
            FillPattern fillPattern = (FillPattern) cbFillPattern.SelectedItem;

            AddPoints(fillPattern);

            CreateGraph();
        }


    }
}