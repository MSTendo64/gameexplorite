using System;
using Sandbox;

public sealed class RandomPos : Component
{
	[Property] private int mapSize = 319488;
	protected override void OnStart()
	{
		Vector3 RandomPlayerPosiotions = new Vector3( new Random().Int( mapSize / 2 ), new Random().Int( mapSize / 2 ), 500 );
		Transform.Position = RandomPlayerPosiotions;
		
	}
}
