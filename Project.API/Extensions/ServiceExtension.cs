using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Project.Core.Entities.Business;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IMapper;
using Project.Core.Interfaces.IRepositories;
using Project.Core.Interfaces.IServices;
using Project.Core.Mapper;
using Project.Core.Services;
using Project.Infrastructure.Repositories;


namespace Project.API.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection RegisterService(this IServiceCollection services)
        {
            #region Services
            services.AddScoped<IProceedService, ProceedService>(); 
            services.AddScoped<IProceedWithWalletService, ProceedWithWalletService>(); 
            services.AddScoped<ITransactionStatusService, TransactionStatusService>(); 
            services.AddScoped<ICancelTransactionService, CancelTransactionService>(); 
            services.AddScoped<IBranchListService, BranchListService>();
            services.AddScoped<IPayWithBankService, PayWithBankService>();
            services.AddScoped<IPaymentStatusService, PaymentStatusService>();
            #endregion

            #region Repositories
            services.AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddTransient<IProceedRepository, ProceedRepository>();   
            services.AddTransient<IProceedWithWalletRepository, ProceedWithWalletRepository>();   
            services.AddTransient<ITransactionStatusRepository, TransactionStatusRepository>();          
            services.AddTransient<ICancelTransactionRepository, CancelTransactionRepository>();
            services.AddTransient<IBranchListRepository, BranchListRepository>();
            services.AddTransient<IPayWithBankRepository, PayWithBankRepository>();
            services.AddTransient<IPaymentStatusRepository, PaymentStatusRepository>();
            #endregion

            #region Mapper
            var configuration = new MapperConfiguration(cfg =>
            {

                cfg.CreateMap<Proceed, ProceedViewModel>();
                cfg.CreateMap<ProceedViewModel, Proceed>();

                cfg.CreateMap<CreateNGNWallet, CreateNGNWalletViewModel>();
                cfg.CreateMap<CreateNGNWalletViewModel, CreateNGNWallet>();

                cfg.CreateMap<ProceedWithWallet, ProceedWithWalletViewModel>();
                cfg.CreateMap<ProceedWithWalletViewModel, ProceedWithWallet>();

                cfg.CreateMap<TransactionStatus, TransactionStatusViewModel>();
                cfg.CreateMap<TransactionStatusViewModel, TransactionStatus>();

                cfg.CreateMap<CancelTransaction, CancelTransactionViewModel>();
                cfg.CreateMap<CancelTransactionViewModel, CancelTransaction>();

                cfg.CreateMap<BranchList, BranchListViewModel>();
                cfg.CreateMap<BranchListViewModel, BranchList>();

                cfg.CreateMap<PayWithBank, PayWithBankViewModel>();
                cfg.CreateMap<PayWithBankViewModel, PayWithBank>();

                cfg.CreateMap<PaymentStatus, PaymentStatusViewModel>();
                cfg.CreateMap<PaymentStatusViewModel, PaymentStatus>();

            });

            IMapper mapper = configuration.CreateMapper();

            // Register the IMapperService implementation with your dependency injection container

            services.AddSingleton<IBaseMapper<Proceed, ProceedViewModel>>(new BaseMapper<Proceed, ProceedViewModel>(mapper));
            services.AddSingleton<IBaseMapper<ProceedViewModel, Proceed>>(new BaseMapper<ProceedViewModel, Proceed>(mapper));

            services.AddSingleton<IBaseMapper<ProceedWithWallet, ProceedWithWalletViewModel>>(new BaseMapper<ProceedWithWallet, ProceedWithWalletViewModel>(mapper));
            services.AddSingleton<IBaseMapper<ProceedWithWalletViewModel, ProceedWithWallet>>(new BaseMapper<ProceedWithWalletViewModel, ProceedWithWallet>(mapper));


            services.AddSingleton<IBaseMapper<CreateNGNWallet, CreateNGNWalletViewModel>>(new BaseMapper<CreateNGNWallet, CreateNGNWalletViewModel>(mapper));
            services.AddSingleton<IBaseMapper<CreateNGNWalletViewModel, CreateNGNWallet>>(new BaseMapper<CreateNGNWalletViewModel, CreateNGNWallet>(mapper));

            services.AddSingleton<IBaseMapper<TransactionStatusViewModel, TransactionStatus>>(new BaseMapper<TransactionStatusViewModel, TransactionStatus>(mapper));
            services.AddSingleton<IBaseMapper<CancelTransactionViewModel, CancelTransaction>>(new BaseMapper<CancelTransactionViewModel, CancelTransaction>(mapper));
            services.AddSingleton<IBaseMapper<BranchListViewModel, BranchList>>(new BaseMapper<BranchListViewModel, BranchList>(mapper));

            services.AddSingleton<IBaseMapper<PayWithBank, PayWithBankViewModel>>(new BaseMapper<PayWithBank, PayWithBankViewModel>(mapper));
            services.AddSingleton<IBaseMapper<PayWithBankViewModel, PayWithBank>>(new BaseMapper<PayWithBankViewModel, PayWithBank>(mapper));

            services.AddSingleton<IBaseMapper<PaymentStatus, PaymentStatusViewModel>>(new BaseMapper<PaymentStatus, PaymentStatusViewModel>(mapper));
            services.AddSingleton<IBaseMapper<PaymentStatusViewModel, PaymentStatus>>(new BaseMapper<PaymentStatusViewModel, PaymentStatus>(mapper));
            #endregion

            return services;
        }
    }
}
