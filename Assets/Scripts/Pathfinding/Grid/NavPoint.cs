using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pathfinding
{
    public class NavPoint
    {
        public int x;
        public int y;
        public NavPointEdge[] edges;

        public NavPoint(int x, int y)
        {
            this.x = x;
            this.y = y;
            this.edges = new NavPointEdge[0];
        }

        public NavPoint Clone()
        {
            var cloned = new NavPoint(x, y);
            cloned.edges = new NavPointEdge[edges.Length];
            Array.Copy(edges, cloned.edges, edges.Length);
            return cloned;
        }
    }
}
