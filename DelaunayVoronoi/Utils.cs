using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractiveDelaunayVoronoi
{
    public class Utils
    {

        public static Random random = new Random();

        public static double GetRandomRange(double min, double max)
        {
            return random.NextDouble() * (max - min) + min;
        }

    }
}
