using System.Net.Http.Json;

namespace ClientRestApi_SCT;

class Program
{
    static async Task Main()
    {
        string baseUrl = "http://localhost:5173/api/";
        // Create a client
        using var client = new HttpClient();
        // Connect to API Server 
        client.BaseAddress = new Uri(baseUrl);

        while (true)
        {
            Console.WriteLine("\n===== MENU =====");
            Console.WriteLine("1. Create Course");
            Console.WriteLine("2. Modify Course");
            Console.WriteLine("3. Delete Course");
            Console.WriteLine("4. Show All Courses");
            Console.WriteLine("5. Create Student");
            Console.WriteLine("6. Modify Student");
            Console.WriteLine("7. Delete Student");
            Console.WriteLine("8. Show All Students");
            Console.WriteLine("9. Create Teacher");
            Console.WriteLine("10. Modify Teacher");
            Console.WriteLine("11. Delete Teacher");
            Console.WriteLine("12. Show All Teachers");
            Console.WriteLine("13. Assign Teacher to Course");
            Console.WriteLine("14. Dismiss Teacher from Course");
            Console.WriteLine("15. Assign Student to Course");
            Console.WriteLine("16. Dismiss Student from Course");
            Console.WriteLine("17. Fill Up Sample Data");
            Console.WriteLine("18. Exit");
            Console.Write("Select option (1-18): ");

            if (!int.TryParse(Console.ReadLine(), out int choice)) continue;

            switch (choice)
            {
                case 1: await CreateCourseAsync(client); break;
                case 2: await ModifyCourseAsync(client); break;
                case 3: await DeleteCourseAsync(client); break;
                case 4: await ShowAllCourseAsync(client); break;
                case 5: await CreateStudentAsync(client); break;
                case 6: await ModifyStudentAsync(client); break;
                case 7: await DeleteStudentAsync(client); break;
                case 8: await ShowAllStudentAsync(client); break;
                case 9: await CreateTeacherAsync(client); break;
                case 10: await ModifyTeacherAsync(client); break;
                case 11: await DeleteTeacherAsync(client); break;
                case 12: await ShowAllTeacherAsync(client); break;
                case 13: await AssignTeacherToCourseAsync(client); break;
                case 14: await DismissTeacherFromCourseAsync(client); break;
                case 15: await AssignStudentToCourseAsync(client); break;
                case 16: await DismissStudentFromCourseAsync(client); break;
                case 17: await FillUpSampleData(client); break;
                case 18: return;
                default: Console.WriteLine("\n<<<<< Invalid option. >>>>>"); break;
            }
        }


        // Common methods
        static async Task CreateCourseAsync(HttpClient client)
        {
            var course = new Course();

            Console.Write("Enter Name: ");
            course.Name = Console.ReadLine() ?? "";

            Console.Write("Enter Department: ");
            course.Department = Console.ReadLine() ?? "";

            Console.Write("Enter Description: ");
            course.Description = Console.ReadLine() ?? "";

            course.StudentIds = new List<int>();
            course.TeacherIds = new List<int>();

            var response = await client.PostAsJsonAsync("Courses", course);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Course>();
                Console.WriteLine($"Course created: ID={created!.CId}, Name={created.Name}");
            }
            else
            {
                Console.WriteLine("\n<<<<< Failed to create course. >>>>>");
            }
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task ModifyCourseAsync(HttpClient client)
        {
            var course = new Course();
            Console.Write("Enter Course ID to update: ");
            if (!int.TryParse(Console.ReadLine(), out int CId))
            {
                Console.WriteLine("\n<<<<< Invalid ID. >>>>>");
                return;
            }

            // First, try to get the current course
            course = await client.GetFromJsonAsync<Course>($"Courses/{CId}");
            if (course == null)
            {
                Console.WriteLine("\n<<<<< Course not found. >>>>>");
                return;
            }

            Console.WriteLine($"Updating course: {course.Name} (ID: {course.CId})");

            Console.Write("New Name (leave empty to keep current): ");
            string? name = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(name)) course.Name = name;

            Console.Write("New Department (leave empty to keep current): ");
            string? department = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(department)) course.Department = department;

            Console.Write("New Description (leave empty to keep current): ");
            string? description = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(description)) course.Description = description;

            var response = await client.PutAsJsonAsync($"Courses/{CId}", course);
            Console.WriteLine(response.IsSuccessStatusCode ? "Course updated." : "\n<<<<< Failed to update course. >>>>>");
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task DeleteCourseAsync(HttpClient client)
        {
            Console.Write("Enter Course ID to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int CId))
            {
                Console.WriteLine("\n<<<<< Invalid ID. >>>>>");
                return;
            }

            var response = await client.DeleteAsync($"Courses/{CId}");
            Console.WriteLine(response.IsSuccessStatusCode ? "Course deleted." : "\n<<<<< Failed to delete course. >>>>>");
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task ShowAllCourseAsync(HttpClient client)
        {
            var courses = await client.GetFromJsonAsync<List<Course>>("Courses");
            if (courses == null || courses.Count == 0)
            {
                Console.WriteLine("\n<<<<< No courses found. >>>>>");
                return;
            }

            Console.WriteLine("=== All Courses ===");
            foreach (var course in courses)
            {
                Console.WriteLine(
                    $"ID: {course.CId}, Name: {course.Name}, Department: {course.Department}, Description: {course.Description}");
            }
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task CreateStudentAsync(HttpClient client)
        {
            var student = new Student();

            Console.Write("Enter First Name: ");
            student.FirstName = Console.ReadLine() ?? "";

            Console.Write("Enter Last Name: ");
            student.LastName = Console.ReadLine() ?? "";

            Console.Write("Enter Information: ");
            student.Information = Console.ReadLine() ?? "";

            student.CourseIds = new List<int>();

            var response = await client.PostAsJsonAsync("Students", student);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Student>();
                Console.WriteLine(
                    $"Student created: ID={created!.SId}, FirstName={created.FirstName}, LastName={created.LastName}");
            }
            else
            {
                Console.WriteLine("\n<<<<< Failed to create student. >>>>>");
            }
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task ModifyStudentAsync(HttpClient client)
        {
            var student = new Student();
            Console.Write("Enter Student ID to update: ");
            if (!int.TryParse(Console.ReadLine(), out int SId))
            {
                Console.WriteLine("\n<<<<< Invalid ID. >>>>>");
                return;
            }

            student = await client.GetFromJsonAsync<Student>($"Students/{SId}");
            if (student == null)
            {
                Console.WriteLine("\n<<<<< Student not found. >>>>>");
                return;
            }

            Console.WriteLine($"Updating student: {student.FirstName} (ID: {student.SId})");

            Console.Write("New Name (leave empty to keep current): ");
            string? fname = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(fname)) student.FirstName = fname;

            Console.Write("New Last Name (leave empty to keep current): ");
            string? lastname = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(lastname)) student.LastName = lastname;

            Console.Write("New Information (leave empty to keep current): ");
            string? information = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(information)) student.Information = information;

            var response = await client.PutAsJsonAsync($"Students/{SId}", student);
            Console.WriteLine(response.IsSuccessStatusCode ? "Student updated." : "\n<<<<< Failed to update student. >>>>>");
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task DeleteStudentAsync(HttpClient client)
        {
            Console.Write("Enter Student ID to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int SId))
            {
                Console.WriteLine("\n<<<<< Invalid ID. >>>>>");
                return;
            }

            var response = await client.DeleteAsync($"Students/{SId}");
            Console.WriteLine(response.IsSuccessStatusCode ? "Student deleted." : "\n<<<<< Failed to delete Student. >>>>>");
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task ShowAllStudentAsync(HttpClient client)
        {
            var students = await client.GetFromJsonAsync<List<Student>>("Students");
            if (students == null || students.Count == 0)
            {
                Console.WriteLine("\n<<<<< No students found. >>>>>");
                return;
            }

            Console.WriteLine("\n=== All Students ===");
            foreach (var student in students)
            {
                Console.WriteLine(
                    $"ID: {student.SId}, First Name: {student.FirstName}, Last Name: {student.LastName}, Information: {student.Information}, Assigned Courses ({student.CourseIds.Count}) : {string.Join(", ", student.CourseIds)}");
            }
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task CreateTeacherAsync(HttpClient client)
        {
            var teacher = new Teacher();

            Console.Write("Enter First Name: ");
            teacher.FirstName = Console.ReadLine() ?? "";

            Console.Write("Enter Last Name: ");
            teacher.LastName = Console.ReadLine() ?? "";

            Console.Write("Enter Department: ");
            teacher.Department = Console.ReadLine() ?? "";

            teacher.CourseIds = new List<int>();

            var response = await client.PostAsJsonAsync("Teachers", teacher);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Teacher>();
                Console.WriteLine(
                    $"Teacher created: ID={created!.TId}, FirstName={created.FirstName}, LastName={created.LastName}");
            }
            else
            {
                Console.WriteLine("\n<<<<< Failed to create teacher. >>>>>");
            }
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task ModifyTeacherAsync(HttpClient client)
        {
            var teacher = new Teacher();
            Console.Write("Enter Teacher ID to update: ");
            if (!int.TryParse(Console.ReadLine(), out int TId))
            {
                Console.WriteLine("\n<<<<< Invalid ID. >>>>>");
                return;
            }

            teacher = await client.GetFromJsonAsync<Teacher>($"Teachers/{TId}");
            if (teacher == null)
            {
                Console.WriteLine("\n<<<<< Student not found. >>>>>");
                return;
            }

            Console.WriteLine($"Updating Teacher: {teacher.FirstName} (ID: {teacher.TId})");

            Console.Write("New Name (leave empty to keep current): ");
            string? fname = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(fname)) teacher.FirstName = fname;

            Console.Write("New Last Name (leave empty to keep current): ");
            string? lastname = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(lastname)) teacher.LastName = lastname;

            Console.Write("New Department (leave empty to keep current): ");
            string? department = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(department)) teacher.Department = department;

            var response = await client.PutAsJsonAsync($"Teachers/{TId}", teacher);
            Console.WriteLine(response.IsSuccessStatusCode ? "Teacher updated." : "\n<<<<< Failed to update Teacher. >>>>>");
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task DeleteTeacherAsync(HttpClient client)
        {
            Console.Write("Enter Teacher ID to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int TId))
            {
                Console.WriteLine("\n<<<<< Invalid ID. >>>>>");
                return;
            }

            var response = await client.DeleteAsync($"Teachers/{TId}");
            Console.WriteLine(response.IsSuccessStatusCode ? "Teacher deleted." : "\n<<<<< Failed to delete Teacher. >>>>>");
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task ShowAllTeacherAsync(HttpClient client)
        {
            var teachers = await client.GetFromJsonAsync<List<Teacher>>("Teachers");
            if (teachers == null || teachers.Count == 0)
            {
                Console.WriteLine("\n<<<<< No Teachers found. >>>>>");
                return;
            }

            Console.WriteLine("\n=== All Teachers ===");
            foreach (var teacher in teachers)
            {
                Console.WriteLine(
                    $"ID: {teacher.TId}, First Name: {teacher.FirstName}, Last Name: {teacher.LastName}, Department: {teacher.Department}, Assigned Courses ({teacher.CourseIds.Count}) : {string.Join(", ", teacher.CourseIds)}");
            }
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task AssignTeacherToCourseAsync(HttpClient client)
        {
            Console.Write($"Enter Course ID: ");
            if (!int.TryParse(Console.ReadLine(), out int CId)) return;

            Console.Write($"Enter Teacher ID: ");
            if (!int.TryParse(Console.ReadLine(), out int TId)) return;

            var res1 = await client.PutAsync($"Courses/{CId}/Teachers/{TId}", null);
            Console.WriteLine(res1.IsSuccessStatusCode ? "AssignedCT." : $"\n<<<<< ErrorCT: {res1.StatusCode} >>>>>");

            var res2 = await client.PutAsync($"Teachers/{TId}/Courses/{CId}", null);
            Console.WriteLine(res2.IsSuccessStatusCode ? "AssignedTC." : $"\n<<<<< ErrorTC: {res2.StatusCode} >>>>>");
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task DismissTeacherFromCourseAsync(HttpClient client)
        {
            Console.Write($"Enter Course ID: ");
            if (!int.TryParse(Console.ReadLine(), out int CId)) return;
            Console.Write($"Enter Teacher ID: ");
            if (!int.TryParse(Console.ReadLine(), out int TId)) return;
            
            var res1 = await client.DeleteAsync($"Courses/{CId}/Teachers/{TId}");
            Console.WriteLine(res1.IsSuccessStatusCode ? "DismissedCT." : $"\n<<<<< ErrorCT: {res1.StatusCode} >>>>>");

            var res2 = await client.DeleteAsync($"Teachers/{TId}/Courses/{CId}");
            Console.WriteLine(res2.IsSuccessStatusCode ? "DismissedTC." : $"\n<<<<< ErrorTC: {res2.StatusCode} >>>>>");
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task DismissStudentFromCourseAsync(HttpClient client)
        {
            Console.Write($"Enter Course ID: ");
            if (!int.TryParse(Console.ReadLine(), out int CId)) return;
            Console.Write($"Enter Student ID: ");
            if (!int.TryParse(Console.ReadLine(), out int SId)) return;
            
            var res1 = await client.DeleteAsync($"Courses/{CId}/Students/{SId}");
            Console.WriteLine(res1.IsSuccessStatusCode ? "DismissedCS." : $"\n<<<<< ErrorCS: {res1.StatusCode} >>>>>");

            var res2 = await client.DeleteAsync($"Students/{SId}/Courses/{CId}");
            Console.WriteLine(res2.IsSuccessStatusCode ? "DismissedSC." : $"\n<<<<< ErrorSC: {res2.StatusCode} >>>>>");
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task AssignStudentToCourseAsync(HttpClient client)
        {
            Console.Write($"Enter Course ID: ");
            if (!int.TryParse(Console.ReadLine(), out int CId)) return;

            Console.Write($"Enter Student ID: ");
            if (!int.TryParse(Console.ReadLine(), out int SId)) return;

            var res1 = await client.PutAsync($"Courses/{CId}/Students/{SId}", null);
            Console.WriteLine(res1.IsSuccessStatusCode ? "AssignedCS." : $"\n<<<<< ErrorCS: {res1.StatusCode} >>>>>");

            var res2 = await client.PutAsync($"Students/{SId}/Courses/{CId}", null);
            Console.WriteLine(res2.IsSuccessStatusCode ? "AssignedSC." : $"\n<<<<< ErrorSC: {res2.StatusCode} >>>>>");
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }
        
        
        static async Task FillUpSampleData(HttpClient client)
        {
            Console.WriteLine($"Filling Up Data...");

            await InsertCourseAsync(client, "Magia Oculta", "Prestidigitación Oscura", "Un Curso de Magia Negra sin los riesgos del oficio");
            await InsertCourseAsync(client, "Inteligencia Artificial para Tontos", "Inteligencia Artificial", "Un Curso de Inteligencia Artificial para no iniciados");
            await InsertCourseAsync(client, "Algebra Lineal para Inteligencia Artificial", "Inteligencia Artificial", "Las bases profundas de la inteligencia artificial");
            await InsertCourseAsync(client, "Arquitectura de Sistemas", "Arquitectura de Sistemas y Sistemas Operativos", "Un Curso de Arquitectura de computadores y electrónica digital");
            await InsertCourseAsync(client, "Sistemas Operativos", "Arquitectura de Sistemas y Sistemas Operativos", "Un Curso de Sistemas Operativos y Programación concurrente");
            await InsertCourseAsync(client, "Programación de Redes", "Arquitectura de Sistemas y Sistemas Operativos", "Un Curso sobre arquitectura cliente servidor y protocolos de telecomunicaciones");
            
            await InsertStudentAsync(client, "Enrique","Pérez","Buen Estudiante");
            await InsertStudentAsync(client, "Julio","Gómez","Muy participativo");
            await InsertStudentAsync(client, "Eduardo","López","Buenas Calificaciones");
            await InsertStudentAsync(client, "Gabriel","Martínez","Muy Inteligente");
            await InsertStudentAsync(client, "Santiago","Fernández","Muy Buenos Aportes");
            await InsertStudentAsync(client, "Felipe","Hernández","Excelente Desempeño");

            await InsertTeacherAsync(client, "Merlin", "Emrys", "Prestidigitación Oscura");
            await InsertTeacherAsync(client, "Franz", "Mayr", "Inteligencia Artificial");
            await InsertTeacherAsync(client, "Hugo", "Mora", "Inteligencia Artificial");
            await InsertTeacherAsync(client, "Santiago", "Pucciarelli", "Arquitectura de Sistemas y Sistemas Operativos");
            await InsertTeacherAsync(client, "Gabriel", "Bentos", "Arquitectura de Sistemas y Sistemas Operativos");
            await InsertTeacherAsync(client, "Enrique", "Latorres", "Arquitectura de Sistemas y Sistemas Operativos");

            await InsertTeacherCourseAsync(client,1,1);
            await InsertTeacherCourseAsync(client,6,1);
            await InsertTeacherCourseAsync(client,2,2);
            await InsertTeacherCourseAsync(client,3,2);
            await InsertTeacherCourseAsync(client,2,3);
            await InsertTeacherCourseAsync(client,3,3);
            await InsertTeacherCourseAsync(client,4,6);
            await InsertTeacherCourseAsync(client,5,5);
            await InsertTeacherCourseAsync(client,6,4);
            await InsertTeacherCourseAsync(client,5,4);
            await InsertTeacherCourseAsync(client,6,5);
            await InsertTeacherCourseAsync(client,4,6);

            await InsertStudentCourseAsync(client, 1, 1);
            await InsertStudentCourseAsync(client, 2, 1);
            await InsertStudentCourseAsync(client, 3, 1);
            await InsertStudentCourseAsync(client, 4, 1);
            await InsertStudentCourseAsync(client, 5, 1);
            await InsertStudentCourseAsync(client, 6, 1);
            await InsertStudentCourseAsync(client, 1, 2);
            await InsertStudentCourseAsync(client, 2, 2);
            await InsertStudentCourseAsync(client, 3, 2);
            await InsertStudentCourseAsync(client, 4, 2);
            await InsertStudentCourseAsync(client, 5, 2);
            await InsertStudentCourseAsync(client, 6, 2);
            await InsertStudentCourseAsync(client, 1, 3);
            await InsertStudentCourseAsync(client, 2, 3);
            await InsertStudentCourseAsync(client, 3, 3);
            await InsertStudentCourseAsync(client, 4, 4);
            await InsertStudentCourseAsync(client, 5, 4);
            await InsertStudentCourseAsync(client, 6, 4);
            await InsertStudentCourseAsync(client, 1, 5);
            await InsertStudentCourseAsync(client, 2, 6);
            await InsertStudentCourseAsync(client, 3, 5);
            await InsertStudentCourseAsync(client, 4, 6);
            await InsertStudentCourseAsync(client, 5, 5);
            await InsertStudentCourseAsync(client, 6, 6);
            
            Console.WriteLine("\n Press Any Key to Continue");
            Console.ReadKey();
        }

        static async Task InsertCourseAsync(HttpClient client, string MyName, string MyDepartment, string MyDescription)
        {
            var course = new Course();
            course.Name = MyName;
            course.Department = MyDepartment;
            course.Description = MyDescription;
            course.StudentIds = new List<int>();
            course.TeacherIds = new List<int>();
            var response = await client.PostAsJsonAsync("Courses", course);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Course>();
                Console.WriteLine($"Course created: ID={created!.CId}, Name={created.Name}");
            }
            else
            {
                Console.WriteLine("\n<<<<< Failed to create course. >>>>>");
            }
        }

        static async Task InsertStudentAsync(HttpClient client, string FirstName, string LastName, string Information)
        {
            var student = new Student();
            student.FirstName = FirstName;
            student.LastName = LastName;
            student.Information = Information;
            student.CourseIds = new List<int>();
            var response = await client.PostAsJsonAsync("Students", student);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Student>();
                Console.WriteLine(
                    $"Student created: ID={created!.SId}, FirstName={created.FirstName}, LastName={created.LastName}");
            }
            else
            {
                Console.WriteLine("\n<<<<< Failed to create student. >>>>>");
            }
        }

        static async Task InsertTeacherAsync(HttpClient client, string FirstName, string LastName,  string Department)
        {
            var teacher = new Teacher();
            teacher.FirstName = FirstName;
            teacher.LastName = LastName;
            teacher.Department = Department;
            teacher.CourseIds = new List<int>();
            var response = await client.PostAsJsonAsync("Teachers", teacher);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Teacher>();
                Console.WriteLine(
                    $"Teacher created: ID={created!.TId}, FirstName={created.FirstName}, LastName={created.LastName}");
            }
            else
            {
                Console.WriteLine("\n<<<<< Failed to create teacher. >>>>>");
            }
        }
        
        static async Task InsertTeacherCourseAsync(HttpClient client, int TId,  int CId)
        {
            var res1 = await client.PutAsync($"Courses/{CId}/Teachers/{TId}", null);
            Console.WriteLine(res1.IsSuccessStatusCode ? "AssignedCT." : $"\n<<<<< ErrorCT: {res1.StatusCode} >>>>>");

            var res2 = await client.PutAsync($"Teachers/{TId}/Courses/{CId}", null);
            Console.WriteLine(res2.IsSuccessStatusCode ? "AssignedTC." : $"\n<<<<< ErrorTC: {res2.StatusCode} >>>>>");
        }
        
        static async Task InsertStudentCourseAsync(HttpClient client, int CId, int SId)
        {
            var res1 = await client.PutAsync($"Courses/{CId}/Students/{SId}", null);
            Console.WriteLine(res1.IsSuccessStatusCode ? "AssignedCS." : $"\n<<<<< ErrorCS: {res1.StatusCode} >>>>>");
            var res2 = await client.PutAsync($"Students/{SId}/Courses/{CId}", null);
            Console.WriteLine(res2.IsSuccessStatusCode ? "AssignedSC." : $"\n<<<<< ErrorSC: {res2.StatusCode} >>>>>");
        }

        
    }
}

