using System.Threading.Tasks;
using ProceduralBiomesTool.Components;
using ProceduralBiomesTool.Resources;

namespace ProceduralBiomesToolEditor;

public class BiomesListView : ListView
{
	public int IconSize { get; set; } = 68;
	
	private readonly TerrainBiomesComponent biomes;
	private readonly List<object> ecotopeItems = new();

	private bool isDragging;
	private ItemDragEvent LastItemDragEvent;
	
	public BiomesListView(TerrainBiomesComponent biomes)
	{
		this.biomes = biomes;
		
		Margin = 8;
		ItemSpacing = 4;
		AcceptDrops = true;
		MinimumHeight = 100;

		ItemSize = new Vector2(IconSize, IconSize + 16);
		ItemAlign = Sandbox.UI.Align.FlexStart;
		ItemSelected = OnItemClicked;
		ItemActivated = OnItemDoubleClicked;
		ItemContextMenu = ShowItemContextMenu;
		
		BuildItems();
	}

	public void BuildItems()
	{
		ecotopeItems.Clear();
		ecotopeItems.AddRange(biomes.Ecotopes);
		ecotopeItems.Add(true); // add a special case we can draw

		SetItems(ecotopeItems);
	}

	protected override void PaintItem(VirtualWidget item)
	{
		var rect = item.Rect.Shrink(0, 0, 0, 16);

		// Special case to show user should drag-drop the ecotope here to add it to the list instead of replacing something
		if (item.Object is bool)
		{
			if(LastItemDragEvent.Item != item)
				return;
			
			if (isDragging)
			{
				Paint.SetPen(Color.White);
				Paint.DrawIcon(rect, "add_circle", 24);

				Paint.SetDefaultFont();
				Paint.DrawText(item.Rect.Shrink(2), "Add New", TextFlag.CenterBottom);
			}
			return;
		}
		
		if (item.Object is not IndexedEcotope ecotope)
		{
			Paint.SetDefaultFont();
			Paint.SetPen(Color.Red);
			Paint.DrawText(item.Rect.Shrink(2), "<type error>", TextFlag.Center);
			return;
		}
		
		if (item.Selected || Paint.HasMouseOver)
		{
			Paint.SetBrush(Theme.Blue.WithAlpha(item.Selected ? 0.5f : 0.2f));
			Paint.ClearPen();
			Paint.DrawRect(item.Rect, 4);
		}

		if (LastItemDragEvent.Item == item)
		{
			Paint.SetBrush(Theme.Red.WithAlpha(0.5f));
			Paint.ClearPen();
			Paint.DrawRect(item.Rect, 4);
		}

		// If we've got a biome ecotope but no resource then it's an empty layer for replacing
		if (!ecotope.IsValid())
		{
			Paint.SetPen(Color.White);
			Paint.SetDefaultFont();
			Paint.DrawText(rect, $"Layer {ecotope.Value}", TextFlag.Center);
			Paint.DrawText(item.Rect.Shrink(2), "Empty", TextFlag.CenterBottom);
			return;
		}

		// We've got an actual ecotope to draw, so draw it
		var asset = AssetSystem.FindByPath(ecotope.Resource.ResourcePath);
		if (asset is null)
		{
			// we should never get here, but handle it anyway
			Paint.SetDefaultFont();
			Paint.SetPen(Color.Red);
			Paint.DrawText(item.Rect.Shrink(2), "<ERROR>", TextFlag.Center);
			Paint.DrawText(item.Rect.Shrink(2), ecotope.Resource.ResourceName, TextFlag.CenterBottom);
			return;
		}
		
		var pixmap = asset.GetAssetThumb();
		Paint.Draw(rect.Shrink(2), pixmap);

		Paint.SetDefaultFont();
		Paint.SetPen(Color.White);
		Paint.DrawText(item.Rect.Shrink(2), ecotope.Resource.ResourceName, TextFlag.CenterBottom);
	}
	
	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush(Theme.ControlBackground);
		Paint.DrawRect(LocalRect, 4);

		base.OnPaint();
	}

	protected void OnItemClicked(object item)
	{
		if (item is not IndexedEcotope entry)
			return;
		
		BiomePainterTool.BiomeValue = entry.Value;
	}

	protected void OnItemDoubleClicked(object item)
	{
		if (item is not IndexedEcotope entry)
			return;
		
		if(!entry.IsValid())
			return;
		
		var asset = AssetSystem.FindByPath(entry.Resource.ResourcePath);
		asset?.OpenInEditor();
	}

	private void ShowItemContextMenu(object item)
	{
		if (item is not IndexedEcotope entry)
			return;

		bool hasValidResource = entry.Resource.IsValid();
		
		var menu = new ContextMenu(this);
		{
			var opt = menu.AddOption("Open In Editor", "edit", () =>
			{
				var asset = AssetSystem.FindByPath(entry.Resource.ResourcePath);
				asset?.OpenInEditor();
			});
			opt.Enabled = hasValidResource;
		}
		{
			var opt = menu.AddOption("Clear", "remove", () =>
			{
				biomes.ClearEcotope(entry.Resource);
				BuildItems();
				BiomePainterTool.ForceRefresh();
			});
			opt.Enabled = hasValidResource;
		}
		menu.AddOption("Remove", "delete", () =>
		{
			biomes.RemoveEcotope(entry);
			BuildItems();
			BiomePainterTool.ForceRefresh();
		});
		menu.OpenAtCursor();
	}

	public override void OnDragHover(DragEvent ev)
	{
		base.OnDragHover(ev);

		LastItemDragEvent = CurrentItemDragEvent;
		isDragging = true;
		
		foreach (var dragAsset in ev.Data.Assets)
		{
			if (!dragAsset.AssetPath?.EndsWith(".ecotope") ?? false)
			{
				continue;
			}
			
			ev.Action = DropAction.Link;
			break;
		}
	}

	public override void OnDragDrop(DragEvent ev)
	{
		base.OnDragDrop(ev);
		
		if (LastItemDragEvent.Item?.Object is IndexedEcotope replace)
		{
			_ = ReplaceDragDropEcotopes(replace, ev.Data.Assets);
		}
		else
		{
			_ = AddDragDropEcotopes(ev.Data.Assets);
		}
		
		LastItemDragEvent = default;
		isDragging = false;
	}

	private async Task AddDragDropEcotopes(IEnumerable<DragAssetData> draggedAssets)
	{
		foreach (var dragAsset in draggedAssets)
		{
			var asset = await dragAsset.GetAssetAsync();
			if (asset is not null && asset.TryLoadResource<EcotopeResource>(out var resource))
			{
				biomes.AttemptToAddEcotope(resource);
			}
		}

		BuildItems();
		BiomePainterTool.ForceRefresh();
	}

	private async Task ReplaceDragDropEcotopes(IndexedEcotope replace, IEnumerable<DragAssetData> draggedAssets)
	{
		foreach (var dragAsset in draggedAssets)
		{
			var asset = await dragAsset.GetAssetAsync();
			if (asset is not null && asset.TryLoadResource<EcotopeResource>(out var resource))
			{
				biomes.ReplaceEcotope(replace, resource);
				break; // we only want to replace with the first one that works
			}
		}
		
		BuildItems();
		BiomePainterTool.ForceRefresh();
	}
	
}
