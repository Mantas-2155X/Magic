using UnityEngine;

namespace Managers
{
	public class RenderManager : MonoBehaviour
	{
		public static RenderManager Instance;

		[SerializeField]
		public FullScreenPassRendererFeature InvertFeature;
		
		[SerializeField]
		public Material InvertMaterial;
		
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
			InvertMaterial.SetFloat(invertIntensity, value);
			InvertFeature.SetActive(value != 0f);
		}
	}
}