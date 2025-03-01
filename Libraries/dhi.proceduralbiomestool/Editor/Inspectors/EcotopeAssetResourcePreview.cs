using System;
using System.Threading.Tasks;
using Editor.Assets;
using ProceduralBiomesTool.Resources;
using FileSystem = Editor.FileSystem;

namespace ProceduralBiomesToolEditor;

[AssetPreview("ecoasset")]
public class EcotopeAssetResourcePreview : AssetPreview
{
	public override float PreviewWidgetCycleSpeed => 0.02f;
	public override bool UsePixelEvaluatorForThumbs => true;

	private int modelsHash;
	
	private Gizmo.Instance gizmoSceneInstance;
	private EcotopeAssetResource ecotopeAsset;
	private EcotopeAssetResourcePreviewSettings previewSettings = new();
	private EcotopeAssetPreviewToolbar previewToolbar;
	
	public EcotopeAssetResourcePreview(Asset asset) : base(asset)
	{
	}
	
	public override Task InitializeAsset()
	{
		var settings = FileSystem.Config.ReadJson<EcotopeAssetResourcePreviewSettings>(EcotopeAssetResourcePreviewSettings.SettingsFile);
		if (settings != null)
		{
			previewSettings = settings;
		}
	
		Rebuild();
		
		return Task.CompletedTask;
	}

	public override Task RenderToPixmap(Pixmap pixmap)
	{
		// @note: when rendering the preview icon then remove the gizmos world first so that the gizmos don't get rendered into the icon
		Camera.Worlds.Remove(gizmoSceneInstance.World);
		Camera.RenderToPixmap(pixmap);
		Camera.Worlds.Add(gizmoSceneInstance.World);
		return Task.CompletedTask;
	}

	private void Rebuild()
	{
		if (PrimarySceneObject.IsValid())
		{
			PrimarySceneObject.Delete();
		}
		
		ecotopeAsset = Asset.LoadResource<EcotopeAssetResource>();
		if (!ecotopeAsset.IsValid())
			return;
		
		var model = GetDefaultModel();
		PrimarySceneObject = new SceneObject(World, model, Transform.Zero);
		SceneSize = model.RenderBounds.Size;
		SceneCenter = model.RenderBounds.Center;

		if (gizmoSceneInstance != null)
		{
			Camera.Worlds.Remove(gizmoSceneInstance.World);
		}
		gizmoSceneInstance ??= new Gizmo.Instance();
		Camera.Worlds.Add(gizmoSceneInstance.World);
	}

	private Model GetDefaultModel()
	{
		var model = ecotopeAsset.Models.FirstOrDefault(x => x != null);
		if (!model.IsValid())
		{
			model = Model.Cube;
		}
		return model;
	}
	
	private int GetModelsHash()
	{
		if (!ecotopeAsset.IsValid())
		{
			ecotopeAsset = Asset.LoadResource<EcotopeAssetResource>();
		}
		if (ecotopeAsset.IsValid() && ecotopeAsset.Models != null)
		{
			int hash = ecotopeAsset.Models.Length;
			foreach (var model in ecotopeAsset.Models)
			{
				hash += model?.GetHashCode() ?? 0;
			}
			return hash;
		}
		return 0;
	}
	
	public override void UpdateScene(float cycle, float timeStep)
	{
		base.UpdateScene(cycle, timeStep);
		
		int newHash = GetModelsHash();
		if (newHash != modelsHash)
		{
			Rebuild();
			if (previewToolbar.IsValid())
			{
				previewToolbar.Refresh();
			}
			modelsHash = newHash;
		}
		
		if(ecotopeAsset == null)
			return;

		using(gizmoSceneInstance?.Push())
		{
			var t = Gizmo.Transform;
			t.Rotation = new Angles(90, 0, 0);
			Gizmo.Transform = t;
			{
				if (previewSettings.ShowFootprintGizmo)
				{
					Gizmo.Draw.Color = previewSettings.FootprintGizmoColor;
					Gizmo.Draw.LineCircle(Vector3.Zero, ecotopeAsset.FootprintRadius, sections: 32);
				}
				if (previewSettings.ShowCollisionGizmo)
				{
					Gizmo.Draw.Color = previewSettings.CollisionGizmoColor;
					Gizmo.Draw.SolidCircle(Vector3.Zero, ecotopeAsset.CollisionRadius, sections: 32);
				}
				if (previewSettings.ShowJitterGizmo)
				{
					Gizmo.Draw.Color = previewSettings.JitterGizmoColor;
					Gizmo.Draw.LineCircle(Vector3.Zero, ecotopeAsset.Jitter, sections: 32);
				}
			}
			Gizmo.Transform = Transform.Zero;
		}
	}
	
	public override Widget CreateToolbar()
	{
		previewToolbar = new EcotopeAssetPreviewToolbar(ecotopeAsset, previewSettings);
		previewToolbar.ChangeModel += OnChangePreviewModel;
		return previewToolbar;
	}

	private void OnChangePreviewModel(int i)
	{
		if (i < 0 || i >= ecotopeAsset.Models.Length)
			return;
		
		var model = ecotopeAsset.Models[i];
		if (model is null)
			return;
		
		PrimarySceneObject.Delete();
		PrimarySceneObject = new SceneObject(World, model, Transform.Zero);
		
		SceneSize = model.RenderBounds.Size;
		SceneCenter = model.RenderBounds.Center;
	}
	
}

internal class EcotopeAssetResourcePreviewSettings
{
	public const string SettingsFile = "ecotope_asset_preview.json";
	
	public bool ShowFootprintGizmo { get; set; } = true;
	public Color FootprintGizmoColor { get; set; } = Color.Yellow;
	
	public bool ShowCollisionGizmo { get; set; } = true;
	public Color CollisionGizmoColor { get; set; } = Color.Red;

	public bool ShowJitterGizmo { get; set; } = true;
	public Color JitterGizmoColor { get; set; } = Color.Cyan;

	public void Reset()
	{
		ShowFootprintGizmo = true;
		FootprintGizmoColor = Color.Yellow;
		ShowCollisionGizmo = true;
		CollisionGizmoColor = Color.Red;
		ShowJitterGizmo = true;
		JitterGizmoColor = Color.Cyan;
	}
	
	public void Save()
	{
		FileSystem.Config.WriteJson(SettingsFile, this);
	}
}

internal class EcotopeAssetPreviewToolbar : Widget
{
	private EcotopeAssetResource ecotopeAssetResource;
	private EcotopeAssetResourcePreviewSettings previewSettings;
	
	public Action<int> ChangeModel;
	
	public EcotopeAssetPreviewToolbar(EcotopeAssetResource ecotopeAssetResource, EcotopeAssetResourcePreviewSettings settings)
	{
		this.ecotopeAssetResource = ecotopeAssetResource;
		previewSettings = settings;
		
		Layout = Layout.Row();
		Layout.Margin = 4;
		Layout.Spacing = 4;
		
		Layout.AddSpacingCell(400);
		
		Build();
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush(Theme.WindowBackground);
		Paint.DrawRect(Layout.OuterRect, 0);
		
		base.OnPaint();
	}

	private void Build()
	{
		Layout.Clear(true);
		
		{
			var btn = new IconButton("settings");
			btn.Layout = Layout.Row();
			btn.MinimumSize = 16;
			btn.MouseLeftPress = () => OpenSettings(btn);
			Layout.Add(btn);
		}
		{
			var btn = new IconButton("🦶");
			btn.MinimumSize = 16;
			btn.MouseLeftPress = ToggleFootprintGizmo;
			btn.ToolTip = "Toggle footprint radius gizmo";
			btn.IsToggle = true;
			btn.IsActive = previewSettings.ShowFootprintGizmo;
			Layout.Add(btn);
		}
		{
			var btn = new IconButton("💥");
			btn.MinimumSize = 16;
			btn.MouseLeftPress = ToggleCollisionGizmo;
			btn.ToolTip = "Toggle collision radius gizmo";
			btn.IsToggle = true;
			btn.IsActive = previewSettings.ShowCollisionGizmo;
			Layout.Add(btn);
		}
		{
			var btn = new IconButton("☕");
			btn.MinimumSize = 16;
			btn.MouseLeftPress = ToggleJitterGizmo;
			btn.ToolTip = "Toggle jitter radius gizmo";
			btn.IsToggle = true;
			btn.IsActive = previewSettings.ShowJitterGizmo;
			Layout.Add(btn);
		}

		if (ecotopeAssetResource.Models?.Any(x => x.IsValid()) ?? false)
		{
			Layout.AddSeparator();

			for (int i = 0; i < ecotopeAssetResource.Models.Length; ++i)
			{
				var model = ecotopeAssetResource.Models[i];
				if (!model.IsValid())
					continue;

				int modelIdx = i;
				var btn = new Button($"{i + 1}");
				btn.MinimumSize = new Vector2(24, 16);
				btn.MouseClick = () => ChangeModel(modelIdx);
				btn.ToolTip = $"Show model {i + 1}";
				Layout.Add(btn);
			}
		}
	}
	
	private void ToggleFootprintGizmo()
	{
		previewSettings.ShowFootprintGizmo = !previewSettings.ShowFootprintGizmo;
		previewSettings.Save();
	}

	private void ToggleCollisionGizmo()
	{
		previewSettings.ShowCollisionGizmo = !previewSettings.ShowCollisionGizmo;
		previewSettings.Save();
	}

	private void ToggleJitterGizmo()
	{
		previewSettings.ShowJitterGizmo = !previewSettings.ShowJitterGizmo;
		previewSettings.Save();
	}

	public void Refresh()
	{
		Build();
	}
	
	public void OpenSettings(Widget parent)
	{
		var popup = new PopupWidget(parent);
		popup.IsPopup = true;
		popup.Layout = Layout.Column();
		popup.Layout.Margin = 16;
		popup.MaximumWidth = 300;

		var ps = new ControlSheet();
		ps.AddProperty(previewSettings, x => x.CollisionGizmoColor);
		ps.AddProperty(previewSettings, x => x.FootprintGizmoColor);
		ps.AddProperty(previewSettings, x => x.JitterGizmoColor);
		popup.Layout.Add(ps);

		var save = new Button("Save");
		save.MouseLeftPress = () => previewSettings.Save();
		popup.Layout.Add(save);
		
		var reset = new Button("Reset to Defaults");
		reset.MouseLeftPress = ResetSettings;
		popup.Layout.Add(reset);
		
		popup.Show();
		popup.Position = parent.ScreenRect.TopRight - popup.Size;
		popup.ConstrainToScreen();
	}
	
	private void ResetSettings()
	{
		previewSettings.Reset();
		Refresh();
	}
	
}
