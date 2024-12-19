using ScriptableObjects;

namespace Weapons.Interfaces
{
	public interface IProjectileWeapon : IWeapon
	{
		public ProjectileData Projectile { get; }
	}
}