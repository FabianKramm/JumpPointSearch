using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiLevelPathfinding
{
    public static class OverlayGraphUtilities
    {
        public static ulong GetLocalCellNumber(int x, int y, int sizeY, int[] offset)
        {
            ulong cellNumber = 0;

            var chunkSize = OverlayGraph.LevelDimensions[OverlayGraph.LevelDimensions.Length - 1];
            x = x % chunkSize;
            y = y % chunkSize;

            for (var i = 0; i < OverlayGraph.LevelDimensions.Length; i++)
            {
                // We do the local cell number at this level
                var cellX = x / OverlayGraph.LevelDimensions[i];
                var cellY = y / OverlayGraph.LevelDimensions[i];
                var cellRows = chunkSize / OverlayGraph.LevelDimensions[i];

                cellNumber |= ((ulong)cellX * (ulong)cellRows + (ulong)cellY) << offset[i];
            }

            return cellNumber;
        }

        public static ulong GetCellNumber(int x, int y, int sizeY, int[] offset)
        {
            ulong cellNumber = 0;
            for (var i = 0; i < OverlayGraph.LevelDimensions.Length; i++)
            {
                // We do the local cell number at this level
                var cellX = x / OverlayGraph.LevelDimensions[i];
                var cellY = y / OverlayGraph.LevelDimensions[i];
                var cellRows = sizeY / OverlayGraph.LevelDimensions[i];

                cellNumber |= ((ulong)cellX * (ulong)cellRows + (ulong)cellY) << offset[i];
            }

            return cellNumber;
        }

        public static ulong GetCellNumberOnLevel(int l, ulong cellNumber, int[] offset)
        {
            return (cellNumber & ~(ulong)(~0 << offset[l])) >> offset[l - 1];
        }

        public static int GetQueryLevel(ulong sCellNumber, ulong tCellNumber, ulong vCellNumber, int[] offset)
        {
            var l_sv = GetHighestDifferingLevel(sCellNumber, vCellNumber, offset);
            var l_tv = GetHighestDifferingLevel(vCellNumber, tCellNumber, offset);

            return l_sv < l_tv ? l_sv : l_tv;
        }

        public static int GetHighestDifferingLevel(ulong c1, ulong c2, int[] offset)
        {
            ulong diff = c1 ^ c2;
            if (diff == 0)
                return 0;

            for (int l = offset.Length - 1; l > 0; --l)
            {
                if (diff >> offset[l - 1] > 0)
                    return l;
            }

            return 0;
        }

        public static ulong TruncateToLevel(ulong cellNumber, int l, int[] offset)
        {
            return cellNumber >> offset[l - 1];
        }
    }
}
