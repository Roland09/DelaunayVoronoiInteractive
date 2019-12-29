using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InteractiveDelaunayVoronoi
{
    /// <summary>
    /// Triangle wrapper class to keep the code independent from the original DelaunayVoronoi implementation
    /// </summary>
    public class Triangle
    {
        public Point[] Vertices { get; }
        public Point CircumCenter { get; }
        public double RadiusSquared;

        public Triangle(Point point1, Point point2, Point point3, Point circumcenter, double radiusSquared)
        {
            Vertices = new Point[3] { point1, point2, point3 };
            CircumCenter = circumcenter;
            RadiusSquared = radiusSquared;
        }
    }
}
