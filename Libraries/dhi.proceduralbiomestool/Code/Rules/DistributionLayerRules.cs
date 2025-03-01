using System;
using ProceduralBiomesTool.Generation;
using ProceduralBiomesTool.Utility;
using Sandbox.Utility;

namespace ProceduralBiomesTool.Rules;

[Title("Generate natural distribution")]
[Description("Creates points in the world that assets can spawn at, in a naturalistic distributed pattern.")]
[Group("Distribution")]
[Icon("add_circle")]
public class GeneratePoissonDiscDistributionLayerRule : EcotopeLayerRule
{
	[Property, Range(0, 1)] public float Density { get; set; } = 1f;
	
	private static readonly float Sqrt2 = (float)Math.Sqrt(2f);
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		float discSize = state.AssetReference.Asset.FootprintRadius;
		float maximumJitter = state.AssetReference.Asset.Jitter;
		float density = state.Density * state.AssetReference.Density * Density;
		
		var points = UniformPoissonDiskSampler.SampleRectangle(state.RandomState, Vector2.Zero, state.Extents, discSize);

		// Apply dithering to create a sparser distribution of points
		for (int i = points.Count - 1; i >= 0; --i)
		{
			var point = points[i];
			var px = point.x / discSize;
			var py = point.y / discSize;
			
			int ditherValue = BayerDithering8x8.GetBinaryDitherValue(px, py, density);
			if (ditherValue == 0)
				points.RemoveAt(i);
		}

		var jitterValue = MathX.Lerp(maximumJitter, 0f, density);
		if (jitterValue > 1f)
		{
			// Apply jitter to all points so that the dithering pattern isn't as obvious at low densities
			// Lerp the jitter to zero as the density increases as the pattern becomes less obvious
			for (int i = 0; i < state.Points.Count; ++i)
			{
				var angle = state.RandomState.Float(MathF.PI * 2f);
				var distance = state.RandomState.Float(jitterValue);
				var jitterOffset = (new Vector2(MathF.Sin(angle), MathF.Cos(angle))).Normal * distance;
				points[i] += jitterOffset;
			}

			// Remove colliding discs
			points.RemoveAll(x => points.Any(y => y != x && x.Distance(y) < discSize * Sqrt2));
		}
		
		// Create ecotope points from vector points
		state.AddFromLocalPoints(points);
	}
}

[Title("Generate grid distribution")]
[Description("Creates points in the world that assets can spawn at, in a grid pattern.")]
[Group("Distribution")]
[Icon("grid_on")]
public class GenerateGridDistributionLayerRule : EcotopeLayerRule
{
	[Property] public Vector2 SpacingMultiplier { get; set; } = Vector2.One;
	[Property] public bool AllowJitter { get; set; } = false;
	
	private static readonly float Sqrt2 = (float)Math.Sqrt(2f);
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		var points = new List<Vector3>();
		
		float discSize = state.AssetReference.Asset.FootprintRadius;
		float discWidth = discSize * SpacingMultiplier.x;
		float discHeight = discSize * SpacingMultiplier.y;

		float tx = 1f - ((state.Extents.x / discWidth) % 1f);
		float ty = 1f - ((state.Extents.y / discHeight) % 1f);

		float startX = state.WorldOrigin.x + tx * discWidth;
		float startY = state.WorldOrigin.y + ty * discHeight;
		float w = state.WorldOrigin.x + state.Extents.x;
		float h = state.WorldOrigin.y + state.Extents.y;

		for (float x = startX; x < w; x += discWidth)
		{
			for (float y = startY; y < h; y += discHeight)
			{
				points.Add(new Vector3(x, y, 0));
			}	
		}
		
		if (AllowJitter)
		{
			var jitterValue = state.AssetReference.Asset.Jitter;
			if (jitterValue > 1f)
			{
				// Apply jitter to all points so that the dithering pattern isn't as obvious at low densities
				// Lerp the jitter to zero as the density increases as the pattern becomes less obvious
				for (int i = 0; i < state.Points.Count; ++i)
				{
					var angle = state.RandomState.Float(MathF.PI * 2f);
					var distance = state.RandomState.Float(jitterValue);
					var jitterOffset = (new Vector2(MathF.Sin(angle), MathF.Cos(angle))).Normal * distance;
					points[i] += new Vector3(jitterOffset.x, jitterOffset.y, 0f);
				}

				// Remove colliding discs
				points.RemoveAll(x => points.Any(y => y != x && x.Distance(y) < discSize * Sqrt2));
			}
		}
		
		// Create ecotope points from vector points
		state.AddFromWorldPoints(points);
	}
}

[Title("Generate noise distribution")]
[Description("Creates points in the world that assets can spawn at, in a naturalistic distributed pattern using noise to adjust the density.")]
[Group("Distribution")]
[Icon("add_circle")]
public class GeneratePoissonDiscPerlinDistributionLayerRule : EcotopeLayerRule
{
	[Property, Range(0, 1)] public float Density { get; set; } = 1f;
	[Property] public float NoiseFrequency { get; set; } = 1f;
	[Property] public float NoisePower { get; set; } = 1f;
	
	private static readonly float Sqrt2 = (float)Math.Sqrt(2f);
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		float discSize = state.AssetReference.Asset.FootprintRadius;
		float maximumJitter = state.AssetReference.Asset.Jitter;
		float density = state.Density * state.AssetReference.Density * Density;

		var noise = Noise.SimplexField(new Noise.Parameters()
		{
			Frequency = NoiseFrequency,
			Seed = state.Seed
		});
		
		var points = UniformPoissonDiskSampler.SampleRectangle(state.RandomState, Vector2.Zero, state.Extents, discSize);

		// Apply dithering to create a sparser distribution of points
		for (int i = points.Count - 1; i >= 0; --i)
		{
			var point = points[i];
			var nx = point.x / discSize;
			var ny = point.y / discSize;

			float noiseValue = MathF.Pow(noise.Sample(nx, ny), NoisePower);
			int ditherValue = BayerDithering8x8.GetBinaryDitherValue(nx, ny, noiseValue * density);
			if (ditherValue == 0)
				points.RemoveAt(i);
		}

		var jitterValue = MathX.Lerp(maximumJitter, 0f, density);
		if (jitterValue > 1f)
		{
			// Apply jitter to all points so that the dithering pattern isn't as obvious at low densities
			// Lerp the jitter to zero as the density increases as the pattern becomes less obvious
			for (int i = 0; i < state.Points.Count; ++i)
			{
				var angle = state.RandomState.Float(MathF.PI * 2f);
				var distance = state.RandomState.Float(jitterValue);
				var jitterOffset = (new Vector2(MathF.Sin(angle), MathF.Cos(angle))).Normal * distance;
				points[i] += jitterOffset;
			}

			// Remove colliding discs
			points.RemoveAll(x => points.Any(y => y != x && x.Distance(y) < discSize * Sqrt2));
		}
		
		// Create ecotope points from vector points
		state.AddFromLocalPoints(points);
	}
}

[Title("Remove randomly by percentage")]
[Description("Removes points randomly based on a set percentage.")]
[Group("Distribution")]
[Icon("remove_circle")]
public class RemoveRandomPercentageLayerRule : EcotopeLayerRule
{
	[Property, Range(0f, 1f)] public float RemovePercentage { get; set; } = 0.5f;
	
	public override void Execute(EcotopeLayerAssetGeneratorState state)
	{
		for (int i = state.Points.Count - 1; i >= 0; --i)
		{
			if (state.RandomState.Float() < RemovePercentage)
			{
				state.Points.RemoveAt(i);
			}
		}
	}
}
