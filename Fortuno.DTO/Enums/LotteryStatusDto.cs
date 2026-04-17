namespace Fortuno.DTO.Enums;

public enum LotteryStatusDto
{
    Draft = 1,
    Open = 2,
    Closed = 3,
    Cancelled = 4
}

public enum RaffleStatusDto
{
    Open = 1,
    Closed = 2,
    Cancelled = 3
}

public enum TicketRefundStateDto
{
    None = 1,
    PendingRefund = 2,
    Refunded = 3
}

public enum NumberTypeDto
{
    Int64 = 1,
    Composed3 = 3,
    Composed4 = 4,
    Composed5 = 5,
    Composed6 = 6,
    Composed7 = 7,
    Composed8 = 8
}

public enum PurchaseAssignmentModeDto
{
    Random = 1,
    UserPicks = 2
}
