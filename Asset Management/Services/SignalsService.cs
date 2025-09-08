using Asset_Management.Controllers;
using Asset_Management.Database;
using Asset_Management.DTO;
using Asset_Management.Hubs;
using Asset_Management.Interfaces;
using Asset_Management.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Security.Claims;

namespace Asset_Management.Services
{
    public enum SignalAction
    {
        AddSignal = 1,
        UpdateSignal = 2,
        DeleteSignal = 3,
    }
    public class SignalsService : ISignalsService
    {
        private readonly AssetDbContext _dbContext;
        private readonly IAssetStorageService _storage;
        private readonly IAssetLogService _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public SignalsService(AssetDbContext dbContext, IAssetStorageService storage, IAssetLogService logger, IHubContext<NotificationHub> hubContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _storage = storage;
            _logger = logger;
            _hubContext = hubContext;
            _httpContextAccessor = httpContextAccessor;
        }

        private string? GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        }
        private string SerializeJson(Asset asset)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };


            string json = JsonConvert.SerializeObject(asset, settings);

            return json;
        }
        public IEnumerable<Signal> GetSignals(int assetId)
        {
            var asset = _dbContext.Assets.Include(a=>a.Signals).FirstOrDefault(a => a.Id == assetId);
            if (asset == null)
                throw new Exception("Asset not found");
            var signals = asset.Signals.ToList();

            return signals;
        }
        public Signal GetSpecificSignal(int assetId, int signalId)
        {
            var asset = _dbContext.Assets.Include(a=>a.Signals).FirstOrDefault(a => a.Id == assetId);
            if (asset == null)
                throw new Exception("Asset not found");
            var signal = asset.Signals.FirstOrDefault(s=>s.Id == signalId);
            if (signal == null)
                throw new Exception("Signal not found");
            return signal;
        }

        private void LoadSignals(Asset parent)
        {
            _dbContext.Entry(parent).Collection(p => p.Signals).Load();
            foreach (var child in parent.Children)
            {
                LoadSignals(child);
            }
        }
        

        private void SaveHierarchyVersion(string? action = null)
        {
            if (string.IsNullOrWhiteSpace(action))
                action = "None";
            //in memory objects reflect db state
            //saving in file for downloading and tracking purpose.
            //recursivley load children in root to represent deep hierarchy
            var root = _dbContext.Assets.FirstOrDefault(a => a.ParentId == null);
            LoadChildren(root);
            _storage.SaveTree(root, action);
        }
        private void LoadChildren(Asset parent)
        {
            _dbContext.Entry(parent).Collection(p => p.Children).Load();
            _dbContext.Entry(parent).Collection(p => p.Signals).Load();
            foreach (var child in parent.Children)
            {
                LoadChildren(child);
            }
        }

        
        public async Task AddSignal(int assetId, GlobalSignalDTO signal)
        {
            var action = "Add Signal";
            var asset = _dbContext.Assets.FirstOrDefault(a => a.Id == assetId);
            if (asset == null)
                throw new Exception("Asset not found");
            try
            {
                asset.Signals.Add(new Signal { Name = signal.Name, ValueType = signal.ValueType, Description = signal.Description });
                _dbContext.SaveChanges();
                SaveHierarchyVersion(action: "Add Signal");
                _logger.Log(action, null, signal.Name);
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveMessage",
                    $"{GetCurrentUser()}",
                    $"Signal added: {signal.Name}"
                );

            }
            catch(DbUpdateException ex)
            {
                throw;
            }

        }
        public async Task UpdateSignal(int assetId, int signalId, GlobalSignalDTO request)
        {
            string action = "Update Signal";
            //write Include as EF core uses lazy loading by default, i.e navigataional properties of Signals are not loaded
            var asset = _dbContext.Assets.Include(a=>a.Signals).FirstOrDefault(a => a.Id == assetId);
            if (asset == null)
                throw new Exception("Asset not found");
            var signal = asset.Signals.FirstOrDefault(s => s.Id == signalId);
            if (signal == null)
                throw new Exception("Signal not found");
            //update changes 
            try
            {
                var currentName = signal.Name;
                signal.Name = request.Name;
                signal.Description = request.Description;
                signal.ValueType = request.ValueType;
                _dbContext.SaveChanges();
                SaveHierarchyVersion( action: "Update Signal");
                _logger.Log(action, null, signal: request.Name);
                await _hubContext.Clients.All.SendAsync(
                    "UpdateSignal",
                    $"{GetCurrentUser()}",
                    $"Signal updated: {currentName} to: {signal.Name}"
                );

            }
            catch (DbUpdateException ex)
            {
                throw;
            }
            

        }

        public async Task DeleteSignal(int signalId, int assetId)
        {
            var action = "Delete Signal";
            var asset = _dbContext.Assets.Include(a => a.Signals).FirstOrDefault(a => a.Id == assetId);
            if (asset == null)
                throw new Exception("Asset not found");
            var signal = asset.Signals.FirstOrDefault(s => s.Id == signalId);
            if (signal == null)
                throw new Exception("Signal not found");


            string signalName = signal.Name; //for sending notification
            Console.WriteLine($"FROM DELETE SIGNAL " + signalName);
            //do deletion
            _dbContext.Signals.Remove(signal);
            _dbContext.SaveChanges();
            SaveHierarchyVersion(action: "Delete Signal");
            _logger.Log(action, null, signal: signal.Name);
            await _hubContext.Clients.All.SendAsync("DeleteSignal", GetCurrentUser() ,$"Deleted Signal {signalName}");
        }




    }

}
