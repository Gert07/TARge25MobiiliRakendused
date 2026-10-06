namespace Example_app;

public partial class DateTimePage : ContentPage
{
	DatePicker datePicker;
	TimePicker timePicker;
	Label datetimeLabel;
	AbsoluteLayout al;
	public DateTimePage()
	{
		datetimeLabel = new Label
		{
			Text = "Vali kuupäev või aeg",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		datePicker = new DatePicker
		{
			MinimumDate = DateTime.Now.AddDays(-15),
			MaximumDate = DateTime.Now.AddDays(15),
			Date = DateTime.Now,
			HorizontalOptions = LayoutOptions.Center,
				Format = "D"
        };
		datePicker.DateSelected += (sender, e) =>
		{
			datetimeLabel.Text = $"Valitud kuupäev: \n{datePicker.Date:D}";
		};
		timePicker = new TimePicker
		{
			Time = DateTime.Now.TimeOfDay,
			//Time=new TimeSpan(12,0,0)
			HorizontalOptions = LayoutOptions.Center,
			Format = "T"
		};
		timePicker.PropertyChanged += (sender, e) =>
		{
			datetimeLabel.Text = $"Valitud kellaaeg: \n{timePicker.Time:T}";
		};
		al = new AbsoluteLayout { Children = { datePicker, timePicker, datetimeLabel } };
		List<View> controls = new List<View> { datePicker, timePicker, datetimeLabel };
		for (int i= 0; i < controls.Count; i++)
		{
			double yKoht = 0.2 + i * 0.2;
			AbsoluteLayout.SetLayoutBounds(controls[i], new Rect(0.5, yKoht, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
			AbsoluteLayout.SetLayoutFlags(controls[i], Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);
		}
		Content = al;
		
	}
}
