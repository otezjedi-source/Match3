using System.Collections.Generic;
using Match3.Core;
using Match3.ECS.Components;
using UnityEngine;
using VContainer;

namespace Match3.Controllers
{
    /// <summary>
    /// Simple audio playback controller. Plays one-shot sound effects.
    /// Called by ECS systems via SoundSyncSystem and UI via direct calls.
    /// </summary>
    public class SoundController
    {
        private AudioSource audioSource;

        private readonly Dictionary<SoundType, AudioClip> sounds = new();

        [Inject]
        public SoundController(GameConfig gameConfig, AudioSource audioSource)
        {
            this.audioSource = audioSource;

            foreach (var sound in gameConfig.SoundsData)
                sounds.Add(sound.type, sound.sound);
        }

        public void Play(SoundType type)
        {
            if (sounds.TryGetValue(type, out var sound))
                audioSource.PlayOneShot(sound);
        }
    }
}
