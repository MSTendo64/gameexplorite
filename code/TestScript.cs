using System;
using System.Net.Http.Headers;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Resources;
using Sandbox.Utility;

public class AdvancedTerrainGenerator
{
	private int width;
	private int height;
	private float baseScale;
	private int octaves;
	private float persistence;
	private int seed;
	private int[] permutation;
	private float mountainFactor;
	private float depressionFactor;

	public AdvancedTerrainGenerator( int width, int height, float baseScale = 100.0f,
								  int octaves = 8, float persistence = 0.55f,
								  int seed = 0, float mountainFactor = 2.0f,
								  float depressionFactor = 0.5f )
	{
		this.width = width;
		this.height = height;
		this.baseScale = baseScale;
		this.octaves = octaves;
		this.persistence = persistence;
		this.seed = seed;
		this.mountainFactor = mountainFactor;
		this.depressionFactor = depressionFactor;
		this.permutation = GeneratePermutation( 256, seed );
	}

	private int[] GeneratePermutation( int size, int seed )
	{
		Random rand = new Random( seed );
		int[] perm = new int[size];
		for ( int i = 0; i < size; i++ ) perm[i] = i;

		for ( int i = size - 1; i > 0; i-- )
		{
			int j = rand.Next( i + 1 );
			(perm[i], perm[j]) = (perm[j], perm[i]);
		}
		return perm;
	}

	private float Fade( float t ) => t * t * t * (t * (t * 6 - 15) + 10);

	private float Lerp( float t, float a, float b ) => a + t * (b - a);

	private float Grad( int hash, float x, float y )
	{
		int h = hash & 15;
		float grad = 1.0f + (h & 7);
		return ((h & 8) != 0) ? -grad : grad;
	}

	private float Perlin( float x, float y )
	{
		int xi = (int)Math.Floor( x ) & 255;
		int yi = (int)Math.Floor( y ) & 255;

		float xf = x - (int)Math.Floor( x );
		float yf = y - (int)Math.Floor( y );

		float u = Fade( xf );
		float v = Fade( yf );

		int a = permutation[xi] + yi;
		int aa = permutation[a & 255];
		int ab = permutation[(a + 1) & 255];
		int b = permutation[(xi + 1) & 255] + yi;
		int ba = permutation[b & 255];
		int bb = permutation[(b + 1) & 255];

		float gradAA = Grad( aa, xf, yf );
		float gradBA = Grad( ba, xf - 1, yf );
		float gradAB = Grad( ab, xf, yf - 1 );
		float gradBB = Grad( bb, xf - 1, yf - 1 );

		float lerpX1 = Lerp( u, gradAA, gradBA );
		float lerpX2 = Lerp( u, gradAB, gradBB );
		return Lerp( v, lerpX1, lerpX2 );
	}

	public byte[] GenerateHeightmap()
	{
		byte[] heightmap = new byte[width * height];
		float[,] heights = new float[width, height];
		float maxHeight = float.MinValue;
		float minHeight = float.MaxValue;

		// Генерация базового ландшафта
		for ( int y = 0; y < height; y++ )
		{
			for ( int x = 0; x < width; x++ )
			{
				float amplitude = 1.0f;
				float frequency = 1.0f;
				float noiseValue = 0.0f;

				for ( int i = 0; i < octaves; i++ )
				{
					float sampleX = x / baseScale * frequency;
					float sampleY = y / baseScale * frequency;

					float noise = Perlin( sampleX, sampleY );

					// Добавление горных хребтов
					if ( i > octaves / 2 )
					{
						noise = 1 - Math.Abs( noise );
					}

					noiseValue += noise * amplitude;
					amplitude *= persistence;
					frequency *= 2.0f;
				}

				// Добавление впадин
				float depressionNoise = Perlin( x / (baseScale * 2), y / (baseScale * 2) ) * depressionFactor;
				noiseValue -= depressionNoise;

				heights[x, y] = noiseValue;
				maxHeight = Math.Max( maxHeight, noiseValue );
				minHeight = Math.Min( minHeight, noiseValue );
			}
		}

		// Постобработка и нормализация
		float heightRange = maxHeight - minHeight;
		for ( int y = 0; y < height; y++ )
		{
			for ( int x = 0; x < width; x++ )
			{
				float normalized = (heights[x, y] - minHeight) / heightRange;

				// Усиление горных регионов
				normalized = (float)Math.Pow( normalized, mountainFactor );

				// Корректировка диапазона
				normalized = Math.Clamp( normalized, 0.0f, 1.0f );

				heightmap[y * width + x] = (byte)(normalized * 255);
			}
		}

		return heightmap;
	}
}

public sealed class TestScript : Component
{
	private Logger logger = new Logger("Terrain Logger");
	private TypeDescription typeDescription;
	private int HMSize;

	private ushort[] hmData;
	private ushort[] hmData;


    protected override void OnAwake()
    {
		if ( FileSystem.Mounted.FileExists( "4x4-mountains.raw" ) )
		{
			byte[] heightmapData = FileSystem.Mounted.ReadAllBytes( "4x4-mountains.raw" ).ToArray();
			HMSize = heightmapData.Length;
			logger.Info( $"Loaded heightmap with {heightmapData.Length} bytes." );

			hmData = GenerateHeightMap( heightmapData.Length );

			Terrain terrainComp = this.GetComponent<Terrain>();

			try
			{
				terrainComp.Storage.HeightMap = hmData;
			}
			catch ( Exception ex )
			{
				logger.Info( ex );
			}

	}

    protected override void OnStart()
    {
		
			
    }

    protected override void OnUpdate()
	{

	}

	private ushort[] ByteToUshortArray( byte[] byteAray)
	{
		ushort[] heightmapDataRaw = new ushort[byteAray.Length];

		for ( int i = 0; i < byteAray.Length; i++ )
		{
			heightmapDataRaw[i] = byteAray[i];
		}

		return heightmapDataRaw;
	}

	private ushort[] GenerateHeightMap(int size )
	{
		ushort[] heightmapDataRaw = new ushort[size*size];

		for ( int i = 0; i < size; i++ )
		{
			heightmapDataRaw[i] = (ushort)new Random().Int(80);
		}

		return heightmapDataRaw;
	}

	private ushort[] GenHMPerlin(int len, int size )
	{
		ushort[] heightmapDataRaw = new ushort[len];
		int x = 0, y = 0;

		try {

			//INoiseField noise = Noise.PerlinField( new Noise.FractalParameters() );
			for ( y = 0; y < size; y++ )	
			{
				for ( x = 0; x < size; x++ )
				{
					heightmapDataRaw[XYToIndex( y, x, size )] = (ushort)(Noise.Fbm( 4, x, y, 0 ) * 65000  );
					////heightmapDataRaw[XYToIndex( y, x, size )] = (ushort)((Noise.Fbm( 4, x, y,0) * 0.5f + 0.5f) * 6500);
					//if ( x > size / 2 )
					//{
					//	heightmapDataRaw[XYToIndex( y, x, size )] = (ushort)((Noise.Fbm( 4, x, y, 0 ) * 0.5f + 0.5f) * 65000);
					//												//(ushort)((Noise.Fbm( 4, x, y, 0 ) * 0.5f + 0.5f) * 6000);
					//}
					//else
					//{
					//	heightmapDataRaw[XYToIndex( y, x, size )] = (ushort)((Noise.Fbm( 4, x, y, 0 ) * 0.5f + 0.5f) * 65000*(1-x/size/2));

					//}
					//heightmapDataRaw[XYToIndex( y, x, 1024 )] = (ushort)((Noise.Perlin(x, y ) * 0.5f + 0.5f) * 6500);
					//heightmapDataRaw[XYToIndex( x, y, size )] = (ushort)(noise.Sample( new Vector2( x, y ) ) * 100);
				}
			}

		}
		catch ( Exception ex )
		{ 
			logger.Info($"heightmapDataRaw size: {heightmapDataRaw.Length} Coordinate X:{x} Y:{y} in index: { XYToIndex( x, y, size)} " );
		}
		
		return heightmapDataRaw;
	}

	int XYToIndex( int x, int y, int width )
	{
		return y * width + x;
	}


}
