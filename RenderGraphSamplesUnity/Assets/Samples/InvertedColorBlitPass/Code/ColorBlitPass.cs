using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class ColorBlitPass : ScriptableRenderPass {
    // Holds the data needed for the render pass.
    class PassData {
        public Material BlitMaterial { get; set; }
        public TextureHandle SourceTexture { get; set; }
    }

    Material m_InvertColorMaterial;
    Material m_BlitMaterial;
    ProfilingSampler m_ProfilingSampler = new("After Opaques");

    // A constructor for Render Pass. When injecting the pass we will make sure to initialize the pass 
    // data and set correctly the render pass event this pass needs to execute.
    public ColorBlitPass(Material invertColorMaterial, Material blitMaterial, RenderPassEvent rpEvent) {
        m_InvertColorMaterial = invertColorMaterial;
        m_BlitMaterial = blitMaterial;
        renderPassEvent = rpEvent;
    }

    // The RecordRenderGraph function will define the Setup and Rendering functions for our render pass.
    // In the Setup we will configure resources such as which textures it reads from and what render textures
    // it writes to. The Rendering delegate will contain the rendering code that will execute in the render
    // graph execution step.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        // Source texture is the active color buffer
        TextureHandle sourceTexture = resourceData.activeColorTexture;
        
        // We will create a temporary destination texture to hold the contents of our blit pass. This
        //texture will match the size and format of the pipeline color buffer.
        RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
        
        // We also don't need the depth buffer
        descriptor.depthBufferBits = 0;

        // The newly created texture will be managed by Render Graph, we don’t have to worry about its lifecycle.
        TextureHandle destinationTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph,
        descriptor, "_TempRT", true);

        // Here we will add a RasterRenderPass that consumes our PassData. This is the first of the two
        // passes we will implement and will Blit from source color buffer to the temporary RT we just created.
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("After Opaque Post-processing pass", out var passData, m_ProfilingSampler)) {
            passData.BlitMaterial = m_InvertColorMaterial;
            passData.SourceTexture = sourceTexture;

            // UseTexture tells the render graph that sourceTexture is going to be read in this pass as input.
            builder.UseTexture(sourceTexture, AccessFlags.Read);

            // UseTextureFragment tells render graph to bind this texture as a framebuffer color attachment at index 0.
            builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);

            // We use a lambda function to define the render function in place so the setup and execute
            // related code to this pass are together. We then call the Blitter.BlitTexture that draws a full screen
            // triangle binding the source texture as a shader resource. The Blitter API will bind this texture as _Blit
            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
        }

        // Now we will add another pass to “resolve” the modified color buffer we have to the pipeline
        // color buffer by doing the reverse blit, from destination to source. Later in this tutorial we will
        // explore some alternatives that we can do to optimize this second blit away and avoid the round trip.
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve", out var passData, m_ProfilingSampler)) {
            passData.BlitMaterial = m_BlitMaterial;

            // Similar to the previous pass, however now we set destination texture as input and source as output.
            passData.SourceTexture = destinationTexture;
            builder.UseTexture(destinationTexture, AccessFlags.Read);
            builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);

            // We use the same BlitTexture API to perform the Blit operation.
            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
        }
    }

    // ExecutePass is the render function for each of the blit render graph recordings. This is good
    // practice to avoid using variables outside of the lambda it is called from.
    // It is static to avoid using member variables which could cause unintended behaviour.
    static void ExecutePass(PassData data, RasterGraphContext rgContext) {
        Blitter.BlitTexture(rgContext.cmd, data.SourceTexture, new Vector4(1, 1, 0, 0), data.BlitMaterial, 0);
    }
}
