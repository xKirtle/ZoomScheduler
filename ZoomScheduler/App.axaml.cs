using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace ZoomScheduler
{
    public class App : Application
    {
        public static OperatingSystemType OSType;
        
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        
        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            //To get which OS is this app running on
            OSType = AvaloniaLocator.Current.GetService<IRuntimePlatform>().GetRuntimeInfo().OperatingSystem;

            //Launch the service
            switch (OSType)
            {
                case OperatingSystemType.WinNT:
                    Process.Start(AppDomain.CurrentDomain.BaseDirectory + @"\ZoomSchedulerService.exe");
                    break;
                
                default:
                    break;
            }
            
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new MainWindow();

            base.OnFrameworkInitializationCompleted();
        }
    }
}