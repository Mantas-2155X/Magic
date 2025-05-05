using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Managers;
using UI;
using UnityEngine;

namespace Scenes
{
	public class World7 : MonoBehaviour
	{
		[SerializeField]
		public ParticleSystem Orb;

		[SerializeField]
		public Transform OrbTr;

		[SerializeField]
		public Light Light;
		
		private float size = 0.0001f;
		
		private readonly List<Rigidbody> objects = new ();
		private readonly Collider[] results = new Collider[500];
		
		public void BeginOrb()
		{
			var player = Player.Instance;
			player.HUD.gameObject.SetActive(false);
			player.Stats.gameObject.SetActive(false);
			player.Notice.gameObject.SetActive(false);
			
			processOrb().Forget();

			var count = Physics.OverlapSphereNonAlloc(Orb.transform.position, 50, results);
			for (var i = 0; i < count; i++)
			{
				var rb = results[i].attachedRigidbody;
				if (rb == null)
					continue;
				
				objects.Add(rb);
			}
		}

		private async UniTaskVoid processOrb()
		{
			await UniTask.WaitForSeconds(2.5f);
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			OrbTr.parent.gameObject.SetActive(true);

			var orbPos = OrbTr.position;

			while (size < 20f)
			{
				await UniTask.WaitForSeconds(0.1f);
				
				if (this == null || !isActiveAndEnabled)
					return;
				
				size += 0.15f;
				
				var shape = Orb.shape;
				shape.radius = size;

				for (var i = 0; i < objects.Count; i++)
				{
					var obj = objects[i];
					if (obj == null)
						continue;
					
					obj.AddForce((orbPos - obj.position).normalized * 1000 * size);
				}

				if (size > 15f)
					Light.bounceIntensity += 0.35f;
			}

			await SceneManager.Instance.ChangeSceneAsync("Title", true, true, false);
		}
	}
}