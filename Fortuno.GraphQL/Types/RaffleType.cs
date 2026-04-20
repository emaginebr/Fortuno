using Fortuno.Domain.Models;
using HotChocolate.Types;

namespace Fortuno.GraphQL.Types;

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
