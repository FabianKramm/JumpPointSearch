using System;
using System.Runtime.CompilerServices;
using System.Threading;
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

        private object s_sync = new object();

        public int SizeX;
        public int SizeY;

        private int chunkCols;
        private int chunkRows;
        private int chunkSize;
        private Chunk[] chunks;

        public ChunkGrid(int sizeX, int sizeY, int chunkSize = 64)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            if (SizeX % chunkSize != 0 || SizeY % chunkSize != 0)
            {
                Debug.LogError("Size x & y needs to be divisable by chunk size");
                return;
            }

            this.chunkSize = chunkSize;
            this.chunkCols = SizeX / chunkSize;
            this.chunkRows = SizeY / chunkSize;
            this.chunks = new Chunk[(SizeX / chunkSize) * (SizeY / chunkSize)];
        }

        private Chunk LoadChunk(int chunkX, int chunkY)
        {
            throw new NotImplementedException();
        }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= SizeX || y < 0 || y >= SizeY)
                return false;

            int chunkX = (x / chunkSize);
            int chunkY = (y / chunkSize);
            int chunkPos = chunkX * chunkRows + chunkY;
            if (chunks[chunkPos] == null)
            {
                lock (s_sync)
                {
                    if (chunks[chunkPos] == null)
                        chunks[chunkPos] = LoadChunk(chunkX, chunkY);
                }
            }

            return chunks[chunkPos].Weights[(x - chunkX * chunkSize) * chunkSize + (y - chunkY * chunkSize)] != 0;
        }

        public void SetWeight(int x, int y, int weight)
        {
            int chunkX = (x / chunkSize);
            int chunkY = (y / chunkSize);
            int chunkPos = chunkX * chunkRows + chunkY;
            if (chunks[chunkPos] == null)
                chunks[chunkPos] = new Chunk((int)chunkSize);

            chunks[chunkPos].Weights[(x - chunkX * chunkSize) * chunkSize + (y - chunkY * chunkSize)] = weight;
        }

        public CellType GetWeight(int x, int y)
        {
            if (x < 0 || x >= SizeX || y < 0 || y >= SizeY)
                return CellType.None;

            int chunkX = (x / chunkSize);
            int chunkY = (y / chunkSize);
            int chunkPos = chunkX * chunkRows + chunkY;
            if (chunks[chunkPos] == null)
            {
                lock (s_sync)
                {
                    if (chunks[chunkPos] == null)
                        chunks[chunkPos] = LoadChunk(chunkX, chunkY);
                }
            }

            return (CellType)chunks[chunkPos].Weights[(x - chunkX * chunkSize) * chunkSize + (y - chunkY * chunkSize)];
        }

        public Position GetSize()
        {
            return new Position((int)SizeX, (int)SizeY);
        }
    }
}
