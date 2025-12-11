namespace ScGen.Lib.Shared.Extensions.Middlewares;

public static class RegisterMiddlewares
{
    public static WebApplication UseMiddlewares(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors(); // Enable CORS for frontend requests
        app.UseCorrelationId();
        app.UseHttpsRedirection();
        app.UseExceptionHandler("/error");
        app.UseResponseCaching();
        app.UseResponseCompression();
        app.UseMiddleware<X402PaymentMiddleware>(); // x402 payment verification
        app.MapControllers();
        app.Run();
        return app;
    }
}