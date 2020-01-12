namespace Pathfinding
{
    public struct SubGoalEdge
    {
        public int toX;
        public int toY;
        public int cost;

        public SubGoalEdge(int x, int y, int cost)
        {
            this.toX = x;
            this.toY = y;
            this.cost = cost;
        }
    }
}