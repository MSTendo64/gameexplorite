using ProceduralBiomesTool.Generation;

namespace ProceduralBiomesTool.Rules;


[Title("Filter out steep terrain angles")]
[Description("Removes objects that are on steep terrain.")]
[Group("Terrain")]
[Icon("show_chart")]
public class RemovePointsOnInvalidTerrainLayerRule : EcotopeLayerRule
{
	[Property, Range(0, 90)] public float MinValidAngle { get; set; } = 0f;
	[Property, Range(0, 90)] public float MaxValidAngle { get; set; } = 40f;
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = state.Points.Count - 1; i >= 0; --i)
		{
			var point = state.Points[i];
			var a = point.Position + Vector3.Up * state.Terrain.TerrainHeight;
			var b = point.Position;

			foreach (var hit in state.Terrain.Scene.PhysicsWorld.Trace.Ray(a, b).RunAll())
			{
				if (hit.Body.GetGameObject().GetComponent<Terrain>() == null)
					continue;

				float angle = hit.Normal.Angle(Vector3.Up);
				if (angle < MinValidAngle || angle > MaxValidAngle)
				{
					state.Points.RemoveAt(i);
					break;
				}
			}
		}
	}
}

[Title("Align to terrain")]
[Description("Aligns objects to the terrain.")]
[Group("Terrain")]
[Icon("landscape")]
public class AlignToTerrainLayerRule : EcotopeLayerRule
{
	[Property, Range(0, 1)] public float MinAngleInfluence { get; set; } = 1f;
	[Property, Range(0, 1)] public float MaxAngleInfluence { get; set; } = 1f;
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = state.Points.Count - 1; i >= 0; --i)
		{
			var point = state.Points[i];
			var a = point.Position + Vector3.Up * state.Terrain.TerrainHeight;
			var b = point.Position;
			
			foreach (var hit in state.Terrain.Scene.PhysicsWorld.Trace.Ray(a, b).RunAll())
			{
				if (hit.Body.GetGameObject().GetComponent<Terrain>() == null)
					continue;
				
				var influence = state.RandomState.Float(MinAngleInfluence, MaxAngleInfluence);
				var terrainRotation = (hit.Normal.EulerAngles - new Angles(-90f, 0, 0)).ToRotation();
				
				point.Position = hit.HitPosition;
				point.Rotation = Rotation.Lerp(point.Rotation, terrainRotation, influence);
				break;
			}

			state.Points[i] = point;
		}
	}
}

[Title("Align to physics collision")]
[Description("Aligns objects to physical collisions.")]
[Group("Terrain")]
[Icon("compress")]
public class AlignToPhysicsCollisionLayerRule : EcotopeLayerRule
{
	[Property, Range(0, 1)] public float MinAngleInfluence { get; set; } = 1f;
	[Property, Range(0, 1)] public float MaxAngleInfluence { get; set; } = 1f;
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = state.Points.Count - 1; i >= 0; --i)
		{
			var point = state.Points[i];
			var a = point.Position + Vector3.Up * state.Terrain.TerrainHeight;
			var b = point.Position;
			
			var hit = state.Terrain.Scene.PhysicsWorld.Trace.Ray(a, b).Run();
			if (hit.Hit)
			{
				var influence = state.RandomState.Float(MinAngleInfluence, MaxAngleInfluence);
				var terrainRotation = (hit.Normal.EulerAngles - new Angles(-90f, 0, 0)).ToRotation();

				point.Position = hit.HitPosition;
				point.Rotation = Rotation.Lerp(point.Rotation, terrainRotation, influence);
				state.Points[i] = point;
			}
		}
	}
}

[Title("Remove objects below height")]
[Description("Removes objects below the specified world height.")]
[Group("Terrain")]
[Icon("arrow_downward")]
public class RemovePointsBelowHeightLayerRule : EcotopeLayerRule
{
	[Property] public float Height { get; set; }
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = state.Points.Count - 1; i >= 0; --i)
		{
			var point = state.Points[i];
			if (point.Position.z < Height)
			{
				state.Points.RemoveAt(i);
			}
		}
	}
}

[Title("Remove objects above height")]
[Description("Removes objects above the specified world height.")]
[Group("Terrain")]
[Icon("arrow_upward")]
public class RemovePointsAboveHeightLayerRule : EcotopeLayerRule
{
	[Property] public float Height { get; set; }
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = state.Points.Count - 1; i >= 0; --i)
		{
			var point = state.Points[i];
			if (point.Position.z > Height)
			{
				state.Points.RemoveAt(i);
			}
		}
	}
}
