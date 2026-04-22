namespace Fortuno.Domain.Enums;

// Pareado 1:1 com ProxyPay.DTO.InvoiceStatusEnum.
public enum TicketOrderStatus
{
    Pending = 1,
    Sent = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5,
    Expired = 6
}
