using System.Collections;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContosoUniversity.Tests.Doubles
{
    /// <summary>
    /// Le contexte que recoit un controleur par son constructeur, simule :
    /// chaque ensemble est appuye sur une liste que le test remplit, et
    /// l'enregistrement est compte au lieu d'atteindre un serveur.
    /// </summary>
    internal sealed class ContexteFactice
    {
        public Mock<SchoolContext> Mock { get; }

        public List<Student> Etudiants { get; } = new List<Student>();
        public List<Instructor> Enseignants { get; } = new List<Instructor>();
        public List<Department> Departements { get; } = new List<Department>();
        public List<Course> Cours { get; } = new List<Course>();
        public List<OfficeAssignment> Bureaux { get; } = new List<OfficeAssignment>();
        public List<CourseAssignment> Attributions { get; } = new List<CourseAssignment>();
        public List<Enrollment> Inscriptions { get; } = new List<Enrollment>();

        public Mock<DbSet<Student>> EnsembleEtudiants { get; }
        public Mock<DbSet<Instructor>> EnsembleEnseignants { get; }
        public Mock<DbSet<Department>> EnsembleDepartements { get; }
        public Mock<DbSet<Course>> EnsembleCours { get; }
        public Mock<DbSet<OfficeAssignment>> EnsembleBureaux { get; }
        public Mock<DbSet<CourseAssignment>> EnsembleAttributions { get; }
        public Mock<DbSet<Enrollment>> EnsembleInscriptions { get; }

        public SchoolContext Contexte => Mock.Object;

        /// <param name="suiviReel">
        /// Laisse le suivi des entites d'Entity Framework fonctionner, pour
        /// les actions qui lisent la valeur d'origine d'une propriete. Le
        /// modele est construit, mais aucune connexion n'est ouverte.
        /// </param>
        public ContexteFactice(bool suiviReel = false)
        {
            DbContextOptions<SchoolContext> options = suiviReel
                ? new DbContextOptionsBuilder<SchoolContext>().UseSqlServer("Server=aucun;Database=modele").Options
                : new DbContextOptionsBuilder<SchoolContext>().Options;

            Mock = new Mock<SchoolContext>(options) { CallBase = suiviReel };

            EnsembleEtudiants = EnsembleFactice.Creer(Etudiants);
            EnsembleEnseignants = EnsembleFactice.Creer(Enseignants);
            EnsembleDepartements = EnsembleFactice.Creer(Departements);
            EnsembleCours = EnsembleFactice.Creer(Cours);
            EnsembleBureaux = EnsembleFactice.Creer(Bureaux);
            EnsembleAttributions = EnsembleFactice.Creer(Attributions);
            EnsembleInscriptions = EnsembleFactice.Creer(Inscriptions);

            Mock.Setup(c => c.Students).Returns(EnsembleEtudiants.Object);
            Mock.Setup(c => c.Instructors).Returns(EnsembleEnseignants.Object);
            Mock.Setup(c => c.Departments).Returns(EnsembleDepartements.Object);
            Mock.Setup(c => c.Courses).Returns(EnsembleCours.Object);
            Mock.Setup(c => c.OfficeAssignments).Returns(EnsembleBureaux.Object);
            Mock.Setup(c => c.CourseAssignments).Returns(EnsembleAttributions.Object);
            Mock.Setup(c => c.Enrollments).Returns(EnsembleInscriptions.Object);

            Mock.Setup(c => c.SaveChanges()).Returns(0);
            Mock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        }

        /// <summary>
        /// Numerote les entites a l'enregistrement, comme la base le fait.
        /// Les personnes partagent une seule table, donc une seule suite de
        /// cles : les etudiants d'abord, les enseignants ensuite.
        /// </summary>
        public void LesClesViennentDeLaBase()
        {
            Mock.Setup(c => c.SaveChanges()).Returns(() =>
            {
                int cle = 1;
                foreach (Person personne in Etudiants.Cast<Person>().Concat(Enseignants))
                {
                    if (personne.ID == 0)
                    {
                        personne.ID = cle;
                    }
                    cle++;
                }

                int cleDepartement = 1;
                foreach (Department departement in Departements)
                {
                    if (departement.DepartmentID == 0)
                    {
                        departement.DepartmentID = cleDepartement;
                    }
                    cleDepartement++;
                }

                return 0;
            });
        }

        /// <summary>
        /// Fait echouer le prochain enregistrement, comme une contrainte de
        /// la base le ferait.
        /// </summary>
        public void EnregistrementImpossible(Exception faute)
        {
            Mock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(faute);
        }

        public void VerifierUnEnregistrement()
        {
            Mock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        public void VerifierAucunEnregistrement()
        {
            Mock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    internal static class EnsembleFactice
    {
        public static Mock<DbSet<TEntite>> Creer<TEntite>(List<TEntite> donnees) where TEntite : class
        {
            IQueryable<TEntite> requete = donnees.AsQueryable();
            var ensemble = new Mock<DbSet<TEntite>>();

            ensemble.As<IQueryable<TEntite>>().Setup(e => e.Provider)
                .Returns(new FournisseurAsynchrone<TEntite>(requete.Provider));
            ensemble.As<IQueryable<TEntite>>().Setup(e => e.Expression).Returns(requete.Expression);
            ensemble.As<IQueryable<TEntite>>().Setup(e => e.ElementType).Returns(requete.ElementType);
            ensemble.As<IQueryable<TEntite>>().Setup(e => e.GetEnumerator())
                .Returns(() => donnees.GetEnumerator());
            // SelectList parcourt la liste sans passer par la version
            // generique de l'enumeration.
            ensemble.As<IEnumerable>().Setup(e => e.GetEnumerator())
                .Returns(() => donnees.GetEnumerator());
            ensemble.As<IAsyncEnumerable<TEntite>>()
                .Setup(e => e.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(() => new EnumerateurAsynchrone<TEntite>(donnees.GetEnumerator()));

            ensemble.Setup(e => e.Add(It.IsAny<TEntite>()))
                .Callback<TEntite>(donnees.Add);
            ensemble.Setup(e => e.AddRange(It.IsAny<TEntite[]>()))
                .Callback<TEntite[]>(elements => donnees.AddRange(elements));
            ensemble.Setup(e => e.Remove(It.IsAny<TEntite>()))
                .Callback<TEntite>(element => donnees.Remove(element));

            return ensemble;
        }
    }
}
