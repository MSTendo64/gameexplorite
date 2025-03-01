using System;
using System.Threading;
using System.Threading.Tasks;
using ProceduralBiomesTool.Components;
using ProceduralBiomesTool.Generation;

namespace ProceduralBiomesToolEditor;

public class TerrainBiomesComponentTool : EditorTool<TerrainBiomesComponent>
{
	public static TerrainBiomesComponentTool Active { get; private set; }
	
	private TerrainBiomesComponent Target { get; set; }

	private bool generateNextUpdate;
	private BiomeGenerationContext currentGenerationContext;
	
	public override void OnEnabled()
	{
		Active = this;
		
		var window = new WidgetWindow(SceneOverlay, "Terrain Biomes");
		window.Layout = Layout.Column();
		window.Layout.Margin = 16;
		window.Layout.Spacing = 8;

		window.Layout.Add(new Button("Regenerate Biomes", "forest")
		{
			Clicked = () =>
			{
				// @hack: calling GenerateAll from the ui button press makes any Scene trace rules not hit anything, but calling it from the tool update works
				generateNextUpdate = true;
			}
		});
		window.Layout.Add(new Button("Delete All Biome Objects", "remove")
		{
			Clicked = ClearAllGeneratedObjects
		});
		window.Layout.Add(new Button("Erase Biome Map", "delete")
		{
			Clicked = ConfirmClearAll,
			Tint = "#aa0000"
		});
		
		AddOverlay(window, TextFlag.RightTop, 10);
	}
	
	public override void OnDisabled()
	{
		if (Active == this)
		{
			Active = null;
		}

		base.OnDisabled();
	}

	public override void OnUpdate()
	{
		Target = GetSelectedComponent<TerrainBiomesComponent>();
		if (!Target.IsValid())
			return;

		if (Target.DrawExtents)
		{
			DrawExtents();
		}
		if (Target.DrawTiles)
		{
			DrawTiles();
		}
		
		// Reset this here or it messes with some built in gizmos
		Gizmo.Draw.LineThickness = 1f;
		
		if (generateNextUpdate)
		{
			generateNextUpdate = false;
			GenerateAll();
		}
	}
	
	private void DrawExtents()
	{
		var origin = Target.WorldPosition;
		var forward = Target.WorldTransform.Forward;
		var right = -Target.WorldTransform.Right;

		var w = Target.Extents.x;
		var h = Target.Extents.y;
		
		var bl = origin;
		var tl = origin + forward * h;
		var br = origin + right * w;
		var tr = origin + forward * h + right * w;
		
		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.LineThickness = 2f;
		Gizmo.Draw.Line(bl, tl);
		Gizmo.Draw.Line(bl, br);
		Gizmo.Draw.Line(tl, tr);
		Gizmo.Draw.Line(br, tr);
	}

	private void DrawTiles()
	{
		var origin = Target.WorldPosition + Vector3.Up * 50;
		var forward = Target.WorldTransform.Forward;
		var right = -Target.WorldTransform.Right;
		var up = Target.WorldTransform.Up;
		
		var w = Target.Extents.x / Target.Tiles.x;
		var h = Target.Extents.y / Target.Tiles.y;
		
		Gizmo.Draw.LineThickness = 2f;
		
		for (int x = 0; x < Target.Tiles.x; x++)
		{
			for (int y = 0; y < Target.Tiles.y; y++)
			{
				Gizmo.Draw.Color = Color.Black;

				var c = origin
				        + (y + 0.5f) * forward * h
				        + (x + 0.5f) * right * w
				        + Target.Terrain.TerrainHeight * 0.5f * up;
				
				var size = new Vector3(w - 10, h - 10, Target.Terrain.TerrainHeight);

				Gizmo.Transform = new Transform()
				{
					Position = c,
					Rotation = Target.WorldRotation
				};
				Gizmo.Draw.LineBBox(BBox.FromPositionAndSize(Vector3.Zero, size));
			}
		}
		
		Gizmo.Transform = Transform.Zero;
	}

	private void CheckTargetTransformIsValid()
	{
		if (Target.GameObject.WorldRotation.Distance(Rotation.Identity) > 0.001f)
		{
			Log.Error("Terrain is not at (0, 0, 0) rotation, biome generation will not work correctly!");
		}
		if (!Target.GameObject.LocalScale.LengthSquared.AlmostEqual(Vector3.One.LengthSquared))
		{
			Log.Error("Terrain is not at (1, 1, 1) scale, biome generation will not work correctly!");
		}
	}
	
	public void GenerateAll()
	{
		if(!Target.IsValid())
			return;
		
		Target.Setup();
		CheckTargetTransformIsValid();
		
		currentGenerationContext?.Cancel();
		
		var context = new BiomeGenerationContext(Target);
		for (int x = 0; x < Target.Tiles.x; x++)
		{
			for (int y = 0; y < Target.Tiles.y; y++)
			{
				context.AddTile(x, y);
			}
		}
		
		// @note: window will take care of its own setup and destruction in case we click away from the tool, don't add as overlay
		_ = new BiomeGenerationWindow(SceneOverlay)
		{
			GenerationContext = context
		};
		
		context.Generate();
		currentGenerationContext = context;
	}
	
	public void RegenerateTiles(IEnumerable<Vector2Int> dirtyTiles)
	{
		if(!Target.IsValid())
			return;

		CheckTargetTransformIsValid();
		
		var context = new BiomeGenerationContext(Target);
		
		// If any tiles are still to generate in the current generator then add them in to this generation context too
		if (currentGenerationContext != null)
		{
			currentGenerationContext.Cancel();
			foreach (var tile in currentGenerationContext.TilesStillToGenerate)
			{
				context.AddTile(tile);
			}
		}
		
		foreach (var tile in dirtyTiles)
		{
			if (IsTileValid(tile))
			{
				context.AddTile(tile);
			}
		}
		
		// @note: window will take care of its own setup and destruction in case we click away from the tool, don't add as overlay
		_ = new BiomeGenerationWindow(SceneOverlay)
		{
			GenerationContext = context
		};
		
		context.Generate();
		currentGenerationContext = context;
	}
	
	private bool IsTileValid(Vector2Int tile)
	{
		return tile.x >= 0 && tile.x < Target.Tiles.x
		                   && tile.y >= 0 && tile.y < Target.Tiles.y;
	}
	
	private void ClearAllGeneratedObjects()
	{
		if (Target.IsValid() && Target.GeneratedObjectsStore.IsValid())
		{
			Target.GeneratedObjectsStore.DeleteAll();
		}
	}
	
	private void ConfirmClearAll()
	{
		EditorUtility.DisplayDialog("Delete All Biomes Data", 
			"Are you sure you want to remove all biomes data? You can not undo this action.",
			"Cancel",
			"Delete",
			Reset, "delete");
	}

	private void Reset()
	{
		if (Target.IsValid())
		{
			Target.Reset();
		}
	}
	
}

internal class BiomeGenerationContext
{
	private static int idCounter;
	
	private readonly int id;
	private readonly TerrainBiomesComponent biomes;
	private readonly List<Vector2Int> tilesToGenerate = new();
	private EcotopeGeneratorTile[] generatorTiles;

	private CancellationTokenSource cancellationTokenSource;
	
	private readonly HashSet<EcotopeGeneratorTile> tilesStillToGenerate = new();
	
	public bool IsComplete { get; private set; }
	public IEnumerable<Vector2Int> TilesStillToGenerate
	{
		get
		{
			lock (tilesToGenerate)
			{
				return tilesStillToGenerate.Select(x => x.TileLocation);
			}
		}
	}
	public IProgress Progress { get; set; }
	
	public BiomeGenerationContext(TerrainBiomesComponent biomes)
	{
		id = idCounter++;
		this.biomes = biomes;
	}

	public void AddTile(int x, int y)
	{
		tilesToGenerate.Add(new Vector2Int(x, y));
	}
	
	public void AddTile(Vector2Int tile)
	{
		if (!tilesToGenerate.Contains(tile))
		{
			tilesToGenerate.Add(tile);
		}
	}
	
	public void Generate()
	{
		Log.Info($"Starting biome generation context {id}, tiles: {tilesToGenerate.Count}");
		
		generatorTiles = new EcotopeGeneratorTile[tilesToGenerate.Count];
		int ti = 0;
		foreach (var t in tilesToGenerate)
		{
			var generator = biomes.CreateTileGenerator(t.x, t.y);
			generatorTiles[ti++] = generator;
			
			lock (tilesStillToGenerate)
			{
				tilesStillToGenerate.Add(generator);
			}
		}
		
		_ = GenerateAsync();
	}

	public void Cancel(bool clearRemainingList = false)
	{
		if(IsComplete)
			return;
		
		Log.Info($"Cancelling biome generation context {id}");
		
		try
		{
			cancellationTokenSource?.Cancel();
		}
		catch (ObjectDisposedException e)
		{
			Log.Warning(e);
		}

		if (clearRemainingList)
		{
			lock (tilesStillToGenerate)
			{
				tilesStillToGenerate.Clear();
			}
		}
		
		IsComplete = true;
		UpdateProgress();
	}

	private async Task GenerateAsync()
	{
		using var source = new CancellationTokenSource();
		cancellationTokenSource = source; 
		var cancellationToken = cancellationTokenSource.Token;
		
		IsComplete = false;
		
		await generatorTiles.ForEachTaskAsync(x => GenerateTile(x, cancellationToken), 8, cancellationToken);
		if(cancellationToken.IsCancellationRequested) 
			return;
		
		IsComplete = true;
		UpdateProgress();
		cancellationTokenSource = null;
		Log.Info($"Biome generation context {id} finished!");
	}

	private async Task GenerateTile(EcotopeGeneratorTile generator, CancellationToken cancellationToken)
	{
		await biomes.GeneratedObjectsStore.DeleteTileAsync(generator.TileLocation, cancellationToken);
		if(cancellationToken.IsCancellationRequested) 
			return;

		await generator.Generate(cancellationToken);
		
		lock(tilesStillToGenerate)
		{
			tilesStillToGenerate.Remove(generator);
		}
		UpdateProgress();
	}

	private void UpdateProgress()
	{
		lock (tilesStillToGenerate)
		{
			float current = generatorTiles.Length - tilesStillToGenerate.Count;
			Progress?.SetProgress(generatorTiles.Length, current);
		}
	}
}

file class BiomeGenerationWindow : WidgetWindow, IProgress
{
	private BiomeGenerationContext generationContext;
	private ProgressBar progressBar;
	
	public BiomeGenerationContext GenerationContext
	{
		set
		{
			generationContext = value;
			generationContext.Progress = this;
		}
	}
	
	public BiomeGenerationWindow(Widget parent = null) : base(parent, "Generating Biomes")
	{
		MinimumSize = new Vector2(400, 50);
		DeleteOnClose = true;
		
		Layout = Layout.Row();
		Layout.Margin = 10;
		Layout.Spacing = 5;
		
		progressBar = new ProgressBar()
		{
			Progress = 0
		};
		Layout.Add(progressBar);
		
		Layout.Add(new IconButton("cancel")
		{
			ToolTip = "Cancel",
			MouseClick = () =>
			{
				generationContext?.Cancel(true);
			},
		});
		
		AdjustSize();
		AlignToParent(TextFlag.CenterTop, 10);
		Show();
	}

	public void SetProgressMessage(string message)
	{
	}

	public void SetProgress(float total, float current)
	{
		progressBar.Progress = current / MathF.Max(total, 1f);
		Update();
	}

	public override void Update()
	{
		base.Update();

		if (generationContext?.IsComplete ?? true)
		{
			Close();
		}
	}
}

file class ProgressBar : Widget
{
	public float Progress { get; set; } = 0f;

	public ProgressBar()
	{
		MinimumHeight = 22f;
		
		Layout = Layout.Column();
		Layout.AddStretchCell();
	}
	
	protected override void OnPaint()
	{
		const float radius = 2;
		
		base.OnPaint();
		
		var rect = LocalRect.Shrink(0, 0, 1, 1);
		var percent = MathX.Clamp(Progress, 0f, 1f);
		
		Paint.ClearPen();
		Paint.ClearBrush();
		
		Paint.SetBrush(Theme.ControlBackground);
		Paint.DrawRect(rect, radius);

		var fill = rect;
		fill.Width *= MathX.Clamp(Progress, 0f, 1f);
		Paint.SetBrush(Theme.Primary);
		Paint.DrawRect(fill, radius);
		
		Paint.SetPen(Theme.White.WithAlpha(0.3f));
		Paint.ClearBrush();
		Paint.DrawRect(rect, radius);
		
		Paint.SetDefaultFont();
		Paint.SetPen(Color.White.WithAlpha(0.7f));
		Paint.DrawText(rect.Shrink(2), $"{(percent * 100):N0}%", TextFlag.Center);
	}
}
