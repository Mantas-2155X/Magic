using System.Collections.Generic;
using AI.Interfaces;
using AYellowpaper.SerializedCollections;
using Managers;
using Objects.Enums;
using UnityEngine;

namespace Objects.Base
{
	public class BaseCharger : BaseObject
	{
		[field: SerializeField]
		public Collider[] Triggers { get; private set; }

		[field: SerializeField]
		public Renderer Renderer { get; private set; }

		[field: SerializeField]
		public float PullForce { get; private set; }
		
		[field: SerializeField]
		public EChargeType Type { get; private set; }

		[field: SerializeField]
		public bool Charged { get; private set; }
		
		[field: SerializeField]
		public bool Activated { get; private set; }
		
		[field: SerializeField]
		public SerializedDictionary<EChargeType, Material> Elements { get; private set; }
		
		private readonly List<IAlive> alives = new ();
		
		private Material activatedMaterial;
		private Color activatedColor;
		
		private Material chargeMaterial;
		private Color chargeColor;

		#region MonoBehaviour

		public override void Awake()
		{
			base.Awake();
			
			setType(Type);
			setActivated(Activated);
			setCharged(Charged);
		}
		
		public void FixedUpdate()
		{
			if (!Activated)
				return;
			
			var pos = transform.position + transform.forward;
			
			for (var i = 0; i < alives.Count; i++)
			{
				var alive = alives[i];
				if (alive == null || !alive.IsAlive)
					continue;

				var rb = alive.Body.Rigidbody;
				var dir = (pos - rb.position).normalized;
				
				rb.AddForce(dir * PullForce, ForceMode.VelocityChange);
			}
		}

		public override void OnDisable()
		{
			base.OnDisable();
			alives.Clear();
		}
		
		public override void OnTriggerEnter(Collider other)
		{
			base.OnTriggerEnter(other);
			
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive) || alives.Contains(alive))
				return;

			for (var i = 0; i < Triggers.Length; i++)
			{
				if (Triggers[i].bounds.Intersects(other.bounds))
					continue;

				return;
			}
			
			alives.Add(alive);
		}
		
		public virtual void OnTriggerExit(Collider other)
		{
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
				return;

			alives.Remove(alive);
		}
		
		#endregion

		#region Charger

		

		#endregion
		
		#region Internals

		private void setType(EChargeType type)
		{
			Type = type;
			
			var mats = Renderer.materials;
			mats[1] = Elements[type];
			Renderer.materials = mats;
		}

		private void setActivated(bool activated)
		{
			Activated = activated;

			if (activatedMaterial == null)
			{
				var mats = Renderer.materials;
				activatedMaterial = mats[2];
				activatedColor = activatedMaterial.color;
				Renderer.materials = mats;
			}
			
			if (activated)
			{
				activatedMaterial.color = activatedColor;
				activatedMaterial.EnableKeyword("_EMISSION");
			}
			else
			{
				activatedMaterial.color = Color.black;
				activatedMaterial.DisableKeyword("_EMISSION");
			}
		}

		private void setCharged(bool charged)
		{
			Charged = charged;

			if (chargeMaterial == null)
			{
				var mats = Renderer.materials;
				chargeMaterial = mats[3];
				chargeColor = chargeMaterial.color;
				Renderer.materials = mats;
			}
			
			if (charged)
			{
				chargeMaterial.color = chargeColor;
				chargeMaterial.EnableKeyword("_EMISSION");
			}
			else
			{
				chargeMaterial.color = Color.black;
				chargeMaterial.DisableKeyword("_EMISSION");
			}
		}
		
		#endregion
	}
}