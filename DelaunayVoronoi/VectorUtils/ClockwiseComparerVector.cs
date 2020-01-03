using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Delaunay.VectorUtils
{
  /// <summary>
  /// Comparer which allows you to sort a list of points clockwise around an origin.
  /// 
  /// Note: Found this online in several repositories, full credit to the person who created it, unfortunately couldn't find the original author.
  /// </summary>
    public class ClockwiseComparerVector : IComparer<Vector>
    {
        /// <summary>
        /// 	ClockwiseComparer provides functionality for sorting a collection of Vectors such
        /// 	that they are ordered clockwise about a given origin.
        /// </summary>

        private Vector m_Origin;

        #region Properties

        /// <summary>
        /// 	Gets or sets the origin.
        /// </summary>
        /// <value>The origin.</value>
        public Vector origin { get { return m_Origin; } set { m_Origin = value; } }

        #endregion

        /// <summary>
        /// 	Initializes a new instance of the ClockwiseComparer class.
        /// </summary>
        /// <param name="origin">Origin.</param>
        public ClockwiseComparerVector(Vector origin)
        {
            m_Origin = origin;
        }

        /// <summary>
        /// 	Initializes a new instance of the ClockwiseComparer class and sets the origin to the mean vector, depending on the positions.
        /// </summary>
        /// <param name="origin">Origin.</param>
        public ClockwiseComparerVector(List<Vector> positions)
        {
            m_Origin = GetMeanVector(positions);
        }

        private Vector GetMeanVector(List<Vector> positions)
        {
            if (positions.Count == 0)
                return new Vector(0, 0);

            double x = 0f;
            double y = 0f;

            foreach (Vector pos in positions)
            {
                x += pos.X;
                y += pos.Y;
            }
            return new Vector(x / (double)positions.Count, y / (double)positions.Count);
        }

        #region IComparer Methods

        /// <summary>
        /// 	Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="first">First.</param>
        /// <param name="second">Second.</param>
        public int Compare(Vector first, Vector second)
        {
            return IsClockwise(first, second, m_Origin);
        }

        #endregion

        /// <summary>
        /// 	Returns 1 if first comes before second in clockwise order.
        /// 	Returns -1 if second comes before first.
        /// 	Returns 0 if the points are identical.
        /// </summary>
        /// <param name="first">First.</param>
        /// <param name="second">Second.</param>
        /// <param name="origin">Origin.</param>
        public static int IsClockwise(Vector first, Vector second, Vector origin)
        {

            //if (first == second)
            if (first.X == second.X && first.Y == second.Y)
                return 0;

            //Vector firstOffset = first - origin;
            Vector firstOffset = new Vector(first.X - origin.X, first.Y - origin.Y);

            //Vector secondOffset = second - origin;
            Vector secondOffset = new Vector(second.X - origin.X, second.Y - origin.Y);


            double angle1 = System.Math.Atan2(firstOffset.X, firstOffset.Y);
            double angle2 = System.Math.Atan2(secondOffset.X, secondOffset.Y);

            if (angle1 < angle2)
                return -1;

            if (angle1 > angle2)
                return 1;

            // Check to see which point is closest
            double firstMagnitude = Math.Sqrt(firstOffset.X * firstOffset.X + firstOffset.Y * firstOffset.Y);
            double secondMagnitude = Math.Sqrt(secondOffset.X * secondOffset.X + secondOffset.Y * secondOffset.Y);

            //return (firstOffset.sqrMagnitude < secondOffset.sqrMagnitude) ? -1 : 1;
            return (firstMagnitude < secondMagnitude) ? -1 : 1;

        }
    }
}
