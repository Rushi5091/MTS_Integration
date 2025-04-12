using Project.Core.Entities.Business;
using Project.Core.Entities.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Interfaces.IServices
{
    public interface IProceedWithWalletService
    {
       // Task<bool> IsExists(string v, int? transaction_ID);
        Task<ProceedResponseViewModel> ProceedWithWallet(ProceedWithWalletViewModel model);
        
        Task<CreateNGNWalletResponseViewModel> CreateNGNWallet(CreateNGNWalletViewModel model);
    }
}
