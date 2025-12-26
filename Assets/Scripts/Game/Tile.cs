using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Match3.Core;
using Spine;
using Spine.Unity;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Match3.Game
{
    public class Tile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private SkeletonAnimation clearAnimation;

        private AsyncOperationHandle<Sprite> spriteHandle;

        public void Init(GameConfig.TileData tileData)
        {
            sprite.gameObject.SetActive(false);
            clearAnimation.gameObject.SetActive(false);

            clearAnimation.skeletonDataAsset = tileData.clearAnim;

            spriteHandle = Addressables.LoadAssetAsync<Sprite>(tileData.spriteRef);
            spriteHandle.Completed += handle =>
            {
                sprite.sprite = handle.Result;
                sprite.gameObject.SetActive(true);
            };
        }

        public void Clear()
        {
            if (spriteHandle.IsValid())
                Addressables.Release(spriteHandle);
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
