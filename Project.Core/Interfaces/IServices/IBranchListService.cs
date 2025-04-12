using Project.Core.Entities.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Interfaces.IServices
{
    public interface IBranchListService
    {
        Task<bool> IsExists(string key, int? value);
        Task<BranchListResponseViewModel> BranchList(BranchListViewModel entity);
    }
}
