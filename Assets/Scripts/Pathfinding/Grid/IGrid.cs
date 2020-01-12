namespace Pathfinding
{
    public interface IGrid
    {
        Position GetSize();
        void SetWeight(int x, int y, int weight);
        bool IsWalkable(int x, int y);
    }
}