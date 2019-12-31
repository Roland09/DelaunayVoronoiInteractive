using InteractiveDelaunayVoronoi;
using SutherlandHodgmanAlgorithm;
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
            return triangulation.Select(item => ToWindowsPoint( item.Circumcenter)).ToList();
        }

        public System.Windows.Point GetPoint( int index)
        {
            Point point = ((List<Point>)points)[index];

            return ToWindowsPoint( point);
        }

        private List<System.Windows.Point> ToWindowsPoints(IEnumerable<Point> points)
        {
            return points.Select(item => ToWindowsPoint( item)).ToList();
        }

        private System.Windows.Point ToWindowsPoint( Point point)
        {
            return new System.Windows.Point(point.X, point.Y);
        }

        public List<InteractiveDelaunayVoronoi.Triangle> GetDelaunayTriangles()
        {
            return triangulation.Select(item => new InteractiveDelaunayVoronoi.Triangle( //
                ToWindowsPoint(item.Vertices[0]), //
                ToWindowsPoint(item.Vertices[1]), //
                ToWindowsPoint(item.Vertices[2]), //
                ToWindowsPoint(item.Circumcenter), //
                item.RadiusSquared //
                )
            ).ToList();
        }
        
        public List<InteractiveDelaunayVoronoi.Edge> GetVoronoiEdges()
        {
            return vornoiEdges.Select(item => new InteractiveDelaunayVoronoi.Edge( //
                ToWindowsPoint( item.Point1), //
                ToWindowsPoint( item.Point2)) //
            ).ToList();
        }

        /// <summary>
        /// Get a list of all polygons per point. This contains duplicate edges if multiple points share the same edge.
        /// </summary>
        /// <returns></returns>
        public List<System.Windows.Point[]> GetAllVoronoiPolygons()
        {
            List<System.Windows.Point[]> allPolygons = new List<System.Windows.Point[]>();

            foreach ( Point point in points)
            {
                List<System.Windows.Point> currentPolygon = new List<System.Windows.Point>();

                List<Point> circumCenterPoints = GetCircumCenterPoints(point);
                foreach (Point circumCenterPoint in circumCenterPoints)
                {
                    currentPolygon.Add(ToWindowsPoint(circumCenterPoint));
                }

                allPolygons.Add(currentPolygon.ToArray());
            }

            return allPolygons;
        }


        /// <summary>
        /// Get a list of all polygons per point. This contains duplicate edges if multiple points share the same edge.
        /// </summary>
        /// <returns></returns>
        public List<Cell> GetAllVoronoiCells()
        {
            return GetAllVoronoiCells(null);
        }

        /// <summary>
        /// Get a list of all polygons per point. This contains duplicate edges if multiple points share the same edge.
        /// Optionally clip the points at the specified polygon.
        /// </summary>
        /// <returns></returns>
        public List<Cell> GetAllVoronoiCells(System.Windows.Point[] clipPolygon)
        {
            List<Cell> allCells = new List<Cell>();

            foreach (Point point in points)
            {
                List<System.Windows.Point> currentPolygon = new List<System.Windows.Point>();

                List<Point> circumCenterPoints = GetCircumCenterPoints(point);

                foreach (Point circumCenterPoint in circumCenterPoints)
                {
                    currentPolygon.Add(ToWindowsPoint(circumCenterPoint));
                }

                Cell cell;

                if (clipPolygon == null)
                {
                    cell = new Cell(currentPolygon.ToArray(), ToWindowsPoint(point));
                }
                else
                {
                    System.Windows.Point[] clippedPoints = SutherlandHodgman.GetIntersectedPolygon(currentPolygon.ToArray(), clipPolygon);

                    // create the cell including polygons and center point
                    cell = new Cell(clippedPoints, ToWindowsPoint(point));
                }

                allCells.Add( cell);
            }

            return allCells;
        }
    }
}
