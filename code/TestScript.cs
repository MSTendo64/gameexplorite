using System;
using System.Net.Http.Headers;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Resources;
using Sandbox.Utility;

public sealed class TestScript : Component
{
	private Logger logger = new Logger("Terrain Logger");
	private ushort[] hmData;


    protected override void OnAwake()
    {
		
		
			hmData = GenHMPerlin( 2048*2048, 2048);

			Terrain terrainComp = this.GetComponent<Terrain>();
			terrainComp.Storage.HeightMap = hmData;

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
