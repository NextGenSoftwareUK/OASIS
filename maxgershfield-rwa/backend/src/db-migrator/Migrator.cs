namespace db_migrator;

public sealed class Migrator(DataContext dbContext, ILogger<Migrator> logger)
{
    public async Task MigrateAsync(CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(MigrateAsync), startTime);

        try
        {
            await dbContext.Database.MigrateAsync(token);

            logger.OperationCompleted(nameof(MigrateAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            logger.OperationException(ex, nameof(MigrateAsync));
            logger.OperationCompleted(nameof(MigrateAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - startTime);
        }
    }
}