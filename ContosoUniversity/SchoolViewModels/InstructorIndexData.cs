using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContosoUniversity.Models.SchoolViewModels
{
    public class InstructorIndexData
    {
        public IEnumerable<Instructor> Instructors { get; set; } = new List<Instructor>();
        public IEnumerable<Course> Courses { get; set; } = new List<Course>();
        public IEnumerable<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}