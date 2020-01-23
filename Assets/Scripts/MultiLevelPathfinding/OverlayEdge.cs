namespace MultiLevelPathfinding
{
    public struct OverlayEdge
    {
        public int NeighborOverlayVertex; // Can be -1 if outside of chunk
        public float Cost;
    }
}
