namespace Pathfinding
{
    public class NavChunkGrid : IGrid
    {
        private IGrid grid;
        private long sizeY;

        public NavChunkGrid(IGrid baseGrid)
        {
            this.grid = baseGrid;
        }

        public Position GetSize()
        {
            return grid.GetSize();
        }

        public int GetWeight(int x, int y)
        {
            throw new System.NotImplementedException();
        }

        public bool IsWalkable(int x, int y)
        {
            throw new System.NotImplementedException();
        }

        public void SetWeight(int x, int y, int weight)
        {
            throw new System.NotImplementedException();
        }
    }
}