using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace ServerRestApi_SCT;

// This class acts as a data access layer.
// It simulates a database using an in-memory
// list and provides CRUD operations. (Create, Read, Update, Delete)
public static class StudentRepository
{
    private static readonly Dictionary<int, Student> students = new();
    private static int nextId = 1;
    private static readonly object _lock = new();

    public static List<Student> GetAll()
    {
        lock (_lock)
        {
            return students.Values.Select(c => Clone(c)).ToList(); // Return copies to avoid outside mutations
        }
    }

    public static Student Get(int id)
    {
        lock (_lock)
        {
            return students.TryGetValue(id, out var student) ? Clone(student) : null;
        }
    }

    public static Student Add(Student student)
    {
        lock (_lock)
        {
            student.SId = nextId++;
            student.CourseIds = new List<int>();
            students[student.SId] = Clone(student);
            return Clone(student);
        }
    }

    public static bool Update(int id, Student student)
    {
        lock (_lock)
        {
            if (!students.ContainsKey(id)) return false;
            student.SId = id;
            student.CourseIds = new List<int>(students[id].CourseIds); // Keep relations
            students[id] = Clone(student);
            return true;
        }
    }

    public static bool Delete(int id)
    {
        lock (_lock)
        {
            return students.Remove(id);
        }
    }

    public static bool AddCourseToStudent( int studentId, int courseId)
    {
        lock (_lock)
        {
            if (!students.TryGetValue(studentId, out var student)) return false;
            if (!student.CourseIds.Contains(courseId))
                student.CourseIds.Add(courseId);
            return true;
        }
    }

    public static bool RemoveCourseFromStudent(int studentId, int courseId)
    {
        lock (_lock)
        {
            if (!students.TryGetValue(studentId, out var student)) return false;
            return student.CourseIds.Remove(courseId);
        }
    }

    private static Student Clone(Student s)
    {
        return new Student
        {
            SId = s.SId,
            FirstName = s.FirstName,
            LastName = s.LastName,
            Information =  s.Information,
            CourseIds = new List<int>(s.CourseIds),
        };
    }
}