using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;

namespace PztGenerator;

internal static class WindowInteropHelperOwner
{
    public static void TrySetOwner(Window window, UIApplication application)
    {
        IntPtr handle = application.MainWindowHandle;

        if (handle != IntPtr.Zero)
        {
            new WindowInteropHelper(window).Owner = handle;
        }
    }
}
