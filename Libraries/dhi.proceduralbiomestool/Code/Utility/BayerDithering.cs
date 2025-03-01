namespace ProceduralBiomesTool.Utility;

public static class BayerDithering
{
	private static readonly int[,] DitherMatrix = {
		{ 0, 8, 2, 10 },
		{ 12, 4, 14, 6 },
		{ 3, 11, 1, 9 },
		{ 15, 7, 13, 5 }
	};
	private static readonly float[,] NormalizedDitherMatrix = {
		{-0.5f, 0.25f, -0.3125f, 0.4375f, },
		{0f, -0.25f, 0.1875f, -0.0625f, },
		{-0.375f, 0.375f, -0.4375f, 0.3125f, },
		{0.125f, -0.125f, 0.0625f, -0.1875f, },
	};
	
	public const int BayerMatrixSize = 4;
	public const int BayerMatrixSizeSq = BayerMatrixSize * BayerMatrixSize;

	public static float GetNormalized(int x, int y)
	{
		return NormalizedDitherMatrix[x % BayerMatrixSize, y % BayerMatrixSize];
	}
	
	public static float GetNormalized(float x, float y)
	{
		int ix = (int)x;
		int iy = (int)y;
		return NormalizedDitherMatrix[ix % BayerMatrixSize, iy % BayerMatrixSize];
	}

	public static int GetBinaryDitherValue(int x, int y, Color color)
	{
		var m = GetNormalized(x, y);
		
		// var r = 1f / BayerMatrixSizeSq;
		// var offset = BayerMatrixSize * (BayerMatrixSize / 2f) - 0.5f;
		// @note: these aren't necessarily correct but they produce the correct output we need 
		var r = 1f / 1.001f;  
		var offset = 0.0f;

		var rChannel = color.r + r * (m - offset);
		return rChannel > 0.5f ? 1 : 0;
	}

	public static int GetBinaryDitherValue(float x, float y, Color color)
	{
		return GetBinaryDitherValue((int)x, (int)y, color);
	}
	
	public static int GetBinaryDitherValue(float x, float y, float value)
	{
		var m = GetNormalized(x, y);
		
		// var r = 1f / BayerMatrixSizeSq;
		// var offset = BayerMatrixSize * (BayerMatrixSize / 2f) - 0.5f;
		// @note: these aren't necessarily correct but they produce the correct output we need 
		var r = 1f / 1.001f;  
		var offset = 0.0f;

		var dither = value + r * (m - offset);
		return dither > 0.5f ? 1 : 0;
	}
	
}

public static class BayerDithering8x8
{
	private static readonly int[,] DitherMatrix = new int[BayerMatrixSize, BayerMatrixSize] {
		{ 0, 32, 8, 40, 2, 34, 10, 42 },
		{ 48, 16, 56, 24, 50, 18, 58, 26 },
		{ 12, 44, 4, 36, 14, 46, 6, 38 },
		{ 60, 28, 52, 20, 62, 30, 54, 22 },
		{ 3, 35, 11, 43, 1, 33, 9, 41 },
		{ 51, 19, 59, 27, 49, 17, 57, 25 },
		{ 15, 47, 7, 39, 13, 45, 5, 37 },
		{ 63, 31, 55, 23, 61, 29, 53, 21 },
	};
	
	private static readonly float[,] NormalizedDitherMatrix = new float[,]
	{
		{-0.5f, 0.25f, -0.3125f, 0.4375f, -0.453125f, 0.296875f, -0.265625f, 0.484375f, },
		{0f, -0.25f, 0.1875f, -0.0625f, 0.046875f, -0.203125f, 0.234375f, -0.015625f, },
		{-0.375f, 0.375f, -0.4375f, 0.3125f, -0.328125f, 0.421875f, -0.390625f, 0.359375f, },
		{0.125f, -0.125f, 0.0625f, -0.1875f, 0.171875f, -0.078125f, 0.109375f, -0.140625f, },
		{-0.46875f, 0.28125f, -0.28125f, 0.46875f, -0.484375f, 0.265625f, -0.296875f, 0.453125f, },
		{0.03125f, -0.21875f, 0.21875f, -0.03125f, 0.015625f, -0.234375f, 0.203125f, -0.046875f, },
		{-0.34375f, 0.40625f, -0.40625f, 0.34375f, -0.359375f, 0.390625f, -0.421875f, 0.328125f, },
		{0.15625f, -0.09375f, 0.09375f, -0.15625f, 0.140625f, -0.109375f, 0.078125f, -0.171875f, },
	};

	public const int BayerMatrixSize = 8;
	public const int BayerMatrixSizeSq = BayerMatrixSize * BayerMatrixSize;
	
	public static float GetNormalized(int x, int y)
	{
		return NormalizedDitherMatrix[x % BayerMatrixSize, y % BayerMatrixSize];
	}
	
	public static float GetNormalized(float x, float y)
	{
		int ix = (int)x;
		int iy = (int)y;
		return NormalizedDitherMatrix[ix % BayerMatrixSize, iy % BayerMatrixSize];
	}
	
	public static int GetBinaryDitherValue(float x, float y, float value)
	{
		var m = GetNormalized(x, y);
		
		// var r = 1f / BayerMatrixSizeSq;
		// var offset = BayerMatrixSize * (BayerMatrixSize / 2f) - 0.5f;
		// @note: these aren't necessarily correct but they produce the correct output we need 
		var r = 1f / 1.001f;  
		var offset = 0.0f;

		var dither = value + r * (m - offset);
		return dither > 0.5f ? 1 : 0;
	}
}
