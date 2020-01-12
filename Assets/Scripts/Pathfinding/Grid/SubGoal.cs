namespace Pathfinding
{
    public class SubGoal
    {
        public int x;
        public int y;
        public SubGoalEdge[] edges;

        public SubGoal(int x, int y)
        {
            this.x = x;
            this.y = y;
            this.edges = new SubGoalEdge[0];
        }
    }
}