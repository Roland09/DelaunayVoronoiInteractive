using System.Collections.Generic;

namespace DelaunayVoronoi
{
    public class Point
    {
        public double X { get; }
        public double Y { get; }
        public HashSet<Triangle> AdjacentTriangles { get; }
        public virtual bool IsBorderPoint { get; } = false;

        public Point(double x, double y)
        {
            X = x;
            Y = y;
            AdjacentTriangles = new HashSet<Triangle>();
        }

        public static Point operator +(Point p1, Point p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Point operator -(Point p1, Point p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }

        /// <summary>
        /// Overwritten to check for duplicates
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is Point point &&
                   X == point.X &&
                   Y == point.Y;
        }

        /// <summary>
        /// Overwritten to check for duplicates
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            return hashCode;
        }
    }

    public class BorderPoint : Point
    {
        public override bool IsBorderPoint { get; } = true;

        public BorderPoint(double x, double y) : base(x, y)
        {
        }
    }


}