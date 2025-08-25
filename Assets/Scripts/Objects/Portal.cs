using Audio;
using Objects.Base;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Objects
{
	public class Portal : BaseObject
	{
		[SerializeField]
		public ParticleSystem System;

		#region Identify / SaveLoad

		public override bool ShouldSave => false;

		#endregion
		
		public override void Spawn(Vector3 position, Vector3 angles)
		{
			base.Spawn(position, angles);
			System.Play(true);
			
			AudioManager.Instance.PlayAtPoint(Addressables.LoadAssetAsync<AudioData>("Audio/Portal.asset").WaitForCompletion(), GetTransform().position);
		}
	}
}