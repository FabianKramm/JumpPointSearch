namespace Pathfinding
{
    public struct NavPointEdge
    {
        public int toX;
        public int toY;
        public float cost;

        public NavPointEdge(int x, int y, float cost)
        {
            this.toX = x;
            this.toY = y;
            this.cost = cost;
        }
    }
}
