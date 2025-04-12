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
    public class BranchListService : IBranchListService
    {
        private readonly IBaseMapper<BranchListViewModel, BranchList> _branchListMapper;
        private readonly IBranchListRepository _branchListRepository;

        public BranchListService(
            IBaseMapper<BranchListViewModel, BranchList> branchListMapper,
            IBranchListRepository branchListRepository)
        {
            _branchListMapper = branchListMapper;
            _branchListRepository = branchListRepository;
        }

        public async Task<bool> IsExists(string key, int? value)
        {
            return await _branchListRepository.IsExists(key, value);
        }

        public async Task<BranchListResponseViewModel> BranchList(BranchListViewModel model)
        {
            // Map ProceedViewModel to Proceed entity
            var entity = _branchListMapper.MapModel(model);

            // Call repository and return response directly
            return await _branchListRepository.BranchList(entity);
        }
    }
}

