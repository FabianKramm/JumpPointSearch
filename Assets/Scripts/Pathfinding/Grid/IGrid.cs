namespace Pathfinding
{
    public interface IGrid
    {
        Position GetSize();
        CellType GetWeight(int x, int y);
        void SetWeight(int x, int y, int weight);
        bool IsWalkable(int x, int y);
    }
}