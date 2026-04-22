using Fortuno.Domain.Enums;
using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class TicketOrderRepository : Repository<TicketOrder>, ITicketOrderRepository<TicketOrder>
{
    public TicketOrderRepository(FortunoContext context) : base(context) { }

    public async Task<TicketOrder?> GetByInvoiceIdAsync(long invoiceId)
        => await _context.TicketOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);

    public Task<int> TryMarkPaidAsync(long purchaseIntentId)
        => TryMarkAsync(purchaseIntentId, TicketOrderStatus.Paid);

    public Task<int> TryMarkExpiredAsync(long purchaseIntentId)
        => TryMarkAsync(purchaseIntentId, TicketOrderStatus.Expired);

    public Task<int> TryMarkCancelledAsync(long purchaseIntentId)
        => TryMarkAsync(purchaseIntentId, TicketOrderStatus.Cancelled);

    private Task<int> TryMarkAsync(long purchaseIntentId, TicketOrderStatus target)
        => _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE fortuna_ticket_orders
               SET status = {(int)target}, updated_at = now()
             WHERE ticket_order_id = {purchaseIntentId}
               AND status = {(int)TicketOrderStatus.Pending}");
}
