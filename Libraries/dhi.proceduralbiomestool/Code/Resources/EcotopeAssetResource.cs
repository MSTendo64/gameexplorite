using System;

namespace ProceduralBiomesTool.Resources;

[GameResource("Ecotope Asset", "ecoasset", "", Icon = "park", Category = "World")]
public class EcotopeAssetResource : GameResource
{
	[Property, Description("Which models will be generated for this resource?")] 
	public Model[] Models { get; set; } = Array.Empty<Model>();
	
	[Property, Description("If an object generates in the same location as another, the object with a higher viability will remove the lower one.")]
	public int Viability { get; set; } = 1;
	
	[Property, Description("No other objects from the same layer can generate within this radius.")]
	public float FootprintRadius { get; set; }
	
	[Property, Description("No other objects at all can generate within this radius.")]
	public float CollisionRadius { get; set; }
	
	[Property, Description("How far away can this object wander from its original position to make things look more randomly distributed?")]
	public float Jitter { get; set; }
}
