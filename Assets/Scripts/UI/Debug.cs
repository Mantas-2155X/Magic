using Managers;
using TMPro;
using UnityEngine;

namespace UI
{
	public class Debug : MonoBehaviour
	{
		[SerializeField]
		public TMP_Text Text;

		[SerializeField]
		public int AverageOver = 5;

		private float time;
		private int count;
		
		public void Update()
		{
			if (count < AverageOver)
			{
				time += Time.unscaledDeltaTime;
				count++;
			}
			else
			{
				Text.text = $"Alive: {AIManager.Instance.AlivesColliderMap.Count}\nFPS: {(int)(count / time)}";
				
				time = 0f;
				count = 0;
			}
		}
	}
}