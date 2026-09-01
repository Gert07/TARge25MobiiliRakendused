
public partial class StartPage : ContentPage
{
    VerticalStackLayout vst;
    ScrollView sv;
    public List<ContentPage> Pages = new List<ContentPage>() { new TextPage(), new FigurePage() };
    public List<string> PageNames = new List<string>() { "Text Page", "Figure Page" };
    public StartPage()
    {
        vst = new VerticalStackLayout { Padding = 20, Spacing = 15 };
        for (int i = 0; i < Pages.Count; i++)
        {
            Button nupp = new Button
            {
                Text = Pages[i],
                FontSize = 20,
                FontFamily = "Arial",
                BackgroundColor = Colors.LightGray,
                TextColor = Colors.Black,
                CornerRadius = 10,
                HeightRequest = 60,
                ZIndex = 1,
            };
            vst.Add(nupp);
            nupp.Clicked += (sender, e) =>
            {
                var valik = Pages[nupp.ZIndex];
                Navigation.PushAsync(valik);
            };
        }
        sv = new ScrollView { Content = vst };
        Content = sv;
    }
    

}