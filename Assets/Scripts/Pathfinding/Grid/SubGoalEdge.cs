namespace Pathfinding
{
    public struct SubGoalEdge
    {
        public int toX;
        public int toY;
        public float cost;

        public SubGoalEdge(int x, int y, float cost)
        {
            this.toX = x;
            this.toY = y;
            this.cost = cost;
        }
    }
}