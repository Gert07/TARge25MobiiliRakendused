namespace Example_app
{
    public partial class AppShell : Shell
    {
        public AppShell(Example_app.Views.StartPage startPage)
        {
            InitializeComponent();
            StartContent.Content = startPage;
        }
    }
}
