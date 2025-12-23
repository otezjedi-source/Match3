using System;
using System.Collections.Generic;
using MiniIT.CORE;
using MiniIT.ECS.Components;
using MiniIT.GAME;
using UnityEngine;
using VContainer;

namespace MiniIT.ECS.Systems
{
    public class MatchDetectionSystem
    {
        [Inject] private readonly EcsWorld _world;
        [Inject] private readonly GameConfig _config;
        [Inject] private readonly GridInitializationSystem _gridSystem;

        private readonly HashSet<Entity> _matchesCache = new HashSet<Entity>();
        private readonly List<Entity> _matchesList = new List<Entity>();
        private bool? _hasPossibleMoves = null;

        public List<Entity> FindMatches()
        {
            _matchesCache.Clear();
            _matchesList.Clear();

            CheckHorizontalMatches(_matchesCache);
            CheckVerticalMatches(_matchesCache);

            _matchesList.AddRange(_matchesCache);

            foreach (var tileEntity in _matchesList)
            {
                if (!_world.HasComponent<MatchedComponent>(tileEntity))
                {
                    _world.AddComponent<MatchedComponent>(tileEntity);
                }
            }

            return _matchesList;
        }

        public bool HasPossibleMoves()
        {
            if (!_hasPossibleMoves.HasValue)
                _hasPossibleMoves = CheckHasPossibleMoves();
            return _hasPossibleMoves.Value;
        }

        public void InvalidateHasPossibleMoves()
        {
            _hasPossibleMoves = null;
        }

        private bool CheckHasPossibleMoves()
        {
            for (int x = 0; x < _config.GridWidth; x++)
            {
                for (int y = 0; y < _config.GridHeight; y++)
                {
                    var cellEntity = _gridSystem.GetCellAt(x, y);
                    if (cellEntity.IsNull)
                        continue;

                    var cellComp = _world.GetComponent<CellComponent>(cellEntity);
                    if (cellComp.TileEntity.IsNull)
                        continue;

                    if (x < _config.GridWidth - 1)
                    {
                        var rightCellEntity = _gridSystem.GetCellAt(x + 1, y);
                        if (!rightCellEntity.IsNull)
                        {
                            var rightCellComp = _world.GetComponent<CellComponent>(rightCellEntity);
                            if (!rightCellComp.TileEntity.IsNull)
                            {
                                if (CanCreateMatch(cellEntity, rightCellEntity))
                                    return true;
                            }
                        }
                    }

                    if (y < _config.GridHeight - 1)
                    {
                        var upCellEntity = _gridSystem.GetCellAt(x, y + 1);
                        if (!upCellEntity.IsNull)
                        {
                            var upCellComp = _world.GetComponent<CellComponent>(upCellEntity);
                            if (!upCellComp.TileEntity.IsNull)
                            {
                                if (CanCreateMatch(cellEntity, upCellEntity))
                                    return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void CheckHorizontalMatches(HashSet<Entity> matches)
        {
            var matchCells = new List<Entity>();

            for (int y = 0; y < _config.GridHeight; y++)
            {
                ScanLine(_config.GridWidth, i => _gridSystem.GetCellAt(i, y), matchCells);

                foreach (var cellEntity in matchCells)
                {
                    var cellComp = _world.GetComponent<CellComponent>(cellEntity);
                    if (!cellComp.TileEntity.IsNull)
                    {
                        matches.Add(cellComp.TileEntity);
                    }
                }
            }
        }

        private void CheckVerticalMatches(HashSet<Entity> matches)
        {
            var matchCells = new List<Entity>();

            for (int x = 0; x < _config.GridWidth; x++)
            {
                ScanLine(_config.GridHeight, i => _gridSystem.GetCellAt(x, i), matchCells);

                foreach (var cellEntity in matchCells)
                {
                    var cellComp = _world.GetComponent<CellComponent>(cellEntity);
                    if (!cellComp.TileEntity.IsNull)
                    {
                        matches.Add(cellComp.TileEntity);
                    }
                }
            }
        }

        private void ScanLine(int length, Func<int, Entity> getCell, List<Entity> matches)
        {
            int i = 0;

            while (i < length)
            {
                var cellEntity = getCell(i);
                if (cellEntity.IsNull)
                {
                    i++;
                    continue;
                }

                var cellComp = _world.GetComponent<CellComponent>(cellEntity);
                if (cellComp.TileEntity.IsNull)
                {
                    i++;
                    continue;
                }

                int j = i + 1;
                var tileEntity = cellComp.TileEntity;
                var tileTypeComp = _world.GetComponent<TileTypeComponent>(tileEntity);
                var type = tileTypeComp.Type;

                while (j < length)
                {
                    var nextCellEntity = getCell(j);
                    if (nextCellEntity.IsNull)
                        break;

                    var nextCellComp = _world.GetComponent<CellComponent>(nextCellEntity);
                    if (nextCellComp.TileEntity.IsNull)
                        break;

                    var nextTileTypeComp = _world.GetComponent<TileTypeComponent>(nextCellComp.TileEntity);
                    if (nextTileTypeComp.Type != type)
                        break;

                    j++;
                }

                int count = j - i;
                if (count >= _config.MatchCount)
                {
                    for (int k = i; k < j; k++)
                    {
                        matches.Add(getCell(k));
                    }
                }

                i = j;
            }
        }

        private bool CanCreateMatch(Entity cellEntityA, Entity cellEntityB)
        {
            var cellCompA = _world.GetComponent<CellComponent>(cellEntityA);
            var cellCompB = _world.GetComponent<CellComponent>(cellEntityB);

            var tileA = cellCompA.TileEntity;
            var tileB = cellCompB.TileEntity;

            cellCompA.TileEntity = tileB;
            cellCompB.TileEntity = tileA;

            var posA = _world.GetComponent<GridPositionComponent>(cellEntityA).Position;
            var posB = _world.GetComponent<GridPositionComponent>(cellEntityB).Position;

            bool hasMatch = IsMatchAt(posA.x, posA.y) || IsMatchAt(posB.x, posB.y);

            cellCompA.TileEntity = tileA;
            cellCompB.TileEntity = tileB;

            return hasMatch;
        }

        private bool IsMatchAt(int x, int y)
        {
            var cellEntity = _gridSystem.GetCellAt(x, y);
            if (cellEntity.IsNull)
                return false;

            var cellComp = _world.GetComponent<CellComponent>(cellEntity);
            if (cellComp.TileEntity.IsNull)
                return false;

            var tileTypeComp = _world.GetComponent<TileTypeComponent>(cellComp.TileEntity);
            var tileType = tileTypeComp.Type;

            var horizontalCount = CountLine(x, y, 1, 0, tileType) + CountLine(x, y, -1, 0, tileType) - 1;
            var verticalCount = CountLine(x, y, 0, 1, tileType) + CountLine(x, y, 0, -1, tileType) - 1;

            return horizontalCount >= _config.MatchCount || verticalCount >= _config.MatchCount;
        }

        private int CountLine(int x, int y, int dx, int dy, TileType type)
        {
            int count = 0;

            while (true)
            {
                var cellEntity = _gridSystem.GetCellAt(x, y);
                if (cellEntity.IsNull)
                    break;

                var cellComp = _world.GetComponent<CellComponent>(cellEntity);
                if (cellComp.TileEntity.IsNull)
                    break;

                var tileTypeComp = _world.GetComponent<TileTypeComponent>(cellComp.TileEntity);
                if (tileTypeComp.Type != type)
                    break;

                count++;
                x += dx;
                y += dy;
            }

            return count;
        }
    }
}
