using Microsoft.Extensions.DependencyInjection;

using Example_app.Services;
using Example_app.Views;

namespace Example_app
{
    public partial class App : Application
    {
        private readonly StartPage _startPage;

        public App(StartPage startPage, SettingsService settings)
        {
            InitializeComponent();
            _startPage = startPage;
            settings.ApplyTheme();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // loome esimese lehe (startPage)

            //Pakime selle NavigationPAge sisse, et saaksime kasutada navigeerimist
            var navPage = new NavigationPage(_startPage)
            {
                BarBackgroundColor = Colors.LightBlue,
                BarTextColor = Colors.White
            };
            return new Window(navPage);
        }
    }
}
