using System.Collections.Generic;

namespace ClientRestApi_SCT
{
    // This is one of ours DTOs (Data Transfer Objects)
    public class Teacher
    {
        public int TId { get; set; } // Unique identifier for each Teachers
        public string FirstName { get; set; } = string.Empty; // Teachers’s first name
        public string LastName { get; set; } = string.Empty; // Teachers’s last name
        public string Department { get; set; } = string.Empty; // Teachers’s Department
        public List<int> CourseIds { get; set; } = new();// Courses given 
    }
}