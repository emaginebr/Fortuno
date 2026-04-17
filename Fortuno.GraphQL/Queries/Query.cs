using Fortuno.DTO.Common;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.Commission;
using Fortuno.DTO.Referrer;
using Fortuno.Infra.Context;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.GraphQL.Queries;

public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Lottery> Lotteries([Service] FortunoContext context)
        => context.Lotteries.AsNoTracking();

    public async Task<Lottery?> LotteryBySlug([Service] FortunoContext context, string slug)
        => await context.Lotteries.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug);

    public async Task<Lottery?> LotteryById([Service] FortunoContext context, long id)
        => await context.Lotteries.AsNoTracking().FirstOrDefaultAsync(x => x.LotteryId == id);

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Raffle> Raffles([Service] FortunoContext context, long lotteryId)
        => context.Raffles.AsNoTracking().Where(x => x.LotteryId == lotteryId);

    public async Task<Raffle?> RaffleById([Service] FortunoContext context, long id)
        => await context.Raffles.AsNoTracking().FirstOrDefaultAsync(x => x.RaffleId == id);

    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Ticket> MyTickets(
        [Service] FortunoContext context,
        [Service] IHttpContextAccessor httpContext,
        long? lotteryId = null,
        long? numberContains = null)
    {
        var userId = httpContext.HttpContext!.User.GetCurrentUserId();
        var q = context.Tickets.AsNoTracking().Where(x => x.UserId == userId);
        if (lotteryId.HasValue) q = q.Where(x => x.LotteryId == lotteryId.Value);
        if (numberContains.HasValue) q = q.Where(x => x.TicketNumber == numberContains.Value);
        return q;
    }

    [Authorize]
    public async Task<ReferrerEarningsPanel> MyReferrerSummary(
        [Service] IReferralService referrals,
        [Service] IHttpContextAccessor httpContext)
    {
        var userId = httpContext.HttpContext!.User.GetCurrentUserId();
        return await referrals.GetEarningsForReferrerAsync(userId);
    }

    [Authorize]
    public async Task<LotteryCommissionsPanel> CommissionsByLottery(
        [Service] IReferralService referrals,
        [Service] IHttpContextAccessor httpContext,
        long lotteryId)
    {
        var userId = httpContext.HttpContext!.User.GetCurrentUserId();
        return await referrals.GetPayablesForLotteryAsync(userId, lotteryId);
    }
}
