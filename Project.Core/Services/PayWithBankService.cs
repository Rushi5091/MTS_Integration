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
    public class PayWithBankService : IPayWithBankService
    {
        private readonly IBaseMapper<PayWithBankViewModel, PayWithBank> _PayWithBankMapper;
        private readonly IPayWithBankRepository _PayWithBankRepository;

        public PayWithBankService(
            IBaseMapper<PayWithBankViewModel, PayWithBank> PayWithBankMapper,
            IPayWithBankRepository PayWithBankRepository)
        {
            _PayWithBankMapper = PayWithBankMapper;
            _PayWithBankRepository = PayWithBankRepository;
        }

 

        public async Task<PayWithBankResponseViewModel> PayWithBank(PayWithBankViewModel model)
        {
            // Map ProceedViewModel to Proceed entity
            var entity = _PayWithBankMapper.MapModel(model);

            // Call repository and return response directly
            return await _PayWithBankRepository.PayWithBank(entity);
        }
    }
}
