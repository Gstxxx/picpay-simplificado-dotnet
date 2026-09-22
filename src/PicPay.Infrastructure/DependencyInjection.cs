using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PicPay.Application.Abstractions;
using PicPay.Infrastructure.Auth;
using PicPay.Infrastructure.Http;
using PicPay.Infrastructure.Notifications;
using PicPay.Infrastructure.Persistence;
using PicPay.Infrastructure.Persistence.Repositories;

namespace PicPay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Connection string 'Postgres' is missing."))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<INotificationOutboxRepository, NotificationOutboxRepository>();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddResilientClient<IPaymentAuthorizer, HttpPaymentAuthorizer, AuthorizerOptions>(AuthorizerOptions.Section);
        services.AddResilientClient<INotificationSender, HttpNotificationSender, NotifierOptions>(NotifierOptions.Section);

        services.AddOptions<OutboxOptions>()
            .BindConfiguration(OutboxOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxWorker>();

        return services;
    }
}
