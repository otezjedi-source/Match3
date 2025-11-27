using System;
using System.Collections.Generic;
using MiniIT.GAME;
using Spine.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MiniIT.CORE
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Match3/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public int GridWidth = 5;
        public int GridHeight = 9;
        public int MatchCount = 3;
        public float SwapDuration = 0.3f;
        public float FallDuration = 0.3f;
        public float MinDragDistance = 0.5f;
        public float MatchDelay = 0.2f;

        [Serializable]
        public class TileData
        {
            public TileType type;
            public AssetReference spriteRef;
            public SkeletonDataAsset clearAnim;
        }

        public List<TileData> TilesData;

        public AudioClip ButtonClickSound;
        public AudioClip MatchSound;
        public AudioClip DropSound;
    }
}
