namespace Pathfinding
{
    public interface IGrid
    {
        long GridToArrayPos(int x, int y);
        bool IsWalkable(int x, int y);
    }
}