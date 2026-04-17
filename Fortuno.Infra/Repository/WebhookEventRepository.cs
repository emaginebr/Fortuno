using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class WebhookEventRepository : Repository<WebhookEvent>, IWebhookEventRepository<WebhookEvent>
{
    public WebhookEventRepository(FortunoContext context) : base(context) { }

    public async Task<bool> ExistsAsync(long invoiceId, string eventType)
        => await _context.WebhookEvents.AsNoTracking()
            .AnyAsync(x => x.InvoiceId == invoiceId && x.EventType == eventType);

    public async Task<WebhookEvent> InsertIfNotExistsAsync(WebhookEvent evt)
    {
        try
        {
            _context.WebhookEvents.Add(evt);
            await _context.SaveChangesAsync();
            return evt;
        }
        catch (DbUpdateException)
        {
            var existing = await _context.WebhookEvents.AsNoTracking()
                .FirstOrDefaultAsync(x => x.InvoiceId == evt.InvoiceId && x.EventType == evt.EventType);
            return existing ?? evt;
        }
    }
}
