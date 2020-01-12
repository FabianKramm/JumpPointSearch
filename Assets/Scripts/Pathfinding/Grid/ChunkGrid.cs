using System.Runtime.CompilerServices;
using UnityEngine;

namespace Pathfinding
{
    public class ChunkGrid : IGrid
    {
        public class Chunk
        {
            public int[] Weights;
            public Chunk(int chunkSize)
            {
                Weights = new int[chunkSize * chunkSize];
            }
        }

        public long SizeX;
        public long SizeY;

        private long chunkCols;
        private long chunkRows;
        private long chunkSize;
        private long chunkMagnitude;
        private Chunk[] chunks;

        public ChunkGrid(int sizeX, int sizeY, int chunkSize)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            if (SizeX % chunkSize != 0 || SizeY % chunkSize != 0)
            {
                Debug.LogError("Size x & y needs to be divisable by chunk size");
                return;
            }

            this.chunkSize = chunkSize;
            this.chunkMagnitude = chunkSize * chunkSize;
            this.chunkCols = SizeX / chunkSize;
            this.chunkRows = SizeY / chunkSize;
            this.chunks = new Chunk[(SizeX / chunkSize) * (SizeY / chunkSize)];
        }

        public void AddChunk(int chunkX, int chunkY, Chunk chunk)
        {
            chunks[chunkX * chunkRows + chunkY] = chunk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= SizeX || y < 0 || y >= SizeY)
                return false;

            long arrayPos = x * SizeY + y;
            long chunkPos = arrayPos / chunkMagnitude;
            return chunks[chunkPos].Weights[arrayPos - chunkPos * chunkMagnitude] != 0;
        }

        public void SetWeight(int x, int y, int weight)
        {
            long arrayPos = x * SizeY + y;
            long chunkPos = arrayPos / chunkMagnitude;
            if (chunks[chunkPos] == null)
                chunks[chunkPos] = new Chunk((int)chunkSize);

            chunks[chunkPos].Weights[arrayPos - chunkPos * chunkMagnitude] = weight;
        }

        public Position GetSize()
        {
            return new Position((int)SizeX, (int)SizeY);
        }
    }
}
