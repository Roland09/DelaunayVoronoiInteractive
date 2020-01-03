using Delaunay.VectorUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InteractiveDelaunayVoronoi
{
    public class PolygonUtils
    {

        /// <summary>
        /// Get the mean vector of the specified polygon
        /// </summary>
        /// <param name="positions"></param>
        /// <returns></returns>
        public static Vector GetMeanVector(List<Vector> positions)
        {
            return GetMeanVector(positions.ToArray());
        }

        /// <summary>
        /// Get the mean vector of the specified polygon
        /// </summary>
        /// <param name="positions"></param>
        /// <returns></returns>
        public static Vector GetMeanVector(Vector[] positions)
        {
            if (positions.Length == 0)
                return new Vector(0, 0);

            double x = 0;
            double y = 0;

            foreach (Vector pos in positions)
            {
                x += pos.X;
                y += pos.Y;
            }
            return new Vector(x / positions.Length, y / positions.Length);
        }

        /// <summary>
        /// Sort the points of the polygon in clockwise order.
        /// </summary>
        /// <param name="polygon"></param>
        public static void SortClockWise(List<Vector> polygon)
        {
            ClockwiseComparerVector comparer = new ClockwiseComparerVector(polygon);
            polygon.Sort(comparer);
        }

        /// <summary>
        /// Find the intersection of the line segments [p1,p2] to [p3,p4]
        /// 
        /// Credits to setchi:
        /// https://github.com/setchi/Unity-LineSegmentsIntersection/blob/master/Assets/LineSegmentIntersection/Scripts/Math2d.cs
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <param name="intersection">The intersection point if there is any</param>
        /// <returns>true if the lines intersect, false otherwise</returns>
        public static bool LineSegmentsIntersection(Vector p1, Vector p2, Vector p3, Vector p4, out Vector intersection)
        {
            intersection = new Vector(0,0);

            var d = (p2.X - p1.X) * (p4.Y - p3.Y) - (p2.Y - p1.Y) * (p4.X - p3.X);

            if (d == 0.0f)
            {
                return false;
            }

            var u = ((p3.X - p1.X) * (p4.Y - p3.Y) - (p3.Y - p1.Y) * (p4.X - p3.X)) / d;
            var v = ((p3.X - p1.X) * (p2.Y - p1.Y) - (p3.Y - p1.Y) * (p2.X - p1.X)) / d;

            if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
            {
                return false;
            }

            intersection.X = p1.X + u * (p2.X - p1.X);
            intersection.Y = p1.Y + u * (p2.Y - p1.Y);

            return true;
        }
    }

}
