using System;
using ProceduralBiomesTool.Resources;

namespace ProceduralBiomesToolEditor.Inspectors;

[EditorForAssetType("ecotope")]
public class EcotopeResourceEditor : Window, IAssetEditor
{
	private static readonly Color ButtonDefaultColor = (Color) "#48494c";
	
	public bool CanOpenMultipleAssets => false;
	
	private Asset editingAsset;
	private EcotopeResource ecotopeResource;
	private string assetName;
	
	private NavigationView view;
	private List<NavigationView.Option> viewLayerOptions = new();
	
	private IconButton removeSelectedLayerButton;
	private bool dirty;
	
	public EcotopeResourceEditor()
	{
		Size = new Vector2(650, 800);
		
		Canvas = new Widget(null);
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Spacing = 4;
		Canvas.Layout.Margin = 4;
		
		BuildToolbar();
		
		Show();
	}
	
	// IAssetEditor
	public void AssetOpen(Asset asset)
	{
		editingAsset = asset;
		assetName = asset.Name;
		
		// Don't load the ecotope resource from the asset directly, but create a copy with the asset's data that we'll manipulate instead
		ecotopeResource = new EcotopeResource();
		ecotopeResource.LoadFromJson(asset.ReadJson());
		if (!ecotopeResource.HandledPostCreationSetup)
		{
			ecotopeResource.ResetToDefaultRules();
			ecotopeResource.HandledPostCreationSetup = true;
		}
		
		Rebuild();
	}
	
	private void BuildToolbar()
	{
		var toolBar = new ToolBar(this, "EcotopeResourceEditorToolbar");
		AddToolBar(toolBar, ToolbarPosition.Top);
		
		toolBar.AddOption("Save", "common/save.png", Save).StatusTip = "Save Ecotope";
	}

	[EditorEvent.Hotload]
	private void Rebuild()
	{
		var container = Canvas.Layout;
		if(container == null || ecotopeResource == null)
			return;
		
		container.Clear(true);
		
		view = new NavigationView(this);
		container.Add(view, 1);
		viewLayerOptions.Clear();
		
		view.AddSectionHeader("Layers");
		for (int i = 0; i < ecotopeResource.Layers?.Length; ++i)
		{
			var idx = i;
			var option = new NavigationView.Option(GetLayerName(i), "park");
			option.CreatePage = () => CreateLayerSettingsPage(ecotopeResource.Layers[idx]);
			option.OpenContextMenu = () => OpenContextMenuOnLayer(idx);
			view.AddPage(option);
			
			viewLayerOptions.Add(option);
		}
		
		view.AddSectionHeader("Global");
		{
			var option = new NavigationView.Option("Global Settings", "forest");
			option.CreatePage = CreateGlobalSettingsPage;
			view.AddPage(option);
		}
		
		{
			var horiz = container.AddRow();
			horiz.Spacing = 8;
			
			var add = new IconButton("add", AddLayer);
			add.ToolTip = "Add New Layer";
			horiz.Add(add);

			var remove = new IconButton("remove", RemoveSelectedLayer);
			remove.Enabled = IsSelectedLayerValid();
			remove.ToolTip = "Remove Selected Layer";
			horiz.Add(remove);
			removeSelectedLayerButton = remove;
			
			horiz.AddStretchCell();
		}
	}
	
	private Widget CreateGlobalSettingsPage()
	{
		var scroller = new ScrollArea(this);
		scroller.Canvas = new Widget();
		scroller.Canvas.Layout = Layout.Column();
		scroller.Canvas.VerticalSizeMode = SizeMode.CanGrow;
		scroller.Canvas.HorizontalSizeMode = SizeMode.Flexible;
		
		var page = new EcotopeResourceGlobalSettingsPage(ecotopeResource)
		{
			RefreshButtons = Refresh,
			SetDirty = SetDirty
		};
		page.Build();
		scroller.Canvas.Layout.Add(page);
		
		return page;
	}

	private Widget CreateLayerSettingsPage(EcotopeLayer layer)
	{
		var scroller = new ScrollArea(this);
		scroller.Canvas = new Widget();
		scroller.Canvas.Layout = Layout.Column();
		scroller.Canvas.VerticalSizeMode = SizeMode.CanGrow;
		scroller.Canvas.HorizontalSizeMode = SizeMode.Flexible;
		
		var page = new EcotopeResourceLayerSettingsPage(ecotopeResource, layer)
		{
			RefreshButtons = Refresh,
			SetDirty = SetDirty
		};
		page.Build();
		scroller.Canvas.Layout.Add(page);
		
		return scroller;
	}
	
	/// <summary>
	/// Open the context menu when right-clicking on a layer in the layers list.
	/// </summary>
	private void OpenContextMenuOnLayer(int index)
	{
		// @note: rebuild the full view after reordering or deleting layers instead of just refreshing it

		void Dirty()
		{
			SetDirty();
			Rebuild();
		}
		
		var menu = new ContextMenu(this);
		menu.AddOption("Move to Top", "arrow_upward", () =>
		{
			ecotopeResource.MoveToTop(index);
			Dirty();
		});
		menu.AddOption("Move Up", "expand_less", () =>
		{
			ecotopeResource.Swap(index, -1);
			Dirty();
		});
		menu.AddOption("Move Down", "expand_more", () =>
		{
			ecotopeResource.Swap(index, 1);
			Dirty();
		});
		menu.AddOption("Move to Bottom", "arrow_downward", () =>
		{
			ecotopeResource.MoveToBottom(index);
			Dirty();
		});
		menu.AddSeparator();
		menu.AddOption("Delete", "clear", () =>
		{
			ecotopeResource.RemoveAt(index);
			Dirty();
		});
		menu.OpenAtCursor();
	}
	
	private void Refresh()
	{
		WindowTitle = $"Ecotope Editor - {(string.IsNullOrWhiteSpace(assetName) ? "untitled" : assetName)}{(dirty ? "*" : string.Empty)}";
		
		if (removeSelectedLayerButton.IsValid())
		{
			removeSelectedLayerButton.Enabled = IsSelectedLayerValid();
		}
	}
	
	private void SetDirty()
	{
		dirty = true;
		Refresh();
	}
	
	private bool IsValidLayerIndex(int index) => index >= 0 && index < ecotopeResource.Layers.Length;
	private string GetLayerName(int index) => $"{index + 1}. {ecotopeResource.Layers[index].LayerName}";
	private bool IsSelectedLayerValid() => IsValidLayerIndex(GetIndexOfSelectedLayer());

	private int GetIndexOfSelectedLayer()
	{
		for (int i = 0; i < viewLayerOptions.Count; ++i)
		{
			if (viewLayerOptions[i].IsSelected)
				return i;
		}
		return -1;
	}
	
	private void AddLayer()
	{
		ecotopeResource.AddNew();
		SetDirty();
		Rebuild();
	}

	private void RemoveSelectedLayer()
	{
		if (IsSelectedLayerValid())
		{
			ecotopeResource.RemoveAt(GetIndexOfSelectedLayer());
			SetDirty();
			Rebuild();
		}
	}
	
	private void Save()
	{
		var path = editingAsset?.AbsolutePath;
		if (string.IsNullOrEmpty(path))
		{
			path = GetSavePath();
		}
		if (path == null)
			return;
		
		Log.Info($"Saving ecotope to asset {editingAsset}");
		string json = ecotopeResource.Serialize().ToString();
		System.IO.File.WriteAllText(path, json);

		dirty = false;
		Refresh();
	}
	
	protected override bool OnClose()
	{
		if (!dirty)
		{
			return true;
		}

		var confirm = new PopupWindow("Save Current Ecotope", "The open ecotope has unsaved changes. Would you like to save now?",
			"Cancel",
			new Dictionary<string, Action>()
			{
				{
					"No", () =>
					{
						dirty = false;
						Close();
					}
				},
				{
					"Yes", () =>
					{
						Save();
						Close();
					}
				}
			}
		);
		confirm.Show();
		return false;
	}
	
	private string GetSavePath()
	{
		var dialog = new FileDialog(null)
		{
			Title = $"Save Ecotope Resource",
			DefaultSuffix = $".ecotope"
		};

		dialog.SelectFile($"untitled.ecotope");
		dialog.SetFindFile();
		dialog.SetModeSave();
		dialog.SetNameFilter("Ecotope (*.ecotope)");
		if (!dialog.Execute())
			return null;

		return dialog.SelectedFile;
	}
	
	// IAssetEditor
	public void SelectMember(string memberName)
	{
		throw new System.NotImplementedException();
	}
	
}

