using System;
using System.Collections.Generic;
using Match3.ECS.Components;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Match3.Core
{
    /// <summary>
    /// Central game configuration. Edit in Unity Inspector.
    /// Validated both in editor (OnValidate) and at runtime (Validate).
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Match3/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid Settings")]
        [Min(3)]
        [Tooltip("Width of the game grid. Minimum 3.")]
        public int GridWidth = 5;

        [Min(3)]
        [Tooltip("Height of the game grid. Minimum 3.")]
        public int GridHeight = 9;

        [Min(1)]
        [Tooltip("Maximum attempts to generate a valid grid without matches.")]
        public int MaxGridInitAttempts = 100;

        [Header("Game Settings")]
        [Range(3, 7)]
        [Tooltip("Number of tiles needed to form a match.")]
        public int MatchCount = 3;

        [Min(1)]
        [Tooltip("Points awarded per matched tile.")]
        public int PointsPerTile = 10;

        [Header("Timings")]
        [Min(0.01f)]
        [Tooltip("Duration of swap animation in seconds.")]
        public float SwapDuration = 0.3f;

        [Min(0.01f)]
        [Tooltip("Base duration of fall animation per cell.")]
        public float FallDuration = 0.3f;

        [Min(0.01f)]
        [Tooltip("Minimum drag distance to register a swap.")]
        public float MinDragDistance = 0.5f;

        [Min(0f)]
        [Tooltip("Delay before clearing matched tiles.")]
        public float MatchDelay = 0.2f;

        [Header("Tiles data")]
        public List<TileData> TilesData;

        [Header("Bonuses data")]
        public List<BonusData> BonusesData;

        [Header("Sounds data")]
        public List<SoundData> SoundsData;

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
            public int matchCount;
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
            GridWidth = Mathf.Max(3, GridWidth);
            GridHeight = Mathf.Max(3, GridHeight);
            
            // MatchCount cannot exceed grid dimensions
            int maxMatchCount = Mathf.Min(GridWidth, GridHeight);
            MatchCount = Mathf.Clamp(MatchCount, 3, maxMatchCount);
            
            // Ensure positive timings
            SwapDuration = Mathf.Max(0.01f, SwapDuration);
            FallDuration = Mathf.Max(0.01f, FallDuration);
            MinDragDistance = Mathf.Max(0.01f, MinDragDistance);
            MatchDelay = Mathf.Max(0f, MatchDelay);
            
            // Ensure positive values
            MaxGridInitAttempts = Mathf.Max(1, MaxGridInitAttempts);
            PointsPerTile = Mathf.Max(1, PointsPerTile);

            ValidateTilesData();
            ValidateBonusesData();
            ValidateSoundsData();
        }

        private void ValidateTilesData()
        {
            if (TilesData == null || TilesData.Count == 0)
            {
                Debug.LogWarning($"[GameConfig] No tile data configured in {name}");
                return;
            }

            // Check for minimum number of tile types (need at least 3 for valid gameplay)
            if (TilesData.Count < 3)
                Debug.LogWarning($"[GameConfig] At least 3 tile types recommended for gameplay variety. Current: {TilesData.Count}");

            // Check for duplicate types
            var seenTypes = new HashSet<TileType>();
            foreach (var data in TilesData)
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
            var seenTypes = new HashSet<BonusType>();
            foreach (var data in BonusesData)
            {
                if (!seenTypes.Add(data.type))
                    Debug.LogWarning($"[GameConfig] Duplicate BonusType found: {data.type}");

                if (data.matchCount <= MatchCount)
                    Debug.LogWarning($"[GameConfig] BonusType {data.type} must be more than {MatchCount}");

                if (data.spriteRef == null || !data.spriteRef.RuntimeKeyIsValid())
                    Debug.LogWarning($"[GameConfig] BonusType {data.type} has no valid sprite reference");

                data.matchCount = Mathf.Max(data.matchCount, MatchCount + 1);
            }
        }

        private void ValidateSoundsData()
        {
            var seenTypes = new HashSet<SoundType>();
            foreach (var data in SoundsData)
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
            
            if (GridWidth < 3 || GridHeight < 3)
            {
                error = "Grid dimensions must be at least 3x3";
                return false;
            }
            
            if (MatchCount < 3 || MatchCount > Mathf.Min(GridWidth, GridHeight))
            {
                error = $"MatchCount must be between 3 and {Mathf.Min(GridWidth, GridHeight)}";
                return false;
            }
            
            if (TilesData == null || TilesData.Count < 2)
            {
                error = "At least 2 tile types are required";
                return false;
            }
            
            // Check if we have enough unique types
            var uniqueTypes = new HashSet<TileType>();
            foreach (var data in TilesData)
            {
                if (data.type != TileType.None && data.IsValid)
                    uniqueTypes.Add(data.type);
            }
            
            if (uniqueTypes.Count < 2)
            {
                error = "At least 2 valid unique tile types are required";
                return false;
            }
            
            return true;
        }
    }
}
