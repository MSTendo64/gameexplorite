using System;
using ProceduralBiomesTool.Components;
using ProceduralBiomesTool.Resources;

namespace ProceduralBiomesTool.Generation;

/// <summary>
/// The state of generation for a single asset within an ecotope layer.
/// This is passed into the layer rules to be transformed.
/// </summary>
public class EcotopeLayerAssetGeneratorState
{
	/// <summary>
	/// Which biome component was this created from? 
	/// </summary>
	public TerrainBiomesComponent Biomes;
	
	/// <summary>
	/// Which ecotope asset is this state associated with?
	/// </summary>
	public EcotopeLayerAssetReference AssetReference;
	
	/// <summary>
	/// In the biome map texture, what byte value in the texture corresponds to this state?
	/// </summary>
	public int BiomeValue;
	
	public int Layer;
	public float Density = 1f;
	public Vector3 WorldOrigin;
	public Vector3 LocalOrigin;
	public Vector2 Extents;
	public int Seed;
	public Random RandomState;
	public List<AssetPoint> Points = new();
	
	public Terrain Terrain => Biomes.Terrain;
	
	public void AddFromLocalPoints(IEnumerable<Vector2> points)
	{
		foreach (var p in points)
		{
			// Check if point falls on the painted biome before adding it
			int x = (int)Math.Floor((LocalOrigin.x + p.x) / Terrain.TerrainSize * Biomes.Resolution);
			int y = (int)Math.Floor((LocalOrigin.y + p.y) / Terrain.TerrainSize * Biomes.Resolution);
			if(x < 0 || x > Biomes.Resolution || y < 0 || y > Biomes.Resolution)
				continue;
			
			int value = Biomes.BiomeTypeMapData[y * Biomes.Resolution + x];
			if (value == BiomeValue)
			{
				Points.Add(CreatePoint(WorldOrigin + new Vector3(p.x, p.y, 0f)));
			}
		}
	}
	
	public void AddFromWorldPoints(IEnumerable<Vector3> points)
	{
		foreach (var p in points)
		{
			// Check if point falls on the painted biome before adding it
			int x = (int)Math.Floor(p.x / Terrain.TerrainSize * Biomes.Resolution);
			int y = (int)Math.Floor(p.y / Terrain.TerrainSize * Biomes.Resolution);
			if(x < 0 || x > Biomes.Resolution || y < 0 || y > Biomes.Resolution)
				continue;
			
			int value = Biomes.BiomeTypeMapData[y * Biomes.Resolution + x];
			if (value == BiomeValue)
			{
				Points.Add(CreatePoint(p));
			}
		}
	}
	
	private AssetPoint CreatePoint(Vector3 position)
	{
		return new AssetPoint()
		{
			ModelIndex = RandomState.Int(AssetReference.Asset.Models.Length - 1),
			Position = position,
			Rotation = Rotation.Identity,
			Scale = 1f,
			Layer = Layer,
			Viability = AssetReference.Asset.Viability,
			FootprintRadius = AssetReference.Asset.FootprintRadius,
			CollisionRadius = AssetReference.Asset.CollisionRadius,
			Tint = Color.White
		};
	}
}
