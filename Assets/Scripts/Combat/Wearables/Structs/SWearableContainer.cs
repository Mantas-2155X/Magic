using System;
using UnityEngine;

namespace Combat.Wearables.Structs
{
	[Serializable]
	public struct SWearableContainer
	{
		[SerializeField]
		public Transform Wear;

		[SerializeField]
		public Transform Drop;
	}
}