using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Match3.Core;
using Match3.Utils;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Match3.Game
{
    public class TileView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private SkeletonAnimation clearAnimation;

        [Header("Drop animation settings")]
        [SerializeField] private Vector3 squashScale = new(7f / 6f, 2f / 3f, 1f);
        [SerializeField] private float squashOffsetY = 0.3f;
        [SerializeField] private float squashDuration = 0.1f;
        [SerializeField] private Vector3 stretchScale = new(8f / 9f, 7f / 6f, 1f);
        [SerializeField] private float stretchDuration = 0.1f;
        [SerializeField] private float recoverDuration = 0.1f;

        private readonly AssetHandle<Sprite> spriteHandle = new();
        private readonly AssetHandle<SkeletonDataAsset> clearAnimHandle = new();
        private CancellationTokenSource cts;

        public void Init(GameConfig.TileData tileData)
        {
            ResetCts();
            Reset();

            InitSpriteAsync(tileData.spriteRef, cts.Token).Forget();
            InitClearAnimAsync(tileData.clearAnimRef, cts.Token).Forget();
        }

        private async UniTaskVoid InitSpriteAsync(AssetReference spriteRef, CancellationToken ct)
        {
            sprite.gameObject.SetActive(false);
            var result = await spriteHandle.LoadAsync(spriteRef, ct);

            if (ct.IsCancellationRequested || result == null)
                return;

            sprite.sprite = result;
            sprite.gameObject.SetActive(true);
        }

        private async UniTaskVoid InitClearAnimAsync(AssetReference clearAnimRef, CancellationToken ct)
        {
            clearAnimation.gameObject.SetActive(false);
            var result = await clearAnimHandle.LoadAsync(clearAnimRef, ct);

            if (ct.IsCancellationRequested || result == null)
                return;

            clearAnimation.skeletonDataAsset = result;
            clearAnimation.Initialize(true);
        }

        private void Reset()
        {
            sprite.transform.localPosition = Vector3.zero;
            sprite.transform.localScale = Vector3.one;
        }

        private void ResetCts()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new();
        }

        public void Clear()
        {
            ResetCts();
            spriteHandle.Release();
            clearAnimHandle.Release();
        }

        private void OnDestroy()
        {
            Clear();
        }

        public async UniTask ClearAnimationAsync(CancellationToken ct = default)
        {
            sprite.gameObject.SetActive(false);
            clearAnimation.gameObject.SetActive(true);
            clearAnimation.AnimationState.SetAnimation(0, "animation", false);
            await WaitForAnimationComplete(clearAnimation, LinkToken(ct));
        }

        public async UniTask DropAnimationAsync(CancellationToken ct = default)
        {
            try
            {
                await DOTween.Sequence()
                    .Append(sprite.transform.DOScale(squashScale, squashDuration))
                    .Join(sprite.transform.DOLocalMoveY(squashOffsetY, squashDuration))
                    .Append(sprite.transform.DOScale(stretchScale, stretchDuration))
                    .Join(sprite.transform.DOLocalMoveY(0f, stretchDuration))
                    .Append(sprite.transform.DOScale(Vector3.one, recoverDuration))
                    .SetLink(gameObject)
                    .ToUniTask(cancellationToken: LinkToken(ct));
            }
            catch (OperationCanceledException)
            {
                Reset();
            }
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

            void OnComplete(TrackEntry e) => tcs.TrySetResult();
        }
        
        private CancellationToken LinkToken(CancellationToken external)
        {
            if (cts == null)
                return external;

            if (external == default)
                return cts.Token;

            return CancellationTokenSource.CreateLinkedTokenSource(external, cts.Token).Token;
        }
    }
}
