using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BlurredBufferMultiObjectOutlineRendererFeature : ScriptableRendererFeature
{
    [SerializeField]
    private RenderPassEvent RenderEvent = RenderPassEvent.AfterRenderingTransparents;
    
    [Space, SerializeField]
    public Material DilationMaterial;
    
    [SerializeField]
    public Material OutlineMaterial;
    
    [SerializeField, Range(1, 60)]
    public int Spread = 10;
    
    private readonly List<Renderer> renderersList = new ();

    private BlurredBufferMultiObjectOutlinePass outlinePass;
    private Renderer[] targetRenderers = Array.Empty<Renderer>();

    private static readonly int spreadId = Shader.PropertyToID("_Spread");

    public void AddRenderers(Renderer[] renderers)
    {
        for (var i = renderers.Length - 1; i >= 0; i--)
            renderersList.Remove(renderers[i]);
        
        renderersList.AddRange(renderers);
        updateRenderers();
    }

    public void RemoveRenderers(Renderer[] renderers)
    {
        for (var i = renderers.Length - 1; i >= 0; i--)
            renderersList.Remove(renderers[i]);
        
        updateRenderers();
    }

    public void ClearRenderers()
    {
        renderersList.Clear();
        updateRenderers();
    }

    public override void Create()
    {
        name = "Multi-Object Outliner";
        outlinePass = new BlurredBufferMultiObjectOutlinePass();
        
        outlinePass.RenderEvent = RenderEvent;
        
        outlinePass.OutlineMaterial = OutlineMaterial;
        outlinePass.DilationMaterial = DilationMaterial;
        
        DilationMaterial.SetInteger(spreadId, Spread);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (outlinePass == null || targetRenderers.Length == 0)
            return;

        outlinePass.Renderers = targetRenderers;
        renderer.EnqueuePass(outlinePass);
    }

    private void updateRenderers()
    {
        targetRenderers = renderersList.ToArray();
        
        if (outlinePass != null)
            outlinePass.Renderers = targetRenderers;
    }
}