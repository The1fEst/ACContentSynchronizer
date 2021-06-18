using System.Linq;
using ACContentSynchronizer.ClientGui.Components;
using ACContentSynchronizer.ClientGui.Models;
using ACContentSynchronizer.ClientGui.ViewModels;
using Avalonia.Collections;
using ReactiveUI;

namespace ACContentSynchronizer.ClientGui.Views {
  public class StatusBarViewModel : ViewModelBase {
    private bool _isOpen;
    public AvaloniaList<StatusBarEntry> Tasks { get; set; } = new();

    private TaskViewModel? _last;

    public TaskViewModel? Last {
      get => _last;
      set => this.RaiseAndSetIfChanged(ref _last, value);
    }

    public bool IsOpen {
      get => _isOpen;
      set => this.RaiseAndSetIfChanged(ref _isOpen, value);
    }

    public void AddTask(TaskViewModel task) {
      var entry = new StatusBarEntry();
      Tasks.Add(entry);
      entry.Run(task, Tasks);
    }

    public void Toggle() {
      IsOpen = !IsOpen;
    }
  }
}
