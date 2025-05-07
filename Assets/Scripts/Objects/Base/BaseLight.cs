using System;
using System.Collections.Generic;
using Managers;
using Newtonsoft.Json.Linq;
using Objects.Events;
using State.States;
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

		[SerializeField]
		public List<ReflectionProbe> UpdateProbes;
		
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

		public override Dictionary<string, JObject> Save()
		{
			var dict = base.Save();
			
			var lightState = BaseLightState.Read(this);
			if (lightState != null)
				dict[typeof(BaseLight).ToString()] = JObject.FromObject(lightState);
			
			return dict;
		}

		public override void Load(Dictionary<string, JObject> data)
		{
			base.Load(data);
			
			if (data.TryGetValue(typeof(BaseLight).ToString(), out var baseLightState))
				BaseLightState.Apply(this, baseLightState.ToObject<BaseLightState>());
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

			if (UpdateProbes == null)
				return;

			for (var i = 0; i < UpdateProbes.Count; i++)
				ProbeManager.Instance.UpdateProbe(UpdateProbes[i]);
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