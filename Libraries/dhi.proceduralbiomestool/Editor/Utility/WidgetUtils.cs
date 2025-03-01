namespace ProceduralBiomesToolEditor.Utility;

public static class WidgetUtils
{
	public static Widget Header(string text)
	{
		var header = new Label(text);
		header.SetStyles("font-weight: bold");
		header.Margin = 10;
		return header;
	}
	
	public static Widget InfoRule(Widget parent, string title, string icon, string description, string cookie)
	{
		var content = new Widget();
		content.Layout = Layout.Column();
		content.Layout.Margin = 10;
		content.Layout.Add(new Label(description));
				
		var toggle = new ExpandGroup(parent);
		toggle.StateCookieName = cookie;
		toggle.Title = title;
		toggle.Icon = icon;
		toggle.SetWidget(content);
		return toggle;
	}
}
