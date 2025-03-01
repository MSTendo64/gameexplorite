using ProceduralBiomesTool.Components;
using Application = Editor.Application;

namespace ProceduralBiomesToolEditor;

public class BiomeBrushSettings
{
	[Property, Range(0, 1.0f, 0.01f)] public float VisualizationOpacity { get; set; } = 0.5f;
	[Property] public bool AutoRegenerate { get; set; } = true;
	[Property, Range(8, 1024)] public int Size { get; set; } = 200;
}

public class BiomeBrushSettingsWidgetWindow : WidgetWindow
{
	class BrushSelectedWidget : Widget
	{
		public BrushSelectedWidget(Widget parent) : base(parent)
		{
			MinimumSize = new(48, 48);
			Cursor = CursorShape.Finger;
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			Paint.Antialiasing = true;

			Paint.ClearPen();
			Paint.DrawRect(LocalRect);

			var pixmap = BiomePainterTool.Brush.Pixmap;
			Paint.Draw(LocalRect.Contain(pixmap.Size), pixmap);
		}

		protected override void OnMouseClick(MouseEvent e)
		{
			var popup = new PopupWidget(null);
			popup.Position = Application.CursorPosition;
			popup.Visible = true;
			popup.Layout = Layout.Column();
			popup.Layout.Margin = 10;
			popup.MaximumSize = new Vector2(300, 150);

			var list = new BiomeBrushListWidget();
			list.BrushSelected += () =>
			{
				popup.Close();
				Update();
			};
			popup.Layout.Add(list);
		}
	}
	
	public BiomeBrushSettingsWidgetWindow(Widget parent, SerializedObject so, TerrainBiomesComponent biomes) : base(parent, "Biome Brush Settings")
	{
		Layout = Layout.Column();
		Layout.Margin = 8;
		MaximumWidth = 300.0f;

		var row = Layout.Row();
		
		// Create the brush selection
		row.Add(new BrushSelectedWidget(this));

		// Create the property sheet for the tool
		var controls = Layout.Column();
		{
			var cs = new ControlSheet();
			cs.AddRow(so.GetProperty(nameof(BiomeBrushSettings.AutoRegenerate)));
			cs.AddRow(so.GetProperty(nameof(BiomeBrushSettings.VisualizationOpacity)));
			cs.AddRow(so.GetProperty(nameof(BiomeBrushSettings.Size)));
		
			cs.SetMinimumColumnWidth(0, 50);
			cs.Margin = new Sandbox.UI.Margin(8, 0, 4, 0);
			
			controls.Add(cs);
		}
		row.Add(controls);
		
		Layout.Add(row);
	}
	
}

public class BiomeBrushSettingsInfoWindow : WidgetWindow
{ 
	public BiomeBrushSettingsInfoWindow(Widget parent, string info) : base(parent, "Biome Brush Settings")
	{
		Layout = Layout.Column();
		Layout.Margin = 8;
		Layout.Add(new Label() { Text = info });
	}
}
