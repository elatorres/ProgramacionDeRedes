using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

// This is the API controller. handles HTTP requests
// It exposes HTTP endpoints to perform operations on teachers:
// Method Endpoint        Purpose
// GET    /api/teachers       Get all teachers
// GET    /api/teachers/{id}  Get a specific teacher
// POST   /api/teachers       Add a new teacher
// PUT    /api/teachers/{id}  Update an existing teacher
// DELETE /api/teachers/{id}  Delete a teacher

namespace ServerRestApi_SCT
{
    [ApiController] // Enables API-specific features like model validation
    [Route("api/[controller]")] // Base URL: api/teachers
    public class TeachersController : ControllerBase
    {
        [HttpGet]
        public ActionResult<List<Teacher>> GetAll() => TeacherRepository.GetAll();

        [HttpGet("{TId}")]
        public ActionResult<Teacher> Get(int TId)
        {
            var teacher = TeacherRepository.Get(TId);
            return teacher == null ? NotFound() : Ok(teacher);
        }

        [HttpPost]
        public ActionResult<Teacher> Add(Teacher Teacher)
        {
            var created = TeacherRepository.Add(Teacher);
            return CreatedAtAction(nameof(Get), new { TId = created.TId }, created);
        }

        [HttpPut("{TId}")]
        public IActionResult UpdateTeacher(int TId, Teacher teacher)
        {
            return TeacherRepository.Update(TId, teacher) ? NoContent() : NotFound();
        }

        [HttpDelete("{TId}")]
        public IActionResult Delete(int TId)
        {
            return TeacherRepository.Delete(TId) ? NoContent() : NotFound();
        }
        
        // Assign Teacher to Course
        [HttpPut("{TId}/Courses/{CId}")]
        public IActionResult AssignTeacherToCourse(int TId, int CId)
        {
            if ((CourseRepository.Get(CId)==null) || (TeacherRepository.Get(TId)==null)) return NotFound();
            var c = CourseRepository.Get(CId); var s = TeacherRepository.Get(TId);
            if (c != null && !c.TeacherIds.Contains(TId)) { c.TeacherIds.Add(TId); s?.CourseIds.Add(CId); }
            return Ok();
        }
        
        // Unassign Teacher from Course
        [HttpDelete("{TId}/Courses/{CId}")]
        public IActionResult UnassignTeacher(int TId, int CId)
        {
            var course = CourseRepository.Get(CId);
            var teacher = TeacherRepository.Get(TId);
            if (course == null || teacher == null) return NotFound();

            course.TeacherIds.Remove(TId);
            teacher.CourseIds.Remove(CId);
            return Ok();
        }
    }
}