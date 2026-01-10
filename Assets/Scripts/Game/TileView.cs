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

        private EntityManager entityMgr;
        private Entity entity;

        #region Init
        public void Init(GameConfig.TileData tileData)
        {
            ResetCts();
            ResetTransforms();

            InitSpriteAsync(tileData.spriteRef, cts.Token).Forget(ex => Debug.LogException(ex));
            InitClearAnimAsync(tileData.clearAnimRef, cts.Token).Forget(ex => Debug.LogException(ex));
        }

        private async UniTask InitSpriteAsync(AssetReference spriteRef, CancellationToken ct)
        {
            if (sprite == null)
                return;

            sprite.gameObject.SetActive(false);
            var result = await spriteHandle.LoadAsync(spriteRef, ct);

            if (ct.IsCancellationRequested || result == null)
                return;

            sprite.sprite = result;
            sprite.gameObject.SetActive(true);
        }

        private async UniTask InitClearAnimAsync(AssetReference clearAnimRef, CancellationToken ct)
        {
            if (clearAnimation == null)
                return;

            clearAnimation.gameObject.SetActive(false);
            var result = await clearAnimHandle.LoadAsync(clearAnimRef, ct);

            if (ct.IsCancellationRequested || result == null)
                return;

            clearAnimation.skeletonDataAsset = result;
            clearAnimation.Initialize(true);
        }
        
        private void OnDestroy()
        {
            Clear();
        }
        #endregion

        #region Binding
        public void Bind(EntityManager entityMgr, Entity entity)
        {
            this.entityMgr = entityMgr;
            this.entity = entity;
        }
        
        public void Unbind()
        {
            entity = Entity.Null;
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
        public void Clear()
        {
            ClearCts();
            spriteHandle.Release();
            clearAnimHandle.Release();
            ResetTransforms();
            Unbind();
        }

        private void ClearCts()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
        #endregion

        #region Animations
        public void PlayClearAnim()
        {
            ClearAnimationAsync().Forget(ex => Debug.LogException(ex));
        }

        private async UniTask ClearAnimationAsync(CancellationToken ct = default)
        {
            if (sprite != null)
                sprite.gameObject.SetActive(false);

            if (clearAnimation != null)
            {
                clearAnimation.gameObject.SetActive(true);
                clearAnimation.AnimationState.SetAnimation(0, "animation", false);

                try
                {
                    using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts?.Token ?? default, ct);
                    await WaitForAnimationComplete(clearAnimation, tokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            Notify<ClearDoneEvent>();
        }

        public void PlayDropAnim()
        {
            DropAnimationAsync().Forget(ex => Debug.LogException(ex));
        }

        private async UniTask DropAnimationAsync(CancellationToken ct = default)
        {
            if (sprite != null)
            {
                try
                {
                    using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts?.Token ?? default, ct);

                    await DOTween.Sequence()
                        .Append(sprite.transform.DOScale(squashScale, squashDuration))
                        .Join(sprite.transform.DOLocalMoveY(squashOffsetY, squashDuration))
                        .Append(sprite.transform.DOScale(stretchScale, stretchDuration))
                        .Join(sprite.transform.DOLocalMoveY(0f, stretchDuration))
                        .Append(sprite.transform.DOScale(Vector3.one, recoverDuration))
                        .SetLink(gameObject)
                        .ToUniTask(cancellationToken: tokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    ResetTransforms();
                    return;
                }
            }

            Notify<DropDoneEvent>();
        }
        #endregion

        #region Helpers
        private async UniTask WaitForAnimationComplete(SkeletonAnimation anim, CancellationToken ct = default)
        {
            if (anim == null)
                return;

            var entry = anim.AnimationState.GetCurrent(0);
            if (entry == null)
                return;

            var tcs = new UniTaskCompletionSource();

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
        
        private void Notify<T>() where T : unmanaged, IComponentData
        {
            if (entity == Entity.Null || !entityMgr.Exists(entity))
                return;

            try
            {
                if (!entityMgr.HasComponent<T>(entity))
                    entityMgr.AddComponentData(entity, default(T));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        #endregion
    }
}
