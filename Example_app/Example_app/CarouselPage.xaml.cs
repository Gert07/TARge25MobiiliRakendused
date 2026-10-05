using System.Collections.ObjectModel;

public class CarouselPage : ContentPage

{

    public class CarouselItem

    {

        public string Title { get; set; }

        public string ImageUrl { get; set; }

    }

    private CarouselView carouselView;

    // List asendati ObservableCollectioniga

    private ObservableCollection<CarouselItem> items;

    private int position = 0;

    public CarouselPage()

    {

        Title = "Karussell - Dünaamiline lisamine";


        // Initsialiseerime ObservableCollectioni

        items = new ObservableCollection<CarouselItem>

        {

            new CarouselItem { Title = "Päikesetõus", ImageUrl = "https://picsum.photos/id/1015/600/400" },

            new CarouselItem { Title = "Metsavaikus", ImageUrl = "https://picsum.photos/id/1016/600/400" },

            new CarouselItem { Title = "Järvepeegel", ImageUrl = "https://picsum.photos/id/1018/600/400" }

        };


        // Karusselli loomine (kood on sama, mis eelmises versioonis)

        carouselView = new CarouselView

        {

            ItemsSource = items,

            HeightRequest = 350,

            PeekAreaInsets = new Thickness(40, 0, 40, 0),


            ItemTemplate = new DataTemplate(() =>

            {

                var frame = new Frame

                {

                    CornerRadius = 15,

                    HasShadow = true,

                    Padding = 0,

                    Margin = new Thickness(5),

                    BackgroundColor = Colors.Black

                };


                var grid = new Grid();

                var image = new Image { Aspect = Aspect.AspectFill };

                image.SetBinding(Image.SourceProperty, "ImageUrl");


                var gradient = new BoxView

                {

                    Background = new LinearGradientBrush

                    {

                        StartPoint = new Point(0, 1),

                        EndPoint = new Point(0, 0),

                        GradientStops = new GradientStopCollection

                        {

                            new GradientStop(Colors.Black.WithAlpha(0.7f), 0),

                            new GradientStop(Colors.Transparent, 1)

                        }

                    }

                };


                var label = new Label

                {

                    TextColor = Colors.White,

                    FontSize = 20,

                    FontAttributes = FontAttributes.Bold,

                    Margin = new Thickness(15),

                    VerticalOptions = LayoutOptions.End

                };

                label.SetBinding(Label.TextProperty, "Title");


                grid.Children.Add(image);

                grid.Children.Add(gradient);

                grid.Children.Add(label);


                frame.Content = grid;

                return frame;

            })

        };


        var indicatorView = new IndicatorView

        {

            IndicatorColor = Colors.LightGray,

            SelectedIndicatorColor = Colors.DarkSlateBlue,

            HorizontalOptions = LayoutOptions.Center,

            Margin = new Thickness(0, 10)

        };

        carouselView.IndicatorView = indicatorView;


        // Nupp elemendi lisamiseks

        var lisaNupp = new Button

        {

            Text = "Lisa uus pilt",

            BackgroundColor = Colors.DarkSlateBlue,

            TextColor = Colors.White,

            CornerRadius = 10,

            Margin = new Thickness(0, 20, 0, 0)

        };


        // Nupu vajutamise sündmus

        lisaNupp.Clicked += (sender, e) =>

        {

            // Lisame kollektsiooni uue elemendi

            items.Add(new CarouselItem

            {

                Title = "Rooma tänavad",

                ImageUrl = "https://picsum.photos/id/1029/600/400"

            });


            // Soovi korral saame karusselli kohe uuele pildile kerida

            carouselView.Position = items.Count - 1;

        };


        // Automaatne kerimine

        Device.StartTimer(TimeSpan.FromSeconds(4), () =>

        {

            if (items == null || items.Count == 0) return false;


            position = (position + 1) % items.Count;

            carouselView.Position = position;

            return true;

        });


        Content = new ScrollView

        {

            Content = new VerticalStackLayout

            {

                Padding = 20,

                Spacing = 20, // Jätab elementide vahele ilusa tühimiku

                Children =

                {

                    carouselView,

                    indicatorView,

                    lisaNupp

                }

            }

        };

    }

}

