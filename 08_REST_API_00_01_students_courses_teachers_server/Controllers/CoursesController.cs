using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ServerRestApi_SCT
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        [HttpGet]
        public ActionResult<List<Course>> GetAll() => CourseRepository.GetAll();

        [HttpGet("{CId}")]
        public ActionResult<Course> Get(int CId)
        {
            var course = CourseRepository.Get(CId);
            return course == null ? NotFound() : Ok(course);
        }

        [HttpPost]
        public ActionResult<Course> Add(Course course)
        {
            var created = CourseRepository.Add(course);
            return CreatedAtAction(nameof(Get), new { CId = created.CId }, created);
        }

        [HttpPut("{CId}")]
        public IActionResult UpdateCourse(int CId, Course course)
        {
            return CourseRepository.Update(CId, course) ? NoContent() : NotFound();
        }

        [HttpDelete("{CId}")]
        public IActionResult Delete(int CId)
        {
            return CourseRepository.Delete(CId) ? NoContent() : NotFound();
        }

        [HttpPut("{CId}/Teachers/{TId}")]
        public IActionResult AssignTeacher(int CId, int TId)
        {
            var course = CourseRepository.Get(CId);
            var teacher = TeacherRepository.Get(TId);
            if (course == null || teacher == null) return NotFound();

            CourseRepository.AddTeacherToCourse(CId, TId);
            TeacherRepository.AddCourseToTeacher(TId, CId);
            return Ok(course);
        }

        [HttpDelete("{CId}/Teachers/{TId}")]
        public IActionResult UnassignTeacher(int CId, int TId)
        {
            var course = CourseRepository.Get(CId);
            var teacher = TeacherRepository.Get(TId);
            if (course == null || teacher == null) return NotFound();

            CourseRepository.RemoveTeacherFromCourse(CId, TId);
            TeacherRepository.RemoveCourseFromTeacher(TId, CId);
            return Ok(course);
        }

        [HttpPut("{CId}/Students/{SId}")]
        public IActionResult AssignStudent(int CId, int SId)
        {
            var course = CourseRepository.Get(CId);
            var student = StudentRepository.Get(SId);
            if (course == null || student == null) return NotFound();

            CourseRepository.AddStudentToCourse(CId, SId);
            StudentRepository.AddCourseToStudent(SId, CId);
            return Ok(course);
        }

        [HttpDelete("{CId}/Students/{SId}")]
        public IActionResult UnassignStudent(int CId, int SId)
        {
            var course = CourseRepository.Get(CId);
            var student = StudentRepository.Get(SId);
            if (course == null || student == null) return NotFound();

            CourseRepository.RemoveStudentFromCourse(CId, SId);
            StudentRepository.RemoveCourseFromStudent(SId, CId);
            return Ok(course);
        }
    }
}
