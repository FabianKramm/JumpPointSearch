using System.Runtime.CompilerServices;

namespace Pathfinding
{
    public class ArrayGrid : IGrid
    {
        public int SizeX;
        public int SizeY;
        public float[] Weights;

        public ArrayGrid(int sizeX, int sizeY)
        {
            SizeX = sizeX;
            SizeY = sizeY;

            Weights = new float[SizeX * SizeY];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GridToArrayPos(int x, int y)
        {
            return x * SizeY + y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWalkable(int x, int y)
        {
            return (x >= 0 && x < SizeX) && (y >= 0 && y < SizeY) && Weights[x * SizeY + y] != 0;
        }
    }
}
