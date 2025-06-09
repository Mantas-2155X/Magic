using System;
using System.Collections.Generic;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Events;
using State.Interfaces;
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
		
		#region Identify / SaveLoad
		
		public override Dictionary<string, JObject> Save()
		{
			var dict = base.Save();
			dict[typeof(BaseLight).ToString()] = JObject.FromObject(new BaseLightState(this));
			
			return dict;
		}

		public override void Load(Dictionary<string, JObject> data)
		{
			base.Load(data);
			
			if (data.TryGetValue(typeof(BaseLight).ToString(), out var baseLightState) && baseLightState != null)
				baseLightState.ToObject<BaseLightState>().Apply(this);
		}
		
		#endregion
		
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

			var previousColor = Gizmos.color;
			Gizmos.color = Color.yellow;
			
			for (var i = 0; i < UpdateProbes.Count; i++)
			{
				var updateProbe = UpdateProbes[i];
				if (updateProbe == null)
					continue;
				
				Gizmos.DrawLine(transform.position, updateProbe.transform.position);
			}
			
			Gizmos.color = previousColor;
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
		
		[JsonObject]
		public class BaseLightState : IState
		{
			[JsonProperty]
			public bool Enabled;

			public BaseLightState() { }
			
			public BaseLightState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not BaseLight baseLight)
					return;

				Enabled = baseLight.Enabled;
			}
			
			public void Apply(object obj)
			{
				if (obj is not BaseLight baseLight)
					return;

				baseLight.Toggle(Enabled);
			}
		}
	}
}