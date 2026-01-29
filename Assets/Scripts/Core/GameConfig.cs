using System;
using System.Collections.Generic;
using Match3.ECS.Components;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Match3.Core
{
    /// <summary>
    /// Central game configuration. Edit in Unity Inspector.
    /// Validated both in editor (OnValidate) and at runtime (Validate).
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Match3/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [FormerlySerializedAs("GridWidth")]
        [Header("Grid Settings")]
        [Min(3)]
        [Tooltip("Width of the game grid. Minimum 3.")]
        public int gridWidth = 5;

        [FormerlySerializedAs("GridHeight")]
        [Min(3)]
        [Tooltip("Height of the game grid. Minimum 3.")]
        public int gridHeight = 9;

        [FormerlySerializedAs("MaxGridInitAttempts")]
        [Min(1)]
        [Tooltip("Maximum attempts to generate a valid grid without matches.")]
        public int maxGridInitAttempts = 100;

        [FormerlySerializedAs("MatchCount")]
        [Header("Game Settings")]
        [Range(3, 7)]
        [Tooltip("Number of tiles needed to form a match.")]
        public int matchCount = 3;

        [FormerlySerializedAs("PointsPerTile")]
        [Min(1)]
        [Tooltip("Points awarded per matched tile.")]
        public int pointsPerTile = 10;

        [FormerlySerializedAs("SwapDuration")]
        [Header("Timings")]
        [Min(0.01f)]
        [Tooltip("Duration of swap animation in seconds.")]
        public float swapDuration = 0.3f;

        [FormerlySerializedAs("FallDuration")]
        [Min(0.01f)]
        [Tooltip("Base duration of fall animation per cell.")]
        public float fallDuration = 0.3f;

        [FormerlySerializedAs("MinDragDistance")]
        [Min(0.01f)]
        [Tooltip("Minimum drag distance to register a swap.")]
        public float minDragDistance = 0.5f;

        [FormerlySerializedAs("MatchDelay")]
        [Min(0f)]
        [Tooltip("Delay before clearing matched tiles.")]
        public float matchDelay = 0.2f;

        [FormerlySerializedAs("TilesData")] [Header("Tiles data")]
        public List<TileData> tilesData;

        [FormerlySerializedAs("BonusesData")] [Header("Bonuses data")]
        public List<BonusData> bonusesData;

        [FormerlySerializedAs("SoundsData")] [Header("Sounds data")]
        public List<SoundData> soundsData;

        [Serializable]
        public class TileData
        {
            public TileType type;
            public AssetReference spriteRef;
            public AssetReference clearAnimRef;

            public bool IsValid =>
                type != TileType.None &&
                spriteRef != null &&
                spriteRef.RuntimeKeyIsValid();
        }

        [Serializable]
        public class BonusData
        {
            public BonusType type;
            [Min(4)] public int matchCount;
            public AssetReference spriteRef;
            public AudioClip sound;
        }

        [Serializable]
        public class SoundData
        {
            public SoundType type;
            public AudioClip sound;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-time validation. Clamps values to valid ranges.
        /// </summary>
        private void OnValidate()
        {
            // Ensure minimum grid size
            gridWidth = Mathf.Max(3, gridWidth);
            gridHeight = Mathf.Max(3, gridHeight);
            
            // MatchCount cannot exceed grid dimensions
            int maxMatchCount = Mathf.Min(gridWidth, gridHeight);
            matchCount = Mathf.Clamp(matchCount, 3, maxMatchCount);
            
            // Ensure positive timings
            swapDuration = Mathf.Max(0.01f, swapDuration);
            fallDuration = Mathf.Max(0.01f, fallDuration);
            minDragDistance = Mathf.Max(0.01f, minDragDistance);
            matchDelay = Mathf.Max(0f, matchDelay);
            
            // Ensure positive values
            maxGridInitAttempts = Mathf.Max(1, maxGridInitAttempts);
            pointsPerTile = Mathf.Max(1, pointsPerTile);

            ValidateTilesData();
            ValidateBonusesData();
            ValidateSoundsData();
        }

        private void ValidateTilesData()
        {
            if (tilesData == null || tilesData.Count == 0)
            {
                Debug.LogWarning($"[GameConfig] No tile data configured in {name}");
                return;
            }

            // Check for minimum number of tile types (need at least 3 for valid gameplay)
            if (tilesData.Count < 3)
                Debug.LogWarning($"[GameConfig] At least 3 tile types recommended for gameplay variety. Current: {tilesData.Count}");

            // Check for duplicate types
            var seenTypes = new HashSet<TileType>();
            foreach (var data in tilesData)
            {
                if (data.type == TileType.None)
                {
                    Debug.LogWarning($"[GameConfig] TileData has TileType.None which is reserved");
                    continue;
                }

                if (!seenTypes.Add(data.type))
                    Debug.LogWarning($"[GameConfig] Duplicate TileType found: {data.type}");

                if (data.spriteRef == null || !data.spriteRef.RuntimeKeyIsValid())
                    Debug.LogWarning($"[GameConfig] TileType {data.type} has no valid sprite reference");
            }
        }

        private void ValidateBonusesData()
        {
            if (bonusesData == null || bonusesData.Count == 0)
                return;
            
            var seenTypes = new HashSet<BonusType>();
            foreach (var data in bonusesData)
            {
                if (!seenTypes.Add(data.type))
                    Debug.LogWarning($"[GameConfig] Duplicate BonusType found: {data.type}");

                if (data.matchCount <= matchCount)
                    Debug.LogWarning($"[GameConfig] BonusType {data.type} must be more than {matchCount}");
                
                if (data.matchCount > Mathf.Min(gridWidth, gridHeight))
                    Debug.LogWarning($"[GameConfig] BonusType {data.type} must not exceed grid dimensions");

                if (data.spriteRef == null || !data.spriteRef.RuntimeKeyIsValid())
                    Debug.LogWarning($"[GameConfig] BonusType {data.type} has no valid sprite reference");

                int maxCount = Mathf.Min(gridWidth, gridHeight);
                data.matchCount = Mathf.Clamp(data.matchCount, matchCount + 1, maxCount);
            }
        }

        private void ValidateSoundsData()
        {
            var seenTypes = new HashSet<SoundType>();
            foreach (var data in soundsData)
            {
                if (!seenTypes.Add(data.type))
                    Debug.LogWarning($"[GameConfig] Duplicate SoundType found: {data.type}");
            }
        }
#endif

        /// <summary>
        /// Validates the configuration at runtime.
        /// Returns true if valid, false otherwise.
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            
            if (gridWidth < 3 || gridHeight < 3)
            {
                error = "Grid dimensions must be at least 3x3";
                return false;
            }
            
            if (matchCount < 3 || matchCount > Mathf.Min(gridWidth, gridHeight))
            {
                error = $"MatchCount must be between 3 and {Mathf.Min(gridWidth, gridHeight)}";
                return false;
            }
            
            if (!ValidateTiles(out error))
                return false;
            
            if (!ValidateBonuses(out error))
                return false;
            
            return true;
        }

        private bool ValidateTiles(out string error)
        {
            error = string.Empty;
            
            if (tilesData == null || tilesData.Count < 2)
            {
                error = "At least 2 tile types are required";
                return false;
            }
            
            var uniqueTypes = new HashSet<TileType>();
            foreach (var data in tilesData)
            {
                if (data.type != TileType.None && data.IsValid)
                    uniqueTypes.Add(data.type);
            }

            if (uniqueTypes.Count < 2)
            {
                error = "At least 2 unique tiles are required";
                return false;
            }

            return true;
        }

        private bool ValidateBonuses(out string error)
        {
            error = string.Empty;
            if (bonusesData == null || bonusesData.Count == 0)
                return true;
            
            var uniqueTypes = new HashSet<BonusType>();
            foreach (var data in bonusesData)
            {
                if (!uniqueTypes.Add(data.type))
                {
                    error = $"[GameConfig] Duplicate BonusType found: {data.type}";
                    return false;
                }

                if (data.matchCount <= matchCount)
                {
                    error = $"[GameConfig] BonusType {data.type} must be more than {matchCount}";
                    return false;
                }

                if (data.matchCount > Mathf.Min(gridWidth, gridHeight))
                {
                    error = $"[GameConfig] BonusType {data.type} must not exceed grid dimensions";
                    return false;
                }

                if (data.spriteRef == null || !data.spriteRef.RuntimeKeyIsValid())
                {
                    error = $"[GameConfig] BonusType {data.type} has no valid sprite reference";
                    return false;
                }
            }
            
            return true;
        }
    }
}
