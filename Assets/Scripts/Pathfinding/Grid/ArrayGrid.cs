using System.Runtime.CompilerServices;

namespace Pathfinding
{
    public class ArrayGrid : IGrid
    {
        public int SizeX;
        public int SizeY;
        public int[] Weights;

        public ArrayGrid(int sizeX, int sizeY)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            Weights = new int[SizeX * SizeY];
        }

        public Position GetSize()
        {
            return new Position(SizeX, SizeY);
        }

        public void SetWeight(int x, int y, int weight)
        {
            Weights[x * SizeY + y] = weight;
        }

        public int GetWeight(int x, int y)
        {
            if (x < 0 || x >= SizeX || y < 0 || y >= SizeY)
                return -1;
            return Weights[x * SizeY + y];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWalkable(int x, int y)
        {
            return (x >= 0 && x < SizeX) && (y >= 0 && y < SizeY) && Weights[x * SizeY + y] != 0;
        }
    }
}
