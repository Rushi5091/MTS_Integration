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
    public class ProceedWithWalletService : IProceedWithWalletService
    {
        private readonly IBaseMapper<ProceedWithWalletViewModel, ProceedWithWallet> _ProceedWithWalletMapper;
        private readonly IBaseMapper<CreateNGNWalletViewModel, CreateNGNWallet> _CreateNGNWalletMapper;
        private readonly IProceedWithWalletRepository _ProceedWithWalletRepository;
        private readonly IProceedWithWalletRepository _CreateNGNWalletRepository;
        public ProceedWithWalletService(
            IBaseMapper<ProceedWithWalletViewModel, ProceedWithWallet> ProceedWithWalletMapper,
            IProceedWithWalletRepository ProceedWithWalletRepository,
            IBaseMapper<CreateNGNWalletViewModel, CreateNGNWallet> createNGNWalletMapper,
            IProceedWithWalletRepository createNGNWalletRepository)
        {
            _ProceedWithWalletMapper = ProceedWithWalletMapper;
            _ProceedWithWalletRepository = ProceedWithWalletRepository;
            _CreateNGNWalletMapper = createNGNWalletMapper;
            _CreateNGNWalletRepository = createNGNWalletRepository;
        }

        //public async Task<bool> IsExists(string key, int? value)
        //{
        //    return await _ProceedWithWalletRepository.IsExists(key, value);
        //}

        public async Task<ProceedResponseViewModel> ProceedWithWallet(ProceedWithWalletViewModel model)
        {
            // Map ProceedViewModel to Proceed entity
            var entity = _ProceedWithWalletMapper.MapModel(model);

            // Call repository and return response directly
            return await _ProceedWithWalletRepository.ProceedWithWallet(entity);
        }


        public async Task<CreateNGNWalletResponseViewModel> CreateNGNWallet(CreateNGNWalletViewModel model)
        {
            // Map ProceedViewModel to Proceed entity
            var entity = _CreateNGNWalletMapper.MapModel(model);

            // Call repository and return response directly
            return await _CreateNGNWalletRepository.CreateNGNWallet(entity);
        }

    }
}
