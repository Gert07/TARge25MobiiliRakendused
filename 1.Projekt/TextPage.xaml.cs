namespace _1.Projekt;

public partial class TextPage : ContentPage
{
	Label lbl;
	Editor editor;
	HorizontalStackLayout hsl;
	List<string> nupud = new List<string>() { "Tagasi", "Avaleht", "Edasi" };
	VerticalStackLayout vst;
    public TextPage()
	{
		lbl = new Label
		{
			Text = "Pealkiri",
			FontSize = 36,
			FontFamily = "Arial",
			TextColor = Colors.Black,
			HorizontalOptions = LayoutOptions.Center,
			FontAttributes = FontAttributes.Bold
		};
	}
}