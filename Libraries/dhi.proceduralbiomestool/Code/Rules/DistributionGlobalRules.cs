using ProceduralBiomesTool.Generation;

namespace ProceduralBiomesTool.Rules;

[Title("Remove object if not near object from another layer")]
[Description("Removes points if they're not near an asset from the specified layer.")]
[Group("Distribution")]
[Icon("remove_circle")]
public class RemoveIfNotNearLayerAssetLayerRule : EcotopeGlobalRule
{
	[Property] public int RemoveAssetsFromLayer { get; set; }
	[Property] public int IfNotNearAssetFromThisLayer { get; set; }
	[Property] public float Distance { get; set; } = 400f;
	
	public override void Execute(PointCloud pointCloud)
	{
		int removeLayerIdx = RemoveAssetsFromLayer - 1;
		int targetLayerIdx = IfNotNearAssetFromThisLayer - 1;
		
		for (int ai = pointCloud.Count - 1; ai >= 0; --ai)
		{
			var a = pointCloud[ai];
			if (a.Layer != removeLayerIdx)
				continue;
			
			bool anyPassed = false;
			for (int bi = 0; bi < pointCloud.Count; ++bi)
			{
				var b = pointCloud[bi];
				if (b.Layer != targetLayerIdx)
					continue;
				
				float distance = Vector3.DistanceBetweenSquared(a.Position, b.Position);
				if (distance < Distance * Distance)
				{
					anyPassed = true;
					break;
				}
			}

			if (!anyPassed)
			{
				pointCloud.RemoveAt(ai);
			}
		}
	}
}

[Title("Remove object if too near object from another layer")]
[Description("Removes points if they're too near an asset from the specified layer.")]
[Group("Distribution")]
[Icon("remove_circle")]
public class RemoveIfNearLayerAssetLayerRule : EcotopeGlobalRule
{
	[Property] public int RemoveAssetsFromLayer { get; set; }
	[Property] public int IfNearAssetFromThisLayer { get; set; }
	[Property] public float Distance { get; set; } = 400f;
	
	public override void Execute(PointCloud pointCloud)
	{
		int removeLayerIdx = RemoveAssetsFromLayer - 1;
		int targetLayerIdx = IfNearAssetFromThisLayer - 1;
		
		for (int ai = pointCloud.Count - 1; ai >= 0; --ai)
		{
			var a = pointCloud[ai];
			if (a.Layer != removeLayerIdx)
				continue;
			
			for (int bi = 0; bi < pointCloud.Count; ++bi)
			{
				var b = pointCloud[bi];
				if (b.Layer != targetLayerIdx)
					continue;
				
				float distance = Vector3.DistanceBetweenSquared(a.Position, b.Position);
				if (distance < Distance * Distance)
				{
					pointCloud.RemoveAt(ai);
					break;
				}
			}
		}
	}
}
