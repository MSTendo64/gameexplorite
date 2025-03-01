using System;
using Editor.TerrainEditor;
using ProceduralBiomesTool.Components;
using Application = Editor.Application;

namespace ProceduralBiomesToolEditor;

[EditorTool]
[Title("Biomes")]
[Description("Paint ecotopes onto the terrain to create natural looking biomes quickly")]
[Icon("forest")]
[Alias("biomes")]
[Group("9")]
public class BiomePainterTool : EditorTool
{
	public static int BiomeValue { get; set; } = 0;
	public static BrushList BrushList { get; set; } = new();
	public static Brush Brush => BrushList.Selected;

	private TerrainBiomesComponent biomes;
	private Terrain terrain;
	
	private bool isDragging;
	private RectInt dirtyRegion;
	private HashSet<Vector2Int> dirtyTiles = new();

	private BiomeBrushSettings brushSettings = new();
	private BiomeBrushSettingsWidgetWindow brushSettingsWindow;
		
	private BrushPreviewSceneObject brushPreviewObject;
	private Transform? brushTransform;

	private Texture biomeTypeVisualizationTexture;
	private Color[] biomeVisualizationColors;
	private Color32[] biomeVisualizationMap;
	private BiomeVisualizationSceneObject biomeVisualizationOverlayObject;

	private static bool forceRefresh;
	
	public override void OnEnabled()
	{
		AllowGameObjectSelection = false;

		biomes = GetSelectedComponent<TerrainBiomesComponent>();
		
		// Select the first available biomes component if one wasn't selected already
		if (!biomes.IsValid())
		{
			Selection.Clear();
			biomes = Scene.GetAllComponents<TerrainBiomesComponent>().FirstOrDefault();
			if (biomes.IsValid())
			{
				Selection.Add(biomes.GameObject);
			}
		}

		// Force update the visualization texture when opening the paint tool so that we know what we're painting
		if (biomes.IsValid())
		{
			biomes.Setup();
			terrain = biomes.Terrain;
			RefreshVisualizationTexture();
		}
		else
		{
			// Can't run the tool if we don't have any biomes
			AddOverlay(new BiomeBrushSettingsInfoWindow(SceneOverlay, "Could not find a Terrain Biomes component, can't use biomes tool!"), TextFlag.RightBottom, 10);
			return;
		}

		// Build the regular brush settings window
		brushSettingsWindow = new BiomeBrushSettingsWidgetWindow(SceneOverlay, EditorUtility.GetSerializedObject(this.brushSettings), biomes);
		AddOverlay(brushSettingsWindow, TextFlag.RightBottom, 10);
		
		biomes.OnReset += OnBiomeComponentReset;
	}
	
	public override void OnDisabled()
	{
		if (biomes.IsValid())
		{
			biomes.OnReset -= OnBiomeComponentReset;
		}

		brushPreviewObject?.Delete();
		brushPreviewObject = null;
		
		biomeVisualizationOverlayObject?.Delete();
		biomeVisualizationOverlayObject = null;

		brushSettingsWindow = null;
	}
	
	private void OnBiomeComponentReset()
	{
		RefreshVisualizationTexture();
	}

	public static void ForceRefresh()
	{
		forceRefresh = true;
	}

	private void RefreshVisualizationTexture()
	{
		if (!biomes.IsValid())
			return;

		int resolution = biomes.Resolution;
		
		// Since we're limiting the max biomes to a byte, just fill the array with the default value since it makes the lookup easier
		biomeVisualizationColors = new Color[TerrainBiomesComponent.MaxBiomes];
		for (int i = 0; i < TerrainBiomesComponent.MaxBiomes; ++i)
		{
			biomeVisualizationColors[i] = Color.Transparent;
		}
		for (int i = 0; i < biomes.Ecotopes.Count; ++i)
		{
			// @note: +1 as we want to reserve 0 as no ecotope
			if (biomes.Ecotopes[i].IsValid())
			{
				biomeVisualizationColors[i + 1] = biomes.Ecotopes[i].Resource.VisualizationColor;
			}
		}
		
		// Build the map that we'll fill the texture from
		if (biomeVisualizationMap == null || biomeVisualizationMap.Length != resolution * resolution)
		{
			biomeVisualizationMap = new Color32[resolution * resolution];
		}
		
		for (int x = 0; x < resolution; ++x)
		{
			for (int y = 0; y < resolution; ++y)
			{
				int index = y * resolution + x;
				var value = biomes.BiomeTypeMapData[index];
				biomeVisualizationMap[index] = biomeVisualizationColors[value];
			}
		}
		
		// Create the visualization texture
		biomeTypeVisualizationTexture?.Dispose();
		biomeTypeVisualizationTexture = Texture.Create(resolution, resolution)
			.WithUAVBinding()
			.WithName("terrain_biome_type_visualization")
			.Finish();
		
		biomeTypeVisualizationTexture.Update(biomeVisualizationMap, 0, 0, resolution, resolution);
	}
	
	public override void OnUpdate()
	{
		base.OnUpdate();
		
		// Make sure both the biomes and terrain components still exist
		if (!biomes.IsValid() || !terrain.IsValid())
			return;

		if (forceRefresh)
		{
			RefreshVisualizationTexture();
			forceRefresh = false;
		}
		
		UpdateBrushPreview();
		UpdateVisualizationOverlay(); // Update the visualization overlay so that we can see which biomes are painted where
		UpdatePainting();
	}

	private void UpdateBrushPreview()
	{
		// Update the paint brush preview
		if (Gizmo.HasMouseFocus)
		{
			if (terrain.RayIntersects(Gizmo.CurrentRay, Gizmo.RayDepth, out var hitPosition))
			{
				brushTransform = new Transform(terrain.WorldPosition + hitPosition);
			}
			else
			{
				brushTransform = default;
			}
		}
		if (brushTransform.HasValue)
		{
			DrawBrushPreview(brushTransform.Value);
		}
	}
	
	private void DrawBrushPreview(Transform transform)
	{
		brushPreviewObject ??= new BrushPreviewSceneObject(Gizmo.World);

		var color = Color.FromBytes(150, 150, 250);
		if (Application.KeyboardModifiers.HasFlag(Sandbox.KeyboardModifiers.Ctrl))
		{
			color = color.AdjustHue(90);
		}

		brushPreviewObject.RenderLayer = SceneRenderLayer.OverlayWithDepth;
		brushPreviewObject.Bounds = BBox.FromPositionAndSize(0, float.MaxValue);
		brushPreviewObject.Transform = transform;
		brushPreviewObject.Radius = brushSettings.Size;
		brushPreviewObject.Texture = Brush.Texture;
		brushPreviewObject.Color = color;
	}
	
	private void UpdateVisualizationOverlay()
	{
		biomeVisualizationOverlayObject ??= new BiomeVisualizationSceneObject(Gizmo.World);

		var center = terrain.WorldPosition
		             + (terrain.WorldTransform.Forward * terrain.TerrainSize * 0.5f * terrain.WorldScale.y)
		             + (-terrain.WorldTransform.Right * terrain.TerrainSize * 0.5f * terrain.WorldScale.x);
		var size = new Vector3(terrain.TerrainSize * terrain.WorldScale.x, terrain.TerrainSize * terrain.WorldScale.y, terrain.TerrainHeight);
		
		biomeVisualizationOverlayObject.RenderLayer = SceneRenderLayer.OverlayWithDepth;
		biomeVisualizationOverlayObject.Bounds = BBox.FromPositionAndSize(0, float.MaxValue);
		biomeVisualizationOverlayObject.Transform = new Transform(center, terrain.WorldRotation, 1f);
		biomeVisualizationOverlayObject.Texture = biomeTypeVisualizationTexture;
		biomeVisualizationOverlayObject.Size = size;
		biomeVisualizationOverlayObject.Color = Color.White;
		biomeVisualizationOverlayObject.Opacity = brushSettings.VisualizationOpacity;
	}

	private void UpdatePainting()
	{
		if (!terrain.RayIntersects(Gizmo.CurrentRay, Gizmo.RayDepth, out var hitPosition))
			return;

		if (Gizmo.IsLeftMouseDown)
		{
			bool shouldSculpt = !isDragging || !Application.CursorDelta.IsNearZeroLength;

			if (!isDragging)
			{
				isDragging = true;

				var uv = new Vector2(hitPosition.x, hitPosition.y) / terrain.Storage.TerrainSize;
				var x = (int)Math.Floor(terrain.Storage.Resolution * uv.x);
				var y = (int)Math.Floor(terrain.Storage.Resolution * uv.y);
				dirtyRegion = new(new Vector2Int(x, y));
			}

			if (shouldSculpt)
			{
				BiomePaintParameters parameters = new()
				{
					HitPosition = hitPosition,
					HitUV = new Vector2(hitPosition.x, hitPosition.y) / terrain.Storage.TerrainSize,
					FlattenHeight = hitPosition.z / terrain.Storage.TerrainHeight,
					Brush = Brush,
					BrushSettings = brushSettings
				};

				OnPaint(parameters);
			}
		}
		else if (isDragging)
		{
			isDragging = false;
			OnPaintEnded();
		}
	}
	
	public void OnPaint(BiomePaintParameters paint)
	{
		int biomeValue = Math.Clamp(BiomeValue, 0, TerrainBiomesComponent.MaxBiomes - 1);
		
		int size = (int)Math.Floor(paint.BrushSettings.Size * 2.0f / terrain.Storage.TerrainSize * biomes.Resolution);
		size = Math.Max(1, size);
		
		var cs = new ComputeShader("cs_terrain_biomes");

		// Set the paint values
		cs.Attributes.Set("BrushStrength", 1 * (Gizmo.IsCtrlPressed ? -1.0f : 1.0f)); // @note: don't want to deal with blending with biomes, just use absolutes
		cs.Attributes.Set("BrushSize", size);
		cs.Attributes.Set("Brush", paint.Brush.Texture);
		cs.Attributes.Set("HeightUV", paint.HitUV);

		// Set which biome we're painting
		cs.Attributes.Set("BiomeMap", biomes.BiomeTypeMapTexture);
		cs.Attributes.Set("BiomeValue", (float)biomeValue / TerrainBiomesComponent.MaxBiomes);
		cs.Attributes.Set("DefaultBiome", 0f);

		// Set which color we're painting into the visualization
		cs.Attributes.Set("VisualizationTexture", biomeTypeVisualizationTexture);
		cs.Attributes.Set("VisualizationColor", biomeVisualizationColors[biomeValue]);
		cs.Attributes.Set("DefaultColor", Color.Transparent);

		var x = (int)Math.Floor(biomes.Resolution * paint.HitUV.x) - size / 2;
		var y = (int)Math.Floor(biomes.Resolution * paint.HitUV.y) - size / 2;
		
		cs.Dispatch(size, size, 1);

		// Grow the dirty region (+1 to be conservative of the floor) 
		dirtyRegion.Add(new RectInt(x, y, size + 1, size + 1));

		for (int ax = -1; ax <= 1; ax++)
		{
			for (int ay = -1; ay <= 1; ay++)
			{
				var p = paint.HitPosition + new Vector3(ax * paint.BrushSettings.Size, ay * paint.BrushSettings.Size, 0f);
				dirtyTiles.Add(biomes.WorldPositionToTile(p));
			}	
		}
	}

	protected virtual void OnPaintEnded()
	{
		// Clamp our dirty region within the bounds of the terrain
		dirtyRegion.Left = Math.Clamp(dirtyRegion.Left, 0, biomes.Resolution - 1);
		dirtyRegion.Right = Math.Clamp(dirtyRegion.Right, 0, biomes.Resolution - 1);
		dirtyRegion.Top = Math.Clamp(dirtyRegion.Top, 0, biomes.Resolution - 1);
		dirtyRegion.Bottom = Math.Clamp(dirtyRegion.Bottom, 0, biomes.Resolution - 1);

		var regionBefore = CopyRegion(biomes.BiomeTypeMapData, biomes.Resolution, dirtyRegion);
		biomes.SyncGpuToCpu(dirtyRegion);
		var regionAfter = CopyRegion(biomes.BiomeTypeMapData, biomes.Resolution, dirtyRegion);
		
		SceneEditorSession.Active.UndoSystem.Insert($"Biome Painting",
			CreateUndoAction(biomes, biomes.BiomeTypeMapData, regionBefore, dirtyRegion),
			CreateUndoAction(biomes, biomes.BiomeTypeMapData, regionAfter, dirtyRegion));
		
		if (brushSettings.AutoRegenerate)
		{
			TerrainBiomesComponentTool.Active?.RegenerateTiles(dirtyTiles);
		}
		dirtyTiles.Clear();
		RefreshVisualizationTexture();
	}

	private T[] CopyRegion<T>(T[] data, int stride, RectInt rect) where T : unmanaged
	{
		T[] region = new T[rect.Width * rect.Height];
		for (int y = 0; y < rect.Height; y++)
		{
			for (int x = 0; x < rect.Width; x++)
			{
				region[x + y * rect.Width] = data[rect.Left + x + (rect.Top + y) * stride];
			}
		}
		
		return region;
	}

	private Action CreateUndoAction<T>(TerrainBiomesComponent biomes, T[] dest, T[] region, RectInt dirtyRect) => () =>
	{
		if (!biomes.IsValid())
			return;

		for (int y = 0; y < dirtyRect.Height; y++)
		{
			for (int x = 0; x < dirtyRect.Width; x++)
			{
				dest[dirtyRect.Left + x + (dirtyRect.Top + y) * biomes.Resolution] = region[x + y * dirtyRect.Width];
			}
		}
		
		biomes.SyncCpuToGpu();
	};
	
}

public struct BiomePaintParameters
{
	public Vector3 HitPosition { get; set; }
	public Vector2 HitUV { get; set; }
	public float FlattenHeight { get; set; }
	public Brush Brush { get; set; }
	public BiomeBrushSettings BrushSettings { get; set; }
}

internal class BrushPreviewSceneObject : SceneCustomObject
{
	public Texture Texture { get; set; }
	public float Radius { get; set; } = 16.0f;
	public Color Color { get; set; } = Color.White;

	public BrushPreviewSceneObject(SceneWorld world) : base(world)
	{
		RenderLayer = SceneRenderLayer.Default;
	}

	public override void RenderSceneObject()
	{
		var material = Material.FromShader("shaders/terrain_brush.shader");

		VertexBuffer buffer = new();
		buffer.Init(true);
		buffer.AddCube(Vector3.Zero, Vector3.One * Radius * 6, Rotation.Identity);

		RenderAttributes attributes = new RenderAttributes();
		attributes.Set("Brush", Texture);
		attributes.Set("Radius", Radius);
		attributes.Set("Color", Color);

		Graphics.GrabDepthTexture("DepthBuffer", attributes, false);
		
		buffer.Draw(material, attributes);
	}
}

internal class BiomeVisualizationSceneObject : SceneCustomObject
{
	public Texture Texture { get; set; }
	public Vector3 Size { get; set; }
	public Color Color { get; set; } = Color.White;
	public float Opacity { get; set; }
	
	public BiomeVisualizationSceneObject(SceneWorld world) : base(world)
	{
		RenderLayer = SceneRenderLayer.Default;
	}

	public override void RenderSceneObject()
	{
		var material = Material.FromShader("shaders/terrain_biome_overlay.shader");
		
		VertexBuffer buffer = new();
		buffer.Init(true);
		buffer.AddCube(Vector3.Zero, Size, Rotation.Identity);

		RenderAttributes attributes = new RenderAttributes();
		attributes.Set("Texture", Texture);
		attributes.Set("Radius", Size.x * 0.5f);
		attributes.Set("Opacity", Opacity);

		Graphics.GrabDepthTexture("DepthBuffer", attributes, false);
		
		buffer.Draw(material, attributes);
	}
}

