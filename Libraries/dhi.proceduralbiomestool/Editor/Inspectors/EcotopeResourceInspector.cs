using Editor.Inspectors;

namespace ProceduralBiomesToolEditor.Inspectors;

[CanEdit("asset:ecotope")]
public class EcotopeResourceInspector : Widget, AssetInspector.IAssetInspector
{
	private Asset Asset { get; set; }
	
	public EcotopeResourceInspector(Widget parent) : base(parent)
	{
		Layout = Layout.Column();
		Layout.Margin = 4;
		Layout.Spacing = 4;

		Rebuild();
	}

	public void SetAsset(Asset asset)
	{
		Asset = asset;
		Rebuild();
	}
	
	[EditorEvent.Hotload]
	private void Rebuild()
	{
		if(Layout == null)
			return;
		
		Layout.Clear(true);
		
		Layout.AddSpacingCell(20);
		
		var horiz = Layout.AddRow();
		horiz.Spacing = 8;
		{
			horiz.AddStretchCell();
			horiz.Add(new Button()
			{
				Text = "Open Editor",
				Icon = "search",
				Clicked = OpenAsset,
				Tint = Theme.Primary
			});
			horiz.AddStretchCell();
		}

		Layout.AddSpacingCell(20);
	}

	private void OpenAsset()
	{
		if (!(Asset?.IsDeleted ?? true))
		{
			Asset.OpenInEditor();
		}
	}

}
