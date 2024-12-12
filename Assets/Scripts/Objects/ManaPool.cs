using AI.Interfaces;
using Objects.Base;
using Objects.Enums;

namespace Objects
{
	public class ManaPool : BasePool
	{
		public override EPoolType Type => EPoolType.Mana;
		
		public override void OnPoolLooped(IAlive alive)
		{
			alive.GenerateMana(Amount, this, true);
		}
	}
}