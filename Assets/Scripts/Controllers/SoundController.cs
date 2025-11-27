using MiniIT.CORE;
using UnityEngine;
using VContainer;

namespace MiniIT.CONTROLLERS
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
