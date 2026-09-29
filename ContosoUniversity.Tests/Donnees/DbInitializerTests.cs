using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Doubles;
using Moq;

namespace ContosoUniversity.Tests.Donnees
{
    public class DbInitializerTests
    {
        private static readonly DateTime Rentree2007 = new DateTime(2007, 9, 1, 0, 0, 0, DateTimeKind.Unspecified);

        private readonly ContexteFactice _contexte = new ContexteFactice();

        public DbInitializerTests()
        {
            _contexte.LesClesViennentDeLaBase();
        }

        [Fact]
        public void Initialize_NAjouteRienQuandLaBaseEstDejaRemplie()
        {
            //Etant donne un etudiant deja enregistre
            _contexte.Etudiants.Add(new Student { LastName = "Alexander", FirstMidName = "Carson" });

            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors
            Assert.Single(_contexte.Etudiants);
            Assert.Empty(_contexte.Enseignants);
            Assert.Empty(_contexte.Cours);
            _contexte.Mock.Verify(c => c.SaveChanges(), Times.Never);
        }

        [Fact]
        public void Initialize_SemeLesHuitEtudiants()
        {
            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors
            Assert.Equal(8, _contexte.Etudiants.Count);
            Assert.Equal(
                new[] { "Alexander", "Alonso", "Anand", "Barzdukas", "Li", "Justice", "Norman", "Olivetto" },
                _contexte.Etudiants.Select(e => e.LastName));

            Student alexander = _contexte.Etudiants[0];
            Assert.Equal("Carson", alexander.FirstMidName);
            Assert.Equal(new DateTime(2010, 9, 1), alexander.EnrollmentDate);
        }

        [Fact]
        public void Initialize_DonneDesDatesSansFuseau()
        {
            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors une date civile, telle que la colonne la recoit
            Assert.Equal(DateTimeKind.Unspecified, _contexte.Etudiants[0].EnrollmentDate!.Value.Kind);
            Assert.Equal(DateTimeKind.Unspecified, _contexte.Enseignants[0].HireDate!.Value.Kind);
        }

        [Fact]
        public void Initialize_SemeLesCinqEnseignantsAvecLeurDateDEmbauche()
        {
            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors
            Assert.Equal(5, _contexte.Enseignants.Count);
            Instructor abercrombie = _contexte.Enseignants.Single(e => e.LastName == "Abercrombie");
            Assert.Equal("Kim", abercrombie.FirstMidName);
            Assert.Equal(new DateTime(1995, 3, 11), abercrombie.HireDate);
            Assert.Equal(new DateTime(2004, 2, 12), _contexte.Enseignants.Single(e => e.LastName == "Zheng").HireDate);
        }

        [Fact]
        public void Initialize_ConfieChaqueDepartementAUnEnseignant()
        {
            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors les cles des enseignants suivent celles des etudiants
            Assert.Equal(4, _contexte.Departements.Count);
            int harui = _contexte.Enseignants.Single(e => e.LastName == "Harui").ID;
            Department engineering = _contexte.Departements.Single(d => d.Name == "Engineering");

            Assert.Equal(harui, engineering.InstructorID);
            Assert.Equal(350000m, engineering.Budget);
            Assert.Equal(Rentree2007, engineering.StartDate);
        }

        [Fact]
        public void Initialize_SemeLesSeptCoursDansLeurDepartement()
        {
            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors
            Assert.Equal(7, _contexte.Cours.Count);
            int engineering = _contexte.Departements.Single(d => d.Name == "Engineering").DepartmentID;
            Course chemistry = _contexte.Cours.Single(c => c.CourseID == 1050);

            Assert.Equal("Chemistry", chemistry.Title);
            Assert.Equal(3, chemistry.Credits);
            Assert.Equal(engineering, chemistry.DepartmentID);
            Assert.Equal(4, _contexte.Cours.Single(c => c.Title == "Calculus").Credits);
        }

        [Fact]
        public void Initialize_DonneUnBureauAuxTroisEnseignantsQuiEnOnt()
        {
            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors
            Assert.Equal(3, _contexte.Bureaux.Count);
            int kapoor = _contexte.Enseignants.Single(e => e.LastName == "Kapoor").ID;
            Assert.Equal("Thompson 304", _contexte.Bureaux.Single(b => b.InstructorID == kapoor).Location);
        }

        [Fact]
        public void Initialize_AttribueLesHuitCoursAuxEnseignants()
        {
            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors Chemistry est enseigne par deux personnes
            Assert.Equal(8, _contexte.Attributions.Count);
            int kapoor = _contexte.Enseignants.Single(e => e.LastName == "Kapoor").ID;
            int harui = _contexte.Enseignants.Single(e => e.LastName == "Harui").ID;

            Assert.Equal(
                new[] { kapoor, harui },
                _contexte.Attributions.Where(a => a.CourseID == 1050).Select(a => a.InstructorID));
        }

        [Fact]
        public void Initialize_InscritLesEtudiantsAvecLeursNotes()
        {
            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors
            Assert.Equal(11, _contexte.Inscriptions.Count);
            int alexander = _contexte.Etudiants.Single(e => e.LastName == "Alexander").ID;
            int anand = _contexte.Etudiants.Single(e => e.LastName == "Anand").ID;

            Assert.Equal(Grade.A, _contexte.Inscriptions.Single(i => i.StudentID == alexander && i.CourseID == 1050).Grade);
            Assert.Equal(Grade.C, _contexte.Inscriptions.Single(i => i.StudentID == alexander && i.CourseID == 4022).Grade);
            Assert.Null(_contexte.Inscriptions.Single(i => i.StudentID == anand && i.CourseID == 1050).Grade);
        }

        [Fact]
        public void Initialize_NInscritPasDeuxFoisLeMemeEtudiantAuMemeCours()
        {
            //Etant donne une inscription deja presente, celle du premier
            //etudiant au premier cours
            _contexte.Inscriptions.Add(new Enrollment { StudentID = 1, CourseID = 1050, Grade = Grade.A });

            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors les dix autres sont ajoutees, celle-la ne l'est pas deux fois
            Assert.Equal(11, _contexte.Inscriptions.Count);
            Assert.Single(_contexte.Inscriptions.Where(i => i.StudentID == 1 && i.CourseID == 1050));
        }

        [Fact]
        public void Initialize_EnregistreApresChaqueGroupe()
        {
            //Lorsque
            DbInitializer.Initialize(_contexte.Contexte);

            //Alors les etudiants, les enseignants et les departements sont
            //enregistres avant que les suivants ne s'y rattachent
            _contexte.Mock.Verify(c => c.SaveChanges(), Times.Exactly(7));
        }
    }
}
