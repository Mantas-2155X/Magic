using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Managers
{
	public class RenderManager : MonoBehaviour
	{
		public static RenderManager Instance;

		private FullScreenPassRendererFeature invertFeature;
		public FullScreenPassRendererFeature InvertFeature
		{
			get
			{
				if (invertFeature != null)
					return invertFeature;
				
				var rend = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).GetRenderer(0);
				if (rend == null)
				{
					Debug.LogError("[RenderManager] Failed to get UniversalRenderPipelineAsset");
					return null;
				}
			
				var field = rend.GetType().GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
				if (field == null)
				{
					Debug.LogError("[RenderManager] Failed to get rendererFeatures field");
					return null;
				}
			
				var features = (List<ScriptableRendererFeature>)field.GetValue(rend);
				if (features == null)
				{
					Debug.LogError("[RenderManager] Failed to get rendererFeatures value");
					return null;
				}

				foreach (var feature in features)
				{
					if (feature.name != "Invert Colors")
						continue;

					invertFeature = (FullScreenPassRendererFeature)feature;
					return invertFeature;
				}

				Debug.LogError("[RenderManager] Invert Colors feature not found");
				return null;
			}
		}
		
		private Material invertMaterial;
		public Material InvertMaterial
		{
			get
			{
				if (invertMaterial != null)
					return invertMaterial;
		
				var feature = InvertFeature;
				if (feature == null)
					return null;

				invertMaterial = feature.passMaterial;
				return invertMaterial;
			}
		}
		
		private static readonly int invertIntensity = Shader.PropertyToID("_Intensity");
		
		public void Awake()
		{
			Instance = this;
		}

		public void OnDisable()
		{
			InvertColors(0f);
		}

		public void InvertColors(float value)
		{
			if (InvertFeature != null)
				InvertFeature.SetActive(value != 0f);
			
			if (InvertMaterial != null)
				InvertMaterial.SetFloat(invertIntensity, value);
		}
	}
}