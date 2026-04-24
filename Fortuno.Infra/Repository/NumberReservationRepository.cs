using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class NumberReservationRepository : Repository<NumberReservation>, INumberReservationRepository<NumberReservation>
{
    public NumberReservationRepository(FortunoContext context) : base(context) { }

    public async Task<List<long>> ListActiveReservedNumbersAsync(long lotteryId)
    {
        var now = DateTime.UtcNow;
        return await _context.NumberReservations.AsNoTracking()
            .Where(x => x.LotteryId == lotteryId && x.ExpiresAt > now)
            .Select(x => x.TicketNumber)
            .ToListAsync();
    }

    public async Task<List<NumberReservation>> ListByUserAndLotteryAsync(long userId, long lotteryId)
    {
        var now = DateTime.UtcNow;
        return await _context.NumberReservations.AsNoTracking()
            .Where(x => x.UserId == userId && x.LotteryId == lotteryId && x.ExpiresAt > now)
            .ToListAsync();
    }

    public async Task<bool> IsNumberReservedAsync(long lotteryId, long ticketNumber)
    {
        var now = DateTime.UtcNow;
        return await _context.NumberReservations.AsNoTracking()
            .AnyAsync(x => x.LotteryId == lotteryId
                && x.TicketNumber == ticketNumber
                && x.ExpiresAt > now);
    }

    public async Task<bool> AreNumbersAvailableAsync(long lotteryId, IEnumerable<long> numbers)
    {
        var now = DateTime.UtcNow;
        var wanted = numbers.ToList();
        var conflict = await _context.NumberReservations.AsNoTracking()
            .AnyAsync(x => x.LotteryId == lotteryId && x.ExpiresAt > now && wanted.Contains(x.TicketNumber));
        if (conflict) return false;
        var sold = await _context.Tickets.AsNoTracking()
            .AnyAsync(x => x.LotteryId == lotteryId && wanted.Contains(x.TicketNumber));
        return !sold;
    }

    public async Task<List<NumberReservation>> InsertBatchAsync(IEnumerable<NumberReservation> reservations)
    {
        var list = reservations.ToList();
        _context.NumberReservations.AddRange(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task ExpireByUserAndLotteryAsync(long userId, long lotteryId)
    {
        var now = DateTime.UtcNow;
        var list = await _context.NumberReservations
            .Where(x => x.UserId == userId && x.LotteryId == lotteryId && x.ExpiresAt > now)
            .ToListAsync();
        foreach (var r in list) r.ExpiresAt = now;
        await _context.SaveChangesAsync();
    }
}
