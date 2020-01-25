namespace ChunkedPathFinding
{
    public struct Edge
    {
        public int FromVertex;
        public int ToVertex; // Can be -1 if out of chunk
        public int ToVertexGridPosition;
        public float Cost;
    }
}
