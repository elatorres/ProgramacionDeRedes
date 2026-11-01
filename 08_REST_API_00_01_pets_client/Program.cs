using System.Net.Http.Json;

// This is the Dog Api Model.
// It should be equal to DogAPi/Models/Dog.cs
public class Dog
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public int Age { get; set; }
}

// This is the client program
class Program
{
    static async Task Main()
    {
        // Create a client
        using var client = new HttpClient();
        // Connect to API Server 
        client.BaseAddress = new Uri("http://localhost:5111/api/Dogs");
        // A Menu for CRUD operations on doggies
        while (true)
        {
            Console.WriteLine("\n=== Dog Menu ===");
            Console.WriteLine("1. Create a Dog");
            Console.WriteLine("2. Update a Dog");
            Console.WriteLine("3. Delete a Dog");
            Console.WriteLine("4. List All Dogs");
            Console.WriteLine("5. Exit");
            Console.Write("Select an option (1-5): ");

            string? choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await CreateDogAsync(client);
                    break;
                case "2":
                    await UpdateDogAsync(client);
                    break;
                case "3":
                    await DeleteDogAsync(client);
                    break;
                case "4":
                    await ListDogsAsync(client);
                    break;
                case "5":
                    Console.WriteLine("Exiting...");
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    static async Task CreateDogAsync(HttpClient client)
    {
        var dog = new Dog();

        Console.Write("Enter Name: ");
        dog.Name = Console.ReadLine() ?? "";

        Console.Write("Enter Breed: ");
        dog.Breed = Console.ReadLine() ?? "";

        Console.Write("Enter Age: ");
        if (!int.TryParse(Console.ReadLine(), out int age))
        {
            Console.WriteLine("Invalid age.");
            return;
        }
        dog.Age = age;

        var response = await client.PostAsJsonAsync("/api/Dogs/", dog);
        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<Dog>();
            Console.WriteLine($"Dog created: ID={created!.Id}, Name={created.Name}");
        }
        else
        {
            Console.WriteLine("Failed to create dog.");
        }
    }

    static async Task UpdateDogAsync(HttpClient client)
    {
        Console.Write("Enter Dog ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        // First, try to get the current dog
        var dog = await client.GetFromJsonAsync<Dog>($"/api/Dogs/{id}");
        // var dog = await client.GetFromJsonAsync<Dog>($"/{id}");
        if (dog == null)
        {
            Console.WriteLine("Dog not found.");
            return;
        }

        Console.WriteLine($"Updating dog: {dog.Name} (ID: {dog.Id})");

        Console.Write("New Name (leave empty to keep current): ");
        string? name = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(name)) dog.Name = name;

        Console.Write("New Breed (leave empty to keep current): ");
        string? breed = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(breed)) dog.Breed = breed;

        Console.Write("New Age (leave empty to keep current): ");
        string? ageStr = Console.ReadLine();
        if (int.TryParse(ageStr, out int newAge)) dog.Age = newAge;

        var response = await client.PutAsJsonAsync($"/api/Dogs/{id}", dog);
        Console.WriteLine(response.IsSuccessStatusCode ? "Dog updated." : "Failed to update dog.");
    }

    static async Task DeleteDogAsync(HttpClient client)
    {
        Console.Write("Enter Dog ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var response = await client.DeleteAsync($"/api/Dogs/{id}");
        Console.WriteLine(response.IsSuccessStatusCode ? "Dog deleted." : "Failed to delete dog.");
    }

    static async Task ListDogsAsync(HttpClient client)
    {
        var dogs = await client.GetFromJsonAsync<List<Dog>>("");
        if (dogs == null || dogs.Count == 0)
        {
            Console.WriteLine("No dogs found.");
            return;
        }

        Console.WriteLine("=== All Dogs ===");
        foreach (var dog in dogs)
        {
            Console.WriteLine($"ID: {dog.Id}, Name: {dog.Name}, Breed: {dog.Breed}, Age: {dog.Age}");
        }
    }
}
