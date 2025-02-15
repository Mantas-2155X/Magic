using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UI
{
	public class Fade : MonoBehaviour
	{
		private static Fade instance;
		public static Fade Instance
		{
			get
			{
				if (instance != null)
					return instance;

				var prefab = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/UI/Fade UI.prefab").WaitForCompletion();
				if (prefab == null)
				{
					UnityEngine.Debug.LogError("[Fade] Failed to load base prefab");
					return null;
				}

				var copy = Instantiate(prefab);
				DontDestroyOnLoad(copy);

				instance = copy.GetComponent<Fade>();
				return instance;
			}
		}

		[SerializeField]
		public CanvasGroup Group;

		[SerializeField]
		public Transform LeftTarget;

		[SerializeField]
		public Transform RightTarget;

		[SerializeField]
		public Transform Projectile;
		
		[SerializeField]
		public float AnimationSpeed;

		private bool returning;
		
		public void Update()
		{
			var targetPos = returning ? LeftTarget.position : RightTarget.position;
			
			var projectilePos = Projectile.position;
			projectilePos.x = Mathf.MoveTowards(projectilePos.x, targetPos.x, AnimationSpeed * Time.unscaledDeltaTime);
			
			Projectile.position = projectilePos;

			if (returning)
			{
				if (projectilePos.x <= targetPos.x + 1)
					returning = false;
			}
			else
			{
				if (projectilePos.x >= targetPos.x - 1)
					returning = true;
			}
		}
		
		public void SetAlpha(float alpha)
		{
			Group.alpha = alpha;
		}
	}
}