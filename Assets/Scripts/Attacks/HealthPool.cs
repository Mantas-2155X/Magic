using Attacks.Base;
using Objects.Enums;

namespace Attacks
{
	public class HealthPool : BasePool
	{
		public override EPoolType Type => EPoolType.Health;
	}
}