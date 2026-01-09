using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace Rendering
{
    public class OutlineRendererFeature : ScriptableRendererFeature
    {
        
        [System.Serializable]
        public class Settings
        {
            public LayerMask outlineLayer = 0;
            public Material maskMaterial; // Hidden/Outline/Mask
            public Material compositeMaterial; // Hidden/Outline/Composite
            public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public Settings settings = new Settings();

        class OutlinePass : ScriptableRenderPass
        {
            private readonly Settings s;
            private FilteringSettings filtering;
            private readonly ShaderTagId shaderTag = new ShaderTagId("UniversalForward");

            private RTHandle maskRT;
            private RTHandle colorRT;

            public OutlinePass(Settings settings)
            {
                s = settings;
                filtering = new FilteringSettings(RenderQueueRange.all, s.outlineLayer);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                desc.colorFormat = RenderTextureFormat.R8;

                RenderingUtils.ReAllocateIfNeeded(ref maskRT, desc, FilterMode.Point, TextureWrapMode.Clamp,
                    name: "_OutlineMaskRT");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (s.maskMaterial == null || s.compositeMaterial == null) return;

                var cmd = CommandBufferPool.Get("OutlinePass");

                // 1) Рендерим маску
                CoreUtils.SetRenderTarget(cmd, maskRT, ClearFlag.All, Color.black);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawing = CreateDrawingSettings(shaderTag, ref renderingData, SortingCriteria.CommonTransparent);
                drawing.overrideMaterial = s.maskMaterial;

                var filteringSettings = filtering;
                context.DrawRenderers(renderingData.cullResults, ref drawing, ref filteringSettings);

                // 2) Композитим контур поверх картинки
                // Берём текущий color target
                colorRT = renderingData.cameraData.renderer.cameraColorTargetHandle;

                s.compositeMaterial.SetTexture("_OutlineMaskTex", maskRT);

                Blitter.BlitCameraTexture(cmd, colorRT, colorRT, s.compositeMaterial, 0);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                // RTHandle живёт между кадрами, Unity сам чистит при dispose feature
            }
        }

        OutlinePass pass;

        public override void Create()
        {
            pass = new OutlinePass(settings)
            {
                renderPassEvent = settings.passEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.maskMaterial == null || settings.compositeMaterial == null) return;
            renderer.EnqueuePass(pass);
        }
    }
}