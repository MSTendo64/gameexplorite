using System.Threading;
using System.Threading.Tasks;

namespace ProceduralBiomesTool.Components;

[Description("Stores generated gameobjects from the Biomes tool.")]
[Icon("widgets")]
[Group("World")]
public class TerrainBiomeGeneratedObjects : Component, Component.ExecuteInEditor
{
	[Property, ReadOnly] private Dictionary<int, GameObject> TileStorageGameObjects { get; set; } = new();
	
	protected override void OnEnabled()
	{
		base.OnEnabled();
		
		HideChildren();
	}

	[Event("scene.open")]
	private void OnSceneOpened()
	{
		HideChildren();
	}

	private void HideChildren()
	{
		int count = GameObject.Children.Count;
		for (int i = 0; i < count; ++i)
		{
			var child = GameObject.Children[i];
			child.Flags = TerrainBiomesComponent.GeneratedObjectFlags;
		}
	}
	
	private int GetIndex(Vector2Int tile)
	{
		// "only" supports up to 32k x 32k tiles but if someone is doing that then a lot more is going to shit itself than this 
		return tile.y * short.MaxValue + tile.x;
	}
	
	public GameObject GetTileParent(Vector2Int tile)
	{
		int index = GetIndex(tile);
		if (TileStorageGameObjects.TryGetValue(index, out var gameObject))
		{
			return gameObject;
		}
		else
		{
			var go = new GameObject(GameObject, true, $"[Generated] Tile Storage {tile.x}, {tile.y}");
			go.Flags = TerrainBiomesComponent.GeneratedObjectFlags;
			TileStorageGameObjects.Add(index, go);
			return go;
		}
	}
	
	public void DeleteAll()
	{
		foreach (var storage in TileStorageGameObjects.Values)
		{
			if(!storage.IsValid())
				continue;
			
			int count = storage.Children.Count;
			for (int i = count - 1; i >= 0; --i)
			{
				var child = storage.Children[i];
				child.DestroyImmediate();
			}
		}
	}

	public async Task DeleteAllAsync()
	{
		foreach (var storage in TileStorageGameObjects.Values)
		{
			if(!storage.IsValid())
				continue;
			
			int count = storage.Children.Count;
			for (int i = count - 1; i >= 0; --i)
			{
				var child = storage.Children[i];
				child.DestroyImmediate();
				await Task.Yield();
			}
		}
	}

	public void DeleteTile(Vector2Int tile)
	{
		int index = GetIndex(tile);
		if (TileStorageGameObjects.TryGetValue(index, out var storage))
		{
			int count = storage.Children.Count;
			for (int i = count - 1; i >= 0; --i)
			{
				var child = storage.Children[i];
				child.DestroyImmediate();
			}
		}
	}

	public async Task DeleteTileAsync(Vector2Int tile, CancellationToken cancellationToken = default)
	{
		// Make sure this is happening on the main thread otherwise it'll error out
		await Task.MainThread();
		
		int index = GetIndex(tile);
		if (TileStorageGameObjects.TryGetValue(index, out var storage))
		{
			int count = storage.Children.Count;
			for (int i = count - 1; i >= 0; --i)
			{
				var child = storage.Children[i];
				child.DestroyImmediate();
				await Task.Yield();
				if(cancellationToken.IsCancellationRequested)
					return;
			}
		}
	}
	
}
