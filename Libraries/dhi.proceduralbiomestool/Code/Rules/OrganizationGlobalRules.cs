using ProceduralBiomesTool.Generation;

namespace ProceduralBiomesTool.Rules;

[Title("Apply tags")]
[Description("Sets the tags that the generated objects should have")]
// [Group("Collision")]
[Icon("tag")]
public class ApplyTagsGlobalRule : EcotopeGlobalRule
{
	[Property] public TagSet Tags { get; set; }
	
	public override void Execute(PointCloud pointCloud)
	{
		for (int i = 0; i < pointCloud.Count; ++i)
		{
			var p = pointCloud[i];
			p.Tags = Tags;
			pointCloud[i] = p;
		}
	}
}
