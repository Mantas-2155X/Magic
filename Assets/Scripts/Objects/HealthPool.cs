using AI.Interfaces;
using Objects.Base;
using Objects.Enums;

namespace Objects
{
	public class HealthPool : BasePool
	{
		public override EPoolType Type => EPoolType.Health;
		
		public override void OnPoolLooped(IAlive alive)
		{
			alive.Heal(Amount, this, true);
		}
	}
}