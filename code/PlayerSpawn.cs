using System;
using Sandbox;
using Sandbox.Diagnostics;

public sealed class PlayerSpawn : Component
{
	[Property] private PrefabFile PlayerPrefab;
	[Property] private int mapSize = 319488;

	private Logger logger = new Logger("Player Spawn");


	protected override void OnStart()
	{
		Vector3 RandomPlayerPosiotions = new Vector3( new Random().Int( mapSize / 2 ), new Random().Int( mapSize / 2 ), 46 );

		GameObject Player = new GameObject( true, "Player" );

		bool success = Player.NetworkSpawn();
		if ( success )
		{
			logger.Info( "Player Spawned" );
			Player.Transform.Position = RandomPlayerPosiotions;
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
