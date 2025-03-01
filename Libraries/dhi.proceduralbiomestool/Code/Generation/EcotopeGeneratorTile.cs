using System;
using System.Threading;
using System.Threading.Tasks;
using ProceduralBiomesTool.Components;
using ProceduralBiomesTool.Resources;

namespace ProceduralBiomesTool.Generation;

/// <summary>
/// A section of the biome used to split the generation into more manageable chunks.
/// </summary>
public class EcotopeGeneratorTile
{
	public readonly TerrainBiomesComponent Biomes;
	public readonly Vector2Int TileLocation;
	
	private readonly List<EcotopeGeneratorTileLayer> layers = new();
	
	// @todo: need to fix footprints in nearby tiles generating too close together
	public EcotopeGeneratorTile(TerrainBiomesComponent biomes, int seed, int x, int y, Vector2 extents, TerrainBiomeGeneratedObjects generatedObjectStore)
	{
		Biomes = biomes;
		TileLocation = new Vector2Int(x, y);

		// If there are no ecotopes in the tile then we don't need to generate anything for this tile
		if (!Biomes.AnyEcotopesPresentInTile(extents, TileLocation.x, TileLocation.y))
			return;
		
		foreach (var (value, ecotope) in Biomes.GetEcotopesPresentInTile(extents, TileLocation.x, TileLocation.y))
		{
			layers.Add(new EcotopeGeneratorTileLayer(biomes, seed, x, y)
			{
				Ecotope = ecotope,
				BiomeValue = value,
				Extents = extents,
				LocalOrigin = new Vector3(biomes.TileExtents.x * x, biomes.TileExtents.y * y),
				WorldOrigin = biomes.WorldPosition + new Vector3(biomes.TileExtents.x * x, biomes.TileExtents.y * y),
				GeneratedObjectStore = generatedObjectStore
			});
		}
	}
	
	public override int GetHashCode()
	{
		// @note: we're going to store the tiles in a hash map, so use the tile location to determine the hash for it
		return TileLocation.GetHashCode();
	}
	
	public async Task Generate(CancellationToken cancellationToken)
	{
		if (layers.Count == 0)
			return;
		
		foreach (var layer in layers)
		{
			await layer.Generate(cancellationToken);
			if (cancellationToken.IsCancellationRequested)
				break;
		}
	}
	
}

/// <summary>
/// A layer of a generated tile which contains the point cloud for a single ecotope resource.
/// </summary>
public class EcotopeGeneratorTileLayer
{
	public readonly TerrainBiomesComponent Biomes;
	public readonly int Seed;
	public readonly Random RandomState;
	public readonly PointCloud PointCloud;
	public Vector2Int Tile;
	public Vector3 WorldOrigin;
	public Vector3 LocalOrigin;
	public Vector2 Extents;
	public EcotopeResource Ecotope;
	public TerrainBiomeGeneratedObjects GeneratedObjectStore;
	public int BiomeValue;

	private EcotopeLayerAssetGeneratorState[] states;
	private EcotopeAssetResource[] assets;
	
	public EcotopeGeneratorTileLayer(TerrainBiomesComponent biomes, int seed, int x, int y)
	{
		Biomes = biomes;
		Tile = new Vector2Int(x, y);
		Seed = seed;
		RandomState = new Random(seed * (x + 1) + y);
		PointCloud = new PointCloud(RandomState);
	}
	
	public async Task Generate(CancellationToken cancellationToken)
	{
		// Generate all rules in a background thread first
		await Biomes.TaskSource.RunInThreadAsync(() =>
		{
			GenerateLayerAssetStates();
			if (cancellationToken.IsCancellationRequested)
				return;
			
			GeneratePointCloud();
			if (cancellationToken.IsCancellationRequested)
				return;
			
			foreach (var rule in Ecotope.Rules)
			{
				rule.Execute(PointCloud);
				if (cancellationToken.IsCancellationRequested)
					return;
			}
		});
		if (cancellationToken.IsCancellationRequested)
			return;
		
		// Then create all the game objects
		await GenerateGameObjects(cancellationToken);
	}
	
	private void GenerateLayerAssetStates()
	{
		int assetCount = 0;
		foreach (var layer in Ecotope.Layers)
		{
			assetCount += layer.Assets?.Length ?? 0;
		}

		states = new EcotopeLayerAssetGeneratorState[assetCount];
		assets = new EcotopeAssetResource[assetCount];

		int assetIndex = 0;
		for (var layerIndex = 0; layerIndex < Ecotope.Layers.Length; layerIndex++)
		{
			var layer = Ecotope.Layers[layerIndex];
			foreach (var asset in layer.Assets)
			{
				var state = new EcotopeLayerAssetGeneratorState()
				{
					AssetReference = asset,
					Layer = layerIndex,
					BiomeValue = BiomeValue,
					Density = Ecotope.Density,
					Biomes = Biomes,
					WorldOrigin = WorldOrigin,
					LocalOrigin = LocalOrigin,
					Extents = Extents,
					Seed = Seed,
					RandomState = RandomState
				};
				
				// Apply the layer rules to each generator
				foreach (var rule in layer.Rules)
				{
					rule.Execute(state);
				}
				
				assets[assetIndex] = asset.Asset;
				states[assetIndex] = state;
				assetIndex++;
			}
		}
	}
	
	private void GeneratePointCloud()
	{
		PointCloud.Clear();

		int id = 0;
		int assetIndex = 0;
		foreach (var state in states)
		{
			foreach (var point in state.Points)
			{
				var p = point;
				p.Id = id++;
				p.AssetIndex = assetIndex;
				PointCloud.Add(p);
			}
			assetIndex++;
		}
	}
	
	private async Task GenerateGameObjects(CancellationToken cancellationToken)
	{
		await Biomes.TaskSource.MainThread();
		
		var parent = GeneratedObjectStore.GetTileParent(Tile);
		
		foreach (var point in PointCloud)
		{
			var asset = assets[point.AssetIndex];
			
			var go = new GameObject(parent, true, $"[Generated] {asset.ResourceName}");
			go.MakeNameUnique();
			go.Flags = TerrainBiomesComponent.GeneratedObjectFlags;
			go.WorldPosition = point.Position;
			go.WorldRotation = point.Rotation;
			go.WorldScale = point.Scale;
			go.Tags.SetFrom(point.Tags);

			var prop = go.AddComponent<Prop>();
			prop.IsStatic = true;
			prop.Model = asset.Models[point.ModelIndex];
			prop.Tint = point.Tint;
			
			await Biomes.TaskSource.Yield();
			if (cancellationToken.IsCancellationRequested)
				break;
		}
	}
	
}
