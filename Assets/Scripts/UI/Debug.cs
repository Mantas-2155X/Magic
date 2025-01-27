using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UI
{
	public class Debug : MonoBehaviour
	{
		private static Debug instance;
		public static Debug Instance
		{
			get
			{
				if (instance != null)
					return instance;

				var prefab = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/UI/Debug UI.prefab").WaitForCompletion();
				if (prefab == null)
				{
					UnityEngine.Debug.LogError("[Debug] Failed to load base prefab");
					return null;
				}

				var copy = Instantiate(prefab);
				DontDestroyOnLoad(copy);

				instance = copy.GetComponent<Debug>();
				return instance;
			}
		}
		
		[SerializeField]
		public TMP_Text Text;

		[SerializeField]
		public TMP_Text Build;
		
		[SerializeField]
		public int AverageOver = 5;

		private float time;
		private int count;

		public void Awake()
		{
			Build.text = $"Build {Application.version}";
		}

		public void Update()
		{
			if (count < AverageOver)
			{
				time += Time.unscaledDeltaTime;
				count++;
			}
			else
			{
				var alive = AIManager.Instance != null ? AIManager.Instance.AlivesColliderMap.Count : 0;
				Text.text = $"Alive: {alive}\nFPS: {(int)(count / time)}";
				
				time = 0f;
				count = 0;
			}
		}
	}
}