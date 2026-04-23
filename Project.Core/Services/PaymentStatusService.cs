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
  
        public class PaymentStatusService : IPaymentStatusService
        {
            private readonly IBaseMapper<PaymentStatusViewModel, PaymentStatus> _PaymentStatusMapper;
            private readonly IPaymentStatusRepository _PaymentStatusRepository;

            public PaymentStatusService(
                IBaseMapper<PaymentStatusViewModel, PaymentStatus> PaymentStatusMapper,
                IPaymentStatusRepository PaymentStatusRepository)
            {
            _PaymentStatusMapper = PaymentStatusMapper;
            _PaymentStatusRepository = PaymentStatusRepository;
            }

            public async Task<PaymentStatusResponseViewModel> PaymentStatus(PaymentStatusViewModel model)
            {
                // Map ProceedViewModel to Proceed entity
                var entity = _PaymentStatusMapper.MapModel(model);

                // Call repository and return response directly
                return await _PaymentStatusRepository.PaymentStatus(entity);
            }
        }
    
}
