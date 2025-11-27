using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MiniIT.CORE;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MiniIT.GAME
{
    public class Tile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private SkeletonAnimation clearAnimation;

        public TileType Type { get; private set; }
        public Cell Cell { get; set; }
        public Vector2Int Position => Cell != null ? Cell.Position : Vector2Int.zero;

        private AsyncOperationHandle<Sprite> spriteHandle;

        public void Init(GameConfig.TileData tileData)
        {
            Type = tileData.type;
            clearAnimation.skeletonDataAsset = tileData.clearAnim;

            spriteHandle = Addressables.LoadAssetAsync<Sprite>(tileData.spriteRef);
            spriteHandle.Completed += handle =>
            {
                sprite.sprite = handle.Result;
            };
        }

        private void OnDestroy()
        {
            if (spriteHandle.IsValid())
            {
                Addressables.Release(spriteHandle);
            }
        }

        public async UniTask MoveToAsync(Vector3 targetPosition, float duration, CancellationToken ct = default)
        {
            var startPosition = transform.position;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested)
                    return;

                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                await UniTask.Yield(ct);
            }

            transform.position = targetPosition;
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
            entry.Complete += OnComplete;

            if (ct != default)
            {
                ct.Register(() =>
                {
                    entry.Complete -= OnComplete;
                    tcs.TrySetCanceled();
                });
            }

            await tcs.Task;

            void OnComplete(TrackEntry e)
            {
                e.Complete -= OnComplete;
                tcs.TrySetResult();
            }
        }
    }
}
