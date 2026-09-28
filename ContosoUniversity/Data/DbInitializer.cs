using System;
using System.Linq;
using ContosoUniversity.Models;

namespace ContosoUniversity.Data
{
    public static class DbInitializer
    {
        /// <summary>
        /// Remplit la base au premier demarrage. Chaque enregistrement est
        /// nomme, puis reutilise pour lier les suivants : la cle est lue sur
        /// l'objet une fois enregistre, sans le rechercher par son nom.
        /// </summary>
        public static void Initialize(SchoolContext context)
        {
            if (context.Students.Any())
            {
                return;
            }

            var alexander = new Student { FirstMidName = "Carson", LastName = "Alexander", EnrollmentDate = Date(2010, 9, 1) };
            var alonso = new Student { FirstMidName = "Meredith", LastName = "Alonso", EnrollmentDate = Date(2012, 9, 1) };
            var anand = new Student { FirstMidName = "Arturo", LastName = "Anand", EnrollmentDate = Date(2013, 9, 1) };
            var barzdukas = new Student { FirstMidName = "Gytis", LastName = "Barzdukas", EnrollmentDate = Date(2012, 9, 1) };
            var li = new Student { FirstMidName = "Yan", LastName = "Li", EnrollmentDate = Date(2012, 9, 1) };
            var justice = new Student { FirstMidName = "Peggy", LastName = "Justice", EnrollmentDate = Date(2011, 9, 1) };
            var norman = new Student { FirstMidName = "Laura", LastName = "Norman", EnrollmentDate = Date(2013, 9, 1) };
            var olivetto = new Student { FirstMidName = "Nino", LastName = "Olivetto", EnrollmentDate = Date(2005, 9, 1) };

            context.Students.AddRange(alexander, alonso, anand, barzdukas, li, justice, norman, olivetto);
            context.SaveChanges();

            var abercrombie = new Instructor { FirstMidName = "Kim", LastName = "Abercrombie", HireDate = Date(1995, 3, 11) };
            var fakhouri = new Instructor { FirstMidName = "Fadi", LastName = "Fakhouri", HireDate = Date(2002, 7, 6) };
            var harui = new Instructor { FirstMidName = "Roger", LastName = "Harui", HireDate = Date(1998, 7, 1) };
            var kapoor = new Instructor { FirstMidName = "Candace", LastName = "Kapoor", HireDate = Date(2001, 1, 15) };
            var zheng = new Instructor { FirstMidName = "Roger", LastName = "Zheng", HireDate = Date(2004, 2, 12) };

            context.Instructors.AddRange(abercrombie, fakhouri, harui, kapoor, zheng);
            context.SaveChanges();

            var rentree = Date(2007, 9, 1);

            var english = new Department { Name = "English", Budget = 350000, StartDate = rentree, InstructorID = abercrombie.ID };
            var mathematics = new Department { Name = "Mathematics", Budget = 100000, StartDate = rentree, InstructorID = fakhouri.ID };
            var engineering = new Department { Name = "Engineering", Budget = 350000, StartDate = rentree, InstructorID = harui.ID };
            var economics = new Department { Name = "Economics", Budget = 100000, StartDate = rentree, InstructorID = kapoor.ID };

            context.Departments.AddRange(english, mathematics, engineering, economics);
            context.SaveChanges();

            var chemistry = new Course { CourseID = 1050, Title = "Chemistry", Credits = 3, DepartmentID = engineering.DepartmentID };
            var microeconomics = new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = economics.DepartmentID };
            var macroeconomics = new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = economics.DepartmentID };
            var calculus = new Course { CourseID = 1045, Title = "Calculus", Credits = 4, DepartmentID = mathematics.DepartmentID };
            var trigonometry = new Course { CourseID = 3141, Title = "Trigonometry", Credits = 4, DepartmentID = mathematics.DepartmentID };
            var composition = new Course { CourseID = 2021, Title = "Composition", Credits = 3, DepartmentID = english.DepartmentID };
            var literature = new Course { CourseID = 2042, Title = "Literature", Credits = 4, DepartmentID = english.DepartmentID };

            context.Courses.AddRange(chemistry, microeconomics, macroeconomics, calculus, trigonometry, composition, literature);
            context.SaveChanges();

            context.OfficeAssignments.AddRange(
                new OfficeAssignment { InstructorID = fakhouri.ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = harui.ID, Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = kapoor.ID, Location = "Thompson 304" });
            context.SaveChanges();

            context.CourseAssignments.AddRange(
                new CourseAssignment { CourseID = chemistry.CourseID, InstructorID = kapoor.ID },
                new CourseAssignment { CourseID = chemistry.CourseID, InstructorID = harui.ID },
                new CourseAssignment { CourseID = microeconomics.CourseID, InstructorID = zheng.ID },
                new CourseAssignment { CourseID = macroeconomics.CourseID, InstructorID = zheng.ID },
                new CourseAssignment { CourseID = calculus.CourseID, InstructorID = fakhouri.ID },
                new CourseAssignment { CourseID = trigonometry.CourseID, InstructorID = harui.ID },
                new CourseAssignment { CourseID = composition.CourseID, InstructorID = abercrombie.ID },
                new CourseAssignment { CourseID = literature.CourseID, InstructorID = abercrombie.ID });
            context.SaveChanges();

            var enrollments = new Enrollment[]
            {
                new Enrollment { StudentID = alexander.ID, CourseID = chemistry.CourseID, Grade = Grade.A },
                new Enrollment { StudentID = alexander.ID, CourseID = microeconomics.CourseID, Grade = Grade.C },
                new Enrollment { StudentID = alexander.ID, CourseID = macroeconomics.CourseID, Grade = Grade.B },
                new Enrollment { StudentID = alonso.ID, CourseID = calculus.CourseID, Grade = Grade.B },
                new Enrollment { StudentID = alonso.ID, CourseID = trigonometry.CourseID, Grade = Grade.B },
                new Enrollment { StudentID = alonso.ID, CourseID = composition.CourseID, Grade = Grade.B },
                new Enrollment { StudentID = anand.ID, CourseID = chemistry.CourseID },
                new Enrollment { StudentID = anand.ID, CourseID = microeconomics.CourseID, Grade = Grade.B },
                new Enrollment { StudentID = barzdukas.ID, CourseID = chemistry.CourseID, Grade = Grade.B },
                new Enrollment { StudentID = li.ID, CourseID = composition.CourseID, Grade = Grade.B },
                new Enrollment { StudentID = justice.ID, CourseID = literature.CourseID, Grade = Grade.B }
            };

            foreach (Enrollment enrollment in enrollments)
            {
                bool dejaInscrit = context.Enrollments.Any(
                    e => e.StudentID == enrollment.StudentID && e.CourseID == enrollment.CourseID);
                if (!dejaInscrit)
                {
                    context.Enrollments.Add(enrollment);
                }
            }
            context.SaveChanges();
        }

        // Une date civile, sans heure ni fuseau, comme la colonne qui la recoit.
        private static DateTime Date(int annee, int mois, int jour)
        {
            return new DateTime(annee, mois, jour, 0, 0, 0, DateTimeKind.Unspecified);
        }
    }
}
