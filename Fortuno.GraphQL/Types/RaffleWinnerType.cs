using Fortuno.Domain.Models;
using HotChocolate.Types;

namespace Fortuno.GraphQL.Types;

public class RaffleWinnerType : ObjectType<RaffleWinner>
{
    protected override void Configure(IObjectTypeDescriptor<RaffleWinner> descriptor)
    {
        descriptor.Name("RaffleWinner");
        descriptor.Field(x => x.RaffleWinnerId).Type<NonNullType<IdType>>();
        descriptor.Field(x => x.RaffleId);
        descriptor.Field(x => x.RaffleAwardId);
        descriptor.Field(x => x.Position);
        descriptor.Field(x => x.PrizeText);
        descriptor.Field(x => x.TicketId);
        descriptor.Field(x => x.UserId);
        descriptor.Field(x => x.CreatedAt);
        // Ticket navegável omitido para evitar descoberta por usuário
        descriptor.Field(x => x.Ticket).Ignore();
    }
}
