namespace ScGen.Lib.Shared.Extensions.DI;

public static class FluentValidation
{
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetAssembly(typeof(ScGenLib)));
        return services;
    }
}