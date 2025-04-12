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
    public class CancelTransactionService : ICancelTransactionService
    {
        private readonly IBaseMapper<CancelTransactionViewModel, CancelTransaction> _CancelTransactionMapper;
        private readonly ICancelTransactionRepository _CancelTransactionRepository;

        public CancelTransactionService(
            IBaseMapper<CancelTransactionViewModel, CancelTransaction> CancelTransactionMapper,
            ICancelTransactionRepository CancelTransactionRepository)
        {
            _CancelTransactionMapper = CancelTransactionMapper;
            _CancelTransactionRepository = CancelTransactionRepository;
        }

        public async Task<bool> IsExists(string key, int? value)
        {
            return await _CancelTransactionRepository.IsExists(key, value);
        }

        public async Task<CancelTransactionResponseViewModel> CancelTransaction(CancelTransactionViewModel model)
        {
            // Map ProceedViewModel to Proceed entity
            var entity = _CancelTransactionMapper.MapModel(model);

            // Call repository and return response directly
            return await _CancelTransactionRepository.CancelTransaction(entity);
        }
    }
}
