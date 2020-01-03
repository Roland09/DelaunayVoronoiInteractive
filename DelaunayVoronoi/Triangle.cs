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
        public Vector[] Vertices { get; }
        public Vector CircumCenter { get; }
        public double RadiusSquared;

        public Triangle(Vector vector1, Vector vector2, Vector vector3, Vector circumcenter, double radiusSquared)
        {
            Vertices = new Vector[3] { vector1, vector2, vector3 };
            CircumCenter = circumcenter;
            RadiusSquared = radiusSquared;
        }
    }
}
