namespace DogApi.Models
{
    // Declares the data model and the operations on each attribute
    // Model: defines what a Dog is (Dog.cs).
    public class Dog
    {
        public int Id { get; set; } // Unique identifier for each Dog
        public string Name { get; set; } = string.Empty; // Dog’s name
        public string Breed { get; set; } = string.Empty; // Dog’s breed
        public int Age { get; set; } // Dog’s age in years
    }
    // This is our only DTO (Data Transfer Object)
}