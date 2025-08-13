using Managers;
using UnityEngine;

namespace Components
{
	public class AutoSave : MonoBehaviour
	{
		public void Trigger()
		{
			StateManager.Instance.AutoSave();
		}
	}
}