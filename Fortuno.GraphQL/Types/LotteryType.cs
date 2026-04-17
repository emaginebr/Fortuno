using Fortuno.Domain.Models;
using HotChocolate.Types;

namespace Fortuno.GraphQL.Types;

public class LotteryType : ObjectType<Lottery>
{
    protected override void Configure(IObjectTypeDescriptor<Lottery> descriptor)
    {
        descriptor.Name("Lottery");
        descriptor.Field(x => x.LotteryId).Type<NonNullType<IdType>>();
        descriptor.Field(x => x.StoreId);
        descriptor.Field(x => x.Name);
        descriptor.Field(x => x.Slug);
        descriptor.Field(x => x.DescriptionMd);
        descriptor.Field(x => x.RulesMd);
        descriptor.Field(x => x.PrivacyPolicyMd);
        descriptor.Field(x => x.TicketPrice);
        descriptor.Field(x => x.TotalPrizeValue);
        descriptor.Field(x => x.TicketMin);
        descriptor.Field(x => x.TicketMax);
        descriptor.Field(x => x.TicketNumIni);
        descriptor.Field(x => x.TicketNumEnd);
        descriptor.Field(x => x.NumberType);
        descriptor.Field(x => x.NumberValueMin);
        descriptor.Field(x => x.NumberValueMax);
        descriptor.Field(x => x.ReferralPercent);
        descriptor.Field(x => x.Status);
        descriptor.Field(x => x.CreatedAt);
        descriptor.Field(x => x.UpdatedAt);
        descriptor.Field(x => x.Images);
        descriptor.Field(x => x.Combos);
        descriptor.Field(x => x.Raffles);
        // CancelReason/CancelledByUserId/CancelledAt ignorados publicamente (audit)
        descriptor.Field(x => x.CancelReason).Ignore();
        descriptor.Field(x => x.CancelledByUserId).Ignore();
        descriptor.Field(x => x.CancelledAt).Ignore();
        descriptor.Field(x => x.Tickets).Ignore(); // evita exposição de tickets de outros usuários
    }
}

public class RaffleType : ObjectType<Raffle>
{
    protected override void Configure(IObjectTypeDescriptor<Raffle> descriptor)
    {
        descriptor.Name("Raffle");
        descriptor.Field(x => x.RaffleId).Type<NonNullType<IdType>>();
        descriptor.Field(x => x.LotteryId);
        descriptor.Field(x => x.Name);
        descriptor.Field(x => x.DescriptionMd);
        descriptor.Field(x => x.RaffleDatetime);
        descriptor.Field(x => x.VideoUrl);
        descriptor.Field(x => x.IncludePreviousWinners);
        descriptor.Field(x => x.Status);
        descriptor.Field(x => x.CreatedAt);
        descriptor.Field(x => x.UpdatedAt);
        descriptor.Field(x => x.Awards);
        descriptor.Field(x => x.Winners);
    }
}

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
