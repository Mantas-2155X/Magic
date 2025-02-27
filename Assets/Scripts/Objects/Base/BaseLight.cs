using Objects.Events;
using Tools;
using UnityEngine;

namespace Objects.Base
{
	public class BaseLight : BaseObject
	{
		[SerializeField]
		public Light Light;

		[SerializeField]
		public bool Enabled;
		
		[SerializeField]
		public Renderer Renderer;
		
		[SerializeField]
		public int MaterialIndex;

		[SerializeField]
		public Color LightColor = Color.white;

		[SerializeField]
		[ColorUsage(false, true)]
		public Color EmissionColor = Color.white * 1.25f;
		
		[SerializeField]
		public OnLightEnabledEvent OnLightEnabledEvent = new ();

		[SerializeField]
		public OnLightDisabledEvent OnLightDisabledEvent = new ();

		private static readonly int emissionColor = Shader.PropertyToID("_EmissionColor");

		private Material material;
		
		public override void Awake()
		{
			base.Awake();
		
			var materials = Renderer.materials;
			material = materials[MaterialIndex];
			Renderer.materials = materials;

			Light.color = LightColor;
			material.SetColor(emissionColor, EmissionColor);
			
			setEnabled();
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnLightEnabledEvent, Color.blue);
			EventTools.DrawListeners(transform, OnLightDisabledEvent, Color.cyan);
		}
#endif
		
		public override void Break(object source)
		{
			Disable();
			base.Break(source);
		}

		#region Light

		public void Enable()
		{
			Toggle(true);
		}
		public void Disable()
		{
			Toggle(false);
		}

		public void Toggle()
		{
			if (Enabled)
				Disable();
			else
				Enable();
		}
		
		public void Toggle(bool state)
		{
			if (Light.enabled == state)
				return;

			Enabled = state;
			
			if (state)
				OnLightEnabledEvent?.Invoke();
			else
				OnLightDisabledEvent?.Invoke();
			
			setEnabled();
		}

		private void setEnabled()
		{
			if (Enabled)
			{
				Light.enabled = true;
				material.EnableKeyword("_EMISSION");
			}
			else
			{
				Light.enabled = false;
				material.DisableKeyword("_EMISSION");
			}
		}

		#endregion
	}
}