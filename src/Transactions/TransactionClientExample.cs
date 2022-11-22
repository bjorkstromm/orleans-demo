using Orleans.Concurrency;
using Orleans.Transactions.Abstractions;

namespace Transactions;

public static class TransactionClientExample
{
    public static async Task Run(IClusterClient client, ITransactionClient transactionClient, decimal amount)
    {
        var from = client.GetGrain<IAccountGrain>(Guid.NewGuid());
        var to = client.GetGrain<IAccountGrain>(Guid.NewGuid());

        try
        {
            await transactionClient.RunTransaction(
                TransactionOption.Create,
                () => Task.WhenAll(
                    from.Withdraw(amount),
                    to.Deposit(amount)));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        var fromBalance = await from.GetBalance();
        var toBalance = await to.GetBalance();

        Console.WriteLine($"From: {fromBalance}\n To: {toBalance}");
    }
}