using CarouselView.Kitchen.Models;
using CarouselView.Kitchen.ViewModels;

namespace CarouselView;

public partial class MainPage : ContentPage
{
    private readonly KitchenViewModel viewModel = new();
    private IDispatcherTimer? autoScrollTimer;
    private Window? ownerWindow;
    private bool isVisible;
    private bool isBackgrounded;
    private bool isShowingDetails;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        isVisible = true;
        isBackgrounded = false;
        ownerWindow = Window;
        if (ownerWindow is not null)
        {
            ownerWindow.Stopped += OnWindowStopped;
            ownerWindow.Resumed += OnWindowResumed;
        }
        if (autoScrollTimer is null)
        {
            autoScrollTimer = Dispatcher.CreateTimer();
            autoScrollTimer.Interval = TimeSpan.FromSeconds(4);
            autoScrollTimer.Tick += OnAutoScrollTick;
        }
        RestartAutoScroll();
    }

    protected override void OnDisappearing()
    {
        isVisible = false;
        viewModel.IsLanguageMenuOpen = false;
        autoScrollTimer?.Stop();
        if (ownerWindow is not null)
        {
            ownerWindow.Stopped -= OnWindowStopped;
            ownerWindow.Resumed -= OnWindowResumed;
            ownerWindow = null;
        }
        base.OnDisappearing();
    }

    private void OnWindowStopped(object? sender, EventArgs e)
    {
        isBackgrounded = true;
        autoScrollTimer?.Stop();
    }

    private void OnWindowResumed(object? sender, EventArgs e)
    {
        isBackgrounded = false;
        RestartAutoScroll();
    }

    private void RestartAutoScroll()
    {
        autoScrollTimer?.Stop();
        if (isVisible && !isBackgrounded && !isShowingDetails && !viewModel.IsLanguageMenuOpen && viewModel.IsPlaying)
            autoScrollTimer?.Start();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KitchenViewModel.IsLanguageMenuOpen))
            RestartAutoScroll();
    }

    protected override bool OnBackButtonPressed()
    {
        if (!viewModel.IsLanguageMenuOpen) return base.OnBackButtonPressed();
        viewModel.IsLanguageMenuOpen = false;
        return true;
    }
    private void OnAutoScrollTick(object? sender, EventArgs e)
    {
        // Use the actual position so manual swiping and automatic scrolling stay in sync.
        if (!FoodCarousel.IsDragging && !FoodCarousel.IsScrolling && viewModel.Dishes.Count > 1)
            FoodCarousel.Position = (FoodCarousel.Position + 1) % viewModel.Dishes.Count;
    }

    private void OnPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        viewModel.Position = e.CurrentPosition;
        RestartAutoScroll();
    }

    private void OnPlaybackClicked(object? sender, EventArgs e)
    {
        viewModel.IsPlaying = !viewModel.IsPlaying;
        RestartAutoScroll();
    }

    private void OnPreviousClicked(object? sender, EventArgs e) => Move(-1);
    private void OnNextClicked(object? sender, EventArgs e) => Move(1);

    private void Move(int offset)
    {
        FoodCarousel.Position = (FoodCarousel.Position + offset + viewModel.Dishes.Count) % viewModel.Dishes.Count;
        RestartAutoScroll();
    }

    private async void OnDishTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Dish dish) await ShowDetailsAsync(dish);
    }

    private async void OnDetailsClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Dish dish }) await ShowDetailsAsync(dish);
    }

    private async Task ShowDetailsAsync(Dish dish)
    {
        if (isShowingDetails) return;
        isShowingDetails = true;
        viewModel.IsLanguageMenuOpen = false;
        autoScrollTimer?.Stop();
        try
        {
            var strings = viewModel.Strings;
            var message = $"{strings["Ingredients"]}\n{dish.Ingredients}\n\n" +
                          $"{strings["Preparation"]}: {dish.Preparation}\n\n" +
                          $"{strings["ChefTip"]}\n{dish.Tip}";
            await DisplayAlertAsync(dish.Title, message, strings["Close"]);
        }
        finally
        {
            isShowingDetails = false;
            RestartAutoScroll();
        }
    }
}
