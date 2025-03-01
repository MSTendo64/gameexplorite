using System;
using ProceduralBiomesTool.Generation;

namespace ProceduralBiomesTool.Rules;

[Title("Remove overlapping footprints")]
[Description("Removes any objects from different layers where their footprints overlap.")]
[Group("Collision")]
[Icon("cancel")]
public class RemoveOverlappingFootprintsGlobalRule : EcotopeGlobalRule
{
	public override void Execute(PointCloud pointCloud)
	{
		var removeIdSet = new HashSet<int>();
		
		for (int ai = 0; ai < pointCloud.Count; ++ai)
		{
			var a = pointCloud[ai];
			
			// Make sure this point isn't flagged to be removed
			if(removeIdSet.Contains(a.Id))
				continue;
			
			for (int bi = ai + 1; bi < pointCloud.Count; ++bi)
			{
				var b = pointCloud[bi];
				
				// Make sure this point isn't flagged to be removed
				if(removeIdSet.Contains(a.Id))
					continue;
				
				// Ignore points that aren't on the same layer
				if(a.Layer != b.Layer)
					continue;
				
				float distance = Vector3.DistanceBetween(a.Position, b.Position);
				if (distance < MathF.Max(a.FootprintRadius, b.FootprintRadius))
				{
					if (a.Viability == b.Viability)
					{
						if (pointCloud.RandomState.NextSingle() > 0.5f)
						{
							removeIdSet.Add(b.Id);
						}
						else
						{
							removeIdSet.Add(a.Id);
							break; // a will be removed, move onto the next point
						}
					}
					else if(a.Viability > b.Viability)
					{
						removeIdSet.Add(b.Id);
					}
					else // a.Viability < b.Viability
					{
						removeIdSet.Add(a.Id);
						break; // a will be removed, move onto the next point
					}
				}
			}	
		}
		
		// Remove the points from the cloud 
		pointCloud.RemoveAll(x => removeIdSet.Contains(x.Id));
	}
}

[Title("Remove colliding objects")]
[Description("Removes any objects where their collision radius' overlap.")]
[Group("Collision")]
[Icon("cancel")]
public class RemoveCollidingObjectsGlobalRule : EcotopeGlobalRule
{
	public override void Execute(PointCloud pointCloud)
	{
		var removeIdSet = new HashSet<int>();
		
		for (int ai = 0; ai < pointCloud.Count; ++ai)
		{
			var a = pointCloud[ai];
			
			// Make sure this point isn't flagged to be removed
			if(removeIdSet.Contains(a.Id))
				continue;
			
			for (int bi = ai + 1; bi < pointCloud.Count; ++bi)
			{
				var b = pointCloud[bi];
				
				// Make sure this point isn't flagged to be removed
				if(removeIdSet.Contains(a.Id))
					continue;

				// Check if the points are colliding
				float distance = Vector3.DistanceBetween(a.Position, b.Position);
				if (distance < a.CollisionRadius + b.CollisionRadius)
				{
					// Remove the point with the smaller collision radius, bigger objects always win
					if (a.CollisionRadius > b.CollisionRadius)
					{
						removeIdSet.Add(b.Id);
					}
				}
			}	
		}
		
		// Remove the points from the cloud 
		pointCloud.RemoveAll(x => removeIdSet.Contains(x.Id));
	}
}
