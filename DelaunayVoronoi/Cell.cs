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
        public Point[] Vertices { get; }

        /// <summary>
        /// The center point around which the vertices are distributed
        /// </summary>
        public Point Center { get; }

        public Cell(Point[] vertices, Point center)
        {
            Vertices = vertices;
            Center = center;
        }
    }
}
