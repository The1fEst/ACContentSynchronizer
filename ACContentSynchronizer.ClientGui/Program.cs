﻿using Avalonia;
using Avalonia.ReactiveUI;

namespace ACContentSynchronizer.ClientGui {
  internal class Program {
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] args) {
      BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() {
      return AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new Win32PlatformOptions { OverlayPopups = true })
        .With(new AvaloniaNativePlatformOptions { OverlayPopups = true })
        .LogToTrace()
        .UseReactiveUI();
    }
  }
}
