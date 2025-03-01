using ProceduralBiomesTool.Components;
using ProceduralBiomesTool.Resources;
using ProceduralBiomesToolEditor.Utility;

namespace ProceduralBiomesToolEditor;

[CustomEditor(typeof(TerrainBiomesComponent))]
public class TerrainBiomesComponentWidget : ComponentEditorWidget
{
	private BiomesListView biomesListView;
	
	public TerrainBiomesComponentWidget(SerializedObject obj) : base(obj)
	{
		SetSizeMode(SizeMode.Default, SizeMode.Default);
		Layout = Layout.Column();
		Layout.Margin = 16;
		Layout.Spacing = 8;
		
		Rebuild();
	}

	private void Rebuild()
	{
		Layout.Clear(true);

		var biomes = SerializedObject.Targets.FirstOrDefault() as TerrainBiomesComponent;
		if(biomes is null)
			return;
		
		// Create a small error if there is no terrain hooked up to the biomes component somehow
		var terrainRefProperty = SerializedObject.GetProperty("Terrain");
		if (terrainRefProperty is null || terrainRefProperty.IsNull)
		{
			Layout.Add(new Label("No terrain component found, can't use terrain biomes!")
			{
				Color = Theme.Red
			});
			return;
		}
		
		// Biomes preview
		{
			var container = Layout.AddColumn();
			container.Add(WidgetUtils.Header("Paintable Ecotopes"));
			
			biomesListView = new BiomesListView(biomes);
			container.Add(biomesListView);
		}
		{
			var row = Layout.AddRow();
			row.AddStretchCell();
			row.Add(new Button("New Ecotope Asset", "add")
			{
				Clicked = NewEcotopeAsset
			});
		}

		CreateControlSheetForGroup("Settings");
		CreateControlSheetForGroup("Editor");
	}

	private void CreateControlSheetForGroup(string groupName)
	{
		bool Filter(SerializedProperty prop)
		{
			if (prop.TryGetAttribute(out GroupAttribute groupAttr))
			{
				return groupAttr.Value.CompareTo(groupName) == 0;
			}
			return false;
		}
		
		var cs = new ControlSheet();
		cs.AddObject(SerializedObject, Filter);
		Layout.Add(cs);
	}
	
	private void NewEcotopeAsset()
	{
		var filepath = EditorUtility.SaveFileDialog("Create Ecotope Asset", "ecotope", $"{Project.Current.GetAssetsPath()}/");
		if (filepath is null)
			return;

		var asset = AssetSystem.CreateResource("ecotope", filepath);
		if (!asset.TryLoadResource<EcotopeResource>(out var ecotopeResource))
			return;

		asset.Compile(true);
		MainAssetBrowser.Instance?.UpdateAssetList();

		var biomes = SerializedObject.Targets.FirstOrDefault() as TerrainBiomesComponent;
		if (biomes.IsValid())
		{
			biomes.AttemptToAddEcotope(ecotopeResource);
		}
		biomesListView?.BuildItems();

		asset.OpenInEditor();
	}
	
}
