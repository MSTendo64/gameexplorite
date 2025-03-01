using System;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Internal;

public sealed class TreeSpanwer : Component
{
	
	[Property] private List<PrefabFile> trees;
	[Property] private int terrainSize = 319488;
	[Property] private int maxTrees = 90000;

	private Logger logger = new Logger("Tree generator");

	protected override void OnStart()
	{
		try
		{
			Vector3[] TreesPosiotions = new Vector3[new Random().Int( maxTrees )];

			for ( int i = 0; i < maxTrees -1; i++ )
			{
				GameObject nextTree = new GameObject();
				
				nextTree.SetPrefabSource(trees[new Random().Int(trees.Count - 1)].ResourcePath);

				nextTree.UpdateFromPrefab();

				nextTree.Name = $"Next Tree {i}";

				TreesPosiotions[i] = new Vector3( new Random().Int( terrainSize / 2 ), new Random().Int( terrainSize / 2 ), 46 );

				//logger.Info( $"Tree spawned on: {TreesPosiotions[i]}" );

				nextTree.Transform.Position = TreesPosiotions[i];
			}
		}
		catch ( Exception ex )
		{
			logger.Error( ex );
		}
		
	}

	protected override void OnUpdate()
	{

	}
}
