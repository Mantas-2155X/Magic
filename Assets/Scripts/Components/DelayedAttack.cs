using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using UnityEngine;

namespace Components
{
	public class DelayedAttack : MonoBehaviour
	{
		[SerializeField]
		public AttackData Data;

		[SerializeField]
		public float AttackAfter;
		
		public void Awake()
		{
			if (Data == null)
				return;
			
			attackDelayed().Forget();
		}

		private async UniTaskVoid attackDelayed()
		{
			await UniTask.WaitForSeconds(AttackAfter);
			
			if (this == null || !isActiveAndEnabled)
				return;

			var tr = transform;
			ObjectManager.Instance.CreateAttack(Data, this, tr.position, Vector3.zero, tr);
		}
	}
}