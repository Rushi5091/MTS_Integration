using Project.Core.Entities.Business;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IMapper;
using Project.Core.Interfaces.IRepositories;
using Project.Core.Interfaces.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Services
{
    public class TransactionStatusService : ITransactionStatusService
    {
        private readonly IBaseMapper<TransactionStatusViewModel, TransactionStatus> _transactionStatusMapper;
        private readonly ITransactionStatusRepository _transactionStatusRepository;

        public TransactionStatusService(
            IBaseMapper<TransactionStatusViewModel, TransactionStatus> transactionStatusMapper,
            ITransactionStatusRepository transactionStatusRepository)
        {
            _transactionStatusMapper = transactionStatusMapper;
            _transactionStatusRepository = transactionStatusRepository;
        }

        public async Task<bool> IsExists(string key, int? value)
        {
            return await _transactionStatusRepository.IsExists(key, value);
        }

        public async Task<TransactionStatusResponseViewModel> TransactionStatus(TransactionStatusViewModel model)
        {
            // Map ProceedViewModel to Proceed entity
            var entity = _transactionStatusMapper.MapModel(model);

            // Call repository and return response directly
            return await _transactionStatusRepository.TransactionStatus(entity);
        }
    }
}
