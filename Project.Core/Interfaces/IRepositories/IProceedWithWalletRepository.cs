using Project.Core.Entities.Business;
using Project.Core.Entities.General;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Project.Core.Interfaces.IRepositories
{
    public interface IProceedWithWalletRepository
    {
        //Task<bool> IsExists(string key, int? value);
        Task<ProceedResponseViewModel> ProceedWithWallet(ProceedWithWallet entity);


        Task<CreateNGNWalletResponseViewModel> CreateNGNWallet(CreateNGNWallet entity);


    }

    //public interface ICreateNGNWalletRepository
    //{
    //    Task<CreateNGNWalletResponseViewModel> CreateNGNWallet(CreateNGNWallet entity);
    //}
}
