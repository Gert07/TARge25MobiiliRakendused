using Example_app.Multilangual.ViewModels;

namespace Example_app.Multilangual.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel();
    }
}
