using Avalonia;
using Themes;

namespace FrameworkUI.Demo
{
    // XGoodFit addition: the entry point, which WPF's build generated from App.xaml.
    // Usage: FrameworkUI.Demo                    run the demo
    //        FrameworkUI.Demo capture <folder>   render the shell in every theme to <folder>/frameworkui-<view>-<theme>.png
    internal static class Program
    {
        [System.STAThread]
        public static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "capture")
            {
                return Capture.Run(args[1]);
            }

            AppBuilder.Configure<App>().UsePlatformDetect().UseThemeFonts().LogToTrace().StartWithClassicDesktopLifetime(args);
            return 0;
        }
    }
}
