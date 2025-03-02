using System.Threading.Tasks;

namespace SWB.Demo;

[Group( "SWB" )]
[Title( "Demo NetworkManager" )]
public class DemoNetworkManager : Component, Component.INetworkListener
{
	[Property] public PrefabScene PlayerPrefab { get; set; }

	private GameObject Player; 

	protected override Task OnLoad()
	{
		if ( !Networking.IsActive )
			Networking.CreateLobby( new() );

		return base.OnLoad();
	}

	public GameObject GetPlayer()
	{
		return Player;
	}

	// Called on host
	void INetworkListener.OnActive( Connection connection )
	{
		var playerGO = PlayerPrefab.Clone();
		playerGO.Name = "Player";
		playerGO.NetworkSpawn( connection );
		Player = playerGO;
	}
}
