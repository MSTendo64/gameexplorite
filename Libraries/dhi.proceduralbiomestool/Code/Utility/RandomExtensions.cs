namespace ProceduralBiomesTool.Utility;

public static class RandomExtensions
{
	public static float NextInRange(this System.Random randomState, float min, float max)
	{
		return min + (max - min) * randomState.NextSingle();
	}
}
