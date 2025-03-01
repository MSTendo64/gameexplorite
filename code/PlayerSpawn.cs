using System;
using Sandbox;
using Sandbox.Diagnostics;

public sealed class PlayerSpawn : Component
{
	[Property] private PrefabFile PlayerPrefab;

	private Logger logger = new Logger("Player Spawn");

	protected override void OnStart()
	{
		GameObject Player = new GameObject();

		bool success = Player.NetworkSpawn();
		if ( success )
		{
			logger.Info( "Player Spawned" );
		}
		else
		{
			// The object could not be spawned, possibly due to lack of permissions
			logger.Warning( "Player not Spawned" );
		}

		Player.SetPrefabSource( PlayerPrefab.ResourcePath );

		Player.UpdateFromPrefab();
	}
}
