using Asset_Management.Controllers;
using Asset_Management.Models;

namespace Asset_Management.Interfaces
{
    public interface ISignalsService
    {
        IEnumerable<Signal> GetSignals(int assetId);

        void AddSignal(int assetId, GlobalSignalDTO signal);

        void UpdateSignal(int assetId, int signalId , GlobalSignalDTO signal);

        void DeleteSignal(int signalId, int assetId);
    }
}
