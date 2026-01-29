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
        private readonly AudioSource audioSource;

        private readonly Dictionary<SoundType, AudioClip> sounds = new();
        private readonly Dictionary<BonusType, AudioClip> bonusSounds = new();

        [Inject]
        public SoundController(GameConfig gameConfig, AudioSource audioSource)
        {
            this.audioSource = audioSource;

            foreach (var sound in gameConfig.soundsData)
                sounds.Add(sound.type, sound.sound);

            foreach (var bonus in gameConfig.bonusesData)
                bonusSounds.Add(bonus.type, bonus.sound);
        }

        /// <summary>
        /// Plays specific ingame sound.
        /// </summary>
        /// <param name="type">SoundType to play</param>
        public void Play(SoundType type)
        {
            if (sounds.TryGetValue(type, out var sound))
                audioSource.PlayOneShot(sound);
        }

        /// <summary>
        /// Plays specific bonus sound.
        /// </summary>
        /// <param name="type">BonusType to play</param>
        public void PlayBonus(BonusType type)
        {
            if (bonusSounds.TryGetValue(type, out var sound))
                audioSource.PlayOneShot(sound);
        }
    }
}
