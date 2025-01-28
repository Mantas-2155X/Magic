using System.Collections.Generic;
using System.Reflection;
using Tools;
using Unified.UniversalBlur.Runtime;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Managers
{
	public class RenderManager
	{
		private static RenderManager instance;
		public static RenderManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new RenderManager();
				return instance;
			}
		}

		private List<string> resolutions;
		public List<string> Resolutions
		{
			get
			{
				if (resolutions != null)
					return resolutions;

				var list = new List<string>();

				var screenResolutions = Screen.resolutions;
				for (var i = 0; i < Screen.resolutions.Length; i++)
				{
					var screenResolution = screenResolutions[i];
					list.AddUnique($"{screenResolution.width}x{screenResolution.height}");
				}

				resolutions = list;
				return resolutions;
			}
		}
		
		private UniversalRenderPipelineAsset renderAsset;
		public UniversalRenderPipelineAsset RenderAsset
		{
			get
			{
				if (renderAsset != null)
					return renderAsset;

				var asset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
				if (asset == null)
				{
					Debug.LogError("[RenderManager] Failed to get UniversalRenderPipelineAsset");
					return null;
				}

				renderAsset = asset;
				return renderAsset;
			}
		}

		private List<ScriptableRendererFeature> renderFeatures;
		public List<ScriptableRendererFeature> RenderFeatures
		{
			get
			{
				if (renderFeatures != null)
					return renderFeatures;

				var asset = RenderAsset;
				if (asset == null)
					return null;

				var rend = asset.GetRenderer(0);
				if (rend == null)
				{
					Debug.LogError("[RenderManager] Failed to get Renderer");
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

				renderFeatures = features;
				return renderFeatures;
			}
		}

		private FullScreenPassRendererFeature invertFeature;
		public FullScreenPassRendererFeature InvertFeature
		{
			get
			{
				if (invertFeature != null)
					return invertFeature;
				
				var features = RenderFeatures;
				if (features == null)
					return null;

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
		
		private ScreenSpaceAmbientOcclusion ssaoFeature;
		public ScreenSpaceAmbientOcclusion SsaoFeature
		{
			get
			{
				if (ssaoFeature != null)
					return ssaoFeature;
				
				var features = RenderFeatures;
				if (features == null)
					return null;

				foreach (var feature in features)
				{
					if (feature.name != "ScreenSpaceAmbientOcclusion")
						continue;

					ssaoFeature = (ScreenSpaceAmbientOcclusion)feature;
					return ssaoFeature;
				}

				Debug.LogError("[RenderManager] SSAO feature not found");
				return null;
			}
		}
		
		private UniversalBlurFeature blurFeature;
		public UniversalBlurFeature BlurFeature
		{
			get
			{
				if (blurFeature != null)
					return blurFeature;
				
				var features = RenderFeatures;
				if (features == null)
					return null;

				foreach (var feature in features)
				{
					if (feature.name != "UniversalBlurFeature")
						continue;

					blurFeature = (UniversalBlurFeature)feature;
					return blurFeature;
				}

				Debug.LogError("[RenderManager] Blur feature not found");
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

		public void InvertColors(float value)
		{
			if (InvertFeature != null)
				InvertFeature.SetActive(value != 0f);
			
			if (InvertMaterial != null)
				InvertMaterial.SetFloat(invertIntensity, value);
		}
	}
}