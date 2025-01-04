using Events;
using UnityEngine;

public class Trigger : MonoBehaviour
{
	[SerializeField]
	public OnTriggerEvent OnTriggerEvent;
	
	private bool triggered;
	
	public void OnTriggerEnter(Collider other)
	{
		if (triggered)
			return;

		triggered = true;
		OnTriggerEvent?.Invoke(other);
	}
}