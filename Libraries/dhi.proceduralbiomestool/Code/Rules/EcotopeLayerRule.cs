using ProceduralBiomesTool.Generation;

namespace ProceduralBiomesTool.Rules;

public abstract class EcotopeLayerRule
{
	// @note: when adding new properties to child classes, make sure that any properties you expose to the editor are public!
	// otherwise the serialization system won't serialize them to file properly and the data will be lost when saving.
	
	[Property, Hide] public string Guid { get; set; } = System.Guid.NewGuid().ToString(); // Used to keep editor ui state
	
	public abstract void Execute(EcotopeLayerAssetGeneratorState state);
}
