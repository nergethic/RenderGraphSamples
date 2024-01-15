using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// This is an example CopyRenderFeature.cs that records a rendering command to copy, or blit
// the contents of the source texture to the color render target of the render pass

public class CopyRenderFeature2 : ScriptableRendererFeature {
    // Render pass that copies the camera’s active color texture to a destination texture.
    // To simplify the code, this sample does not use the destination texture elsewhere in the frame. You can use the frame debugger to inspect its contents.
    class CopyRenderPass : ScriptableRenderPass {
        // This class stores the data that the render pass needs. The RecordRenderGraph method populates the data and the render graph passes it as a parameter to the rendering function.
        class PassData {
            internal TextureHandle copySourceTexture;
        }
 
        // Rendering function that generates the rendering commands for the render pass.
        // The RecordRenderGraph method instructs the render graph to use it with the SetRenderFunc method.
        static void ExecutePass(PassData data, RasterGraphContext context) {
            // Records a rendering command to copy, or blit, the contents of the source texture to the color render target of the render pass.
            // The RecordRenderGraph method sets the destination texture as the render target with the UseTextureFragment method.
            Blitter.BlitTexture(context.cmd, data.copySourceTexture, new Vector4(1, 1, 0, 0), 0, false);
        }
 
        // This method adds and configures one or more render passes in the render graph.
        // This process includes declaring their inputs and outputs, but does not include adding commands to command buffers.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            string passName = "Copy To Debug Texture";
 
            // Add a raster render pass to the render graph. The PassData type parameter determines the type of the passData out variable
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData)) {
                // UniversalResourceData contains all the texture handles used by URP, including the active color and depth textures of the camera
 
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
 
                // Populate passData with the data needed by the rendering function of the render pass
 
                // Use the camera’s active color texture as the source texture for the copy
                passData.copySourceTexture = resourceData.activeColorTexture;
 
                // Create a destination texture for the copy based on the settings, such as dimensions, of the textures that the camera uses.
                // Set msaaSamples to 1 to get a non-multisampled destination texture.
                // Set depthBufferBits to 0 to ensure that the CreateRenderGraphTexture method creates a color texture and not a depth texture.
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
 
                // For demonstrative purposes, this sample creates a transient, or temporary, destination texture.
                // UniversalRenderer.CreateRenderGraphTexture is a helper method that calls the RenderGraph.CreateTexture method.
                // It simplifies your code when you have a RenderTextureDescriptor instance instead of a TextureDesc instance.
                TextureHandle destination =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "CopyTexture", false);
 
                // Declare that this render pass uses the source texture as a read-only input
                builder.UseTexture(passData.copySourceTexture);
 
                // Declare that this render pass uses the temporary destination texture as its color render target.
                // This is similar to cmd.SetRenderTarget prior to the RenderGraph API.
                builder.SetRenderAttachment(destination, 0);
 
                // RenderGraph automatically determines that it can remove this render pass because its results, which are stored in the temporary destination texture, are not used by other passes.
                // For demonstrative purposes, this sample turns off this behavior to make sure that RenderGraph executes the render pass.
                builder.AllowPassCulling(false);
 
                // Set the ExecutePass method as the rendering function that RenderGraph calls for the render pass.
                // This sample uses a lambda expression to avoid memory allocations.
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
 
    CopyRenderPass m_CopyRenderPass;
 
    public override void Create() {
        m_CopyRenderPass = new CopyRenderPass();
 
        // Configure the injection point in which URP runs the pass
        m_CopyRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }
 
    // URP calls this method every frame, once for each Camera. This method lets you inject ScriptableRenderPass instances into the scriptable Renderer.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(m_CopyRenderPass);
    }
}
