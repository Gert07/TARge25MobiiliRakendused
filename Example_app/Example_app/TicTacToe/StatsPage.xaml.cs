using Example_app.TicTacToe.Models;

namespace Example_app.TicTacToe;

public partial class StatsPage : ContentPage
{
    public StatsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadStats();
    }

    private void LoadStats()
    {
        XWinsLabel.Text = $"X Võidud: {GameStats.XWins}";
        OWinsLabel.Text = $"O Võidud: {GameStats.OWins}";
        DrawsLabel.Text = $"Viigid: {GameStats.Draws}";
    }

    private void OnResetStatsClicked(object sender, EventArgs e)
    {
        GameStats.Reset();
        LoadStats();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}