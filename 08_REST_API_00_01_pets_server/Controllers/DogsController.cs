using Microsoft.AspNetCore.Mvc;
using DogApi.Models;

// This is the API controller. handles HTTP requests
// It exposes HTTP endpoints to perform operations on dogs:
// Method Endpoint        Purpose
// GET    /api/dogs       Get all dogs
// GET    /api/dogs/{id}  Get a specific dog
// POST   /api/dogs       Add a new dog
// PUT    /api/dogs/{id}  Update an existing dog
// DELETE /api/dogs/{id}  Delete a dog

namespace DogApi.Controllers
{
    [ApiController] // Enables API-specific features like model validation
    [Route("api/[controller]")] // Base URL: api/dogs
    public class DogsController : ControllerBase
    {
        [HttpGet]
        public ActionResult<List<Dog>> GetAll() => DogRepository.GetAll();

        [HttpGet("{id}")]
        public ActionResult<Dog> Get(int id)
        {
            var dog = DogRepository.Get(id);
            return dog == null ? NotFound() : Ok(dog);
        }

        [HttpPost]
        public ActionResult<Dog> Add(Dog dog)
        {
            var created = DogRepository.Add(dog);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, Dog dog)
        {
            return DogRepository.Update(id, dog) ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            return DogRepository.Delete(id) ? NoContent() : NotFound();
        }
    }
}