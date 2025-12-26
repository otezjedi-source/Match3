using System;
using System.Collections.Generic;
using Match3.Game;
using Spine.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Match3.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Match3/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid Settings")]
        public int GridWidth = 5;
        public int GridHeight = 9;

        [Header("Game Settings")]
        public int MatchCount = 3;
        public int ScoreForTile = 10;

        [Header("Timings")]
        public float SwapDuration = 0.3f;
        public float FallDuration = 0.3f;
        public float MinDragDistance = 0.5f;
        public float MatchDelay = 0.2f;

        [Header("Tiles Data")]
        public List<TileData> TilesData;

        [Header("Sounds")]
        public AudioClip ButtonClickSound;
        public AudioClip MatchSound;
        public AudioClip DropSound;

        [Serializable]
        public class TileData
        {
            public TileType type;
            public AssetReference spriteRef;
            public SkeletonDataAsset clearAnim;
        }
    }
}
