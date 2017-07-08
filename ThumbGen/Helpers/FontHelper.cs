using System.IO;
using System.Runtime.InteropServices;

namespace ThumbGen
{
    public enum FontActionResult
    {
        Error,
        NotFound,
        RemovedSuccessfully,
        AlreadyInstalled,
        InstalledSuccessfully
    }

    public class FontHelper
    {
        [DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
        private static extern int AddFontResource([In][MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        [DllImport("gdi32.dll", EntryPoint = "RemoveFontResourceW", SetLastError = true)]
        private static extern int RemoveFontResource([In][MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        public static FontActionResult InstallFont(string fontFilename)
        {
            if (string.IsNullOrEmpty(fontFilename) || !File.Exists(fontFilename))
            {
                return FontActionResult.Error;
            }

            var result = AddFontResource(fontFilename);
            var error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                return FontActionResult.Error;
            }
            return (result == 0) ? FontActionResult.AlreadyInstalled : FontActionResult.InstalledSuccessfully;
        }

        public static FontActionResult RemoveFont(string fontFilename)
        {
            if (string.IsNullOrEmpty(fontFilename) || !File.Exists(fontFilename))
            {
                return FontActionResult.Error;
            }

            var result = RemoveFontResource(fontFilename);
            var error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                return FontActionResult.Error;
            }
            return (result == 0) ? FontActionResult.NotFound : FontActionResult.RemovedSuccessfully;
        }
    }
}
