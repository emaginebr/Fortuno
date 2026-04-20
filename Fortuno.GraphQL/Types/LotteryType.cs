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
