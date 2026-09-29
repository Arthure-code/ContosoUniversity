using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;

namespace ContosoUniversity.Tests.Modeles
{
    /// <summary>
    /// Les annotations des modeles, lues comme la liaison de modele les lit :
    /// une valeur absente doit etre refusee plutot que remplacee par zero ou
    /// par le premier janvier de l'an un.
    /// </summary>
    public class ValidationTests
    {
        private static List<ValidationResult> Valider(object modele)
        {
            var fautes = new List<ValidationResult>();
            Validator.TryValidateObject(modele, new ValidationContext(modele), fautes, validateAllProperties: true);
            return fautes;
        }

        [Fact]
        public void Etudiant_RefuseUneDateDInscriptionAbsente()
        {
            //Etant donne un etudiant sans date
            var etudiant = new Student { LastName = "Tremblay", FirstMidName = "Jeanne" };

            //Lorsque
            List<ValidationResult> fautes = Valider(etudiant);

            //Alors
            Assert.Contains(fautes, f => f.ErrorMessage == "La date d'inscription est requise.");
        }

        [Fact]
        public void Etudiant_AccepteUnEtudiantComplet()
        {
            //Etant donne un etudiant complet
            var etudiant = new Student
            {
                LastName = "Tremblay",
                FirstMidName = "Jeanne",
                EnrollmentDate = new DateTime(2026, 9, 1)
            };

            //Alors
            Assert.Empty(Valider(etudiant));
        }

        [Fact]
        public void Personne_RefuseUnNomVide()
        {
            //Etant donne un etudiant sans nom
            var etudiant = new Student { FirstMidName = "Jeanne", EnrollmentDate = new DateTime(2026, 9, 1) };

            //Lorsque
            List<ValidationResult> fautes = Valider(etudiant);

            //Alors
            Assert.Contains(fautes, f => f.ErrorMessage == "Le nom est requis.");
        }

        [Fact]
        public void Enseignant_RefuseUneDateDEmbaucheAbsente()
        {
            //Etant donne un enseignant sans date d'embauche
            var enseignant = new Instructor { LastName = "Tremblay", FirstMidName = "Jeanne" };

            //Lorsque
            List<ValidationResult> fautes = Valider(enseignant);

            //Alors
            Assert.Contains(fautes, f => f.ErrorMessage == "La date d'embauche est requise.");
        }

        [Fact]
        public void Cours_RefuseUnNumeroAZero()
        {
            //Etant donne un cours dont le numero n'a pas ete envoye
            var cours = new Course { CourseID = 0, Title = "Astronomy", Credits = 3, DepartmentID = 3 };

            //Lorsque
            List<ValidationResult> fautes = Valider(cours);

            //Alors
            Assert.Contains(fautes, f => f.ErrorMessage == "Le numéro doit se situer entre 1 et 9999.");
        }

        [Fact]
        public void Cours_RefuseUnDepartementNonChoisi()
        {
            //Etant donne un cours sans departement
            var cours = new Course { CourseID = 5001, Title = "Astronomy", Credits = 3, DepartmentID = 0 };

            //Lorsque
            List<ValidationResult> fautes = Valider(cours);

            //Alors
            Assert.Contains(fautes, f => f.ErrorMessage == "Le département est requis.");
        }

        [Fact]
        public void Cours_RefuseUnTitreTropCourt()
        {
            //Etant donne un titre de deux lettres
            var cours = new Course { CourseID = 5001, Title = "As", Credits = 3, DepartmentID = 3 };

            //Alors
            Assert.NotEmpty(Valider(cours));
        }

        [Fact]
        public void Cours_AccepteUnCoursComplet()
        {
            //Etant donne un cours complet
            var cours = new Course { CourseID = 5001, Title = "Astronomy", Credits = 3, DepartmentID = 3 };

            //Alors
            Assert.Empty(Valider(cours));
        }

        [Fact]
        public void Departement_RefuseUnBudgetEtUneDateAbsents()
        {
            //Etant donne un departement sans budget ni date de debut
            var departement = new Department { Name = "History" };

            //Lorsque
            List<ValidationResult> fautes = Valider(departement);

            //Alors
            Assert.Contains(fautes, f => f.ErrorMessage == "Le budget est requis.");
            Assert.Contains(fautes, f => f.ErrorMessage == "La date de début est requise.");
        }

        [Fact]
        public void Departement_AccepteUnDepartementComplet()
        {
            //Etant donne un departement complet
            var departement = new Department
            {
                Name = "History",
                Budget = 50000m,
                StartDate = new DateTime(2026, 9, 1)
            };

            //Alors
            Assert.Empty(Valider(departement));
        }

        [Fact]
        public void Personne_ComposeLeNomCompletAPartirDesDeuxNoms()
        {
            //Etant donne un etudiant
            var etudiant = new Student { LastName = "Alexander", FirstMidName = "Carson" };

            //Alors
            Assert.Equal("Alexander, Carson", etudiant.FullName);
        }
    }
}
