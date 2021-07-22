using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ACContentSynchronizer.ClientGui.Models;
using ACContentSynchronizer.ClientGui.Tasks;
using ACContentSynchronizer.ClientGui.ViewModels;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using ReactiveUI;

namespace ACContentSynchronizer.ClientGui.Views {
  public class ServerViewModel : ViewModelBase, IDisposable {
    private AvaloniaList<ContentEntry> _cars = new();

    private ContentEntry? _selectedCar;

    private ServerEntry _serverEntry = new();

    private ContentEntry _track = new();

    public ServerEntry ServerEntry {
      get => _serverEntry;
      set => this.RaiseAndSetIfChanged(ref _serverEntry, value);
    }

    public AvaloniaList<ContentEntry> Cars {
      get => _cars;
      set => this.RaiseAndSetIfChanged(ref _cars, value);
    }

    public ContentEntry? SelectedCar {
      get {
        if (_selectedCar != null) {
          return _selectedCar;
        }

        Application.Current.TryFindResource("CarPlaceholder", out var placeholder);

        if (placeholder is Image { Source: Bitmap bitmap }) {
          return new() {
            Preview = bitmap,
          };
        }

        return null;
      }
      set {
        if (value != null) {
          ReactiveCommand.CreateFromTask(() => Booking(value)).Execute();
        }

        this.RaiseAndSetIfChanged(ref _selectedCar, value);
      }
    }

    public ContentEntry Track {
      get => _track;
      set => this.RaiseAndSetIfChanged(ref _track, value);
    }

    public void Dispose() {
      _selectedCar?.Dispose();
      _track.Dispose();
    }

    private async Task Booking(ContentEntry value) {
      using var kunosClient = new KunosClient(ServerEntry.Ip, ServerEntry.HttpPort);
      var json = await kunosClient.Booking(value.DirectoryName, value.Variation, "fEst", "EE",
        Settings.Instance.SteamId.ToString(), ServerEntry.Password);
    }

    public void Join() {
    }

    public async Task Refresh() {
      using var dataReceiver = new DataReceiver(ServerEntry.Http);
      try {
        var info = await dataReceiver.GetServerInfo();

        if (info.Any()) {
          Cars = new(info["SERVER"]["CARS"].Split(";").Select(x => new ContentEntry {
            DirectoryName = x,
            Name = ContentUtils.GetCarName(x, Settings.Instance.GamePath),
            Preview = GetCarPreview(x),
            Variation = "1MF777",
          }));

          var track = info["SERVER"]["TRACK"];
          var trackName = ContentUtils.GetTrackName(track, Settings.Instance.GamePath)
            .FirstOrDefault();

          Track = new() {
            DirectoryName = track,
            Name = trackName?.Name ?? track,
            Preview = GetTrackPreview(track),
          };

          await UpdateCars();
        }
      } catch {
        if (dataReceiver.Client.BaseAddress?.AbsoluteUri != ServerEntry.Http) {
          return;
        }

        Cars = new();
        Track = new();
      }
    }

    public void ValidateContent() {
      StatusBar.Instance.AddTask(new ValidationTask(ServerEntry));
    }

    private Bitmap? GetCarPreview(string entry) {
      var carDirectory = ContentUtils.GetCarDirectory(entry, Settings.Instance.GamePath);
      if (string.IsNullOrEmpty(carDirectory)) {
        return null;
      }
      var carSkinsDirectory = Path.Combine(carDirectory, "skins");
      if (!Directory.Exists(carSkinsDirectory)) {
        return null;
      }
      var skins = Directory.GetDirectories(carSkinsDirectory);
      var rnd = new Random();
      var skin = skins[rnd.Next(0, skins.Length)];
      return new
        (Path.Combine(skin, "preview.jpg"));
    }

    private Bitmap? GetTrackPreview(string entry) {
      var trackDirectory = ContentUtils.GetTrackDirectories(entry, Settings.Instance.GamePath)
        .FirstOrDefault();
      if (string.IsNullOrEmpty(trackDirectory)) {
        return null;
      }
      var trackPreview = Path.Combine(trackDirectory, "preview.png");
      if (!File.Exists(trackPreview)) {
        return null;
      }

      return new(trackPreview);
    }

    public async Task UpdateCars() {
      var dataReceiver = new DataReceiver(ServerEntry.Http);
      var carsUpdate = await dataReceiver.GetCarsUpdate(Settings.Instance.SteamId);

      if (carsUpdate.Any()) {
        foreach (var update in carsUpdate) {
          var car = Cars.FirstOrDefault(x => x.DirectoryName == update.Name);

          if (car == null) {
            continue;
          }

          car.Count = update.Count;
          car.Allowed = update.Allowed;
        }
      }
    }
  }
}
