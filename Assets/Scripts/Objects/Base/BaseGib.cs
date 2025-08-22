using System;
using AI.Interfaces;
using Managers;
using Newtonsoft.Json.Linq;
using Objects.Enums;
using State;
using State.Interfaces;
using UnityEngine;

namespace Objects.Base
{
	public class BaseGib : BaseObject
	{
		[SerializeField]
		public EGibType Type;
		
		[SerializeField]
		public float Amount;
		
		public override void OnDestroy()
		{
			base.OnDestroy();
			
			if (ObjectData.Name != "OBJECT_CORE_NAME")
				return;

			var rend = GetComponent<Renderer>();
			if (rend == null) 
				return;

			var mats = rend.sharedMaterials;
			for (var i = mats.Length - 1; i >= 0; i--)
			{
				var mat = mats[i];
				if (!mat.name.EndsWith("(Instance)"))
					continue;

				Destroy(mat);
			}
		}

		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			switch (Type)
			{
				case EGibType.Health:
					user.RestoreHealth(Amount, this);
					break;
				case EGibType.Mana:
					user.RestoreMana(Amount, this);
					break;
				case EGibType.Energy:
					user.RestoreEnergy(Amount, this);
					break;
				default:
					throw new NotImplementedException();
			}
			
			return true;
		}
	}
}