using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Match3.Bubbles
{
    public class BubbleRenderer : MonoBehaviour
    {
        private struct BubbleData
        {
            public float3 position;
            public float size;
            public float speed;
            public float wobbleOffset;
            public float age;
            public float lifetime;
            public float colorSeed;
            public float sizeVariation;
        }
        
        [SerializeField] private BubbleSettings settings;
        
        public BubbleSettings Settings => settings;
        public Matrix4x4[] Matrices { get; private set; }
        public Vector4[] BubbleInfos { get; private set; }
        public Material MaterialInstance { get; private set; }

        public int Count => settings != null ? settings.maxBubbles : 0;
        public bool IsReady => bubbles != null && Matrices != null && BubbleInfos != null && MaterialInstance != null;
        
        private BubbleData[] bubbles;
        private int cachedMaxBubbles;

        public static BubbleRenderer Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (settings.bubbleMesh == null)
            {
                Debug.LogError("[BubbleRenderer] BubbleMesh is null]");
                return;
            }

            if (settings.bubbleMaterial == null)
            {
                Debug.LogError("[BubbleRenderer] BubbleMaterial is null");
                return;
            }

            Init();
        }

        private void Init()
        {
            cachedMaxBubbles = settings.maxBubbles;
            
            bubbles = new BubbleData[settings.maxBubbles];
            Matrices = new Matrix4x4[settings.maxBubbles];
            BubbleInfos = new Vector4[settings.maxBubbles];
            
            ClearMaterialInstance();
            MaterialInstance = new(settings.bubbleMaterial);
            
            for (int i = 0; i < Matrices.Length; i++)
                Matrices[i] = Matrix4x4.identity;

            for (int i = 0; i < settings.maxBubbles; i++)
                SpawnBubble(ref bubbles[i]);

            UpdateMatrices();
        }

        private void OnDestroy()
        {
            ClearMaterialInstance();
        }
        
        private void ClearMaterialInstance()
        {
            if (MaterialInstance == null)
                return;
            
            if (Application.isPlaying)
                Destroy(MaterialInstance);
            else
                DestroyImmediate(MaterialInstance);
        }

        private void SpawnBubble(ref BubbleData bubble)
        {
            bubble.position = new(
                Random.Range(settings.spawnRangeX.x, settings.spawnRangeX.y),
                Random.Range(settings.spawnRangeY.x, settings.spawnRangeY.y),
                0
            );
            bubble.size = Random.Range(settings.sizeRange.x, settings.sizeRange.y);
            bubble.speed = Random.Range(settings.speedRange.x, settings.speedRange.y);
            bubble.wobbleOffset = Random.Range(0f, math.PI * 2f);
            bubble.age = 0f;
            bubble.lifetime = Random.Range(settings.lifetimeRange.x, settings.lifetimeRange.y);
            bubble.colorSeed = Random.value;
            bubble.sizeVariation = Random.value;
        }

        private void Update()
        {
            if (bubbles == null)
                return;

            if (settings.maxBubbles != cachedMaxBubbles)
            {
                Init();
                return;
            }

            float dt = Time.deltaTime;
            float time = Time.time;

            for (int i = 0; i < bubbles.Length; i++)
            {
                ref var b = ref bubbles[i];
                
                b.age += dt;
                if (b.age >= b.lifetime)
                {
                    SpawnBubble(ref b);
                    continue;
                }

                b.position.x += math.sin(time * 2f + b.wobbleOffset) * 0.3f * dt;
                b.position.y += b.speed * dt;
            }
            
            UpdateMatrices();
        }

        private void UpdateMatrices()
        {
            for (int i = 0; i < bubbles.Length; i++)
            {
                ref var b = ref bubbles[i];
                
                float normalizedAge = b.age / b.lifetime;
                
                // Pop scale animation (optional - shader also handles size via z component)
                float popScale = 1f;
                if (normalizedAge > settings.popStartThreshold)
                {
                    float popPhase = settings.popStartThreshold;
                    float popProgress = (normalizedAge - popPhase) / (1f - popPhase);
                    popScale = 1f + popProgress * settings.popSizeIncrease;
                }
                
                // Position and scale only - rotation handled by shader
                Matrices[i] = Matrix4x4.TRS( b.position, Quaternion.identity, Vector3.one * (b.size * popScale));
                
                // Pass data to shader: x = age, y = colorSeed, z = size variation
                BubbleInfos[i] = new Vector4(normalizedAge, b.colorSeed, b.sizeVariation, 0);
            }
        }
    }
}