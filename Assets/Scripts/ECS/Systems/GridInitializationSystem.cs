using MiniIT.CORE;
using MiniIT.ECS.Components;
using MiniIT.FACTORIES;
using MiniIT.GAME;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace MiniIT.ECS.Systems
{
    public class GridInitializationSystem
    {
        [Inject] private readonly EcsWorld _world;
        [Inject] private readonly GameConfig _config;
        [Inject] private readonly CellFactory _cellFactory;
        [Inject] private readonly TileFactory _tileFactory;

        private Entity[,] _cellEntities;

        public void Initialize()
        {
            _cellEntities = new Entity[_config.GridWidth, _config.GridHeight];

            for (int x = 0; x < _config.GridWidth; x++)
            {
                for (int y = 0; y < _config.GridHeight; y++)
                {
                    var position = new Vector2Int(x, y);
                    var worldPosition = new Vector3(x, y, 0);

                    var cellView = _cellFactory.Create(position, worldPosition);
                    var cellEntity = CreateCellEntity(position, worldPosition, cellView);

                    cellView.Entity = cellEntity;
                    _cellEntities[x, y] = cellEntity;
                }
            }

            for (int x = 0; x < _config.GridWidth; x++)
            {
                for (int y = 0; y < _config.GridHeight; y++)
                {
                    var cellEntity = _cellEntities[x, y];
                    var cellComponent = _world.GetComponent<CellComponent>(cellEntity);
                    var gridPos = _world.GetComponent<GridPositionComponent>(cellEntity);
                    var worldPos = _world.GetComponent<WorldPositionComponent>(cellEntity);

                    var tileType = GetAllowedTileType(gridPos.Position);
                    var tileView = _tileFactory.CreateSpecificType(worldPos.Position, tileType);
                    var tileEntity = CreateTileEntity(cellEntity, tileType, gridPos.Position, worldPos.Position, tileView);

                    tileView.Entity = tileEntity;
                    cellComponent.TileEntity = tileEntity;

                    var cellView = _world.GetComponent<ViewComponent>(cellEntity).View as Cell;
                    if (cellView != null)
                    {
                        cellView.Tile = tileView;
                        tileView.Cell = cellView;
                    }
                }
            }
        }

        public Entity GetCellAt(Vector2Int position)
        {
            if (position.x < 0 || position.x >= _config.GridWidth ||
                position.y < 0 || position.y >= _config.GridHeight)
                return Entity.Null;

            return _cellEntities[position.x, position.y];
        }

        public Entity GetCellAt(int x, int y)
        {
            return GetCellAt(new Vector2Int(x, y));
        }

        private Entity CreateCellEntity(Vector2Int gridPos, Vector3 worldPos, Cell cellView)
        {
            var entity = _world.CreateEntity();

            var gridPosComp = _world.AddComponent<GridPositionComponent>(entity);
            gridPosComp.Position = gridPos;

            var worldPosComp = _world.AddComponent<WorldPositionComponent>(entity);
            worldPosComp.Position = worldPos;

            var cellComp = _world.AddComponent<CellComponent>(entity);
            cellComp.TileEntity = Entity.Null;

            var viewComp = _world.AddComponent<ViewComponent>(entity);
            viewComp.View = cellView;

            return entity;
        }

        private Entity CreateTileEntity(Entity cellEntity, TileType type, Vector2Int gridPos, Vector3 worldPos, Tile tileView)
        {
            var entity = _world.CreateEntity();

            var tileTypeComp = _world.AddComponent<TileTypeComponent>(entity);
            tileTypeComp.Type = type;

            var tileComp = _world.AddComponent<TileComponent>(entity);
            tileComp.CellEntity = cellEntity;

            var gridPosComp = _world.AddComponent<GridPositionComponent>(entity);
            gridPosComp.Position = gridPos;

            var worldPosComp = _world.AddComponent<WorldPositionComponent>(entity);
            worldPosComp.Position = worldPos;

            var viewComp = _world.AddComponent<ViewComponent>(entity);
            viewComp.View = tileView;

            return entity;
        }

        private TileType GetAllowedTileType(Vector2Int position)
        {
            var forbiddenTypes = new HashSet<TileType>();

            if (position.x >= 2)
            {
                var left1 = _cellEntities[position.x - 1, position.y];
                var left2 = _cellEntities[position.x - 2, position.y];

                var tile1 = _world.GetComponent<CellComponent>(left1)?.TileEntity;
                var tile2 = _world.GetComponent<CellComponent>(left2)?.TileEntity;

                if (tile1 != null && !tile1.Value.IsNull && tile2 != null && !tile2.Value.IsNull)
                {
                    var type1 = _world.GetComponent<TileTypeComponent>(tile1.Value)?.Type;
                    var type2 = _world.GetComponent<TileTypeComponent>(tile2.Value)?.Type;

                    if (type1 == type2)
                    {
                        forbiddenTypes.Add(type1.Value);
                    }
                }
            }

            if (position.y >= 2)
            {
                var down1 = _cellEntities[position.x, position.y - 1];
                var down2 = _cellEntities[position.x, position.y - 2];

                var tile1 = _world.GetComponent<CellComponent>(down1)?.TileEntity;
                var tile2 = _world.GetComponent<CellComponent>(down2)?.TileEntity;

                if (tile1 != null && !tile1.Value.IsNull && tile2 != null && !tile2.Value.IsNull)
                {
                    var type1 = _world.GetComponent<TileTypeComponent>(tile1.Value)?.Type;
                    var type2 = _world.GetComponent<TileTypeComponent>(tile2.Value)?.Type;

                    if (type1 == type2)
                    {
                        forbiddenTypes.Add(type1.Value);
                    }
                }
            }

            var availableTypes = new List<TileType>();
            for (int i = 0; i < _config.TilesData.Count; i++)
            {
                if (!forbiddenTypes.Contains(_config.TilesData[i].type))
                {
                    availableTypes.Add(_config.TilesData[i].type);
                }
            }

            if (availableTypes.Count == 0)
            {
                return _config.TilesData[Random.Range(0, _config.TilesData.Count)].type;
            }

            return availableTypes[Random.Range(0, availableTypes.Count)];
        }

        public void ResetTiles()
        {
            ClearTiles();
            CreateNewTiles();
        }

        private void ClearTiles()
        {
            for (int x = 0; x < _config.GridWidth; x++)
            {
                for (int y = 0; y < _config.GridHeight; y++)
                {
                    var cellEntity = _cellEntities[x, y];
                    var cellComp = _world.GetComponent<CellComponent>(cellEntity);

                    if (!cellComp.TileEntity.IsNull)
                    {
                        var tileEntity = cellComp.TileEntity;
                        var tileView = _world.GetComponent<ViewComponent>(tileEntity).View as Tile;

                        if (tileView != null)
                        {
                            _tileFactory.Return(tileView);
                        }

                        _world.DestroyEntity(tileEntity);
                        cellComp.TileEntity = Entity.Null;

                        var cellView = _world.GetComponent<ViewComponent>(cellEntity).View as Cell;
                        if (cellView != null)
                        {
                            cellView.Tile = null;
                        }
                    }
                }
            }
        }

        private void CreateNewTiles()
        {
            for (int x = 0; x < _config.GridWidth; x++)
            {
                for (int y = 0; y < _config.GridHeight; y++)
                {
                    var cellEntity = _cellEntities[x, y];
                    var cellComponent = _world.GetComponent<CellComponent>(cellEntity);
                    var gridPos = _world.GetComponent<GridPositionComponent>(cellEntity);
                    var worldPos = _world.GetComponent<WorldPositionComponent>(cellEntity);

                    var tileType = GetAllowedTileType(gridPos.Position);
                    var tileView = _tileFactory.CreateSpecificType(worldPos.Position, tileType);
                    var tileEntity = CreateTileEntity(cellEntity, tileType, gridPos.Position, worldPos.Position, tileView);

                    tileView.Entity = tileEntity;
                    cellComponent.TileEntity = tileEntity;

                    var cellView = _world.GetComponent<ViewComponent>(cellEntity).View as Cell;
                    if (cellView != null)
                    {
                        cellView.Tile = tileView;
                        tileView.Cell = cellView;
                    }
                }
            }
        }

        public void Cleanup()
        {
            _cellEntities = null;
        }
    }
}
