namespace ChunkedPathfinding
{
    public struct Edge
    {
        public int ToVertex; // Can be -1 if out of chunk
        public int ToVertexGridPosition;
        public float Cost;
    }
}
