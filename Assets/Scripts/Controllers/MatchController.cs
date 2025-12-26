using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Game;
using Unity.Collections;
using Unity.Mathematics;
using VContainer;

namespace Match3.Controllers
{
    public class MatchController : IDisposable
    {
        private readonly GameConfig config;
        private readonly GridController gridController;

        private readonly NativeHashSet<int2> matches;
        private readonly List<int2> matchesList = new();
        private bool? hasPossibleMoves = null;

        [Inject]
        public MatchController(GameConfig config, GridController gridController)
        {
            this.config = config;
            this.gridController = gridController;
            matches = new(config.GridWidth * config.GridHeight, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (matches.IsCreated)
                matches.Dispose();
        }

        #region Matches check
        public List<int2> FindMatches()
        {
            matches.Clear();

            ScanLines(true);
            ScanLines(false);

            matchesList.Clear();
            foreach (var match in matches)
                matchesList.Add(match);

            return matchesList;
        }

        private void ScanLines(bool horizontal)
        {
            int linesCount = horizontal ? config.GridHeight : config.GridWidth;
            int lineLength = horizontal ? config.GridWidth : config.GridHeight;

            for (int i = 0; i < linesCount; i++)
                ScanLine(i, lineLength, horizontal);
        }

        private void ScanLine(int lineIdx, int length, bool horizontal)
        {
            int i = 0;

            while (i < length)
            {
                int x = horizontal ? i : lineIdx;
                int y = horizontal ? lineIdx : i;

                var type = gridController.GetTileTypeAt(x, y);
                if (type == TileType.None)
                {
                    i++;
                    continue;
                }

                int j = i + 1;
                while (j < length)
                {
                    int nx = horizontal ? j : lineIdx;
                    int ny = horizontal ? lineIdx : j;

                    if (gridController.GetTileTypeAt(nx, ny) != type)
                        break;

                    j++;
                }

                if (j - i >= config.MatchCount)
                {
                    for (int k = i; k < j; k++)
                        matches.Add(horizontal ? new(k, lineIdx) : new(lineIdx, k));
                }

                i = j;
            }
        }
        #endregion

        #region Possible moves
        public bool HasPossibleMoves()
        {
            if (!hasPossibleMoves.HasValue)
                hasPossibleMoves = CheckHasPossibleMoves();
            return hasPossibleMoves.Value;
        }

        private bool CheckHasPossibleMoves()
        {
            for (int x = 0; x < config.GridWidth; x++)
            {
                for (int y = 0; y < config.GridHeight; y++)
                {
                    if (TrySwapCheck(x, y, x + 1, y))
                        return true;
                    if (TrySwapCheck(x, y, x, y + 1))
                        return true;
                }
            }
            return false;
        }

        private bool TrySwapCheck(int x1, int y1, int x2, int y2)
        {
            if (!gridController.IsValidPosition(x2, y2))
                return false;

            var typeA = gridController.GetTileTypeAt(x1, y1);
            var typeB = gridController.GetTileTypeAt(x2, y2);

            if (typeA == TileType.None || typeB == TileType.None)
                return false;

            gridController.Swap(new(x1, y1), new(x2, y2));
            bool hasMatch = HasMatchAt(x1, y1) || HasMatchAt(x2, y2);
            gridController.Swap(new(x1, y1), new(x2, y2));
            return hasMatch;
        }

        private bool HasMatchAt(int x, int y)
        {
            return HasLine(x, y, true) || HasLine(x, y, false);
        }

        private bool HasLine(int x, int y, bool horizontal)
        {
            var type = gridController.GetTileTypeAt(x, y);
            if (type == TileType.None)
                return false;

            int count = 1;

            int dx = horizontal ? 1 : 0;
            int dy = horizontal ? 0 : 1;

            int cx = x + dx;
            int cy = y + dy;

            while (gridController.IsValidPosition(cx, cy) && gridController.GetTileTypeAt(cx, cy) == type)
            {
                count++;
                cx += dx;
                cy += dy;
            }
            
            cx = x - dx;
            cy = y - dy;

            while (gridController.IsValidPosition(cx, cy) && gridController.GetTileTypeAt(cx, cy) == type)
            {
                count++;
                cx -= dx;
                cy -= dy;
            }

            return count >= config.MatchCount;
        }

        // Must be called after any grid changes (swap, fall, fill)
        public void InvalidateHasPossibleMoves()
        {
            hasPossibleMoves = null;
        }
        #endregion
    }
}
