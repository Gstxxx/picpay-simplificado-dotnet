using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PicPay.Application.Auth;
using PicPay.Application.Users;

namespace PicPay.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Enabled = false;
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        return services;
    }
}
