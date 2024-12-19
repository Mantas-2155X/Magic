using ScriptableObjects;

namespace Weapons.Interfaces
{
	public interface IProjectileWeapon : IWeapon
	{
		public float Force { get; }
		
		public ProjectileData Projectile { get; }
	}
}