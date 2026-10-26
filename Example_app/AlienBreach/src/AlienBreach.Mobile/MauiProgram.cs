using Microsoft.Extensions.Logging;

using AlienBreach.Core.Persistence;
using AlienBreach.Services;

namespace AlienBreach;

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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<IGameRepository>(_ => new SqliteGameRepository(
            Path.Combine(FileSystem.AppDataDirectory, "alien-breach.db")));
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<IAudioPlayback, PlatformAudioPlayback>();
        builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
        builder.Services.AddSingleton<GameAudioService>();
		return builder.Build();
	}
}
