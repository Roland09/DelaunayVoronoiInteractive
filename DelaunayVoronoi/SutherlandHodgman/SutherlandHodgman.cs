using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace SutherlandHodgmanAlgorithm
{
    /// <summary>
    /// Using Sutherland-Hodgman algorithm to clip the polygon at the borders.
    /// Algorithm from here: 
    /// https://rosettacode.org/wiki/Sutherland-Hodgman_polygon_clipping#C.23
    /// </summary>
    public static class SutherlandHodgman
    {
        #region Class: Edge
 
        /// <summary>
        /// This represents a line segment
        /// </summary>
        private class Edge
        {
            public Edge(Vector from, Vector to)
            {
                this.From = from;
                this.To = to;
            }
 
            public readonly Vector From;
            public readonly Vector To;
        }
 
        #endregion
 
        /// <summary>
        /// This clips the subject polygon against the clip polygon (gets the intersection of the two polygons)
        /// </summary>
        /// <remarks>
        /// Based on the psuedocode from:
        /// http://en.wikipedia.org/wiki/Sutherland%E2%80%93Hodgman
        /// </remarks>
        /// <param name="subjectPoly">Can be concave or convex</param>
        /// <param name="clipPoly">Must be convex</param>
        /// <returns>The intersection of the two polygons (or null)</returns>
        public static Vector[] GetIntersectedPolygon(Vector[] subjectPoly, Vector[] clipPoly)
        {
            if (subjectPoly.Length < 3 || clipPoly.Length < 3)
            {
                throw new ArgumentException(string.Format("The polygons passed in must have at least 3 points: subject={0}, clip={1}", subjectPoly.Length.ToString(), clipPoly.Length.ToString()));
            }
 
            List<Vector> outputList = subjectPoly.ToList();
 
            //	Make sure it's clockwise
            if (!IsClockwise(subjectPoly))
            {
                outputList.Reverse();
            }
 
            //	Walk around the clip polygon clockwise
            foreach (Edge clipEdge in IterateEdgesClockwise(clipPoly))
            {
                List<Vector> inputList = outputList.ToList();		//	clone it
                outputList.Clear();
 
                if (inputList.Count == 0)
                {
                    //	Sometimes when the polygons don't intersect, this list goes to zero.  Jump out to avoid an index out of range exception
                    break;
                }

                Vector S = inputList[inputList.Count - 1];
 
                foreach (Vector E in inputList)
                {
                    if (IsInside(clipEdge, E))
                    {
                        if (!IsInside(clipEdge, S))
                        {
                            Vector? point = GetIntersect(S, E, clipEdge.From, clipEdge.To);
                            if (point == null)
                            {
                                // removed this: we don't want the app to crash
                                // throw new ApplicationException("Line segments don't intersect");        //	may be colinear, or may be a bug
                                Debug.Print("Line segments don't intersect");

                            }
                            else
                            {
                                outputList.Add(point.Value);
                            }
                        }
 
                        outputList.Add(E);
                    }
                    else if (IsInside(clipEdge, S))
                    {
                        Vector? point = GetIntersect(S, E, clipEdge.From, clipEdge.To);
                        if (point == null)
                        {
                            // removed this: we don't want the app to crash
                            //throw new ApplicationException("Line segments don't intersect");		//	may be colinear, or may be a bug
                            Debug.Print("Line segments don't intersect");
                        }
                        else
                        {
                            outputList.Add(point.Value);
                        }
                    }
 
                    S = E;
                }
            }
 
            //	Exit Function
            return outputList.ToArray();
        }
 
        #region Private Methods
 
        /// <summary>
        /// This iterates through the edges of the polygon, always clockwise
        /// </summary>
        private static IEnumerable<Edge> IterateEdgesClockwise(Vector[] polygon)
        {
            if (IsClockwise(polygon))
            {
                #region Already clockwise
 
                for (int cntr = 0; cntr < polygon.Length - 1; cntr++)
                {
                    yield return new Edge(polygon[cntr], polygon[cntr + 1]);
                }
 
                yield return new Edge(polygon[polygon.Length - 1], polygon[0]);
 
                #endregion
            }
            else
            {
                #region Reverse
 
                for (int cntr = polygon.Length - 1; cntr > 0; cntr--)
                {
                    yield return new Edge(polygon[cntr], polygon[cntr - 1]);
                }
 
                yield return new Edge(polygon[0], polygon[polygon.Length - 1]);
 
                #endregion
            }
        }
 
        /// <summary>
        /// Returns the intersection of the two lines (line segments are passed in, but they are treated like infinite lines)
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://stackoverflow.com/questions/14480124/how-do-i-detect-triangle-and-rectangle-intersection
        /// </remarks>
        private static Vector? GetIntersect(Vector line1From, Vector line1To, Vector line2From, Vector line2To)
        {
            Vector direction1 = line1To - line1From;
            Vector direction2 = line2To - line2From;
            double dotPerp = (direction1.X * direction2.Y) - (direction1.Y * direction2.X);
 
            // If it's 0, it means the lines are parallel so have infinite intersection points
            if (IsNearZero(dotPerp))
            {
                return null;
            }
 
            Vector c = line2From - line1From;
            double t = (c.X * direction2.Y - c.Y * direction2.X) / dotPerp;
            //if (t < 0 || t > 1)
            //{
            //    return null;		//	lies outside the line segment
            //}
 
            //double u = (c.X * direction1.Y - c.Y * direction1.X) / dotPerp;
            //if (u < 0 || u > 1)
            //{
            //    return null;		//	lies outside the line segment
            //}
 
            //	Return the intersection point
            return line1From + (t * direction1);
        }
 
        private static bool IsInside(Edge edge, Vector test)
        {
            bool? isLeft = IsLeftOf(edge, test);
            if (isLeft == null)
            {
                //	Colinear points should be considered inside
                return true;
            }
 
            return !isLeft.Value;
        }
        private static bool IsClockwise(Vector[] polygon)
        {
            for (int cntr = 2; cntr < polygon.Length; cntr++)
            {
                bool? isLeft = IsLeftOf(new Edge(polygon[0], polygon[1]), polygon[cntr]);
                if (isLeft != null)		//	some of the points may be colinear.  That's ok as long as the overall is a polygon
                {
                    return !isLeft.Value;
                }
            }
 
            throw new ArgumentException("All the points in the polygon are colinear");
        }
 
        /// <summary>
        /// Tells if the test point lies on the left side of the edge line
        /// </summary>
        private static bool? IsLeftOf(Edge edge, Vector test)
        {
            Vector tmp1 = edge.To - edge.From;
            Vector tmp2 = test - edge.To;
 
            double x = (tmp1.X * tmp2.Y) - (tmp1.Y * tmp2.X);		//	dot product of perpendicular?
 
            if (x < 0)
            {
                return false;
            }
            else if (x > 0)
            {
                return true;
            }
            else
            {
                //	Colinear points;
                return null;
            }
        }
 
        private static bool IsNearZero(double testValue)
        {
            return Math.Abs(testValue) <= .000000001d;
        }
 
        #endregion
    }
}
