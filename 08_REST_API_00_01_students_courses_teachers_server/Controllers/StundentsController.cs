using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

// This is the API controller. handles HTTP requests
// It exposes HTTP endpoints to perform operations on students:
// Method Endpoint        Purpose
// GET    /api/students       Get all students
// GET    /api/students/{id}  Get a specific student
// POST   /api/students       Add a new student
// PUT    /api/students/{id}  Update an existing student
// DELETE /api/students/{id}  Delete a student

namespace ServerRestApi_SCT
{
    [ApiController] // Enables API-specific features like model validation
    [Route("api/[controller]")] // Base URL: api/students
    public class StudentsController : ControllerBase
    {
        [HttpGet]
        public ActionResult<List<Student>> GetAll() => StudentRepository.GetAll();

        [HttpGet("{SId}")]
        public ActionResult<Student> Get(int SId)
        {
            var student = StudentRepository.Get(SId);
            return student == null ? NotFound() : Ok(student);
        }

        [HttpPost]
        public ActionResult<Student> Add(Student Student)
        {
            var created = StudentRepository.Add(Student);
            return CreatedAtAction(nameof(Get), new { SId = created.SId }, created);
        }

        [HttpPut("{SId}")]
        public IActionResult UpdateStudent(int SId, Student student)
        {
            return StudentRepository.Update(SId, student) ? NoContent() : NotFound();
        }

        [HttpDelete("{SId}")]
        public IActionResult Delete(int SId)
        {
            return StudentRepository.Delete(SId) ? NoContent() : NotFound();
        }
        
        // Assign Student to Course
        [HttpPut("{SId}/Courses/{CId}")]
        public IActionResult AssignCourseToStudent(int SId, int CId)
        {
            if ((CourseRepository.Get(CId)==null) || (StudentRepository.Get(SId)==null)) return NotFound();
            var c = CourseRepository.Get(CId); var s = StudentRepository.Get(SId);
            if (!c.StudentIds.Contains(SId)) { c.StudentIds.Add(SId); s.CourseIds.Add(CId); }
            return Ok();
        }
        
        // Unassign Student from Course
        [HttpDelete("{SId}/Courses/{CId}")]
        public IActionResult UnassignCourseToStudent(int SId, int CId)
        {
            var course = CourseRepository.Get(CId);
            var student = StudentRepository.Get(SId);
            if (course == null || student == null) return NotFound();

            course.StudentIds.Remove(SId);
            student.CourseIds.Remove(CId);
            return Ok();
        }
    }
}