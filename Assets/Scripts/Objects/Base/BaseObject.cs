using Objects.Interfaces;
using ScriptableObjects;
using UnityEngine;

namespace Objects.Base
{
	public class BaseObject : MonoBehaviour, IObject
	{
		[field: SerializeField]
		public ObjectData ObjectData { get; private set; }
	}
}