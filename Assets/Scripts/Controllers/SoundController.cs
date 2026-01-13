using Match3.Core;
using UnityEngine;
using VContainer;

namespace Match3.Controllers
{
    public class SoundController
    {
        [Inject] private readonly GameConfig gameConfig;
        [Inject] private readonly AudioSource audioSource;

        public void PlayBtnClick()
        {
            audioSource.PlayOneShot(gameConfig.ButtonClickSound);
        }

        public void PlaySwap()
        {
            audioSource.PlayOneShot(gameConfig.SwapSound);
        }

        public void PlayMatch()
        {
            audioSource.PlayOneShot(gameConfig.MatchSound);
        }

        public void PlayDrop()
        {
            audioSource.PlayOneShot(gameConfig.DropSound);
        }
    }
}
