using System;
using Combat.Enums;
using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Scenes
{
	public class World4 : MonoBehaviour
	{
		[SerializeField]
		public float AttackEvery = 1f;

		[SerializeField]
		public float DamageEvery = 0.25f;

		[SerializeField]
		public float DamageAmount = 3f;

		[SerializeField]
		public float MinimumVelocity = 2f;
		
		[SerializeField]
		public float DivideBy = 1.002f;

		[SerializeField]
		public Vector2 RangeX = new (-8.27f, 8.27f);

		[SerializeField]
		public Vector2 RangeZ = new (-8.27f, 8.27f);

		[SerializeField]
		public float Y = -0.75f;

		[SerializeField]
		public TMP_Text Timer;

		[SerializeField]
		public TMP_Text Info;
		
		private string text = "Dodge attacks for as long as possible.\nIf you stop moving, you will take damage.";
		
		private float startTime;
		private int currentCharacter;
		
		public void Start()
		{
			startTime = Time.time;
			
			var world = World.World.Instance;
			var spawnPoint = world.SpawnPoints.GetChild(Random.Range(0, world.SpawnPoints.childCount));
			
			AIManager.Instance.CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles, (PlayerData)ObjectManager.Instance.GetAlive("AI_PLAYER_WORLD4_NAME"));
			
			attackLoop().Forget();
			damageLoop().Forget();
			textLoop().Forget();
		}

		public void Update()
		{
			var timeSpan = TimeSpan.FromSeconds(Time.time - startTime);
			Timer.text = $"<mspace=0.6em>{timeSpan.Minutes:00}:{timeSpan.Seconds:00}:{timeSpan.Milliseconds:000}</mspace>";
		}
		
		private async UniTaskVoid attackLoop()
		{
			await UniTask.WaitForSeconds(10f);
			
			if (this == null || !isActiveAndEnabled)
				return;

			var incinerate = ObjectManager.Instance.GetAttack("ATTACK_INCINERATE_NAME");
			
			while (true)
			{
				await UniTask.WaitForSeconds(AttackEvery);

				if (this == null || !isActiveAndEnabled)
					return;

				if (AttackEvery <= 0)
				{
					await SceneManager.Instance.ChangeSceneAsync("Title", true, true, false);
					return;
				}

				var hit = new RaycastHit
				{
					point = new Vector3(Random.Range(RangeX.x, RangeX.y), Y, Random.Range(RangeZ.x, RangeZ.y)),
					normal = Vector3.up
				};

				ObjectManager.Instance.CreateAttack(incinerate, this, hit, null);
				AttackEvery /= DivideBy;
			}
		}
		
		private async UniTaskVoid damageLoop()
		{
			await UniTask.WaitForSeconds(15f);
			
			if (this == null || !isActiveAndEnabled)
				return;

			var player = AIManager.Instance.Player;
			
			while (true)
			{
				await UniTask.WaitForSeconds(DamageEvery);

				if (this == null || !isActiveAndEnabled)
					return;
				
				if (player == null || !player.IsAlive)
					return;
				
				if (player.Body.Rigidbody.linearVelocity.magnitude >= MinimumVelocity)
					continue;
				
				player.Damage(DamageAmount, this, EElement.Unknown);
			}
		}

		private async UniTaskVoid textLoop()
		{
			while (currentCharacter <= text.Length)
			{
				await UniTask.WaitForSeconds(0.1f);
				
				if (this == null || !isActiveAndEnabled)
					return;
				
				Info.text = text[..currentCharacter];
				currentCharacter++;
			}

			await UniTask.WaitForSeconds(1f);
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			Info.gameObject.SetActive(false);
		}
	}
}