using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Transactions;

var host = Host.CreateDefaultBuilder()
    .UseOrleans(siloBuilder => siloBuilder
        .UseLocalhostClustering()
        .UseTransactions()
        .AddMemoryGrainStorageAsDefault())
    .Build();

var client = host.Services.GetRequiredService<IClusterClient>();
var transactionClient = host.Services.GetRequiredService<ITransactionClient>();

await host.StartAsync();

var amount = decimal.Parse(args[0]);

await AtmExample.Run(client, amount);

await TransactionClientExample.Run(client, transactionClient, amount);

await host.StopAsync();