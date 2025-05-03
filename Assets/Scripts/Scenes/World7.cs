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

		private Light light;
		private float size = 0.0001f;
		private List<Rigidbody> objects = new ();
		
		public void BeginOrb()
		{
			var player = Player.Instance;
			player.HUD.gameObject.SetActive(false);
			player.Stats.gameObject.SetActive(false);

			light = Orb.GetComponentInChildren<Light>();
			
			processOrb().Forget();

			var colliders = Physics.OverlapSphere(Orb.transform.position, 50);
			for (var i = 0; i < colliders.Length; i++)
			{
				var rb = colliders[i].attachedRigidbody;
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
			
			Orb.transform.parent.gameObject.SetActive(true);

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
					
					obj.AddForce((Orb.transform.position - obj.position).normalized * 1000 * size);
				}

				if (size > 15f)
					light.bounceIntensity += 0.35f;
			}

			await SceneManager.Instance.ChangeSceneAsync("Title", true, true, false);
		}
	}
}