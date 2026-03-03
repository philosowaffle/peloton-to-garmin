using Common.Database;

namespace ClientUI
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

			IServiceProvider serviceProvider = null;
#if WINDOWS10_0_17763_0_OR_GREATER
			serviceProvider = MauiWinUIApplication.Current.Services;
#elif ANDROID
			serviceProvider = MauiApplication.Current.Services;
#elif IOS || MACCATALYST
			serviceProvider = MauiUIApplicationDelegate.Current.Services;
#else
			serviceProvider = null;
#endif

			///////////////////////////////////////////////////////////
			/// MIGRATIONS
			///////////////////////////////////////////////////////////
			try
			{
				var migrationService = serviceProvider.GetService<IDbMigrations>();
				migrationService!.MigrateDeviceInfoFileToListAsync().GetAwaiter().GetResult();
			} catch (Exception e)
			{
				Console.Out.WriteLine(e.ToString());
				throw;
			}
		}
	}
}