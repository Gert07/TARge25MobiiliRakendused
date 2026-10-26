using Example_app.ViewModels;

namespace Example_app.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Activate();
    }

    protected override void OnDisappearing()
    {
        _viewModel.Deactivate();
        base.OnDisappearing();
    }

    private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}
