using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Match3.Core;
using Match3.ECS.Components;
using Match3.Utils;
using Spine;
using Spine.Unity;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Match3.Game
{
    /// <summary>
    /// Visual representation of a tile. Handles sprite loading and animations.
    /// Paired with an ECS entity that holds the actual game data.
    /// </summary>
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

        // Addressable handles for async asset loading
        private readonly AssetHandle<Sprite> spriteHandle = new();
        private readonly AssetHandle<SkeletonDataAsset> clearAnimHandle = new();

        private CancellationTokenSource cts;
        private EntityManager entityManager;
        private Entity entity;
        private bool isCleared;

        #region Init
        /// <summary>
        /// Initialize the view with tile data. Loads assets asynchronously.
        /// </summary>
        public void Init(GameConfig.TileData tileData, EntityManager entityManager, Entity entity)
        {
            this.entityManager = entityManager;
            this.entity = entity;
            isCleared = false;

            ResetCts();
            ResetTransforms();

            var token = cts.Token;
            InitSpriteAsync(tileData.spriteRef, token).Forget(ex => Debug.LogError($"[TileView] Failed InitSpriteAsync: {ex.Message}"));
            InitClearAnimAsync(tileData.clearAnimRef, token).Forget(ex => Debug.LogError($"[TileView] Failed InitClearAnimAsync: {ex.Message}"));
        }

        private async UniTask InitSpriteAsync(AssetReference spriteRef, CancellationToken ct)
        {
            if (sprite == null || ct.IsCancellationRequested)
                return;

            sprite.gameObject.SetActive(false);

            try
            {
                var result = await spriteHandle.LoadAsync(spriteRef, ct);
                if (ct.IsCancellationRequested || result == null)
                    return;

                sprite.sprite = result;
                sprite.gameObject.SetActive(true);
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask InitClearAnimAsync(AssetReference clearAnimRef, CancellationToken ct)
        {
            if (clearAnimation == null || ct.IsCancellationRequested)
                return;

            clearAnimation.gameObject.SetActive(false);

            try
            {
                var result = await clearAnimHandle.LoadAsync(clearAnimRef, ct);
                if (ct.IsCancellationRequested || result == null)
                    return;

                clearAnimation.skeletonDataAsset = result;
                clearAnimation.Initialize(true);
            }
            catch (OperationCanceledException) { }
        }
        
        private void OnDestroy()
        {
            Clear();
        }
        #endregion

        #region Reset
        private void ResetCts()
        {
            ClearCts();
            cts = new();
        }

        private void ResetTransforms()
        {
            if (sprite != null)
            {
                sprite.transform.localPosition = Vector3.zero;
                sprite.transform.localScale = Vector3.one;
                sprite.gameObject.SetActive(false);
            }

            if (clearAnimation != null)
                clearAnimation.gameObject.SetActive(false);
        }
        #endregion

        #region Clear
        /// <summary>
        /// Release resources and reset state for pooling.
        /// </summary>
        public void Clear()
        {
            if (isCleared)
                return;

            isCleared = true;
            ClearCts();
            spriteHandle.Release();
            clearAnimHandle.Release();
            ResetTransforms();
            entity = Entity.Null;
        }

        private void ClearCts()
        {
            if (cts == null)
                return;

            try
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
                cts.Dispose();
            }
            catch (ObjectDisposedException) { }
            finally
            {
                cts = null;
            }
        }
        #endregion

        #region Animations
        /// <summary>
        /// Play tile destruction animation (Spine). Notifies ECS when complete.
        /// </summary>
        public void PlayClearAnim()
        {
            if (isCleared)
                return;
            ClearAnimationAsync().Forget(ex =>
            {
                if (ex is not OperationCanceledException)
                    Debug.LogError($"[TileView] Failed to play clear animation: {ex.Message}");
            });
        }

        private async UniTask ClearAnimationAsync(CancellationToken ct = default)
        {
            if (isCleared || cts == null || cts.IsCancellationRequested)
            {
                Notify<ClearDoneEvent>();
                return;
            }

            if (sprite != null)
                sprite.gameObject.SetActive(false);

            if (clearAnimation != null)
            {
                clearAnimation.gameObject.SetActive(true);
                clearAnimation.AnimationState.SetAnimation(0, "animation", false);

                try
                {
                    await WaitForAnimationComplete(clearAnimation, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            Notify<ClearDoneEvent>();
        }

        /// <summary>
        /// Play squash-and-stretch animation when tile lands. Notifies ECS when complete.
        /// </summary>
        public void PlayDropAnim()
        {
            if (isCleared)
                return;
            DropAnimationAsync().Forget(ex =>
            {
                if (ex is not OperationCanceledException)
                    Debug.LogError($"[TileView] Failed to play drop animation: {ex.Message}");
            });
        }

        private async UniTask DropAnimationAsync(CancellationToken ct = default)
        {
            if (isCleared || cts == null || cts.IsCancellationRequested)
            {
                Notify<DropDoneEvent>();
                return;
            }

            if (sprite != null)
            {
                try
                {
                    // Classic squash & stretch: squash on impact, stretch up, settle
                    await DOTween.Sequence()
                        .Append(sprite.transform.DOScale(squashScale, squashDuration))
                        .Join(sprite.transform.DOLocalMoveY(squashOffsetY, squashDuration))
                        .Append(sprite.transform.DOScale(stretchScale, stretchDuration))
                        .Join(sprite.transform.DOLocalMoveY(0f, stretchDuration))
                        .Append(sprite.transform.DOScale(Vector3.one, recoverDuration))
                        .SetLink(gameObject)
                        .ToUniTask(cancellationToken: cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Reset to normal state if cancelled
                    if (sprite != null && sprite.transform != null)
                    {
                        sprite.transform.localScale = Vector3.one;
                        sprite.transform.localPosition = Vector3.zero;
                    }
                    return;
                }
            }

            Notify<DropDoneEvent>();
        }
        #endregion

        #region Helpers
        private async UniTask WaitForAnimationComplete(SkeletonAnimation anim, CancellationToken ct = default)
        {
            if (anim == null || ct.IsCancellationRequested)
                return;

            var entry = anim.AnimationState.GetCurrent(0);
            if (entry == null)
                return;

            var tcs = new UniTaskCompletionSource();

            void OnComplete(TrackEntry e) => tcs.TrySetResult();

            entry.Complete += OnComplete;

            try
            {
                await tcs.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                entry.Complete -= OnComplete;
            }
        }

        /// <summary>
        /// Add a component to the paired ECS entity to signal animation completion.
        /// </summary>
        private void Notify<T>() where T : unmanaged, IComponentData
        {
            if (isCleared || entity == Entity.Null)
                return;
            if (World.DefaultGameObjectInjectionWorld?.IsCreated != true)
                return;
            if (!entityManager.Exists(entity))
                return;

            try
            {
                if (!entityManager.HasComponent<T>(entity))
                    entityManager.AddComponentData(entity, default(T));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TileView] Failed to notify {typeof(T)}: {ex.Message}");
            }
        }
        #endregion
    }
}
