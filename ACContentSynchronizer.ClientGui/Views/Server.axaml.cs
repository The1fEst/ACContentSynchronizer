using ACContentSynchronizer.ClientGui.Components;
using ACContentSynchronizer.ClientGui.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ACContentSynchronizer.ClientGui.Views {
  public class Server : DisposableControl {
    public Server() {
    }

    public Server(ServerViewModel? vm) {
      DataContext = vm;
      InitializeComponent();
    }

    private void InitializeComponent() {
      AvaloniaXamlLoader.Load(this);
    }
  }
}
