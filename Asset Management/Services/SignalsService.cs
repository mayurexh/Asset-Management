using Asset_Management.Controllers;
using Asset_Management.Database;
using Asset_Management.Interfaces;
using Asset_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Asset_Management.Services
{
    public class SignalsService : ISignalsService
    {
        private readonly AssetDbContext _dbContext;
        public SignalsService(AssetDbContext dbContext)
        {
            _dbContext = dbContext;
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


        public void AddSignal(int assetId, GlobalSignalDTO signal)
        {
            var asset = _dbContext.Assets.FirstOrDefault(a => a.Id == assetId);
            if (asset == null)
                throw new Exception("Asset not found");
            try
            {
                asset.Signals.Add(new Signal { Name = signal.Name, ValueType = signal.ValueType, Description = signal.Description });
                _dbContext.SaveChanges();
            }
            catch(DbUpdateException ex)
            {
                throw;
            }
            

        }
        public void UpdateSignal(int assetId, int signalId, GlobalSignalDTO request)
        {
            //write Include as EF core uses lazy loading by default, i.e navigataional properties of Signals are not loaded
            var asset = _dbContext.Assets.Include(a=>a.Signals).FirstOrDefault(a => a.Id == assetId);
            if (asset == null)
                throw new Exception("Asset not found");
            var signal = asset.Signals.FirstOrDefault(s => s.Id == signalId);
            if (signal == null)
                throw new Exception("Signal not found");
            //update changes 
            signal.Name = request.Name;
            signal.Description = request.Description;
            signal.ValueType = request.ValueType;
            _dbContext.SaveChanges();

        }

        public void DeleteSignal(int signalId, int assetId)
        {
            var asset = _dbContext.Assets.Include(a => a.Signals).FirstOrDefault(a => a.Id == assetId);
            if (asset == null)
                throw new Exception("Asset not found");
            var signal = asset.Signals.FirstOrDefault(s => s.Id == signalId);
            if (signal == null)
                throw new Exception("Signal not found");

            //do deletion
            _dbContext.Signals.Remove(signal);
            _dbContext.SaveChanges();
        }




    }

}
