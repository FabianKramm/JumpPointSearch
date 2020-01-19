using System;

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

        public SubGoal Clone()
        {
            var cloned = new SubGoal(x, y);
            cloned.edges = new SubGoalEdge[edges.Length];
            Array.Copy(edges, cloned.edges, edges.Length);
            return cloned;
        }
    }
}