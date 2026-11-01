using System.Collections.Generic;

namespace ServerRestApi_SCT
{
    // namespace ServerRestApi_SCT.Models;
    // This is one of ours DTOs (Data Transfer Objects)
    public class Student
    {
        public int SId { get; set; } // Unique identifier for each Student
        public string FirstName { get; set; } = string.Empty; // Student's first name
        public string LastName { get; set; } = string.Empty; // Student's last name
        public string Information { get; set; } // Student's extra information
        public List<int> CourseIds { get; set; } // Courses Taken
    }
}