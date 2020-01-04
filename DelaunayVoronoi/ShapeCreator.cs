using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InteractiveDelaunayVoronoi
{
    public class ShapeCreator
    {
        public static double Deg2Rad = 0.0174532924F;

        public static List<double> CreateAngleList(int angleStepCount, bool randomAngleMovement, bool randomStartAngle)
        {
            List<double> angleRadList = new List<double>();

            if (randomAngleMovement)
            {
                for (int i = 0; i < angleStepCount; i++)
                {

                    double angleRad = Deg2Rad * Utils.GetRandomRange( 0, 360);

                    // avoid duplicates
                    if (angleRadList.Contains(angleRad))
                    {
                        // the randomness should be enough to not have duplicates. just output some message in case we run into problems with not enough vertices later
                        continue;
                    }

                    angleRadList.Add(angleRad);
                }
            }
            else
            {
                // start with a random angle; otherwise with 0 we'd get similarities among different shapes
                double startAngle = 0;

                if (randomStartAngle) {
                    startAngle = Deg2Rad * Utils.GetRandomRange(0, 360);
                }

                for (int i = 0; i < angleStepCount; i++)
                {
                    double angleRad = startAngle + i * (double) Math.PI * 2 / angleStepCount;

                    angleRadList.Add(angleRad);
                }

            }

            // sort ascending
            angleRadList.Sort();

            return angleRadList;
        }


        // resample the polygon by creating vertices which are created by casting lines from mean vector to the polygon lines and using the intersection points
        // this assumes that the mean vector is enclosed in a polygon
        public static List<Vector> ResamplePolygon(List<Vector> polygon, Vector meanVector, List<double> angleRadList, bool keepOriginalShape)
        {
            double length = meanVector.Length * 2;

            List<Vector> resampledPolygon = new List<Vector>();

            // add existing points in order to keep the general voronoi shape
            if (keepOriginalShape)
            {
                resampledPolygon.AddRange(polygon);
            }

            for (int i = 0; i < angleRadList.Count; i++)
            {

                // convert angle from deg to rad
                double angle = angleRadList[i];

                double x = meanVector.X + length * Math.Cos(angle);
                double z = meanVector.X + length * Math.Sin(angle);

                // position on the ellipse
                Vector line1PositionA = meanVector;
                Vector line1PositionB = new Vector(x, z);

                for (var j = 0; j < polygon.Count; j++)
                {

                    int currIndex = j;
                    int nextIndex = j + 1;
                    if (nextIndex == polygon.Count)
                    {
                        nextIndex = 0;
                    }

                    Vector line2PositionA = new Vector(polygon[currIndex].X, polygon[currIndex].Y);
                    Vector line2PositionB = new Vector(polygon[nextIndex].X, polygon[nextIndex].Y);


                    Vector intersection = new Vector( 0,0);

                    // try and find an intersection. if one is found, add the point to the list
                    if (PolygonUtils.LineSegmentsIntersection(line1PositionA, line1PositionB, line2PositionA, line2PositionB, out intersection))
                    {
                        resampledPolygon.Add(intersection);
                        break;
                    }

                }


            }

            // sort again
            PolygonUtils.SortClockWise(resampledPolygon);

            return resampledPolygon;
        }

        /// <summary>
        /// Modifies a polygon and creates a random shape.
        /// 
        /// Algorithm:
        /// 
        /// * create a new polygon, subdivided this way:
        ///   + get center of polygon
        ///   + create a list of angles for iteration
        ///   + iterate through the angles, create a line using the angle and find the intersection with a line of the polygon
        /// * iterate through the subdivided polygon
        ///   + move the vertex towards the center
        ///   + consider previous vertex in order to not move in too large steps
        /// </summary>
        public static List<Vector> CreateRandomShape(List<Vector> polygon, double ellipseRelaxationFactor, int angleStepCount, bool randomAngleMovement, bool keepOriginalShape, bool randomStartAngle)
        {

            Vector meanVector = PolygonUtils.GetMeanVector(polygon);
            double length = meanVector.Length * 2;

            // get the list of angles to step through
            List<double> angleRadList = CreateAngleList(angleStepCount, randomAngleMovement, randomStartAngle);

            // resample the original polygon by angle line intersection
            List<Vector> resampledPolygon = ResamplePolygon(polygon, meanVector, angleRadList, keepOriginalShape);
            
            #region create new polygon using the intersections
            List<Vector> newPolygon = new List<Vector>();

            for (int i = 0; i < resampledPolygon.Count; i++)
            {
                Vector curr = resampledPolygon[i];

                // position on the ellipse
                Vector position = curr;

                // from center to position
                double distance = (position - meanVector).Length;
                Vector direction = (position - meanVector);
                direction.Normalize();

                // move from center towards new position. but not too much, let it depend on the previous distance
                {
                    // move initially from 0 to max distance. otherwise use the previous value
                    double min = distance * ellipseRelaxationFactor;
                    double max = distance;

                    // the radius must be smaller during the next iteration, we are navigating around an ellipse => clamp the values
                    if (min > max)
                        min = max;

                    double moveDistance = Utils.GetRandomRange(min, max);

                    distance = moveDistance;

                }

                position = meanVector + distance * direction;

                newPolygon.Add(position);

            }
            #endregion create new polygon using the intersections

            /*
             * TODO: Douglas-Peucker reduction
             */

            // sort again
            PolygonUtils.SortClockWise(newPolygon);

            return newPolygon;

        }


    }
}
