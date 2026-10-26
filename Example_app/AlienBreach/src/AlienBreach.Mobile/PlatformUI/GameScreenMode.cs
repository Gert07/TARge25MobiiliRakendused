namespace AlienBreach.PlatformUI;

public static class GameScreenMode
{
    public static bool IsActive { get; private set; }
#if ANDROID
    private static Android.Views.LayoutInDisplayCutoutMode? previousCutout;
#endif

    public static void SetActive(bool active)
    {
        IsActive = active;
#if ANDROID
        var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
        if (window?.DecorView is null) return;
        AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(window, !active);
        if (OperatingSystem.IsAndroidVersionAtLeast(28) && window.Attributes is { } attributes)
        {
            if (active)
            {
                previousCutout ??= attributes.LayoutInDisplayCutoutMode;
                attributes.LayoutInDisplayCutoutMode = Android.Views.LayoutInDisplayCutoutMode.ShortEdges;
            }
            else
            {
                attributes.LayoutInDisplayCutoutMode = previousCutout ?? Android.Views.LayoutInDisplayCutoutMode.Default;
                previousCutout = null;
            }
            window.Attributes = attributes;
        }
        Apply();
#endif
    }

    public static void Apply()
    {
#if ANDROID
        var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
        if (window?.DecorView is null) return;
        var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller is null) return;
        if (IsActive)
        {
            controller.SystemBarsBehavior = AndroidX.Core.View.WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            controller.Hide(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
        }
        else controller.Show(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
#endif
    }
}
