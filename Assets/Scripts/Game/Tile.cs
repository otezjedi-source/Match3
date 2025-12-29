using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Match3.Core;
using Match3.Utils;
using Spine;
using Spine.Unity;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Match3.Game
{
    public class Tile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private SkeletonAnimation clearAnimation;

        private AssetHandle<Sprite> spriteHandle = new();
        private AssetHandle<SkeletonDataAsset> clearAnimHandle = new();

        public void Init(GameConfig.TileData tileData)
        {
            sprite.gameObject.SetActive(false);
            clearAnimation.gameObject.SetActive(false);

            InitSpriteAsync(tileData.spriteRef).Forget();
            InitClearAnimAsync(tileData.clearAnimRef).Forget();
        }

        private async UniTaskVoid InitSpriteAsync(AssetReference spriteRef)
        {
            sprite.sprite = await spriteHandle.LoadAsync(spriteRef);
            sprite.gameObject.SetActive(true);
        }

        private async UniTaskVoid InitClearAnimAsync(AssetReference clearAnimRef)
        {
            clearAnimation.skeletonDataAsset = await clearAnimHandle.LoadAsync(clearAnimRef);
            clearAnimation.Initialize(true);
        }

        public void Clear()
        {
            spriteHandle.Release();
            clearAnimHandle.Release();
        }

        private void OnDestroy()
        {
            Clear();
        }

        public async UniTask MoveToAsync(float3 targetPosition, float duration, CancellationToken ct = default)
        {
            await transform.DOMove(targetPosition, duration)
                .SetEase(Ease.Linear)
                .ToUniTask(cancellationToken: ct);
        }

        public async UniTask ClearAnimationAsync(CancellationToken ct = default)
        {
            sprite.gameObject.SetActive(false);
            clearAnimation.gameObject.SetActive(true);
            clearAnimation.AnimationState.SetAnimation(0, "animation", false);
            await WaitForAnimationComplete(clearAnimation, ct);
        }

        public async UniTask DropAnimationAsync()
        {
            await DOTween.Sequence()
                .Append(sprite.transform.DOScale(new Vector3(7f / 6, 2f / 3, 1), 0.1f))
                .Insert(0, sprite.transform.DOLocalMoveY(0.3f, 0.1f))
                .Append(sprite.transform.DOScale(new Vector3(8f / 9, 7f / 6, 1), 0.1f))
                .Insert(0.1f, sprite.transform.DOLocalMoveY(0f, 0.1f))
                .Append(sprite.transform.DOScale(Vector3.one, 0.1f))
                .Play()
                .AsyncWaitForCompletion();
        }
        
        private async UniTask WaitForAnimationComplete(SkeletonAnimation skeletonAnimation, CancellationToken ct = default)
        {
            var tcs = new UniTaskCompletionSource();
            var entry = skeletonAnimation.AnimationState.GetCurrent(0);
            
            if (entry == null)
                return;

            entry.Complete += OnComplete;

            try
            {
                await tcs.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                entry.Complete -= OnComplete;
            }

            void OnComplete(TrackEntry e)
            {
                tcs.TrySetResult();
            }
        }
    }
}
