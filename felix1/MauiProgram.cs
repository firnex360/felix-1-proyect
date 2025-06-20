using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT;
using System;

#endif
//database related
using felix1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.LifecycleEvents;

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
			})

			// code reference: https://learn.microsoft.com/en-us/answers/questions/1470337/how-do-i-maximize-the-windows-screen-in-a-net-maui
			.ConfigureLifecycleEvents(events =>
			{
			#if WINDOWS
				events.AddWindows(w =>
				{
					w.OnWindowCreated(window =>
					{
						window.ExtendsContentIntoTitleBar = false;

						IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
						WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
						AppWindow appWindow = AppWindow.GetFromWindowId(myWndId);

						if (appWindow.Presenter is OverlappedPresenter presenter)
						{
							presenter.Maximize();
						}
					});
				});
			#endif
			});

        // Register the EF Core DbContext
        builder.Services.AddDbContext<AppDbContext>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
