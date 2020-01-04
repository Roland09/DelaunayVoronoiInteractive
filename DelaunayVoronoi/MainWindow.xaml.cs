using DelaunayVoronoi;
using SutherlandHodgmanAlgorithm;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Edge = InteractiveDelaunayVoronoi.Edge;
using System.Linq;
using System.Diagnostics;

namespace InteractiveDelaunayVoronoi
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

        private DelaunayVoronoiGraph graph = new DelaunayVoronoiGraph();

        int initialPointCount = 5;

        /// <summary>
        /// If clip at bounds is active, then this will describe the margin around the canvas that is being used for the clipping.
        /// </summary>
        int clipAtBoundsMargin = 50;

        bool mousePressedDuringMove = false;
        int movementPointIndex = -1;

        System.Windows.Threading.DispatcherTimer relaxationTimer;
        double relaxationSpeed = 1;
        int relaxationUpdateIntervalMs = 10;
        double stopDistance = 10; // distance to stop relaxation, otherwise we'd only get jitter

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

            // key event handlers
            #region keyboard handlers
            this.KeyDown += KeyboardHandler;
            #endregion keyboard handlers

            // mouse event handlers
            #region mouse handlers
            Graph.MouseLeftButtonDown += MouseLeftButtonDownHandler;
            Graph.MouseMove += MouseMoveHandler;
            #endregion mouse handlers

            #region component event handlers
            // checkbox visualization changes
            cbDrawCircumCenters.Checked += GraphUpdateHandler;
            cbDrawCircumCenters.Unchecked += GraphUpdateHandler;

            cbDrawCircumCircles.Checked += GraphUpdateHandler;
            cbDrawCircumCircles.Unchecked += GraphUpdateHandler;

            cbDrawPoints.Checked += GraphUpdateHandler;
            cbDrawPoints.Unchecked += GraphUpdateHandler;

            cbDrawMeanVector.Checked += GraphUpdateHandler;
            cbDrawMeanVector.Unchecked += GraphUpdateHandler;

            cbDrawDelaunay.Checked += GraphUpdateHandler;
            cbDrawDelaunay.Unchecked += GraphUpdateHandler;

            cbDrawVoronoi.Checked += GraphUpdateHandler;
            cbDrawVoronoi.Unchecked += GraphUpdateHandler;

            cbDrawClipVoronoi.Checked += GraphUpdateHandler;
            cbDrawClipVoronoi.Unchecked += GraphUpdateHandler;

            cbDrawVoronoiFillCurrent.Checked += GraphUpdateHandler;
            cbDrawVoronoiFillCurrent.Unchecked += GraphUpdateHandler;

            cbDrawVoronoiFillAll.Checked += GraphUpdateHandler;
            cbDrawVoronoiFillAll.Unchecked += GraphUpdateHandler;

            cbClipAtBounds.Checked += GraphUpdateHandler;
            cbClipAtBounds.Unchecked += GraphUpdateHandler;

            cbDrawRandomShape.Checked += GraphUpdateHandler;
            cbDrawRandomShape.Unchecked += GraphUpdateHandler;

            sldrResampling.ValueChanged += GraphUpdateHandler;
            sldrRelaxation.ValueChanged += GraphUpdateHandler;

            #endregion component event handlers

            #region timers

            relaxationTimer = new System.Windows.Threading.DispatcherTimer();
            relaxationTimer.Tick += new EventHandler(relaxationTimer_Tick);
            relaxationTimer.Interval = new TimeSpan(0, 0, 0, 0, relaxationUpdateIntervalMs);

            UpdateRelaxationButtonStates();

            #endregion timers

            // disable the voronoi clip drawing, it's only for testing purposes for the GetAllClippedVoronoiPolygons method
            // needs some adjustment, eg dynamically adding points doesn't work when it is activated
            cbDrawClipVoronoi.IsEnabled = false;

            // create the graph with initial settings
            // delayed initialization because we need to have the UI bounds calculated in order to determine the width and height
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    InitGraph();
                }
                )
                , DispatcherPriority.ContextIdle, null);
        }

        /// <summary>
        /// Clear the graph
        /// </summary>
        private void ClearGraph()
        {
            // get width and height from the canvas border, canvas itself doesn't provide that information
            double width = CanvasBorder.ActualWidth;
            double height = CanvasBorder.ActualHeight;

            CreateGraph(0, width, height);
        }

        /// <summary>
        /// Clear the graph and add a number of points.
        /// </summary>
        private void InitGraph()
        {
            int pointCount = GetSelectedPointCount();

            // get width and height from the canvas border, canvas itself doesn't provide that information
            double width = CanvasBorder.ActualWidth;
            double height = CanvasBorder.ActualHeight;

            CreateGraph(pointCount, width, height);
        }

        private int GetSelectedPointCount()
        {
            int pointCount = initialPointCount;
            if (!Int32.TryParse(tfPointCount.Text, out pointCount))
            {
                pointCount = initialPointCount;
            }

            if (pointCount < 0)
            {
                pointCount = 0;
            }

            return pointCount;
        }

 
        /// <summary>
        /// Add new point at mouse click position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            // don't let the event be handled in the MouseMoveHandler as well
            e.Handled = true;

            // add point at clicked position
            graph.AddPoint(e.GetPosition(Graph).X, e.GetPosition(Graph).Y);

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
                    // don't let the event be handled in the MouseLeftButtonDownHandler as well
                    e.Handled = true;

                    // add point at clicked position
                    graph.AddPoint(e.GetPosition(Graph).X, e.GetPosition(Graph).Y);

                    CreateGraph();

                    mousePressedDuringMove = false;

                    return;
                }
            }
            #endregion Move Click Handler

            // don't let the event be handled in the MouseLeftButtonDownHandler as well
            e.Handled = true;

            // set last point to mouse move position
            graph.SetLastPoint(e.GetPosition(Graph).X, e.GetPosition(Graph).Y);

            movementPointIndex = graph.GetLastPointIndex();

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
                        graph.AddPoint(100 + i * 30, 100);
                    }
                    break;

                case FillPattern.Vertical:

                    for (int i = 0; i < 20; i++)
                    {
                        graph.AddPoint(100, 100 + i * 30);

                    }
                    break;

                case FillPattern.Diagonal:
                    for (int i = 0; i < 20; i++)
                    {
                        graph.AddPoint(100 + i * 30, 100 + i * 30);
                    }
                    break;

                case FillPattern.Cross:

                    graph.AddPoint(200, 100);
                    graph.AddPoint(200, 200);
                    graph.AddPoint(200, 300);
                    graph.AddPoint(100, 200);
                    graph.AddPoint(300, 200);

                    break;

                case FillPattern.Circle:

                    int angleStepCircle = 30;

                    for (int i = 0; i < 360; i += angleStepCircle)
                    {
                        float radius = 120f;
                        int x = (int)(Math.Cos(Math.PI / 180 * i) * radius);
                        int y = (int)(Math.Sin(Math.PI / 180 * i) * radius);

                        graph.AddPoint(400 + x, 150 + y);
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

                        graph.AddPoint( 400 + x, 200 + y);
                    }
                    break;
            }
        }

        public void CreateGraph(int amount, double maxX, double maxY)
        {
            graph.GeneratePoints(amount, maxX, maxY);

            CreateGraph();

        }

        /// <summary>
        /// Create the graph using the points list
        /// </summary>
        private void CreateGraph()
        {

            // clear the canvas
            Graph.Children.Clear();

            graph.CreateGraph();

            List<InteractiveDelaunayVoronoi.Triangle> triangulation = graph.GetDelaunayTriangles();
            List<InteractiveDelaunayVoronoi.Edge> voronoiEdges = graph.GetVoronoiEdges();

            #region visualization

            if (cbDrawDelaunay.IsChecked.GetValueOrDefault())
            {
                DrawTriangulation(triangulation);

            }

            if (cbDrawPoints.IsChecked.GetValueOrDefault())
            {
                DrawPoints( graph.GetPoints());

            }

            if (cbDrawCircumCenters.IsChecked.GetValueOrDefault())
            {
                DrawCircumCenters(graph.GetCircumCenterPoints());
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
                DrawVoronoi(voronoiEdges);

            }

            if (cbDrawClipVoronoi.IsChecked.GetValueOrDefault())
            {
                DrawAllClippedPolygons();
            }

            if(cbDrawMeanVector.IsChecked.GetValueOrDefault())
            {
                DrawMeanVector();
            }

            if( cbDrawRandomShape.IsChecked.GetValueOrDefault())
            {
                DrawRandomShape();
            }
            #endregion visualization

        }

        /// <summary>
        /// Draw the mean vector point for all cells.
        /// </summary>
        private void DrawMeanVector()
        {
            // bounding box
            Vector[] clipPolygon = null;

            if (cbClipAtBounds.IsChecked.GetValueOrDefault())
            {
                clipPolygon = GetClipPolygon(clipAtBoundsMargin);
            }

            // get all cells
            List<Cell> allCells = graph.GetAllVoronoiCells(clipPolygon);

            // iterate through all cells and draw the mean vector point
            foreach( Cell cell in allCells)
            {
                Vector meanVector = DelaunayVoronoiGraph.GetMeanVector(cell);

                DrawPoint(meanVector, Brushes.Green);
            }
        }


        /// <summary>
        /// Draw the point list
        /// </summary>
        /// <param name="points"></param>
        private void DrawPoints(IEnumerable<Vector> points)
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
        private void DrawCircumCenters(IEnumerable<Vector> circumCenters)
        {
            foreach (var point in circumCenters)
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

                var ellipseX = point.X - myEllipse.Height * 0.5;
                var ellipseY = point.Y - myEllipse.Width * 0.5;

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
                    StrokeThickness = 0.6,
                    //Fill = System.Windows.Media.Brushes.AliceBlue,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = System.Math.Sqrt(triangle.RadiusSquared) * 2,
                    Height = System.Math.Sqrt(triangle.RadiusSquared) * 2
                };

                var ellipseX = triangle.CircumCenter.X - myEllipse.Height * 0.5;
                var ellipseY = triangle.CircumCenter.Y - myEllipse.Width * 0.5;

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
                    StrokeThickness = 0.6,

                    X1 = edge.Vector1.X,
                    X2 = edge.Vector2.X,
                    Y1 = edge.Vector1.Y,
                    Y2 = edge.Vector2.Y
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

                    X1 = edge.Vector1.X,
                    X2 = edge.Vector2.X,
                    Y1 = edge.Vector1.Y,
                    Y2 = edge.Vector2.Y
                };

                Graph.Children.Add(line);
            }
        }

        /// <summary>
        /// Fill the voronoi section of the current point during move interaction
        /// </summary>
        private void DrawVoronoiFillCurrent()
        {
            // ensure it's the last one
            if (movementPointIndex < 0 || movementPointIndex != graph.GetLastPointIndex())
                return;

            Vector point = graph.GetPoint(movementPointIndex);
            List<Vector> circumCenterPoints = graph.GetCircumCenterPoints(movementPointIndex);

            foreach (Vector circumCenterPoint in circumCenterPoints)
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

            for( int i=0; i < graph.GetPoints().Count; i++)
            {
                List<Vector> circumCenterPoints = graph.GetCircumCenterPoints( i);

                if (circumCenterPoints.Count < 3)
                    continue;

                // fill polygon
                Color color = GetRandomColor(i);

                DrawPolygon(circumCenterPoints, color);

            }

        }

        private void DrawRandomShape()
        {
            // bounding box
            Vector[] clipPolygon = null;

            if (cbClipAtBounds.IsChecked.GetValueOrDefault())
            {
                clipPolygon = GetClipPolygon(clipAtBoundsMargin);
            }

            // get all cells
            List<Cell> allCells = graph.GetAllVoronoiCells(clipPolygon);

            // iterate through all cells and draw the mean vector point
            for (int i = 0; i < allCells.Count; i++)
            {
                Cell cell = allCells[i];
                DrawRandomShape(cell);
            }
        }

        private void DrawRandomShape(Cell cell)
        {
            int angleStepCount = (int) sldrResampling.Value;
            double ellipseRelaxationFactor = sldrRelaxation.Value; // 0.90;
            bool randomStartAngle = false;
            bool randomAngleMovement = false;
            bool keepOriginalShape = false;

            List<Vector> shape = ShapeCreator.CreateRandomShape(cell.Vertices.ToList<Vector>(), ellipseRelaxationFactor, angleStepCount, randomAngleMovement, keepOriginalShape, randomStartAngle);

            DrawPolygon(shape, Colors.LawnGreen);
        }

        /// <summary>
        /// Get a clip polygon depending on the clip at bounds settings. Either the canvas or with a margin.
        /// </summary>
        /// <returns></returns>
        private Vector[] GetClipPolygon()
        {
            return GetClipPolygon(GetClipMargin());
        }

        /// <summary>
        /// Get the clip margin. This is a constant if clip at margin is selected, otherwise it's 0, i. e. the canvas bounds.
        /// </summary>
        /// <returns></returns>
        private double GetClipMargin()
        {
            if (cbClipAtBounds.IsChecked.GetValueOrDefault())
            {
                return clipAtBoundsMargin;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Create a polygon which consists of the bounding box plus a margin
        /// </summary>
        /// <returns></returns>
        private Vector[] GetClipPolygon( double margin)
        {
            // clip polygon, bounding box
            double left = 0 + margin;
            double top = 0 + margin;
            double right = CanvasBorder.ActualWidth - margin;
            double bottom = CanvasBorder.ActualHeight - margin;

            Vector[] clipPolygon = new Vector[] { new Vector(left, top), new Vector(right, top), new Vector(right, bottom), new Vector(left, bottom) };

            return clipPolygon;
        }

        /// <summary>
        /// Clip the specified polygon at the canvas bounds. Consider a margin.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private Vector[] ClipAtBounds(Vector[] currentPolygon)
        {
            // ensure there are data
            if (currentPolygon.Length == 0)
                return currentPolygon;

            // clip polygon, bounding box
            Vector[] clipPolygon = GetClipPolygon( clipAtBoundsMargin);

            Vector[] intersectedPolygon = SutherlandHodgman.GetIntersectedPolygon(currentPolygon, clipPolygon);

            return intersectedPolygon;
        }

        /// <summary>
        /// Get a list of all polygons per point. 
        /// This contains duplicate edges if multiple points share the same edge.
        /// The polygons are clipped at the canvas bounds.
        /// Basically this is the method to use. It gives you all Voronoi polygons exactly on the Canvas
        /// </summary>
        /// <returns></returns>
        private List<Vector[]> GetAllClippedVoronoiPolygons()
        {
            // bounding box
            Vector[] clipPolygon = GetClipPolygon(clipAtBoundsMargin);

            List<Vector[]> allVoronoiPolygons = new List<Vector[]>();

            foreach(Vector[]  polygon in graph.GetAllVoronoiPolygons())
            {
                Vector[] intersectedPolygon = SutherlandHodgman.GetIntersectedPolygon(polygon, clipPolygon);
                allVoronoiPolygons.Add(intersectedPolygon);
            }          

            return allVoronoiPolygons;
        }

        /// <summary>
        /// Draw all Voronoi polygons. This overlaps the Voronoi drawing, so it's better to have either this or the normal voronoi drawing.
        /// </summary>
        private void DrawAllClippedPolygons()
        {
            List<Vector[]> allPolygons = GetAllClippedVoronoiPolygons();

            for (int i = 0; i < allPolygons.Count; i++)
            {
                // get clipped voronoi polygon
                Vector[] voronoiPolygon = allPolygons[i];

                // draw point
                SolidColorBrush pointBrush = new SolidColorBrush( Colors.Yellow);
                foreach (Vector point in voronoiPolygon)
                {
                    DrawPoint(point, pointBrush);
                }

                // draw polygon

                // convert to point collection
                PointCollection pointCollection = new PointCollection();
                foreach (Vector point in voronoiPolygon)
                {
                    pointCollection.Add( new System.Windows.Point( point.X, point.Y));
                }

                // fill polygon
                Color color = GetRandomColor(i);

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
        }

        /// <summary>
        /// Draw the polygon of the given points using the given color
        /// </summary>
        /// <param name="circumCenterPoints"></param>
        /// <param name="color"></param>
        private void DrawPolygon(List<Vector> circumCenterPoints, Color color)
        {
            // convert to System.Windows.Point array
            Vector[] currentPolygon = circumCenterPoints.ToArray();

            Vector[] intersectedPolygon;

            // optionally clip at bounds
            if (cbClipAtBounds.IsChecked.GetValueOrDefault())
            {
                intersectedPolygon = ClipAtBounds(currentPolygon);
            }
            // othrwise keep the polygon as it is with vertices outside the canvas
            else
            {
                intersectedPolygon = currentPolygon;
            }

            // convert to point collection
            PointCollection pointCollection = new PointCollection();
            foreach (Vector point in intersectedPolygon)
            {
                pointCollection.Add( new System.Windows.Point( point.X, point.Y));
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
        private void DrawPoint(Vector point, SolidColorBrush brush)
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
        /// Custom keyboard event handler
        /// 
        ///     + ctrl+enter: toggle fullscreen
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyboardHandler(object sender, KeyEventArgs e)
        {
            // ctrl+enter: toggle fullscreen
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                if (this.WindowState == WindowState.Normal)
                {
                    // fullscreen
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                }
                else
                {
                    this.WindowStyle = WindowStyle.ThreeDBorderWindow;
                    this.WindowState = WindowState.Normal;
                }

            }
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

        private void btRelax_Click(object sender, RoutedEventArgs e)
        {
            Vector[] clipPolygon = GetClipPolygon();

            graph.Relax( clipPolygon);

            CreateGraph();
        }

        private void btRelaxStart_Click(object sender, RoutedEventArgs e)
        {
            relaxationTimer.Start();

            UpdateRelaxationButtonStates();
        }

        private void btRelaxStop_Click(object sender, RoutedEventArgs e)
        {
            relaxationTimer.Stop();

            UpdateRelaxationButtonStates();

        }

        private void UpdateRelaxationButtonStates()
        {
            btRelaxStart.IsEnabled = !relaxationTimer.IsEnabled;
            btRelaxStop.IsEnabled = relaxationTimer.IsEnabled;
        }

        private void relaxationTimer_Tick(object sender, EventArgs e)
        {
            Vector[] clipPolygon = GetClipPolygon();

            graph.RelaxTowardsCentroid( relaxationSpeed, clipPolygon, stopDistance);

            CreateGraph();
        }

        /// <summary>
        /// Add the number of specified random points, specified in the points textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtAddPoints_Click(object sender, RoutedEventArgs e)
        {
            int pointCount = GetSelectedPointCount();

            double width = CanvasBorder.ActualWidth;
            double height = CanvasBorder.ActualHeight;

            for ( int i=0; i < pointCount; i++)
            {
                double x = Utils.GetRandomRange(0, width);
                double y = Utils.GetRandomRange(0, height);

                graph.AddPoint( x, y);

            }

            CreateGraph();

        }
    }
}