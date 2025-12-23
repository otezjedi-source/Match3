using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Core;
using Match3.Factories;
using Match3.Game;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Match3.Controllers
{
    public class GridController
    {
        [Inject] private readonly GameConfig config;
        [Inject] private readonly CellFactory cellFactory;
        [Inject] private readonly TileFactory tileFactory;
        [Inject] private readonly SoundController soundController;

        private Cell[,] cells = null;

        #region Initialization
        public void Init()
        {
            CreateCells();
            CreateTiles();
        }

        private void CreateCells()
        {
            cells = new Cell[config.GridWidth, config.GridHeight];

            for (int x = 0; x < config.GridWidth; x++)
            {
                for (int y = 0; y < config.GridHeight; y++)
                {
                    var gridPos = new Vector2Int(x, y);
                    var worldPos = new Vector3(x, y, 0);
                    cells[x, y] = cellFactory.Create(gridPos, worldPos);
                }
            }
        }

        public void ResetTiles()
        {
            ClearTiles();
            CreateTiles();
        }
        #endregion

        #region Cells access
        public Cell GetCell(int x, int y)
        {
            if (x < 0 || x >= config.GridWidth || y < 0 || y >= config.GridHeight)
            {
                return null;
            }
            return cells[x, y];
        }

        public Cell GetCellAtWorldPosition(Vector3 worldPosition)
        {
            int x = Mathf.RoundToInt(worldPosition.x);
            int y = Mathf.RoundToInt(worldPosition.y);
            return GetCell(x, y);
        }
        #endregion

        #region Tiles control
        private void CreateTiles()
        {
            for (int x = 0; x < config.GridWidth; x++)
            {
                for (int y = 0; y < config.GridHeight; y++)
                {
                    CreateTile(x, y);
                }
            }
        }

        private void CreateTile(int x, int y)
        {
            var cell = cells[x, y];
            var availableTypes = GetAllowedTileTypes(x, y);

            Tile tile;

            if (availableTypes.Count == 0)
            {
                tile = tileFactory.Create(cell.transform.position);
            }
            else
            {
                var rnd = Random.Range(0, availableTypes.Count);
                tile = tileFactory.CreateSpecificType(cell.transform.position, availableTypes[rnd]);
            }

            PlaceTile(tile, cell);
        }

        private void ClearTiles()
        {
            foreach (var cell in cells)
            {
                if (cell.Tile != null)
                {
                    Object.Destroy(cell.Tile.gameObject);
                    cell.Tile = null;
                }
            }
        }

        private void PlaceTile(Tile tile, Cell cell)
        {
            cell.Tile = tile;
            tile.Cell = cell;
        }
        
        private List<TileType> GetAllowedTileTypes(int x, int y)
        {
            var allowedTypes = new HashSet<TileType>((TileType[])Enum.GetValues(typeof(TileType)));

            CheckHorizontal(x, y, allowedTypes);
            CheckVertical(x, y, allowedTypes);

            return new List<TileType>(allowedTypes);

            void CheckHorizontal(int x, int y, HashSet<TileType> allowed)
            {
                if (x < 2)
                {
                    return;
                }

                var tile1 = cells[x - 1, y].Tile;
                var tile2 = cells[x - 2, y].Tile;

                if (tile1 != null && tile2 != null && tile1.Type == tile2.Type)
                {
                    allowed.Remove(tile1.Type);
                }
            }

            void CheckVertical(int x, int y, HashSet<TileType> allowed)
            {
                if (y < 2)
                {
                    return;
                }

                var tile1 = cells[x, y - 1].Tile;
                var tile2 = cells[x, y - 2].Tile;

                if (tile1 != null && tile2 != null && tile1.Type == tile2.Type)
                {
                    allowed.Remove(tile1.Type);
                }
            }
        }
        #endregion

        #region Tile movement
        public async UniTask SwapTilesAsync(Cell cellA, Cell cellB, CancellationToken ct = default)
        {
            (cellA.Tile, cellB.Tile) = (cellB.Tile, cellA.Tile);
            cellA.Tile.Cell = cellA;
            cellB.Tile.Cell = cellB;

            await UniTask.WhenAll(
                cellA.Tile.MoveToAsync(cellA.transform.position, config.SwapDuration, ct),
                cellB.Tile.MoveToAsync(cellB.transform.position, config.SwapDuration, ct)
            );
        }

        public async UniTask RemoveTileAsync(Tile tile)
        {
            if (tile.Cell != null)
            {
                tile.Cell.Tile = null;
            }

            await tile.ClearAnimationAsync();
            tileFactory.Return(tile);
        }
        #endregion

        #region Fall and fill
        public async UniTask FallTilesAsync(CancellationToken ct = default)
        {
            var movedTiles = new List<Tile>();

            for (int x = 0; x < config.GridWidth; x++)
            {
                for (int y = 0; y < config.GridHeight; y++)
                {
                    var cell = cells[x, y];
                    if (!cells[x, y].IsEmpty)
                        continue;

                    for (int yAbove = y + 1; yAbove < config.GridHeight; yAbove++)
                    {
                        var cellAbove = cells[x, yAbove];
                        if (cellAbove.IsEmpty)
                        {
                            continue;
                        }

                        MoveTileToCell(cellAbove.Tile, cell);
                        movedTiles.Add(cell.Tile);
                        break;
                    }
                }
            }

            if (movedTiles.Count > 0)
            {
                await AnimateTilesFall(movedTiles, ct);
            }
        }

        public async UniTask FillEmptyCellsAsync(CancellationToken ct = default)
        {
            var newTiles = new List<Tile>();

            for (int x = 0; x < config.GridWidth; x++)
            {
                FillColumn(x, newTiles);
            }

            if (newTiles.Count > 0)
            {
                await AnimateTilesFall(newTiles, ct);
            }
        }

        private void MoveTileToCell(Tile tile, Cell target)
        {
            tile.Cell.Tile = null;
            PlaceTile(tile, target);
        }

        private void FillColumn(int x, List<Tile> tiles)
        {
            int emptyCount = CountEmptyInColumn(x);

            for (int i = 0; i < emptyCount; i++)
            {
                int targetY = config.GridHeight - emptyCount + i;
                var cell = cells[x, targetY];

                var spawnPos = new Vector3(x, config.GridHeight + i, 0);
                var tile = tileFactory.Create(spawnPos);

                PlaceTile(tile, cell);
                tiles.Add(tile);
            }
        }

        private int CountEmptyInColumn(int x)
        {
            int count = 0;
            for (int y = 0; y < config.GridHeight; y++)
            {
                if (cells[x, y].IsEmpty) count++;
            }
            return count;
        }

        private async UniTask AnimateTilesFall(List<Tile> tiles, CancellationToken ct)
        {
            await UniTask.WhenAll(tiles.Select(t => t.MoveToAsync(t.Cell.transform.position, config.FallDuration, ct)));
            soundController.PlayDrop();
            await UniTask.WhenAll(tiles.Select(t => t.DropAnimationAsync()));
        }
        #endregion   
    }
}
