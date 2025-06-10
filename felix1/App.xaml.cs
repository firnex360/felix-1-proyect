namespace felix1;

public partial class App : Application
{
	public App()
	{
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NNaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXleeXVcQmJcUkdwXkBWYUA=");

		InitializeComponent();

		MainPage = new AppShell();
        MainPage = new NavigationPage(new MainPage()); //CHECKING - adding... this, to new page

    }
}
