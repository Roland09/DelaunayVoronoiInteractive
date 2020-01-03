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
        public Vector Center { get; }

        public Cell(Vector[] vertices, Vector center)
        {
            Vertices = vertices;
            Center = center;
        }
    }
}
