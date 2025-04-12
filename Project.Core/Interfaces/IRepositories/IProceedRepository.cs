using Project.Core.Entities.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Interfaces.IRepositories
{
    public interface IProceedRepository
    {
        Task<bool> IsExists(string key, int? value);
        Task<ProceedResponseViewModel> Proceed(Proceed entity);
    }
}
