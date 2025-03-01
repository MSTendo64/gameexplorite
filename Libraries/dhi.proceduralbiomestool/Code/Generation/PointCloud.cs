using System;
using System.Collections;

namespace ProceduralBiomesTool.Generation;

public class PointCloud : IEnumerable<AssetPoint>
{
	private readonly List<AssetPoint> points = new();

	public System.Random RandomState { get; private set; }
	public IReadOnlyList<AssetPoint> Points => points;
	public int Count => Points.Count;

	public PointCloud(System.Random randomState)
	{
		this.RandomState = randomState;
	}
	
	public AssetPoint this[int i]
	{
		get => points[i];
		set => points[i] = value;
	}

	public void Clear()
	{
		points.Clear();
	}

	public void Add(AssetPoint point)
	{
		points.Add(point);
	}

	public void RemoveAt(int index)
	{
		points.RemoveAt(index);
	}

	public int RemoveAll(Predicate<AssetPoint> predicate)
	{
		return points.RemoveAll(predicate);
	}

	public IEnumerator<AssetPoint> GetEnumerator()
	{
		return points.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}

public struct AssetPoint
{
	public int Id;
	public int AssetIndex;
	public int ModelIndex;
	public Vector3 Position;
	public Rotation Rotation;
	public float Scale;
	public int Layer;
	public int Viability;
	public float FootprintRadius;
	public float CollisionRadius;
	public Color Tint;
	public TagSet Tags;
}
