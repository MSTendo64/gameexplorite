using System;
using System.Net.Http.Headers;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Resources;
using Sandbox.Utility;

public sealed class TestScript : Component
{
	private Logger logger = new Logger("Terrain Logger");
	private TypeDescription typeDescription;
	private int HMSize;

	[Property] List<TerrainMaterial> NewTerrMat;

	protected override void OnStart()
    {

		if ( FileSystem.Mounted.FileExists( "4x4-mountains.raw" ) )
		{
			byte[] heightmapData = FileSystem.Mounted.ReadAllBytes( "4x4-mountains.raw" ).ToArray();
			HMSize = heightmapData.Length;
			logger.Info( $"Loaded heightmap with {heightmapData.Length} bytes." );

			//GameObject terrain = new GameObject();
			//terrain.Name = "Terrain";
			Terrain terrainComp = this.GetComponent<Terrain>();

			ushort[] hmData = GenerateHeightMap( heightmapData.Length );

			try
			{
				logger.Info( hmData[new Random().Int( hmData.Length )] );

				terrainComp.Storage.HeightMap = hmData;
			}
			catch ( Exception ex ) 
			{ 
				logger.Info( ex );
			}
			
		}
		else
		{
			logger.Warning( "Heightmap file not found in Mounted FileSystem." );
		}		
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
		ushort[] heightmapDataRaw = new ushort[size];

		for ( int i = 0; i < size; i++ )
		{
			heightmapDataRaw[i] = (ushort)new Random().Int(180);
		}

		return heightmapDataRaw;
	}

	private ushort[] GenHMPerlin(int size )
	{
		ushort[] heightmapDataRaw = new ushort[size * size];
		int x = 0, y = 0;

		try {

			INoiseField noise = Noise.PerlinField( new Noise.FractalParameters() );

			for ( x = 0; x < size; x++ )
			{
				for ( y = 0; y < size; y++ )
				{
					heightmapDataRaw[XYToIndex( x, y, size )] = (ushort)(noise.Sample( new Vector2( x, y ) ) * 100);
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
