using System;
using UnityEngine;

namespace Combat.Structs
{
	[Serializable]
	public struct SElementStat
	{
		[SerializeField]
		public float Add;
		
		[SerializeField]
		public float Multiply;
	}
}