using System;
using ProceduralBiomesTool.Generation;
using Sandbox.Utility;

namespace ProceduralBiomesTool.Rules;

[Title("Apply random yaw")]
[Description("Apply a random rotation around the yaw axis to objects.")]
[Group("Transformation")]
[Icon("refresh")]
public class ApplyRandomYawLayerRule : EcotopeLayerRule
{
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = 0; i < state.Points.Count; ++i)
		{
			var p = state.Points[i];
			p.Rotation *= Rotation.FromYaw(state.RandomState.Float(0f, 360f));
			state.Points[i] = p;
		}
	}
}

[Title("Apply random scale")]
[Description("Apply a random scale to objects.")]
[Group("Transformation")]
[Icon("zoom_out_map")]
public class ApplyRandomScaleLayerRule : EcotopeLayerRule
{
	[Property] public RangedFloat ScaleRange { get; set; } = new RangedFloat(0.9f, 1.1f);
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = 0; i < state.Points.Count; ++i)
		{
			var p = state.Points[i];
			p.Scale = state.RandomState.Float(ScaleRange.Min, ScaleRange.Max);
			p.FootprintRadius *= p.Scale;
			p.CollisionRadius *= p.Scale;
			state.Points[i] = p;
		}
	}
}

[Title("Apply random tint")]
[Description("Apply a random tint to objects.")]
[Group("Transformation")]
[Icon("palette")]
public class ApplyRandomTintLayerRule : EcotopeLayerRule
{
	[Property] public Gradient Tint { get; set; } = Color.White;
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = 0; i < state.Points.Count; ++i)
		{
			var p = state.Points[i];
			p.Tint = Tint.Evaluate(state.RandomState.Float());
			state.Points[i] = p;
		}
	}
}

[Title("Apply noise scale")]
[Description("Apply a scale based on noise to objects.")]
[Group("Transformation")]
[Icon("zoom_out_map")]
public class ApplyNoiseScaleLayerRule : EcotopeLayerRule
{
	[Property] public RangedFloat ScaleRange { get; set; } = new RangedFloat(0.9f, 1.1f);
	[Property] public float NoiseFrequency { get; set; } = 1f;
	[Property] public float NoisePower { get; set; } = 1f;
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		float discSize = state.AssetReference.Asset.FootprintRadius;
		var noise = Noise.SimplexField(new Noise.Parameters()
		{
			Frequency = NoiseFrequency,
			Seed = state.Seed
		});
		
		for (int i = 0; i < state.Points.Count; ++i)
		{
			var p = state.Points[i];
			var nx = p.Position.x / discSize;
			var ny = p.Position.y / discSize;
			var value = MathF.Pow(noise.Sample(nx, ny), NoisePower);
			
			p.Scale = MathX.Lerp(ScaleRange.Min, ScaleRange.Max, value);
			p.FootprintRadius *= p.Scale;
			p.CollisionRadius *= p.Scale;
			state.Points[i] = p;
		}
	}
}

[Title("Apply noise tint")]
[Description("Apply a tint based on noise to objects.")]
[Group("Transformation")]
[Icon("palette")]
public class ApplyNoiseTintLayerRule : EcotopeLayerRule
{
	[Property] public Gradient Tint { get; set; } = Color.White;
	[Property] public float NoiseFrequency { get; set; } = 1f;
	[Property] public float NoisePower { get; set; } = 1f;
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		float discSize = state.AssetReference.Asset.FootprintRadius;
		var noise = Noise.SimplexField(new Noise.Parameters()
		{
			Frequency = NoiseFrequency,
			Seed = state.Seed
		});
		
		for (int i = 0; i < state.Points.Count; ++i)
		{
			var p = state.Points[i];
			var nx = p.Position.x / discSize;
			var ny = p.Position.y / discSize;
			var value = MathF.Pow(noise.Sample(nx, ny), NoisePower);
			
			p.Tint = Tint.Evaluate(value);
			state.Points[i] = p;
		}
	}
}

[Title("Apply noise yaw")]
[Description("Apply a yaw to objects based on generated noise.")]
[Group("Transformation")]
[Icon("refresh")]
public class ApplyNoiseYawLayerRule : EcotopeLayerRule
{
	[Property] public float NoiseFrequency { get; set; } = 1f;
	[Property] public float NoisePower { get; set; } = 1f;
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		float discSize = state.AssetReference.Asset.FootprintRadius;
		var noise = Noise.SimplexField(new Noise.Parameters()
		{
			Frequency = NoiseFrequency,
			Seed = state.Seed
		});
		
		for (int i = 0; i < state.Points.Count; ++i)
		{
			var p = state.Points[i];
			var nx = p.Position.x / discSize;
			var ny = p.Position.y / discSize;
			var value = MathF.Pow(noise.Sample(nx, ny), NoisePower);
			
			p.Rotation = Rotation.FromYaw(value * 360f);
			p.FootprintRadius *= p.Scale;
			p.CollisionRadius *= p.Scale;
			state.Points[i] = p;
		}
	}
}

[Title("Round yaw to nearest value")]
[Description("Round the yaw rotation to the nearest.")]
[Group("Transformation")]
[Icon("start")]
public class RoundYawLayerRule : EcotopeLayerRule
{
	[Property] public float RoundToNearest { get; set; }
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = 0; i < state.Points.Count; ++i)
		{
			var p = state.Points[i];

			float yaw = p.Rotation.Yaw();
			yaw = MathF.Round(yaw / RoundToNearest) * RoundToNearest;
			
			p.Rotation = Rotation.FromYaw(yaw);
			p.FootprintRadius *= p.Scale;
			p.CollisionRadius *= p.Scale;
			state.Points[i] = p;
		}
	}
}
