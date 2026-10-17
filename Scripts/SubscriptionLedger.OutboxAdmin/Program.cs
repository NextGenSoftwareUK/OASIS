using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.SubscriptionLedger.OutboxAdmin;

return await OutboxAdminCommand.RunAsync(args, new ConfigurationBuilder().AddEnvironmentVariables().Build(), Console.Out, Console.Error);
