using System.Collections.Generic;
using AI.Interfaces;
using Managers;
using UnityEngine;

public class Parent : MonoBehaviour
{
	private readonly Dictionary<IAlive, Transform> alives = new ();

	public void OnTriggerEnter(Collider other)
	{
		if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive) || alives.ContainsKey(alive))
			return;

		var tr = alive.GetTransform();
		alives.Add(alive, tr.parent);
		tr.parent = transform;
	}

	public void OnTriggerExit(Collider other)
	{
		if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive) || !alives.TryGetValue(alive, out var parent))
			return;

		var tr = alive.GetTransform();
		tr.parent = parent;
		alives.Remove(alive);
	}
}