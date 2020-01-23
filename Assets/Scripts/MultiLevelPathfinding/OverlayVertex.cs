using System.Collections.Generic;

namespace MultiLevelPathfinding
{
    public struct OverlayVertex
    {
        public int OriginalVertex;
        public int OriginalEdge;
        public int NeighborOverlayVertex; // Can be -1 if outside of chunk
        public List<OverlayEdge>[] OverlayEdges;
    }
}
