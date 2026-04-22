using FluentValidation;
using FluentValidation.AspNetCore;
using Fortuno.API.Validators;
using Fortuno.Application;
using Fortuno.DTO.Common;
using Fortuno.DTO.Settings;
using Fortuno.GraphQL;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using NAuth.ACL;

// Schema usa `timestamp without time zone`; mantém comportamento pré-Npgsql 6
// aceitando DateTime com Kind=UTC sem conversão a timestamptz.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Fortuno wiring (DI centralizado em Fortuno.Application/Startup)
builder.Services.AddFortuno(builder.Configuration);

// FluentValidation (validadores auto-registrados via assembly scan)
builder.Services.AddValidatorsFromAssemblyContaining<LotteryInsertInfoValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Controllers + JSON camelCase (constituição §2)
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Autenticação NAuth
builder.Services.AddNAuthAuthentication();
builder.Services.AddAuthorization();

// GraphQL (HotChocolate)
builder.Services.AddFortunoGraphQL();

// Swashbuckle (FR-044 documentação REST)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Fortuno API", Version = "v1" });
    c.AddSecurityDefinition("Basic", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "NAuth Basic token"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Basic"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS — AllowAnyOrigin apenas quando config permitir (constituição §4)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var corsSection = builder.Configuration.GetSection("Cors");
        var allowAny = corsSection.GetValue<bool>("AllowAnyOrigin");
        if (allowAny)
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        else
            policy.WithOrigins().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Exception handler global (constituição §6)
app.UseExceptionHandler(err =>
{
    err.Run(async ctx =>
    {
        var feature = ctx.Features.Get<IExceptionHandlerFeature>();
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        var resp = ApiResponse.Fail(feature?.Error.Message ?? "Erro interno.");
        await ctx.Response.WriteAsJsonAsync(resp);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Fortuno.API.Middlewares.EnsureUserReferrerMiddleware>();
app.MapControllers();
app.MapGraphQL("/graphql");

app.Run();
