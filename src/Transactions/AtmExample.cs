using Orleans.Concurrency;
using Orleans.Transactions.Abstractions;

namespace Transactions;

public static class AtmExample
{
    public static async Task Run(IClusterClient client, decimal amount)
    {
        var atm = client.GetGrain<IAtmGrain>(0);

        var from = client.GetGrain<IAccountGrain>(Guid.NewGuid());
        var to = client.GetGrain<IAccountGrain>(Guid.NewGuid());

        try
        {
            await atm.Transfer(from, to, amount);
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

public interface IAtmGrain : IGrainWithIntegerKey
{
    [Transaction(TransactionOption.Create)]
    Task Transfer(IAccountGrain from, IAccountGrain to, decimal amountToTransfer);
}

public interface IAccountGrain : IGrainWithGuidKey
{
    [Transaction(TransactionOption.Join)]
    Task Withdraw(decimal amount);

    [Transaction(TransactionOption.Join)]
    Task Deposit(decimal amount);

    [Transaction(TransactionOption.CreateOrJoin)]
    Task<decimal> GetBalance();
}

[GenerateSerializer]
public record class Balance
{
    [Id(0)]
    public decimal Value { get; set; } = 1_000;
}

[StatelessWorker]
public class AtmGrain : Grain, IAtmGrain
{
    public Task Transfer(
        IAccountGrain from,
        IAccountGrain to,
        decimal amount) =>
        Task.WhenAll(
            from.Withdraw(amount),
            to.Deposit(amount));
}

[Reentrant]
public class AccountGrain : Grain, IAccountGrain
{
    private readonly ITransactionalState<Balance> _balance;

    public AccountGrain(
        [TransactionalState(nameof(balance))]
        ITransactionalState<Balance> balance) =>
        _balance = balance ?? throw new ArgumentNullException(nameof(balance));

    public Task Deposit(decimal amount) =>
        _balance.PerformUpdate(
            balance => balance.Value += amount);

    public Task Withdraw(decimal amount) =>
        _balance.PerformUpdate(balance =>
        {
            if (balance.Value < amount)
            {
                throw new InvalidOperationException(
                    $"Withdrawing {amount} credits from account " +
                    $"\"{this.GetPrimaryKeyString()}\" would overdraw it." +
                    $" This account has {balance.Value} credits.");
            }

            balance.Value -= amount;
        });

    public Task<decimal> GetBalance() =>
        _balance.PerformRead(balance => balance.Value);
}