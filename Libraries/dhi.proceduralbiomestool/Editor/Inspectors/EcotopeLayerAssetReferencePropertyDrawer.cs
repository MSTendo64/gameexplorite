using ProceduralBiomesTool.Resources;

namespace ProceduralBiomesToolEditor;

[CustomEditor(typeof(EcotopeLayerAssetReference))]
public class EcotopeLayerAssetReferencePropertyDrawer : ControlWidget
{
	public override bool SupportsMultiEdit => false;

	public EcotopeLayerAssetReferencePropertyDrawer(SerializedProperty property) : base(property)
	{
		Layout = Layout.Row();
		Layout.Spacing = 2;

		var value = property.GetValue<EcotopeLayerAssetReference>();
		if (value == null)
		{
			property.SetValue(new EcotopeLayerAssetReference());
		}
		
		var so = property.GetValue<EcotopeLayerAssetReference>()?.GetSerialized();
		if (so is null)
		{
			Layout.Add(new Label("null serialized object"));
			return;
		}

		so.TryGetProperty(nameof(EcotopeLayerAssetReference.Asset), out var assetProp);
		so.TryGetProperty(nameof(EcotopeLayerAssetReference.Density), out var densityProp);

		Layout.Add(new ResourceControlWidget(assetProp));
		Layout.Add(new FloatControlWidget(densityProp)
		{
			HorizontalSizeMode = SizeMode.Default,
		});
	}

	protected override void OnPaint()
	{
		// do nothing
	}
}
