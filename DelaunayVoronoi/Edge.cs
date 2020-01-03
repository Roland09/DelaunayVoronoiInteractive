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
        public Vector Vector1 { get; }
        public Vector Vector2 { get; }

        public Edge(Vector vector1, Vector vector2)
        {
            Vector1 = vector1;
            Vector2 = vector2;
        }
    }
}
