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
    public class ProceedService : IProceedService
    {
        private readonly IBaseMapper<ProceedViewModel, Proceed> _proceedMapper;
        private readonly IProceedRepository _proceedRepository;

        public ProceedService(
            IBaseMapper<ProceedViewModel, Proceed> proceedMapper,
            IProceedRepository proceedRepository)
        {
            _proceedMapper = proceedMapper;
            _proceedRepository = proceedRepository;
        }

        public async Task<bool> IsExists(string key, int? value)
        {
            return await _proceedRepository.IsExists(key, value);
        }

        public async Task<ProceedResponseViewModel> Proceed(ProceedViewModel model)
        {
            // Map ProceedViewModel to Proceed entity
            var entity = _proceedMapper.MapModel(model);

            // Call repository and return response directly
            return await _proceedRepository.Proceed(entity);
        }
    }
}
