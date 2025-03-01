namespace ProceduralBiomesTool.Utility;

public static class ListExtensions
{
    
	public static void Shuffle<T>(this IList<T> list)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = Game.Random.Next(0, n + 1);
			(list[k], list[n]) = (list[n], list[k]);
		}
	}
    
	public static void Shuffle<T>(this IList<T> list, System.Random randomizer)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = randomizer.Next(n + 1);
			(list[k], list[n]) = (list[n], list[k]);
		}
	}
    
}
