using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace ServerRestApi_SCT;

// This class acts as a data access layer.
// It simulates a database using an in-memory
// list and provides CRUD operations. (Create, Read, Update, Delete)
public static class CourseRepository
{
    private static readonly Dictionary<int, Course> courses = new();
    private static int nextId = 1;
    private static readonly object _lock = new();

    public static List<Course> GetAll()
    {
        lock (_lock)
        {
            return courses.Values.Select(c => Clone(c)).ToList(); // Return copies to avoid outside mutations
        }
    }

    public static Course Get(int id)
    {
        lock (_lock)
        {
            return courses.TryGetValue(id, out var course) ? Clone(course) : null;
        }
    }

    public static Course Add(Course course)
    {
        lock (_lock)
        {
            course.CId = nextId++;
            course.TeacherIds = new List<int>();
            course.StudentIds = new List<int>();
            courses[course.CId] = Clone(course);
            return Clone(course);
        }
    }

    public static bool Update(int id, Course course)
    {
        lock (_lock)
        {
            if (!courses.ContainsKey(id)) return false;
            course.CId = id;
            course.TeacherIds = new List<int>(courses[id].TeacherIds); // Keep relations
            course.StudentIds = new List<int>(courses[id].StudentIds);
            courses[id] = Clone(course);
            return true;
        }
    }

    public static bool Delete(int id)
    {
        lock (_lock)
        {
            return courses.Remove(id);
        }
    }

    public static bool AddStudentToCourse(int courseId, int studentId)
    {
        lock (_lock)
        {
            if (!courses.TryGetValue(courseId, out var course)) return false;
            if (!course.StudentIds.Contains(studentId))
                course.StudentIds.Add(studentId);
            return true;
        }
    }

    public static bool RemoveStudentFromCourse(int courseId, int studentId)
    {
        lock (_lock)
        {
            if (!courses.TryGetValue(courseId, out var course)) return false;
            return course.StudentIds.Remove(studentId);
        }
    }

    public static bool AddTeacherToCourse(int courseId, int teacherId)
    {
        lock (_lock)
        {
            if (!courses.TryGetValue(courseId, out var course)) return false;
            if (!course.TeacherIds.Contains(teacherId))
                course.TeacherIds.Add(teacherId);
            return true;
        }
    }

    public static bool RemoveTeacherFromCourse(int courseId, int teacherId)
    {
        lock (_lock)
        {
            if (!courses.TryGetValue(courseId, out var course)) return false;
            return course.TeacherIds.Remove(teacherId);
        }
    }

    private static Course Clone(Course c)
    {
        return new Course
        {
            CId = c.CId,
            Name = c.Name,
            Department =  c.Department,
            Description = c.Description,
            TeacherIds = new List<int>(c.TeacherIds),
            StudentIds = new List<int>(c.StudentIds)
        };
    }
}