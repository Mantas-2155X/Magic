using System;
using UnityEngine;

namespace ScriptableObjects.Structs
{
	[Serializable]
	public struct SSlow
	{
		[SerializeField]
		public float Amount;
		
		[SerializeField]
		public float Duration;
	}
}