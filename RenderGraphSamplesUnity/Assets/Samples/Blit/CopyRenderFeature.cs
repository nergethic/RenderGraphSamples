using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
 
public class CopyRenderFeature : ScriptableRendererFeature {
    class CopyRenderPass : ScriptableRenderPass {
        // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
        class PassData {
            internal TextureHandle src;
        }
 
        // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        static void ExecutePass(PassData data, RasterGraphContext context) {
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1,1,0,0), 0, false);
        }
     
        // This is where the renderGraph handle can be accessed.
        // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            const string passName = "Copy To Debug Texture";
         
            // This simple pass copies the active color texture to a new texture. This sample is for API demonstrative purposes,
            // so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.
 
            // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData)) {
                // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
                // The active color and depth textures are the main color and depth buffers that the camera renders into
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
             
                // Fill up the passData with the data needed by the pass
             
                // Get the active color texture through the frame data, and set it as the source texture for the blit
                passData.src = resourceData.activeColorTexture;
             
                // The destination texture is created here,
                // the texture is created with the same dimensions as the active color texture, but with no depth buffer, being a copy of the color texture
                // we also disable MSAA as we don't need multisampled textures for this sample
             
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
             
                TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "CopyTexture", false);
             
                // We declare the src texture as an input dependency to this pass, via UseTexture()
                builder.UseTexture(passData.src);
 
                // Setup as a render target via SetRenderAttachment, which is the equivalent of using the old cmd.SetRenderTarget
                builder.SetRenderAttachment(destination, 0);
             
                // We disable culling for this pass for the demonstrative purpose of this sampe, as normally this pass would be culled,
                // since the destination texture is not used anywhere else
                builder.AllowPassCulling(false);
 
                // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
 
    CopyRenderPass m_CopyRenderPass;
 
    /// <inheritdoc/>
    public override void Create() {
        m_CopyRenderPass = new CopyRenderPass {
            // Configures where the render pass should be injected.
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
    }
 
    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(m_CopyRenderPass);
    }
}