using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UI
{
	public class Title : MonoBehaviour
	{
		private bool ignoreInput;

		public void OnNewGame()
		{
			if (ignoreInput)
				return;

			ignoreInput = true;
			Addressables.LoadSceneAsync("Scenes/World3");
		}

		public void OnContinue()
		{
			if (ignoreInput)
				return;
			
			throw new NotImplementedException();
		}

		public void OnLoad()
		{
			if (ignoreInput)
				return;
			
			throw new NotImplementedException();
		}

		public void OnSave()
		{
			if (ignoreInput)
				return;
			
			throw new NotImplementedException();
		}

		public void OnSettings()
		{
			if (ignoreInput)
				return;
			
			throw new NotImplementedException();
		}

		public void OnQuitGame()
		{
			if (ignoreInput)
				return;
			
			#if UNITY_EDITOR
				UnityEditor.EditorApplication.ExitPlaymode();
			#else
				Application.Quit();
			#endif
		}
	}
}