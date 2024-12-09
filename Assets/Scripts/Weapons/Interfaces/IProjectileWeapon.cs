using System;
using AI.Interfaces;
using UnityEngine;

namespace Weapons.Interfaces
{
	public interface IProjectileWeapon : IWeapon
	{
		public float Force { get; }
		public Type Projectile { get; }
	}
}