using DogApi.Models;

namespace DogApi
{
    // This class acts as a data access layer.
    // It simulates a database using an in-memory
    // list and provides CRUD operations. (Create, Read, Update, Delete)
    public static class DogRepository
    {
        private static List<Dog> Dogs = new();  // In-memory list/store of Dog objects
        private static int nextId = 1; // Auto-increment ID

        public static List<Dog> GetAll() => Dogs; // get all dogs

        // Get dog with id
        public static Dog? Get(int id) => Dogs.FirstOrDefault(d => d.Id == id); 

        public static Dog Add(Dog dog) // Add a Dog
        {
            dog.Id = nextId++; // Get next id
            Dogs.Add(dog); // Add it to the database
            return dog; // return the dog with id
        }
        
        // Update attributes of dogs by id
        public static bool Update(int id, Dog updatedDog)
        {
            var dog = Get(id);
            if (dog == null) return false;

            dog.Name = updatedDog.Name;
            dog.Breed = updatedDog.Breed;
            dog.Age = updatedDog.Age;
            return true;
        }

        public static bool Delete(int id) // delete dog by id
        {
            var dog = Get(id);
            return dog != null && Dogs.Remove(dog);
        }
    }
}
