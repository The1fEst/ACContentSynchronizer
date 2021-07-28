using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ACContentSynchronizer.Models;
using ACContentSynchronizer.Server.Hubs;
using ACContentSynchronizer.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace ACContentSynchronizer.Server.Controllers {
  [ApiController]
  public class SynchronizationController : ControllerBase {
    private readonly IConfiguration _configuration;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ServerConfigurationService _serverConfiguration;

    public SynchronizationController(IConfiguration configuration,
                                     ServerConfigurationService serverConfiguration,
                                     IHubContext<NotificationHub> hubContext) {
      _configuration = configuration;
      _serverConfiguration = serverConfiguration;
      _hub = hubContext;
    }

    [HttpGet("rofl")]
    public string Rolf() {
      return "mem";
    }

    [HttpGet("getManifest")]
    public Manifest GetManifest() {
      var gamePath = _configuration.GetValue<string>("GamePath");
      var trackName = _serverConfiguration.GetTrackName();
      var cars = _serverConfiguration.GetCars();

      if (string.IsNullOrEmpty(trackName)) {
        throw new NullReferenceException(nameof(trackName));
      }

      return ContentUtils.GetManifest(gamePath, cars, trackName);
    }

    [HttpPost("prepareContent")]
    public async Task<string> PrepareContent(Manifest manifest) {
      await _hub.Clients.All.SendAsync(HubMethods.Message.ToString(), "Content uploaded");
      var gamePath = _configuration.GetValue<string>("GamePath");

      var content = ContentUtils.PrepareContent(gamePath, manifest);
      content.Session = HttpContext.Connection.Id;
      ProcessedContent.AvailableContents.Add(content);

      return HttpContext.Connection.Id;
    }

    [HttpGet("packContent")]
    public async Task PackContent(string session, string client) {
      var content = ProcessedContent.AvailableContents.FirstOrDefault(x => x.Session == session);

      if (content == null) {
        return;
      }

      content.OnProgress += async (progress, entry) => {
        await _hub.Clients.Client(client).SendAsync(HubMethods.PackProgress.ToString(), progress, entry);
      };
      await content.Pack(HttpContext.Connection.Id);
      ProcessedContent.AvailableContents.Remove(content);
    }

    [HttpGet("cancelPack")]
    public void CancelPack(string session) {
      var content = ProcessedContent.AvailableContents.FirstOrDefault(x => x.Session == session);
      if (content == null) {
        return;
      }

      content.AbortPacking();
      ProcessedContent.AvailableContents.Remove(content);
    }

    [HttpGet("downloadContent")]
    public async Task DownloadContent(string session, string client) {
      var path = Path.Combine(session, Constants.ContentArchive);
      await using var fileStream = System.IO.File.OpenRead(path);

      await _hub.Clients.Client(client)
        .SendAsync(HubMethods.PackProgress
          .ToString(), 0, $"content size {fileStream.Length}");

      Response.Headers["Content-Type"] = Constants.ContentType;
      Response.Headers["Content-Length"] = fileStream.Length.ToString();
      await fileStream.CopyToAsync(Response.Body);
      fileStream.Close();
      await fileStream.DisposeAsync();
      DirectoryUtils.DeleteIfExists(session, true);
    }

    [HttpPost("getUpdateManifest")]
    public Manifest GetUpdatableEntries(Manifest manifest) {
      var gamePath = _configuration.GetValue<string>("GamePath");
      return ContentUtils.CompareContent(gamePath, manifest);
    }

    [HttpPost("updateContent")]
    [DisableRequestSizeLimit]
    public async Task UpdateContent(string adminPassword, string client) {
      _serverConfiguration.CheckAccess(adminPassword);

      var gamePath = _configuration.GetValue<string>("GamePath");
      await _serverConfiguration.GetArchive(Request,
        HttpContext.Connection.Id,
        client);

      ContentUtils.UnpackContent(HttpContext.Connection.Id);
      ContentUtils.ApplyContent(gamePath, HttpContext.Connection.Id);

      await _hub.Clients.All.SendAsync(HubMethods.Message.ToString(), "Content uploaded");
    }

    [HttpPost("refreshServer")]
    public async Task RefreshServer(Manifest manifest, string adminPassword) {
      _serverConfiguration.CheckAccess(adminPassword);

      await _serverConfiguration.UpdateConfig(manifest);
      await _serverConfiguration.RunServer();

      await _hub.Clients.All.SendAsync(HubMethods.Message.ToString(), "Server rebooted");
    }

    [HttpGet("getServerProps")]
    public ServerProps? GetServerProps() {
      return _serverConfiguration.GetServerProps();
    }

    [HttpGet("getServerInfo")]
    public Dictionary<string, Dictionary<string, string>>? GetServerInfo() {
      return _serverConfiguration.GetServerInfo();
    }
  }
}
