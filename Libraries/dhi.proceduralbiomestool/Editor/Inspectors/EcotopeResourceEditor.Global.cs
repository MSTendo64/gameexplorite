using System;
using ProceduralBiomesTool.Resources;
using ProceduralBiomesTool.Rules;
using ProceduralBiomesToolEditor.Utility;

namespace ProceduralBiomesToolEditor;

public class EcotopeResourceGlobalSettingsPage : Widget
{
	public Action RefreshButtons;
	public Action SetDirty;
	
	private readonly EcotopeResource ecotopeResource;
	
	private Widget rulesContainer;
	
	public EcotopeResourceGlobalSettingsPage(EcotopeResource resource) : base(null)
	{
		ecotopeResource = resource;

		Layout = Layout.Column();
		Layout.Margin = 10;
	}

	public void Build()
	{ 
		BuildSettings();
		BuildRules();
		
		RefreshButtons();
	}

	private void Refresh()
	{
		BuildRules();
	}

	private void Dirty()
	{
		SetDirty();
		Refresh();
	}
	
	private void BuildSettings()
	{
		Layout.Add(WidgetUtils.Header("Global Settings"));
			
		var cs = new ControlSheet();
		cs.AddProperty(ecotopeResource, x => x.VisualizationColor);
		cs.AddProperty(ecotopeResource, x => x.Density);
		Layout.Add(cs);
	}

	private void BuildRules()
	{
		// Remove the old rules container if one exists when we rebuild
		rulesContainer?.Destroy();
		
		// Create the rules in their own container so that we can delete the whole section and remake it if necessary
		var container = new Widget();
		container.Layout = Layout.Column();
		container.Layout.Margin = 0;
		rulesContainer = container;
		
		var body = container.Layout;
		
		body.Add(WidgetUtils.Header("Rules"));
		
		// Add a first rule that shows the layers will be generated first
		body.Add(WidgetUtils.InfoRule(container,
			"1. Generate Layers",
			"layers",
			"Generate layer assets and apply the layer rules.\nThis rule is automatic and can not be moved or deleted.",
			$"{ecotopeResource.ResourceName}.global_rules.0"));
		
		// Create the actual global rules
		for (int i = 0; i < ecotopeResource.Rules?.Count; ++i)
		{
			var idx = i;
			var rule = ecotopeResource.Rules[i];
			var ruleType = rule.GetType();
			
			var controlSheetContainer = new Widget();
			controlSheetContainer.Layout = Layout.Column();
			
			// Figure out if there are any properties to actually display for this rule
			var propsCount = EditorTypeLibrary.GetPropertyDescriptions(rule).Count(x => !x.HasAttribute<HideAttribute>());
			if (propsCount == 0)
			{
				// If there aren't, then display some text so that its obvious there is nothing to edit
				controlSheetContainer.Layout.Margin = 10;
				controlSheetContainer.Layout.Add(new Label("There are no editable properties for this rule."));
			}
			else
			{
				// Otherwise add the control sheet for the rules properties so we can edit them
				var cs = new ControlSheet();
				cs.AddObject(rule.GetSerialized());
				controlSheetContainer.Layout.Add(cs);
			}
			
			// Create a toggle group containing the properties
			var toggle = new ExpandGroup(container);
			toggle.StateCookieName = GetRuleCookie(rule);
			toggle.Title = $"{i + 2}. {DisplayInfo.ForType(ruleType).Name}"; // +2 due to start rule
			toggle.ToolTip = DisplayInfo.ForType(ruleType).Description;
			toggle.Icon = DisplayInfo.ForType(ruleType).Icon;
			toggle.MouseRightClick = () => OpenContextMenuOnGlobalRule(idx);
			toggle.SetWidget(controlSheetContainer);
			body.Add(toggle);
		}
		
		// Add a final rule that shows the game objects will be generated at the end
		body.Add(WidgetUtils.InfoRule(container,
			$"{ecotopeResource.Rules?.Count + 2}. Generate Game Objects",
			"add_circle",
			"Create game objects from the generated biome data.\nThis rule is automatic and can not be moved or deleted.",
			$"{ecotopeResource.ResourceName}.global_rules.last"));
		
		body.AddSpacingCell(8f);
		
		var horiz = body.AddRow();
		horiz.Spacing = 8;
		horiz.AddStretchCell();
		horiz.Add(new Button()
		{
			Text = "Add Rule",
			Icon = "add",
			Tint = Theme.Primary,
			Clicked = OpenContextMenuCreateGlobalRules
		});
		horiz.Add(new IconButton("history")
		{
			ToolTip = "Reset to Default",
			OnClick = () =>
			{
				ecotopeResource.ResetToDefaultRules();
				Dirty();
			}
		});
		horiz.AddStretchCell();
		
		container.Layout.AddStretchCell();
		Layout.Add(container);
	}
	
	/// <summary>
	/// Open the context menu when clicking the Create Rule button when viewing the global settings.
	/// </summary>
	private void OpenContextMenuCreateGlobalRules()
	{
		// Create all available options
		var options = new List<(string name, TypeDescription type)>();
		foreach (var type in TypeLibrary.GetTypes<EcotopeGlobalRule>())
		{
			if (type.TargetType == typeof(EcotopeGlobalRule))
				continue;
			
			string name = type.Title;
			if (!string.IsNullOrEmpty(type.Group))
			{
				name = $"{type.Group}/{name}";
			}

			options.Add((name, type));
		}
		
		// Sort alphabetically
		options.Sort((a, b) => a.name.CompareTo(b.name));

		// Then build the context menu from this list
		var menu = new ContextMenu(this);
		foreach (var pair in options)
		{
			string[] path = pair.name.Split("/");
			menu.AddOption(path, pair.type.Icon, () =>
			{
				var rule = ecotopeResource.Rules.AddNew(pair.type);
				ProjectCookie.Set(GetRuleCookie(rule), true); // rules start expanded
				
				Dirty();
			});
		}
		menu.OpenAtCursor();
	}
	
	/// <summary>
	/// Open the context menu when right-clicking on a rule when viewing the global settings.
	/// </summary>
	private void OpenContextMenuOnGlobalRule(int index)
	{
		var menu = new ContextMenu(this);
		menu.AddOption("Move to Top", "arrow_upward", () =>
		{
			ecotopeResource.Rules.MoveToTop(index);
			Dirty();
		});
		menu.AddOption("Move Up", "expand_less", () =>
		{
			ecotopeResource.Rules.Swap(index, -1);
			Dirty();
		});
		menu.AddOption("Move Down", "expand_more", () =>
		{
			ecotopeResource.Rules.Swap(index, 1);
			Dirty();
		});
		menu.AddOption("Move to Bottom", "arrow_downward", () =>
		{
			ecotopeResource.Rules.MoveToBottom(index);
			Dirty();
		});
		menu.AddSeparator();
		menu.AddOption("Delete", "clear", () =>
		{
			ecotopeResource.Rules.RemoveAt(index);
			Dirty();
		});
		menu.OpenAtCursor();
	}
	
	public override void ChildValuesChanged(Widget source)
	{
		base.ChildValuesChanged(source);
		SetDirty();
	}
	
	private string GetRuleCookie(EcotopeGlobalRule rule) => $"ecotopes.rule.{rule.Guid}";
	
}
