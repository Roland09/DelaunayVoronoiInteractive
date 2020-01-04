using System.Windows;

namespace InteractiveDelaunayVoronoi
{
    /// <summary>
    /// A cell containing the site and 
    /// </summary>
    public class Cell
    {
        /// <summary>
        /// The vertices which build the voronoi cell
        /// </summary>
        public Vector[] Vertices { get; }

        /// <summary>
        /// The center point around which the vertices are distributed
        /// </summary>
        public Vector Centroid { get; }
        
        /// <summary>
        /// The point that was used for the Delaunay Triangulation
        /// </summary>
        public Vector DelaunayPoint { get; }

        public Cell(Vector[] vertices, Vector centroid, Vector delaunayPoint)
        {
            Vertices = vertices;
            Centroid = centroid;
            DelaunayPoint = delaunayPoint;
        }
    }
}
