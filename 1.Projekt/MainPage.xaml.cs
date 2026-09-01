namespace _1.Projekt
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
            ResetBtn.Text = $"Reset Counter";
            dotnetBot.Rotation += 10;

            //Genereerime juhusliku värvi
            var random = new Random();
            var color = Color.FromRgb(random.Next(256), random.Next(256), random.Next(256));
            ResetBtn.BackgroundColor = color; //Määrame nupu taustavärvi juhuslikuks värviks
            var opacity = dotnetBot.Opacity -= 0.1; //Määrame pildi läbipaistvuse
            var scale = dotnetBot.Scale -= 0.1; //Määrame pildi suuruse
            var cornerRadius = CounterBtn.CornerRadius += 5; //Määrame pildi nurkade ümardamise

            //Elementide peitmine ja näitamine
            if (count % 10 == 0)
            {
                dotnetBot.IsVisible = false; //Peidame pildi
            }
            else
            {
                dotnetBot.IsVisible = true; //Näitame pildi
            }
        }

        private void ResetBtn_Clicked(object sender, EventArgs e)
        {
            count = 0;
            CounterBtn.Text = $"Clicked {count} times";
            ResetBtn.Text = $"Reset Done";
            dotnetBot.Rotation = 0;
            ResetBtn.ClearValue(Button.BackgroundColorProperty); //Eemaldame nupu taustavärvi
            dotnetBot.IsVisible = true;
            dotnetBot.Opacity = 1; //Määrame pildi läbipaistvuse tagasi 1
            dotnetBot.Scale = 1; //Määrame pildi suuruse tagasi 1
            CounterBtn.CornerRadius = 0; //Määrame pildi nurkade ümardamise tagasi 0
        }

        private void LeftRight_Clicked(object sender, EventArgs e)
        {
            if (dotnetBot.HorizontalOptions == LayoutOptions.Start)
            {
                dotnetBot.HorizontalOptions = LayoutOptions.End;
            }
            else
            {
                dotnetBot.HorizontalOptions = LayoutOptions.Start;
            }
        }
    }
}
