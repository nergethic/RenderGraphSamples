using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class InjectColorInvertPass : MonoBehaviour {
    // We will need two materials, one for inverting the color buffer contents, and the other for a
    // regular point sampling to resolve the color buffer to camera color buffer. These properties are
    // serializable, so we will create material in our project and assign them to this Monobehaviour.
    public Material InvertColorMaterial;
    public Material BlitColorMaterial;
    private ColorBlitPass m_ColorBlitPass = null;

    // We create the render pass and register to the RenderPipelineManager.beginCameraRendering
    // callback. This will cause our InjectPass to be called for each camera in the frame.
    void OnEnable() {
        CreateRenderPass();
        RenderPipelineManager.beginCameraRendering += InjectPass;
    }

    // Deregisters the callback
    void OnDisable() {
        RenderPipelineManager.beginCameraRendering -= InjectPass;
    }

    void CreateRenderPass() {
        if (InvertColorMaterial == null || BlitColorMaterial == null) {
            Debug.Log("One or more materials are null. The pass won't be created and injected.");
            return;
        }

        // Creates the render pass and tells it to be injects at AfterRenderingSkybox events.
        m_ColorBlitPass = new ColorBlitPass(InvertColorMaterial, BlitColorMaterial, RenderPassEvent.AfterRenderingSkybox);
    }

    // This is our actual delegate to inject the pass. This will be called for all cameras including the scene view camera, previews and skybox.
    void InjectPass(ScriptableRenderContext renderContext, Camera currCamera) {
        if (m_ColorBlitPass == null) {
            CreateRenderPass();
        }

        // we only want to execute our pass for game and VR cameras. Otherwise we would end up inverting
        // colors in the scene view and that would be undesirable.
        if (currCamera.cameraType == CameraType.Game || currCamera.cameraType == CameraType.VR) {
            // Finally we enqueue the color blit pass in URP, by accessing the camera renderer and calling EnqueuePass
            var data = currCamera.GetUniversalAdditionalCameraData();
            data.scriptableRenderer.EnqueuePass(m_ColorBlitPass);
        }
    }
}
