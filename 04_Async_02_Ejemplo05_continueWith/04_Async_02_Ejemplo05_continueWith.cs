using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static void Main()   // No asíncrono...
    {
        // Inicio las tareas independientes
        Task<User> userTask = GetUserAsync();
        Task<decimal> balanceTask = GetAccountBalanceAsync();
        Task<List<string>> transactionsTask = GetTransactionHistoryAsync();

        // Combino todas las tareas en una lista de tareas. 
        // Y cuando todas terminan me genera otra tarea 'ContinueWith( Task...'
        Task.WhenAll(userTask, balanceTask, transactionsTask).ContinueWith(prevTasks =>
        {
            // Extrae los resultados de todas las tareas completadas
            var user = userTask.Result;
            var balance = balanceTask.Result;
            var transactions = transactionsTask.Result;

            // Genera el informe
            Console.WriteLine("\n📄 Generating Report...");
            Console.WriteLine($"👤 User: {user.Name} (ID: {user.Id})");
            Console.WriteLine($"💰 Balance: ${balance}");
            Console.WriteLine("📜 Transactions:");
            foreach (var txn in transactions)
            {
                Console.WriteLine($"   - {txn}");
            }

            Console.WriteLine("\n✅ Report generated.");
        }).Wait(); // Espera por las tareas en Main porque no es asincrónico

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }

    static Task<User> GetUserAsync()
    {
        return Task.Run(async () =>
        {
            await Task.Delay(500);
            Console.WriteLine("User data fetched.");
            return new User { Id = 1, Name = "Alice" };
        });
    }

    static Task<decimal> GetAccountBalanceAsync()
    {
        return Task.Run(async () =>
        {
            await Task.Delay(700);
            Console.WriteLine("Account balance fetched.");
            return 1250.75m;
        });
    }

    static Task<List<string>> GetTransactionHistoryAsync()
    {
        return Task.Run(async () =>
        {
            await Task.Delay(900);
            Console.WriteLine("Transaction history fetched.");
            return new List<string> { "Deposit $500", "Withdraw $200", "Transfer $100" };
        });
    }

    class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
