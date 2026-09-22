namespace Example_app;

public partial class StartPage : ContentPage
{
	VerticalStackLayout vst;
	ScrollView sv;
	public List<ContentPage> lehed = new List<ContentPage>() { new TextPage(), new FigurePage(), new ValgusfoorPage(), new DateTimePage(), new StepperSliderPage(), new TreePage(), new Popup(), new GridPage(), 
		new Example_app.TicTacToe.TripsuPage() };
	public List<string> Lehenimed = new List<string>() { "Tekstid", "Kujundus", "Valgusfoor", "Aeg", "Stepper", "Puu", "Popup", "GridPage", "Tic-Tac-Toe" };
	public StartPage()
	{
		vst = new VerticalStackLayout { Padding = 20, Spacing = 20 };
		for (int i=0;i<lehed.Count; i++)
		{
			var valik = lehed[i];

			Button nupp = new Button
			{
				Text = Lehenimed[i],
				FontSize = 30,
				FontFamily = "OpenSans-Regular",
				BackgroundColor = Colors.LightGray,
				TextColor = Colors.Black,
				CornerRadius = 10,
				HeightRequest = 60,
				ZIndex = i
			};
			vst.Add(nupp);
			nupp.Clicked += (sender, e) =>
			{
				Navigation.PushAsync(valik);
			};
		}

        Button nullinupp = new Button
        {
            Text = "Nulli seaded (testimiseks)",
            BackgroundColor = Colors.Red,
            TextColor = Colors.White,
            CornerRadius = 10,
            HeightRequest = 50,
            Margin = new Thickness(0, 30, 0, 0)
        };

        nullinupp.Clicked += async (sender, e) =>
        {
            Preferences.Default.Remove("FirstStart");
            await DisplayAlertAsync("Edukalt nullitud", "Mälu on tühjendatud", "OK");
        };

        vst.Add(nullinupp);


        sv = new ScrollView { Content = vst };
        Content = sv;
    }

	//POP UP Aken
	protected override async void OnAppearing()
	{
		base.OnAppearing();

		bool onEsimeneStart = Preferences.Default.Get("FirstStart", true);

		if (onEsimeneStart)
		{
			bool vastus = await DisplayAlertAsync("Tere tulemast!","Kas soovid lühikest juhendit?", "Jah", "Ei");

			if (vastus)
			{
				await DisplayAlertAsync("Siin on lühike juhend:", "Vali menüüst sobiv teema ja uuri kas elemendid töötavad", "Ok");
			}

			Preferences.Default.Set("FirstStart", false);
		}
	}

}