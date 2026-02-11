using UnityEngine;

namespace Match3.Bubbles
{
    /// <summary>
    /// Bubbles rendering configuration. Edit in Unity Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "BubblesSettings", menuName = "Match3/BubblesSettings")]
    public class BubbleSettings : ScriptableObject
    {
        [Header("References")]
        public Material bubbleMaterial;
        public Mesh bubbleMesh;
        
        [Header("Spawn")]
        public int maxBubbles = 50;
        public Vector2 spawnRangeX = new(-3f, 3f);
        public Vector2 spawnRangeY = new(-6f, 6f);
        public Vector2 sizeRange = new(0.3f, 0.8f);
        public Vector2 speedRange = new(0.5f, 1.5f);
        
        [Header("Lifetime & Pop")]
        public Vector2 lifetimeRange = new(2.5f, 3.5f);
        [Range(0f, 1f)] public float popStartThreshold = 0.8f;
        [Range(0f, 1f)] public float popSizeIncrease = 0.3f;
        
        [Header("Color Animation")]
        [Range(0f, 5f)] public float colorSpeed = 1.0f;
        [Range(0f, 3f)] public float iridescenceIntensity = 1.5f;
        [Range(0f, 2f)] public float rotationSpeed = 0.3f;
        
        [Header("Visual Effects")]
        [Range(0f, 2f)] public float rimIntensity = 0.8f;
        [Range(0f, 5f)] public float rimPulseSpeed = 2.0f;
        [Range(8f, 128f)] public float specularPower = 32.0f;
        [Range(0f, 5f)] public float specularIntensity = 2.0f;
        
        [Header("Transparency")]
        [Range(0f, 3f)] public float alphaMultiplier = 1.2f;
        [Range(0f, 1f)] public float alphaCenter = 0.2f;
        
        [Header("Size Variation")]
        [Range(0.5f, 1f)] public float sizeMin = 0.7f;
        [Range(1f, 2f)] public float sizeMax = 1.3f;
        
        [Header("Wobble Animation")]
        [Range(0f, 0.2f)] public float wobbleAmount = 0.05f;
        [Range(0f, 5f)] public float wobbleSpeed = 2.0f;
        
        [Header("Pop Animation")]
        [Range(0f, 5f)] public float popBrightness = 3.0f;
        [Range(0f, 1f)] public float crackIntensity = 0.6f;
        [Range(10f, 50f)] public float crackFrequency = 25.0f;
        [Range(5f, 30f)] public float crackAnimSpeed = 12.0f;
        
        [Header("Lighting")]
        public Vector3 lightDirection = new(0.5f, 1f, 0.5f);
    }    
}
