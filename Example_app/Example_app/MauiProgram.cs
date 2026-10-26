using Microsoft.Extensions.Logging;

using Example_app.Models;
using Example_app.Services;
using Example_app.ViewModels;
using Example_app.Views;
using Example_app.Views.Game;

namespace Example_app
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton(_ =>
            {
                var database = new AppDatabase(Path.Combine(FileSystem.AppDataDirectory, "example_app.db3"));
                database.ImportLegacyData(
                    new AppSettings { HasSeenWelcome = !Preferences.Default.Get("FirstStart", true) },
                    new GameStats
                    {
                        XWins = Preferences.Default.Get("stats_x_wins", 0),
                        OWins = Preferences.Default.Get("stats_o_wins", 0),
                        Draws = Preferences.Default.Get("stats_draws", 0)
                    });
                return database;
            });
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<StartPage>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<TripsuPage>();
            builder.Services.AddTransient<RulesPage>();
            builder.Services.AddTransient<StatsPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
