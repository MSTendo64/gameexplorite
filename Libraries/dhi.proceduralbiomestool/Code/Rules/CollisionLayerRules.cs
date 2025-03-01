using ProceduralBiomesTool.Generation;

namespace ProceduralBiomesTool.Rules;

[Title("Remove objects under overhangs")]
[Description("Remove any objects that are under something solid.")]
[Group("Collision")]
[Icon("arrow_upward")]
public class RemovePointsUnderCeilingsLayerRule : EcotopeLayerRule
{
	[Property] public TagSet WithAnyTags { get; set; } = [];
	[Property] public TagSet WithAllTags { get; set; } = [];
	[Property] public TagSet WithoutTags { get; set; } = ["generated"];

	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = state.Points.Count - 1; i >= 0; --i)
		{
			var point = state.Points[i];
			var origin = point.Position + Vector3.Up * (point.CollisionRadius + 1);
			var destination = point.Position + Vector3.Up * state.Terrain.TerrainHeight;
			
			var trace = state.Terrain.Scene.PhysicsWorld.Trace.Sphere(point.CollisionRadius, origin, destination);
			if (!WithAnyTags.IsEmpty)
			{
				trace = trace.WithAnyTags(WithAnyTags);
			}
			if (!WithAllTags.IsEmpty)
			{
				trace = trace.WithAllTags(WithAllTags);
			}
			if (!WithoutTags.IsEmpty)
			{
				trace = trace.WithoutTags(WithoutTags);
			}
			
			var tr = trace.Run();
			if (tr.Hit)
			{
				state.Points.RemoveAt(i);
			}
		}
	}
}

[Title("Remove objects colliding with solids")]
[Description("Remove any objects that are touching something solid.")]
[Group("Collision")]
[Icon("circle")]
public class RemovePointsTouchingCollisionLayerRule : EcotopeLayerRule
{
	[Property] public TagSet WithAnyTags { get; set; } = [];
	[Property] public TagSet WithAllTags { get; set; } = [];
	[Property] public TagSet WithoutTags { get; set; } = ["generated"];

	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = state.Points.Count - 1; i >= 0; --i)
		{
			var point = state.Points[i];
			var origin = point.Position + Vector3.Up * (point.CollisionRadius + 1);
			
			var trace = state.Terrain.Scene.PhysicsWorld.Trace.Sphere(point.CollisionRadius, origin, origin);
			if (!WithAnyTags.IsEmpty)
			{
				trace = trace.WithAnyTags(WithAnyTags);
			}
			if (!WithAllTags.IsEmpty)
			{
				trace = trace.WithAllTags(WithAllTags);
			}
			if (!WithoutTags.IsEmpty)
			{
				trace = trace.WithoutTags(WithoutTags);
			}
			
			var tr = trace.Run();
			if (tr.Hit)
			{
				state.Points.RemoveAt(i);
			}
		}
	}
}
