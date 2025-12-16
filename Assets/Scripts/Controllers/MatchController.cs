using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MiniIT.CORE;
using MiniIT.GAME;
using VContainer;

namespace MiniIT.CONTROLLERS
{
    public class MatchController
    {
        [Inject] private readonly GameConfig config;
        [Inject] private readonly GridController gridController;

        private readonly HashSet<Tile> matchesCache = new HashSet<Tile>();
        private readonly List<Tile> matchesList = new List<Tile>();
        private bool? hasPossibleMoves = null;

        #region Public methods
        public List<Tile> FindMatches()
        {
            matchesCache.Clear();
            matchesList.Clear();

            CheckHorizontalMatches(matchesCache);
            CheckVerticalMatches(matchesCache);

            matchesList.AddRange(matchesCache);
            return matchesList;
        }

        public async UniTask DestroyMatchesAsync(List<Tile> matches)
        {
            var tasks = matches.ConvertAll(tile => gridController.RemoveTileAsync(tile));
            await UniTask.WhenAll(tasks);
        }

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
                    var cell = gridController.GetCell(x, y);
                    if (cell == null || cell.IsEmpty)
                    {
                        continue;
                    }

                    // try swap right
                    if (x < config.GridWidth - 1)
                    {
                        var rightCell = gridController.GetCell(x + 1, y);
                        if (rightCell != null && !rightCell.IsEmpty)
                        {
                            if (CanCreateMatch(cell, rightCell))
                            {
                                return true;
                            }
                        }
                    }

                    // try swap up
                    if (y < config.GridHeight - 1)
                    {
                        var upCell = gridController.GetCell(x, y + 1);
                        if (upCell != null && !upCell.IsEmpty)
                        {
                            if (CanCreateMatch(cell, upCell))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public void InvalidateHasPossibleMoves()
        {
            hasPossibleMoves = null;
        }
        #endregion

        #region Matches check
        private void CheckHorizontalMatches(HashSet<Tile> matches)
        {
            var matchCells = new List<Cell>();

            for (int y = 0; y < config.GridHeight; y++)
            {
                ScanLine(config.GridWidth, i => gridController.GetCell(i, y), matchCells);

                foreach (var cell in matchCells)
                {
                    matches.Add(cell.Tile);
                }
            }
        }

        private void CheckVerticalMatches(HashSet<Tile> matches)
        {
            var matchCells = new List<Cell>();

            for (int x = 0; x < config.GridWidth; x++)
            {
                ScanLine(config.GridHeight, i => gridController.GetCell(x, i), matchCells);

                foreach (var cell in matchCells)
                {
                    matches.Add(cell.Tile);
                }
            }
        }

        private void ScanLine(int length, Func<int, Cell> getCell, List<Cell> matches)
        {
            int i = 0;

            while (i < length)
            {
                var cell = getCell(i);
                if (cell == null || cell.IsEmpty)
                {
                    i++;
                    continue;
                }

                int j = i + 1;
                var type = cell.Tile.Type;

                while (j < length)
                {
                    var next = getCell(j);
                    if (next == null || next.IsEmpty || next.Tile.Type != type)
                    {
                        break;
                    }

                    j++;
                }

                int count = j - i;
                if (count >= config.MatchCount)
                {
                    for (int k = i; k < j; k++)
                    {
                        matches.Add(getCell(k));
                    }
                }

                i = j;
            }
        }
        #endregion

        #region Move check
        private bool CanCreateMatch(Cell cellA, Cell cellB)
        {
            (cellA.Tile, cellB.Tile) = (cellB.Tile, cellA.Tile);

            bool hasMatch = IsMatchAt(cellA.Position.x, cellA.Position.y) ||
                            IsMatchAt(cellB.Position.x, cellB.Position.y);

            (cellA.Tile, cellB.Tile) = (cellB.Tile, cellA.Tile);

            return hasMatch;
        }

        private bool IsMatchAt(int x, int y)
        {
            var cell = gridController.GetCell(x, y);
            if (cell == null || cell.IsEmpty)
            {
                return false;
            }

            var tileType = cell.Tile.Type;
            var horizontalCount = CountLine(x, y, 1, 0, tileType) +
                CountLine(x, y, -1, 0, tileType) - 1;
            var verticalCount = CountLine(x, y, 0, 1, tileType) +
                CountLine(x, y, 0, -1, tileType) - 1;

            return horizontalCount >= config.MatchCount || verticalCount >= config.MatchCount;
        }

        private int CountLine(int x, int y, int dx, int dy, TileType type)
        {
            int count = 0;

            while (true)
            {
                var c = gridController.GetCell(x, y);
                if (c == null || c.IsEmpty || c.Tile.Type != type)
                {
                    break;
                }

                count++;
                x += dx;
                y += dy;
            }

            return count;
        }
        #endregion
    }
}
