using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Physics;
using SWB.Base;
using SWB.Demo;

public sealed class WorldWeapon : Component, Component.ITriggerListener
{

	[Property] private string weaponName;

	private static string weaponNameLogger;

	Logger logger;

	Collider collider;

	protected override void OnAwake()
	{
		collider = GetComponent<Collider>();

		weaponNameLogger = weaponName;

		logger = new Logger( $"Weapon {weaponNameLogger} logger" );
	}

	public void OnTriggerEnter( Collider other )
	{
		logger.Error( "Столкновение началось с: " + other.GameObject );

		if ( other.GameObject.GetComponentInParent<DemoPlayer>() != null )
		{
			if ( WeaponRegistry.Instance.Weapons[weaponName] != null )
			{
				other.GameObject.GetComponentInParent<DemoPlayer>().GiveWeapon( "swb_colt" );
				GameObject.Enabled = false;
			}
		}
	}

}
