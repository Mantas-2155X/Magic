using System;
using Combat.Wearables.Enums;
using UnityEngine;

namespace Combat.Wearables.Structs
{
	[Serializable]
	public struct SWearableContainer
	{
		[SerializeField]
		public EWearableType Type;
		
		[SerializeField]
		public Transform Wear;

		[SerializeField]
		public Transform Drop;
	}
}