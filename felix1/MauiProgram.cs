using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;

//database related
using felix1.Data;
using Microsoft.EntityFrameworkCore;

namespace felix1;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .ConfigureSyncfusionCore()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
            
        // Register the EF Core DbContext
        builder.Services.AddDbContext<AppDbContext>();



#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
