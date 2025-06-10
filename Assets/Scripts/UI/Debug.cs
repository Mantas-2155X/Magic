using System.Threading;
using Cysharp.Threading.Tasks;
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
		public GameObject Error;
		
		[SerializeField]
		public int AverageOver = 5;

		[SerializeField]
		public float ErrorDuration = 4f;
		
		private float time;
		private int count;

		private CancellationTokenSource cancellationToken = new ();
		
		public void Awake()
		{
			Application.logMessageReceived += logReceived;
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

		public void OnDestroy()
		{
			Application.logMessageReceived -= logReceived;
		}

		private void logReceived(string logString, string stackTrace, LogType type)
		{
			if (type is LogType.Log or LogType.Warning)
				return;

			cancellationToken?.Cancel();
			cancellationToken = new CancellationTokenSource();
			
			showError(cancellationToken.Token).Forget();
		}
		
		private async UniTaskVoid showError(CancellationToken token)
		{
			Error.SetActive(true);
			
			await UniTask.WaitForSeconds(ErrorDuration, true, cancellationToken: token);
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			Error.SetActive(false);
		}
	}
}