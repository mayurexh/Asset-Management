using Asset_Management.Controllers;
using Asset_Management.DTO;
using Asset_Management.Models;

namespace Asset_Management.Interfaces
{
    public interface ISignalsService
    {
        IEnumerable<Signal> GetSignals(int assetId);

        Signal GetSpecificSignal(int assetId, int signalId);

        Task AddSignal(int assetId, GlobalSignalDTO signal);

        Task UpdateSignal(int assetId, int signalId , GlobalSignalDTO signal);

        Task DeleteSignal(int signalId, int assetId);
    }
}
