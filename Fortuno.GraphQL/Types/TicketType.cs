using Fortuno.Domain.Models;
using HotChocolate.Types;

namespace Fortuno.GraphQL.Types;

public class TicketType : ObjectType<Ticket>
{
    protected override void Configure(IObjectTypeDescriptor<Ticket> descriptor)
    {
        descriptor.Name("Ticket");
        descriptor.Field(x => x.TicketId).Type<NonNullType<IdType>>();
        descriptor.Field(x => x.LotteryId);
        descriptor.Field(x => x.UserId);
        descriptor.Field(x => x.InvoiceId);
        descriptor.Field(x => x.TicketNumber);
        descriptor.Field(x => x.RefundState);
        descriptor.Field(x => x.CreatedAt);
        descriptor.Field(x => x.Lottery).Ignore();
    }
}
