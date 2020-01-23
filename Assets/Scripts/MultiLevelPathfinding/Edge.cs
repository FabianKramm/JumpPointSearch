using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiLevelPathfinding
{
    public struct Edge
    {
        public int FromVertex;
        public int ToVertex; // Can be -1 if out of chunk
        public int ToVertexGridPosition;
        public float Cost;
    }
}
