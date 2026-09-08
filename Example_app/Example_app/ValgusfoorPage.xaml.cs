namespace Example_app;

public partial class ValgusfoorPage : ContentPage
{
    int count = 0;
    private CancellationTokenSource _nightModeCts;

    public ValgusfoorPage()
    {
        InitializeComponent();
        SetLightButtonsState(false); // Nupud on alguses inaktiivsed
    }

    private void SetLightButtonsState(bool isEnabled)
    {
        RedBtn.IsEnabled = isEnabled;
        YellowBtn.IsEnabled = isEnabled;
        GreenBtn.IsEnabled = isEnabled;
    }

    private void StopNightMode()
    {
        _nightModeCts?.Cancel();
        _nightModeCts?.Dispose();
        _nightModeCts = null;
    }

    private void StartButton_Clicked(object sender, EventArgs e)
    {
        StopNightMode();

        count++;
        ValgusfoorLabel.Text = "Valgusfoor töötab";

        // Lubame nuppude vajutamise
        SetLightButtonsState(true);

        RedBtn.BackgroundColor = Colors.Red;
        YellowBtn.BackgroundColor = Colors.Yellow;
        GreenBtn.BackgroundColor = Colors.Green;
    }

    private void ResetButton_Clicked(object sender, EventArgs e)
    {
        StopNightMode();

        count = 0;
        ValgusfoorLabel.Text = "Valgusfoor ei tööta";

        // Keelame nuppude vajutamise
        SetLightButtonsState(false);

        RedBtn.BackgroundColor = Colors.Gray;
        YellowBtn.BackgroundColor = Colors.Gray;
        GreenBtn.BackgroundColor = Colors.Gray;
    }

    private void RedBtn_Clicked(object sender, EventArgs e)
    {
        RedBtn.BackgroundColor = Colors.Red;
        RedBtn.Text = "Seis";
        YellowBtn.Text = "Kollane";
        GreenBtn.Text = "Roheline";
    }

    private void YellowBtn_Clicked(object sender, EventArgs e)
    {
        YellowBtn.BackgroundColor = Colors.Yellow;
        RedBtn.Text = "Punane";
        YellowBtn.Text = "Valmis";
        GreenBtn.Text = "Roheline";
    }

    private void GreenBtn_Clicked(object sender, EventArgs e)
    {
        GreenBtn.BackgroundColor = Colors.Green;
        RedBtn.Text = "Punane";
        YellowBtn.Text = "Kollane";
        GreenBtn.Text = "Mine";
    }

    private async void NightBtn_Clicked(object sender, EventArgs e)
    {
        StopNightMode();

        // Öörežiimis ei saa tulesid käsitsi vajutada
        SetLightButtonsState(false);

        ValgusfoorLabel.Text = "Öörežiim";
        RedBtn.BackgroundColor = Colors.Gray;
        GreenBtn.BackgroundColor = Colors.Gray;

        _nightModeCts = new CancellationTokenSource();
        await BlinkYellowLightAsync(_nightModeCts.Token);
    }

    private async Task BlinkYellowLightAsync(CancellationToken token)
    {
        bool isYellow = false;

        try
        {
            while (!token.IsCancellationRequested)
            {
                isYellow = !isYellow;
                YellowBtn.BackgroundColor = isYellow ? Colors.Yellow : Colors.Gray;
                await Task.Delay(500, token);
            }
        }
        catch (TaskCanceledException)
        {
            // Tsükkel katkestati
        }
    }

    private void Liikumine(object? sender, EventArgs e)
    {
        Button nupp = sender as Button;
        if (nupp?.ZIndex == 0)
        {
            Navigation.PushAsync(new TextPage());
        }
        else if (nupp?.ZIndex == 1)
        {
            Navigation.PopToRootAsync();
        }
        else if (nupp?.ZIndex == 2)
        {
            Navigation.PushAsync(new FigurePage());
        }
    }
}