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

				var copy = Addressables.InstantiateAsync("Assets/Prefabs/UI/Debug UI.prefab").WaitForCompletion();
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
				var npcs = 0;

				var aiManager = AIManager.Instance;
				if (aiManager != null)
				{
					npcs = aiManager.AlivesColliderMap.Count;

					if (aiManager.Player != null && aiManager.Player.IsAlive)
						npcs--;
				}

				Text.text = $"NPCs: {npcs}\nFPS: {(int)(count / time)}";
				
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