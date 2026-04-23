using Project.Core.Entities.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Interfaces.IServices
{
    public interface IPaymentStatusService
    {
        Task<PaymentStatusResponseViewModel> PaymentStatus(PaymentStatusViewModel model);
    }
}
