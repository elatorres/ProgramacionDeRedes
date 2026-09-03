using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Lanzo unas tareas independientes
        Task<User> userTask = GetUserAsync();
        Task<decimal> balanceTask = GetAccountBalanceAsync();
        Task<List<string>> transactionsTask = GetTransactionHistoryAsync();

        // Espero que todas las tareas se completen usando WhenAll
        await Task.WhenAll(userTask, balanceTask, transactionsTask);

        // Ahora continuo cuando todos los resultados están disponibles.
        Task reportTask = Task.Run(() =>
        {
            var user = userTask.Result;
            var balance = balanceTask.Result;
            var transactions = transactionsTask.Result;
            // y genero un reporte....
            Console.WriteLine("\n📄 Generating Report...");
            Console.WriteLine($"👤 User: {user.Name} (ID: {user.Id})");
            Console.WriteLine($"💰 Balance: ${balance}");
            Console.WriteLine("📜 Transactions:");
            foreach (var txn in transactions)
            {
                Console.WriteLine($"   - {txn}");
            }
        });
        // Espero a que haya completado ...
        await reportTask;
        // e informo...
        Console.WriteLine("\n✅ Report generated.");
    }

    static async Task<User> GetUserAsync()
    {
        await Task.Delay(500); // Simulate latency
        Console.WriteLine("User data fetched.");
        return new User { Id = 1, Name = "Alice" };
    }

    static async Task<decimal> GetAccountBalanceAsync()
    {
        await Task.Delay(700);
        Console.WriteLine("Account balance fetched.");
        return 1250.75m;
    }

    static async Task<List<string>> GetTransactionHistoryAsync()
    {
        await Task.Delay(900);
        Console.WriteLine("Transaction history fetched.");
        return new List<string> { "Deposit $500", "Withdraw $200", "Transfer $100" };
    }

    class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}