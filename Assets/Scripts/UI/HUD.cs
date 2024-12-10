using AI;
using AI.Base;
using AI.Interfaces;
using Managers;
using Objects;
using Objects.Interfaces;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class HUD : MonoBehaviour
	{
		[SerializeField]
		public TMP_Text LookTarget;
		
		[SerializeField]
		public Image Crosshair;

		[SerializeField]
		public float LookTargetDistance = 2f;
		
		public void Awake()
		{
			gameObject.SetActive(false);
			
			BaseAlive.OnDeathEvent.AddListener(OnDeath);
			BaseAlive.OnSpawnEvent.AddListener(OnSpawn);
		}

		public void Update()
		{
			if (LookTarget.text != "")
				LookTarget.text = "";
			
			var player = AIManager.Instance.Player;
			if (player == null)
				return;

			Crosshair.color = player.IsGrounded() ? Color.white : Color.red;
			
			if (Physics.Raycast(player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out var hit, LookTargetDistance, ~LayerMaskTools.Mask1))
			{
				var pickupable = hit.collider.GetComponent<IPickupable>();
				if (pickupable == null)
					return;

				switch (pickupable)
				{
					case DroppedWeapon weapon:
						LookTarget.text = weapon.Weapon.GetType().Name;
						break;
					default:
						LookTarget.text = pickupable.GetGameObject().name;
						break;
				}
			}
		}
		
		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not Player)
				return;
			
			gameObject.SetActive(false);
		}
		
		public void OnSpawn(IAlive alive)
		{
			if (alive is not Player)
				return;

			gameObject.SetActive(true);
		}
	}
}