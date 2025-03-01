using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ProceduralBiomesTool.Generation;
using ProceduralBiomesTool.Resources;
using Sandbox.Diagnostics;

namespace ProceduralBiomesTool.Components;

[Title("Terrain Biomes")]
[Description("Allows you to paint ecotopes directly onto terrain.")]
[Icon("forest")]
[Group("World")]
public class TerrainBiomesComponent : Component, Component.ExecuteInEditor
{
	// @note: changing this above 256 means you're going to run into a lot of places you need to update.
	// So probably don't change it unless you really know what you're doing.
	public const int MaxBiomes = 256;
	public const GameObjectFlags GeneratedObjectFlags = GameObjectFlags.Attachment | GameObjectFlags.Hidden;
	
	/// <summary>
	/// Which ecoptopes are present in this biomes component and which byte values on the type map are they assigned to.
	/// </summary>
	[Property] public List<IndexedEcotope> Ecotopes { get; set; } = new();
	
	/// <summary>
	/// This is the saved data of the biome type map, it's a base 64 string of the byte array representing the texture.
	/// </summary>
	[Property] private string BiomeTypeMapBase64 { get; set; }
	
	/// <summary>
	/// Which terrain component is this biomes component linked to?
	/// </summary>
	[Property, Group("Settings"), RequireComponent, ReadOnly] public Terrain Terrain { get; set; }
	/// <summary>
	/// Where will the generated biome objects be stored?
	/// </summary>
	[Property, Group("Settings")] public GameObject GeneratedBiomesObject { get; set; }
	[Property, Group("Settings"), ReadOnly] public int Resolution { get; set; }
	[Property, Group("Settings")] public int Seed { get; private set; }
	[Property, Group("Settings")] public Vector2Int Tiles { get; private set; } = new Vector2Int(20, 20);
	
	// Editor only data but helpful for controlling tools without much effort
	[Property, Group("Editor")] public bool DrawExtents { get; set; }
	[Property, Group("Editor")] public bool DrawTiles { get; set; }
	
	public byte[] BiomeTypeMapData { get; set; }
	public Texture BiomeTypeMapTexture { get; private set; }

	public Vector2 Extents => new Vector2(Terrain.TerrainSize, Terrain.TerrainSize);
	public Vector2 TileExtents => Extents / Tiles;
	
	public TaskSource TaskSource => Task;

	private TerrainBiomeGeneratedObjects generatedObjectsStore;
	public TerrainBiomeGeneratedObjects GeneratedObjectsStore
	{
		get
		{
			if (!GeneratedBiomesObject.IsValid())
			{
				GeneratedBiomesObject = new GameObject();
				GeneratedBiomesObject.Name = $"[Generated] {Terrain.GameObject.Name} Biomes";
				GeneratedBiomesObject.Flags = GameObjectFlags.None;
			}
			if (!generatedObjectsStore.IsValid())
			{
				generatedObjectsStore = GeneratedBiomesObject.GetOrAddComponent<TerrainBiomeGeneratedObjects>();
			}
			return generatedObjectsStore;
		}
	}

	public Action OnReset;
	
	protected override void OnEnabled()
	{
		base.OnEnabled();
		Setup();
	}

	protected override Task OnLoad()
	{
		Setup();
		return base.OnLoad();
	}

	public void Setup()
	{
		Terrain = GetComponent<Terrain>();
		Ecotopes ??= new List<IndexedEcotope>();
		
		if (!Terrain.IsValid())
		{
			Log.Error("Can't create terrain biomes on a biomes component with a null terrain reference!");
			return;
		}
		if (!Terrain.Storage.IsValid())
		{
			Log.Error("Can't create terrain biomes on a biomes component with a terrain with no storage!");
			return;
		}
		
		Resolution = Terrain.Storage.Resolution;
		if (Seed == 0)
		{
			Seed = Game.Random.Next();
		}
		
		if (string.IsNullOrWhiteSpace(BiomeTypeMapBase64))
		{
			Resolution = Terrain.Storage.Resolution;
			BiomeTypeMapData = new byte[Resolution * Resolution];
		}
		else
		{
			byte[] data = Convert.FromBase64String(BiomeTypeMapBase64);
			BiomeTypeMapData = Decompress(data);
		}
		CreateTextureMaps();
	}
	
	private void CreateTextureMaps()
	{
		Assert.NotNull(BiomeTypeMapData);
		
		BiomeTypeMapTexture?.Dispose();
		BiomeTypeMapTexture = Texture.Create(Resolution, Resolution, ImageFormat.I8)
			.WithData(BiomeTypeMapData)
			.WithUAVBinding()
			.WithName("terrain_biome_type_map")
			.Finish();
		
		SyncCpuToGpu();
	}
	
	public void SyncCpuToGpu()
	{
		BiomeTypeMapTexture.Update(BiomeTypeMapData.AsSpan());
	}

	public void SyncGpuToCpu(RectInt region)
	{
		// Clamp within our resolution
		region.Left = Math.Clamp(region.Left, 0, Resolution - 1);
		region.Right = Math.Clamp(region.Right, 0, Resolution - 1);
		region.Top = Math.Clamp(region.Top, 0, Resolution - 1);
		region.Bottom = Math.Clamp(region.Bottom, 0, Resolution - 1);
		
		var regionTuple = (region.Left, region.Top, region.Width, region.Height);
		BiomeTypeMapTexture.GetPixels(regionTuple, 0, 0, BiomeTypeMapData.AsSpan(), ImageFormat.I8, regionTuple, BiomeTypeMapTexture.Width);

		Serialize();
	}
	
	private void Serialize()
	{
		BiomeTypeMapBase64 = Convert.ToBase64String(Compress(BiomeTypeMapData));
	}
	
	private static byte[] Decompress(byte[] compressedData)
	{
		using MemoryStream memoryStream = new MemoryStream(compressedData);
		using MemoryStream destination = new MemoryStream();
		using DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
		deflateStream.CopyTo(destination);
		return destination.ToArray();
	}

	private static byte[] Compress(byte[] data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress);
		deflateStream.Write(data, 0, data.Length);
		deflateStream.Flush();
		return memoryStream.ToArray();
	}
	
	public bool AnyEcotopesPresentInTile(Vector2 tileExtents, int tx, int ty)
	{
		int w = (int)Math.Floor(tileExtents.x / Terrain.TerrainSize * Resolution);
		int h = (int)Math.Floor(tileExtents.y / Terrain.TerrainSize * Resolution);
		int left = w * tx;
		int top = h * ty;
		
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				byte b = BiomeTypeMapData[left + x + (top + y) * Resolution];
				if (b > 0)
					return true;
			}
		}

		return false;
	}
	
	public IEnumerable<(byte, EcotopeResource)> GetEcotopesPresentInTile(Vector2 tileExtents, int tx, int ty)
	{
		EcotopeResource[] resourceMap = ArrayPool<EcotopeResource>.Shared.Rent(MaxBiomes);
		bool[] validityMap = ArrayPool<bool>.Shared.Rent(MaxBiomes);
		for (int i = 0; i < Ecotopes.Count; ++i)
		{
			resourceMap[i + 1] = Ecotopes[i].Resource;
			validityMap[i + 1] = Ecotopes[i].IsValid();
		}

		int w = (int)Math.Floor(tileExtents.x / Terrain.TerrainSize * Resolution);
		int h = (int)Math.Floor(tileExtents.y / Terrain.TerrainSize * Resolution);
		int left = w * tx;
		int top = h * ty;
		
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				byte b = BiomeTypeMapData[left + x + (top + y) * Resolution];
				if (b > 0 && validityMap[b])
				{
					yield return (b, resourceMap[b]);
					validityMap[b] = false; // Set the validity map to false so we don't iterate over this resource again
				}
			}
		}
		
		ArrayPool<EcotopeResource>.Shared.Return(resourceMap);
		ArrayPool<bool>.Shared.Return(validityMap);
	}

	public Vector2Int WorldPositionToTile(Vector3 position)
	{
		int tx = MathX.FloorToInt(position.x / TileExtents.x);
		int ty = MathX.FloorToInt(position.y / TileExtents.y);
		return new Vector2Int(tx, ty);
	}
	
	/// <summary>
	/// Erase all data stored in the biome maps and destroy all gameobject in the biomes.
	/// </summary>
	public override void Reset()
	{
		Setup();

		if (Tiles.x == 0 || Tiles.y == 0)
		{
			Tiles = new Vector2Int(20, 20);
		}
		Seed = Game.Random.Next();
		BiomeTypeMapBase64 = string.Empty;
		BiomeTypeMapData = new byte[Resolution * Resolution];
		CreateTextureMaps();
		
		OnReset?.Invoke();
	}

	private bool TryGetFreeEcotopeIndex(out int index)
	{
		index = -1;
		
		for (int i = 1; i < MaxBiomes; ++i)
		{
			if(Ecotopes.Any(x => x.Value == i))
				continue;

			index = i;
			break;
		}

		return index > 0;
	}

	/// <summary>
	/// Add an ecotope to the biomes and assign it a value it will be painted at.
	/// </summary>
	public void AttemptToAddEcotope(EcotopeResource resource)
	{
		// Can't add resource twice
		if (resource == null || Ecotopes.Any(x => x.Resource == resource))
			return;

		if (TryGetFreeEcotopeIndex(out int index))
		{
			Ecotopes.Add(new IndexedEcotope()
			{
				Value = index,
				Resource = resource
			});
		}
		else
		{
			Log.Warning($"Couldn't find a free index to add {resource} to biomes!");
		}
	}
	
	/// <summary>
	/// Completely remove an ecotope from the biomes and allow it to be replaced.
	/// </summary>
	public void RemoveEcotope(IndexedEcotope indexedEcotope)
	{
		if (indexedEcotope.Resource.IsValid())
		{
			Ecotopes.RemoveAll(x => x.Resource == indexedEcotope.Resource);
		}
		else
		{
			Ecotopes.RemoveAll(x => x.Value == indexedEcotope.Value);
		}
	}

	/// <summary>
	/// Clear an ecotope out of the biomes but leave its index intact so we can replace it.
	/// </summary>
	public void ClearEcotope(EcotopeResource resource)
	{
		foreach (var layer in Ecotopes)
		{
			if (layer.Resource == resource)
			{
				layer.Resource = null;
			}
		}
	}

	/// <summary>
	/// Replace an existing ecotope with another so that it generates in its place.
	/// </summary>
	public void ReplaceEcotope(IndexedEcotope replace, EcotopeResource resource)
	{
		// Can't add resource twice
		if (resource == null || Ecotopes.Any(x => x.Resource == resource))
			return;
		
		replace.Resource = resource;
	}

	public EcotopeGeneratorTile CreateTileGenerator(int x, int y)
	{
		return new EcotopeGeneratorTile(this, Seed, x, y, TileExtents, GeneratedObjectsStore);
	}
	
}

public class IndexedEcotope : IValid
{
	[Property] public int Value { get; set; }
	[Property] public EcotopeResource Resource { get; set; }

	public bool IsValid => Value is > 0 and < TerrainBiomesComponent.MaxBiomes && Resource.IsValid();

	public override string ToString()
	{
		return $"[{nameof(IndexedEcotope)} {Resource} ({Value})]";
	}
}
