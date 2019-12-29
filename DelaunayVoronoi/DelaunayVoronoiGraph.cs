using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelaunayVoronoi
{
    /// <summary>
    /// DelaunayVoronoi wrapper class to keep the code independent from the original DelaunayVoronoi implementation
    /// </summary>
    public class DelaunayVoronoiGraph
    {
        private DelaunayTriangulator delaunay = new DelaunayTriangulator();
        private Voronoi voronoi = new Voronoi();

        private IEnumerable<Triangle> triangulation;
        private IEnumerable<Edge> vornoiEdges;

        /// <summary>
        /// The list of points on the screen. 
        /// Initialized with empty list to avoid nullpointer exceptions.
        /// </summary>
        private IEnumerable<Point> points = new List<Point>();

        public void GeneratePoints(int amount, double maxX, double maxY)
        {
            this.points = delaunay.GeneratePoints(amount, maxX, maxY);
        }

        public void AddPoint(double x, double y)
        {
            Point point = new Point(x, y);

            // add new point to points list
            ((List<Point>)points).Add(point);
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

        public void SetLastPoint(double x, double y)
        {
            Point point = new Point(x, y);

            // add new point to points list
            int count = ((List<Point>)points).Count;
            if (count > 2)
            {
                int lastPointIndex = count - 1;
                ((List<Point>)points)[lastPointIndex] = point;
            }
        }

        public int GetLastPointIndex()
        {
            return ((List<Point>)points).Count - 1;
        }

        public void CreateGraph()
        {
            // reset data
            ResetData();

            // delaunay
            var delaunayTimer = Stopwatch.StartNew();
            this.triangulation = delaunay.BowyerWatson(points);
            delaunayTimer.Stop();

            // voronoi
            var voronoiTimer = Stopwatch.StartNew();
            vornoiEdges = voronoi.GenerateEdgesFromDelaunay(triangulation);
            voronoiTimer.Stop();
        }

        public List<System.Windows.Point> GetPoints()
        {
            return ToWindowsPoints(this.points);
        }

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

        public List<System.Windows.Point> GetCircumCenterPoints(int index)
        {
            Point point = ((List<Point>)points)[index];

            List<Point> circumCenterPoints = GetCircumCenterPoints(point);

            return ToWindowsPoints( circumCenterPoints);
        }

        public List<System.Windows.Point> GetCircumCenterPoints()
        {
            return triangulation.Select(item => new System.Windows.Point()
            {
                X = item.Circumcenter.X,
                Y = item.Circumcenter.Y
            }).ToList();
        }

        public System.Windows.Point GetPoint( int index)
        {
            Point point = ((List<Point>)points)[index];

            return new System.Windows.Point(point.X, point.Y);
        }

        private List<System.Windows.Point> ToWindowsPoints(IEnumerable<Point> points)
        {
            return points.Select(item => new System.Windows.Point()
            {
                X = item.X,
                Y = item.Y,
            }).ToList();
        }

        public List<InteractiveDelaunayVoronoi.Triangle> GetDelaunayTriangles()
        {
            return triangulation.Select(item => new InteractiveDelaunayVoronoi.Triangle( //
                new System.Windows.Point(item.Vertices[0].X, item.Vertices[0].Y), //
                new System.Windows.Point(item.Vertices[1].X, item.Vertices[1].Y), //
                new System.Windows.Point(item.Vertices[2].X, item.Vertices[2].Y), //
                new System.Windows.Point(item.Circumcenter.X, item.Circumcenter.Y), //
                item.RadiusSquared //
                )
            ).ToList();
        }
        
        public List<InteractiveDelaunayVoronoi.Edge> GetVoronoiEdges()
        {
            return vornoiEdges.Select(item => new InteractiveDelaunayVoronoi.Edge( //
                new System.Windows.Point( item.Point1.X, item.Point1.Y), //
                new System.Windows.Point(item.Point2.X, item.Point2.Y)) //
            ).ToList();
        }
    }
}
