using Project.Core.Entities.Business;
using Project.Core.Entities.General;


namespace Project.Core.Interfaces.IServices
{
    public interface ITransactionStatusService
    {
        Task<bool> IsExists(string key, int? value);
        Task<TransactionStatusResponseViewModel> TransactionStatus(TransactionStatusViewModel entity);
    }
}
