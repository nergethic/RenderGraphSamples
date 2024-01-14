using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ColorInvertFeature : ScriptableRendererFeature {
    public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
    public Material InvertColorMaterial;
    public Material BlitColorMaterial;

    ColorBlitPass pass;

    public override void Create() {
        if (InvertColorMaterial == null || BlitColorMaterial == null) {
            Debug.Log("One or more materials are null. The pass won't be created and injected.");
            return;
        }

        pass = new ColorBlitPass(InvertColorMaterial, BlitColorMaterial, injectionPoint);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(pass);
    }
}
