using Project.Core.Entities.Business;
using Project.Core.Entities.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Interfaces.IRepositories
{
    public interface ICancelTransactionRepository
    {
        Task<bool> IsExists(string key, int? value);
        Task<CancelTransactionResponseViewModel> CancelTransaction(CancelTransaction entity);
    }
}
