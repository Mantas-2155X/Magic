namespace State.Interfaces
{
	public interface IState
	{
		public void Read(object obj);
		public void Apply(object obj);
	}
}