using Fortuno.Domain.Models;
using Fortuno.GraphQL.Queries;
using Fortuno.GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Fortuno.GraphQL;

public static class Startup
{
    public static IServiceCollection AddFortunoGraphQL(this IServiceCollection services)
    {
        services.AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<Query>()
            .AddType<LotteryType>()
            .AddType<RaffleType>()
            .AddType<RaffleWinnerType>()
            .AddType<TicketType>()
            .AddProjections()
            .AddFiltering()
            .AddSorting();

        return services;
    }
}
