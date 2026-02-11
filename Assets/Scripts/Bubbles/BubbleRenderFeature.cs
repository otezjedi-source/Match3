using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Match3.Bubbles
{
    public class BubbleRenderFeature : ScriptableRendererFeature
    {
        // Color Animation
        private static readonly int ColorSpeed = Shader.PropertyToID("_ColorSpeed");
        private static readonly int IridescenceIntensity = Shader.PropertyToID("_IridescenceIntensity");
        private static readonly int RotationSpeed = Shader.PropertyToID("_RotationSpeed");
        
        // Visual Effects
        private static readonly int RimIntensity = Shader.PropertyToID("_RimIntensity");
        private static readonly int RimPulseSpeed = Shader.PropertyToID("_RimPulseSpeed");
        private static readonly int SpecularPower = Shader.PropertyToID("_SpecularPower");
        private static readonly int SpecularIntensity = Shader.PropertyToID("_SpecularIntensity");
        
        // Transparency
        private static readonly int AlphaMultiplier = Shader.PropertyToID("_AlphaMultiplier");
        private static readonly int AlphaCenter = Shader.PropertyToID("_AlphaCenter");
        
        // Size Variation
        private static readonly int SizeMin = Shader.PropertyToID("_SizeMin");
        private static readonly int SizeMax = Shader.PropertyToID("_SizeMax");
        
        // Wobble Animation
        private static readonly int WobbleAmount = Shader.PropertyToID("_WobbleAmount");
        private static readonly int WobbleSpeed = Shader.PropertyToID("_WobbleSpeed");
        
        // Pop Animation
        private static readonly int PopStartThreshold = Shader.PropertyToID("_PopStartThreshold");
        private static readonly int PopBrightness = Shader.PropertyToID("_PopBrightness");
        private static readonly int CrackIntensity = Shader.PropertyToID("_CrackIntensity");
        private static readonly int CrackFrequency = Shader.PropertyToID("_CrackFrequency");
        private static readonly int CrackAnimSpeed = Shader.PropertyToID("_CrackAnimSpeed");
        
        // Lighting
        private static readonly int LightDirection = Shader.PropertyToID("_LightDirection");
        
        private static readonly int BubbleInfoArray = Shader.PropertyToID("_BubbleInfoArray");
        
        [SerializeField] private BubbleRenderSettings settings;
        
        private BubbleRenderPass renderPass;
        
        public override void Create()
        {
            renderPass = new()
            {
                renderPassEvent = settings.renderPassEvent
            };
        }
        
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;

            if (BubbleRenderer.Instance == null || !BubbleRenderer.Instance.IsReady)
                return;

            var bubbleSettings = BubbleRenderer.Instance.Settings;
            if (bubbleSettings == null || bubbleSettings.bubbleMaterial == null || bubbleSettings.bubbleMesh == null)
                return;

            renderer.EnqueuePass(renderPass);
        }
        
        [Serializable]
        public class BubbleRenderSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
        }

        private class BubbleRenderPass : ScriptableRenderPass
        {
            private Matrix4x4[] cachedMatrices;
            private Vector4[] cachedBubbleInfos;
            
            private class PassData
            {
                public Material material;
                public Mesh mesh;
                public Matrix4x4[] matrices;
                public Vector4[] bubbleInfos;
                public int count;
                
                // Shader properties
                public float colorSpeed;
                public float iridescenceIntensity;
                public float rotationSpeed;
                public float rimIntensity;
                public float rimPulseSpeed;
                public float specularPower;
                public float specularIntensity;
                public float alphaMultiplier;
                public float alphaCenter;
                public float sizeMin;
                public float sizeMax;
                public float wobbleAmount;
                public float wobbleSpeed;
                public float popStartThreshold;
                public float popBrightness;
                public float crackIntensity;
                public float crackFrequency;
                public float crackAnimSpeed;
                public Vector3 lightDirection;
            }
            
            private static void ExecutePass(PassData data, RasterGraphContext context)
            {
                if (data.material == null || data.mesh == null)
                    return;

                if (data.matrices == null || data.count <= 0)
                    return;
                
                data.material.SetFloat(ColorSpeed, data.colorSpeed);
                data.material.SetFloat(IridescenceIntensity, data.iridescenceIntensity);
                data.material.SetFloat(RotationSpeed, data.rotationSpeed);
                data.material.SetFloat(RimIntensity, data.rimIntensity);
                data.material.SetFloat(RimPulseSpeed, data.rimPulseSpeed);
                data.material.SetFloat(SpecularPower, data.specularPower);
                data.material.SetFloat(SpecularIntensity, data.specularIntensity);
                data.material.SetFloat(AlphaMultiplier, data.alphaMultiplier);
                data.material.SetFloat(AlphaCenter, data.alphaCenter);
                data.material.SetFloat(SizeMin, data.sizeMin);
                data.material.SetFloat(SizeMax, data.sizeMax);
                data.material.SetFloat(WobbleAmount, data.wobbleAmount);
                data.material.SetFloat(WobbleSpeed, data.wobbleSpeed);
                data.material.SetFloat(PopStartThreshold, data.popStartThreshold);
                data.material.SetFloat(PopBrightness, data.popBrightness);
                data.material.SetFloat(CrackIntensity, data.crackIntensity);
                data.material.SetFloat(CrackFrequency, data.crackFrequency);
                data.material.SetFloat(CrackAnimSpeed, data.crackAnimSpeed);
                data.material.SetVector(LightDirection, data.lightDirection);
                
                if (data.bubbleInfos != null && data.bubbleInfos.Length > 0)
                    data.material.SetVectorArray(BubbleInfoArray, data.bubbleInfos);

                context.cmd.DrawMeshInstanced(data.mesh, 0, data.material, 0, data.matrices, data.count);
            }
            
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var bubble = BubbleRenderer.Instance;
                if (bubble == null || bubble.Count <= 0)
                    return;

                var settings = bubble.Settings;
                int count = bubble.Count;

                if (cachedMatrices == null || cachedMatrices.Length != count)
                    cachedMatrices = new Matrix4x4[count];
                
                if (cachedBubbleInfos == null || cachedBubbleInfos.Length != count)
                    cachedBubbleInfos = new Vector4[count];

                Array.Copy(bubble.Matrices, cachedMatrices, count);
                Array.Copy(bubble.BubbleInfos, cachedBubbleInfos, count);

                var resourceData = frameData.Get<UniversalResourceData>();

                using var builder = renderGraph.AddRasterRenderPass<PassData>("Bubble Draw", out var passData);
                passData.material = bubble.MaterialInstance;
                passData.mesh = settings.bubbleMesh;
                passData.matrices = cachedMatrices;
                passData.bubbleInfos = cachedBubbleInfos;
                passData.count = count;
                
                passData.colorSpeed = settings.colorSpeed;
                passData.iridescenceIntensity = settings.iridescenceIntensity;
                passData.rotationSpeed = settings.rotationSpeed;
                passData.rimIntensity = settings.rimIntensity;
                passData.rimPulseSpeed = settings.rimPulseSpeed;
                passData.specularPower = settings.specularPower;
                passData.specularIntensity = settings.specularIntensity;
                passData.alphaMultiplier = settings.alphaMultiplier;
                passData.alphaCenter = settings.alphaCenter;
                passData.sizeMin = settings.sizeMin;
                passData.sizeMax = settings.sizeMax;
                passData.wobbleAmount = settings.wobbleAmount;
                passData.wobbleSpeed = settings.wobbleSpeed;
                passData.popStartThreshold = settings.popStartThreshold;
                passData.popBrightness = settings.popBrightness;
                passData.crackIntensity = settings.crackIntensity;
                passData.crackFrequency = settings.crackFrequency;
                passData.crackAnimSpeed = settings.crackAnimSpeed;
                passData.lightDirection = settings.lightDirection;

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) => ExecutePass(data, ctx));
            }
        }
    }
}
