using Match3.Core;
using UnityEngine;
using VContainer;

namespace Match3.Controllers
{
    public class SoundController
    {
        [Inject] private readonly GameConfig config;
        [Inject] private readonly AudioSource audioSource;

        public void PlayBtnClick()
        {
            audioSource.PlayOneShot(config.ButtonClickSound);
        }

        public void PlayMatch()
        {
            audioSource.PlayOneShot(config.MatchSound);
        }

        public void PlayDrop()
        {
            audioSource.PlayOneShot(config.DropSound);
        }
    }
}
