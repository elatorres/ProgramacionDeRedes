using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace ServerRestApi_SCT;

// This class acts as a data access layer.
// It simulates a database using an in-memory
// list and provides CRUD operations. (Create, Read, Update, Delete)
public static class TeacherRepository
{
    private static readonly Dictionary<int, Teacher> teachers = new();
    private static int nextId = 1;
    private static readonly object _lock = new();

    public static List<Teacher> GetAll()
    {
        lock (_lock)
        {
            return teachers.Values.Select(c => Clone(c)).ToList(); // Return copies to avoid outside mutations
        }
    }

    public static Teacher Get(int id)
    {
        lock (_lock)
        {
            return teachers.TryGetValue(id, out var teacher) ? Clone(teacher) : null;
        }
    }

    public static Teacher Add(Teacher teacher)
    {
        lock (_lock)
        {
            teacher.TId = nextId++;
            teacher.CourseIds = new List<int>();
            teachers[teacher.TId] = Clone(teacher);
            return Clone(teacher);
        }
    }

    public static bool Update(int id, Teacher teacher)
    {
        lock (_lock)
        {
            if (!teachers.ContainsKey(id)) return false;
            teacher.TId = id;
            teacher.CourseIds = new List<int>(teachers[id].CourseIds); // Keep relations
            teachers[id] = Clone(teacher);
            return true;
        }
    }

    public static bool Delete(int id)
    {
        lock (_lock)
        {
            return teachers.Remove(id);
        }
    }

    public static bool AddCourseToTeacher( int teacherId, int courseId)
    {
        lock (_lock)
        {
            if (!teachers.TryGetValue(teacherId, out var teacher)) return false;
            if (!teacher.CourseIds.Contains(courseId))
                teacher.CourseIds.Add(courseId);
            return true;
        }
    }

    public static bool RemoveCourseFromTeacher(int teacherId, int courseId)
    {
        lock (_lock)
        {
            if (!teachers.TryGetValue(teacherId, out var teacher)) return false;
            return teacher.CourseIds.Remove(courseId);
        }
    }

    private static Teacher Clone(Teacher t)
    {
        return new Teacher
        {
            TId = t.TId,
            FirstName = t.FirstName,
            LastName = t.LastName,
            Department =  t.Department,
            CourseIds = new List<int>(t.CourseIds),
        };
    }
}