#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT;
#endif

namespace felix1;

public static class WindowUtils
{
#if WINDOWS
    public static void MaximizeWindow(Window window)
    {
        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }
    }
#endif
}
