using System;

namespace ProceduralBiomesTool.Utility;

// Adapted from https://theinstructionlimit.com/fast-uniform-poisson-disk-sampling-in-c
// > Adapated from java source by Herman Tulleken
// > http://www.luma.co.za/labs/2008/02/27/poisson-disk-sampling/
// > The algorithm is from the "Fast Poisson Disk Sampling in Arbitrary Dimensions" paper by Robert Bridson
// > http://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf

public static class UniformPoissonDiskSampler
{
	private const int DefaultPointsPerIteration = 30;
	private static readonly float Sqrt2 = (float)Math.Sqrt(2f);

	private struct Settings
	{
		public Vector2 TopLeft, LowerRight, Center;
		public Vector2 Dimensions;
		public bool HasRejectionDistance;
		public float? RejectionSqDistance;
		public float MinimumDistance;
		public float CellSize;
		public int GridWidth, GridHeight;
	}

	private struct State
	{
		public Random RandomState;
		public Vector2?[,] Grid;
		public List<Vector2> ActivePoints, Points;
	}
	
	public static List<Vector2> SampleCircle(Random randomState, Vector2 center, float radius, float minimumDistance, int pointsPerIteration = DefaultPointsPerIteration)
	{
		return Sample(randomState, center - new Vector2(radius), center + new Vector2(radius), radius, minimumDistance, pointsPerIteration);
	}
	
	public static List<Vector2> SampleRectangle(Random randomState, Vector2 topLeft, Vector2 lowerRight, float minimumDistance, int pointsPerIteration = DefaultPointsPerIteration)
	{
		return Sample(randomState, topLeft, lowerRight, null, minimumDistance * 1.5f, pointsPerIteration);
	}

	private static List<Vector2> Sample(Random randomState, Vector2 topLeft, Vector2 lowerRight, float? rejectionDistance, float minimumDistance, int pointsPerIteration)
	{
		var settings = new Settings
		{
			TopLeft = topLeft,
			LowerRight = lowerRight,
			Dimensions = lowerRight - topLeft,
			Center = (topLeft + lowerRight) / 2,
			CellSize = minimumDistance / Sqrt2,
			MinimumDistance = minimumDistance,
			HasRejectionDistance = rejectionDistance > 0,
			RejectionSqDistance = rejectionDistance * rejectionDistance
		};
		settings.GridWidth = (int)(settings.Dimensions.x / settings.CellSize) + 1;
		settings.GridHeight = (int)(settings.Dimensions.y / settings.CellSize) + 1;

		var state = new State
		{
			RandomState = randomState,
			Grid = new Vector2?[settings.GridWidth, settings.GridHeight],
			ActivePoints = new List<Vector2>(),
			Points = new List<Vector2>()
		};

		AddFirstPoint(ref settings, ref state);

		while (state.ActivePoints.Count != 0)
		{
			var listIndex = state.RandomState.Next(state.ActivePoints.Count);

			var point = state.ActivePoints[listIndex];
			var found = false;

			for (var k = 0; k < pointsPerIteration; k++)
				found |= AddNextPoint(point, ref settings, ref state);

			if (!found)
				state.ActivePoints.RemoveAt(listIndex);
		}

		return state.Points;
	}

	private static void AddFirstPoint(ref Settings settings, ref State state)
	{
		var added = false;
		while (!added)
		{
			var xr = settings.TopLeft.x + settings.Dimensions.x * state.RandomState.Float();
			var yr = settings.TopLeft.y + settings.Dimensions.y * state.RandomState.Float();
			var p = new Vector2(xr, yr);
			
			if (settings.HasRejectionDistance && Vector2.DistanceSquared(settings.Center, p) > settings.RejectionSqDistance)
				continue;
			added = true;

			var index = Denormalize(p, settings.TopLeft, settings.CellSize);
			state.Grid[(int)index.x, (int)index.y] = p;
			state.ActivePoints.Add(p);
			state.Points.Add(p);
		}
	}

	private static bool AddNextPoint(Vector2 point, ref Settings settings, ref State state)
	{
		var found = false;
		var q = GenerateRandomAround(ref state, point, settings.MinimumDistance);

		if (q.x >= settings.TopLeft.x && q.x < settings.LowerRight.x &&
		    q.y > settings.TopLeft.y && q.y < settings.LowerRight.y &&
		    (!settings.HasRejectionDistance || Vector2.DistanceSquared(settings.Center, q) <= settings.RejectionSqDistance))
		{
			var qIndex = Denormalize(q, settings.TopLeft, settings.CellSize);
			var tooClose = false;

			for (var i = (int)Math.Max(0, qIndex.x - 2); i < Math.Min(settings.GridWidth, qIndex.x + 3) && !tooClose; i++)
			{
				for (var k = (int)Math.Max(0, qIndex.y - 2); k < Math.Min(settings.GridHeight, qIndex.y + 3) && !tooClose; k++)
				{
					if (state.Grid[i, k].HasValue && Vector2.Distance(state.Grid[i, k].Value, q) < settings.MinimumDistance)
						tooClose = true;
				}
			}

			if (!tooClose)
			{
				found = true;
				state.ActivePoints.Add(q);
				state.Points.Add(q);
				state.Grid[(int)qIndex.x, (int)qIndex.y] = q;
			}
		}

		return found;
	}

	private static Vector2 GenerateRandomAround(ref State state, Vector2 center, float minimumDistance)
	{
		var radius = minimumDistance + minimumDistance * state.RandomState.Float();
		var angle = MathF.PI * 2 * state.RandomState.Float();
		var newX = radius * MathF.Sin(angle);
		var newY = radius * MathF.Cos(angle);
		return new Vector2(center.x + newX, center.y + newY);
	}

	private static Vector2 Denormalize(Vector2 point, Vector2 origin, double cellSize)
	{
		return new Vector2((int)((point.x - origin.x) / cellSize), (int)((point.y - origin.y) / cellSize));
	}
}
