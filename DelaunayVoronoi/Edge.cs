using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InteractiveDelaunayVoronoi
{
    /// <summary>
    /// Edge wrapper class to keep the code independent from the original DelaunayVoronoi implementation
    /// </summary>
    public class Edge
    {
        public Point Point1 { get; }
        public Point Point2 { get; }

        public Edge(Point point1, Point point2)
        {
            Point1 = point1;
            Point2 = point2;
        }
    }
}
