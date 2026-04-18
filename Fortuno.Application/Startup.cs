using Fortuno.Domain.Models;
using Fortuno.DTO.Settings;
using Fortuno.Infra.AppServices;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;
using Fortuno.Infra.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NAuth.ACL;
using zTools.ACL;
using zTools.ACL.Interfaces;
using zTools.DTO.Settings;

namespace Fortuno.Application;

public static class Startup
{
    public static IServiceCollection AddFortuno(this IServiceCollection services, IConfiguration config)
    {
        // Settings
        services.Configure<FortunoSettings>(s =>
        {
            s.Database.ConnectionString = config.GetConnectionString("FortunoContext") ?? string.Empty;
            config.GetSection("NAuth").Bind(s.NAuth);
            config.GetSection("ProxyPay").Bind(s.ProxyPay);
            config.GetSection("ZTools").Bind(s.ZTools);
            config.GetSection("Cors").Bind(s.Cors);
        });

        // DbContext
        services.AddDbContext<FortunoContext>((sp, options) =>
        {
            var cs = config.GetConnectionString("FortunoContext");
            options.UseNpgsql(cs);
        });

        // HttpContext accessor (usado por NAuthAppService para ler token Basic)
        services.AddHttpContextAccessor();

        // Repositories
        services.AddScoped<ILotteryRepository<Lottery>, LotteryRepository>();
        services.AddScoped<ILotteryImageRepository<LotteryImage>, LotteryImageRepository>();
        services.AddScoped<ILotteryComboRepository<LotteryCombo>, LotteryComboRepository>();
        services.AddScoped<ITicketRepository<Ticket>, TicketRepository>();
        services.AddScoped<IRaffleRepository<Raffle>, RaffleRepository>();
        services.AddScoped<IRaffleAwardRepository<RaffleAward>, RaffleAwardRepository>();
        services.AddScoped<IRaffleWinnerRepository<RaffleWinner>, RaffleWinnerRepository>();
        services.AddScoped<IUserReferrerRepository<UserReferrer>, UserReferrerRepository>();
        services.AddScoped<IInvoiceReferrerRepository<InvoiceReferrer>, InvoiceReferrerRepository>();
        services.AddScoped<IRefundLogRepository<RefundLog>, RefundLogRepository>();
        services.AddScoped<INumberReservationRepository<NumberReservation>, NumberReservationRepository>();
        services.AddScoped<IWebhookEventRepository<WebhookEvent>, WebhookEventRepository>();

        // NAuth (inclui IUserClient, IRoleClient, TenantDelegatingHandler)
        services.Configure<NAuth.DTO.Settings.NAuthSetting>(config.GetSection("NAuth"));
        services.AddNAuth();

        // zTools (IFileClient, IStringClient etc.)
        services.Configure<zToolsetting>(config.GetSection("ZTools"));
        services.AddHttpClient<IFileClient, FileClient>();
        services.AddHttpClient<IStringClient, StringClient>();

        // ProxyPay (HTTP client tipado)
        services.AddHttpClient<IProxyPayAppService, ProxyPayAppService>();

        // AppServices (wrappers do Fortuno)
        services.AddScoped<INAuthAppService, NAuthAppService>();
        services.AddScoped<IZToolsAppService, ZToolsAppService>();

        // Domain Services — fundacionais
        services.AddScoped<Fortuno.Domain.Interfaces.INumberCompositionService, Fortuno.Domain.Services.NumberCompositionService>();
        services.AddScoped<Fortuno.Domain.Interfaces.ISlugService, Fortuno.Domain.Services.SlugService>();
        services.AddScoped<Fortuno.Domain.Interfaces.IStoreOwnershipGuard, Fortuno.Domain.Services.StoreOwnershipGuard>();

        // Domain Services — US1 (Lottery creation and publish)
        services.AddScoped<Fortuno.Domain.Interfaces.ILotteryService, Fortuno.Domain.Services.LotteryService>();
        services.AddScoped<Fortuno.Domain.Interfaces.ILotteryImageService, Fortuno.Domain.Services.LotteryImageService>();
        services.AddScoped<Fortuno.Domain.Interfaces.IRaffleService, Fortuno.Domain.Services.RaffleService>();
        services.AddScoped<Fortuno.Domain.Interfaces.IRaffleAwardService, Fortuno.Domain.Services.RaffleAwardService>();

        // Domain Services — US2 (Purchase + Tickets + Referrer)
        services.AddScoped<Fortuno.Domain.Interfaces.IUserReferrerService, Fortuno.Domain.Services.UserReferrerService>();
        services.AddScoped<Fortuno.Domain.Interfaces.IPurchaseService, Fortuno.Domain.Services.PurchaseService>();
        services.AddScoped<Fortuno.Domain.Interfaces.ITicketService, Fortuno.Domain.Services.TicketService>();

        // Domain Services — US4 (Management + Refund)
        services.AddScoped<Fortuno.Domain.Interfaces.ILotteryComboService, Fortuno.Domain.Services.LotteryComboService>();
        services.AddScoped<Fortuno.Domain.Interfaces.IRefundService, Fortuno.Domain.Services.RefundService>();

        // Domain Services — US5 (Referrals)
        services.AddScoped<Fortuno.Domain.Interfaces.IReferralService, Fortuno.Domain.Services.ReferralService>();

        return services;
    }
}
