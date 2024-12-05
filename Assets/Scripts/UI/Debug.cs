using TMPro;
using UnityEngine;

namespace UI
{
	public class Debug : MonoBehaviour
	{
		[SerializeField]
		public TMP_Text FPS;

		[SerializeField]
		public int AverageOver = 5;

		private float time;
		private int count;
		
		public void Update()
		{
			if (count < AverageOver)
			{
				time += Time.deltaTime;
				count++;
			}
			else
			{
				FPS.text = $"FPS: {(int)(count / time)}";
				
				time = 0f;
				count = 0;
			}
		}
	}
}