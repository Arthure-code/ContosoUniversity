using ContosoUniversity.Controllers;
using ContosoUniversity.Models;
using ContosoUniversity.Models.SchoolViewModels;
using ContosoUniversity.Tests.Doubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContosoUniversity.Tests.Controleurs
{
    public class InstructorsControllerTests
    {
        private static readonly string[] CoursDeLEnseignant = { "Composition" };
        private static readonly int[] NumerosDesCours = { 1050, 2021 };
        private static readonly bool[] CasesAttendues = { false, true };
        private static readonly string[] Chemistry = { "1050" };

        private readonly ContexteFactice _contexte = new ContexteFactice();
        private readonly InstructorsController _controleur;
        private readonly Course _chemistry = new Course { CourseID = 1050, Title = "Chemistry", Credits = 3, DepartmentID = 3 };
        private readonly Course _composition = new Course { CourseID = 2021, Title = "Composition", Credits = 3, DepartmentID = 1 };

        public InstructorsControllerTests()
        {
            _controleur = new InstructorsController(_contexte.Contexte);
            _contexte.Cours.Add(_chemistry);
            _contexte.Cours.Add(_composition);
        }

        /// <summary>
        /// Un enseignant avec son bureau et le cours qu'il donne deja.
        /// </summary>
        private Instructor Enseignant(int id = 9)
        {
            var enseignant = new Instructor
            {
                ID = id,
                LastName = "Abercrombie",
                FirstMidName = "Kim",
                HireDate = new DateTime(1995, 3, 11),
                OfficeAssignment = new OfficeAssignment { InstructorID = id, Location = "Smith 17" }
            };
            enseignant.CourseAssignments.Add(new CourseAssignment
            {
                InstructorID = id,
                CourseID = _composition.CourseID,
                Course = _composition
            });
            _contexte.Enseignants.Add(enseignant);
            return enseignant;
        }

        private void LaRechercheParCleTrouve(Instructor? enseignant)
        {
            _contexte.EnsembleEnseignants
                .Setup(e => e.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync(enseignant);
        }

        [Fact]
        public async Task Index_RetourneLesEnseignants()
        {
            //Etant donne un enseignant enregistre
            Enseignant();

            //Lorsque
            IActionResult resultat = await _controleur.Index(null, null);

            //Alors
            var vue = Assert.IsType<ViewResult>(resultat);
            var modele = Assert.IsType<InstructorIndexData>(vue.Model);
            Assert.Single(modele.Instructors);
            Assert.Empty(modele.Courses);
        }

        [Fact]
        public async Task Index_ListeLesCoursDeLEnseignantChoisi()
        {
            //Etant donne un enseignant qui donne un cours
            Enseignant();
            Formulaire.Poser(_controleur);

            //Lorsque on le choisit
            IActionResult resultat = await _controleur.Index(9, null);

            //Alors ses cours paraissent a cote de la liste
            var modele = Assert.IsType<InstructorIndexData>(Assert.IsType<ViewResult>(resultat).Model);
            Assert.Equal(CoursDeLEnseignant, modele.Courses.Select(c => c.Title));
            Assert.Equal(9, _controleur.ViewData["InstructorID"]);
        }

        [Fact]
        public async Task Details_RetourneLEnseignantDemande()
        {
            //Etant donne un enseignant enregistre
            Enseignant();

            //Lorsque
            IActionResult resultat = await _controleur.Details(9);

            //Alors
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("Abercrombie", Assert.IsType<Instructor>(vue.Model).LastName);
        }

        [Fact]
        public async Task Details_RetourneIntrouvableSansIdentifiant()
        {
            Assert.IsType<NotFoundResult>(await _controleur.Details(null));
        }

        [Fact]
        public async Task Create_EnregistreLEnseignant()
        {
            //Etant donne un enseignant saisi
            var enseignant = new Instructor { LastName = "Tremblay", FirstMidName = "Jeanne", HireDate = new DateTime(2026, 1, 5) };
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Create(enseignant);

            //Alors
            _contexte.Mock.Verify(c => c.Add(enseignant), Times.Once);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task Edit_ProposeChaqueCoursAvecSaCaseCochee()
        {
            //Etant donne un enseignant qui donne le second cours
            Enseignant();
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Edit(9);

            //Alors les deux cours sont proposes, un seul est coche
            Assert.IsType<ViewResult>(resultat);
            var cours = Assert.IsType<List<AssignedCourseData>>(_controleur.ViewData["Courses"]);
            Assert.Equal(NumerosDesCours, cours.Select(c => c.CourseID));
            Assert.Equal(CasesAttendues, cours.Select(c => c.Assigned));
        }

        [Fact]
        public async Task Edit_RetourneIntrouvableSansIdentifiant()
        {
            Assert.IsType<NotFoundResult>(await _controleur.Edit(null));
            Assert.IsType<NotFoundResult>(await _controleur.Edit(404));
        }

        [Fact]
        public async Task Edit_EnregistreLesChampsEtLeCoursCoche()
        {
            //Etant donne un enseignant, et un formulaire qui coche l'autre cours
            Instructor enseignant = Enseignant();
            Formulaire.Poser(_controleur, new Dictionary<string, string>
            {
                ["LastName"] = "Abercrombie",
                ["FirstMidName"] = "Kim",
                ["HireDate"] = "1995-03-11",
                ["OfficeAssignment.Location"] = "Gowan 27"
            });

            //Lorsque
            IActionResult resultat = await _controleur.Edit(9, Chemistry);

            //Alors le cours coche est attribue, l'autre est retire
            Assert.Equal("Gowan 27", enseignant.OfficeAssignment!.Location);
            Assert.Contains(enseignant.CourseAssignments, a => a.CourseID == 1050);
            _contexte.Mock.Verify(c => c.Remove(It.Is<CourseAssignment>(a => a.CourseID == 2021)), Times.Once);
            _contexte.VerifierUnEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task Edit_RetireLeBureauLaisseVide()
        {
            //Etant donne un formulaire ou l'emplacement du bureau est efface
            Instructor enseignant = Enseignant();
            Formulaire.Poser(_controleur, new Dictionary<string, string>
            {
                ["LastName"] = "Abercrombie",
                ["FirstMidName"] = "Kim",
                ["HireDate"] = "1995-03-11",
                ["OfficeAssignment.Location"] = "  "
            });

            //Lorsque
            await _controleur.Edit(9, Array.Empty<string>());

            //Alors l'enseignant n'a plus de bureau
            Assert.Null(enseignant.OfficeAssignment);
        }

        [Fact]
        public async Task Edit_VideLesCoursQuandAucuneCaseNEstCochee()
        {
            //Etant donne un enseignant qui donne un cours
            Instructor enseignant = Enseignant();
            Formulaire.Poser(_controleur, new Dictionary<string, string> { ["LastName"] = "Abercrombie" });

            //Lorsque aucune case n'est renvoyee
            await _controleur.Edit(9, null!);

            //Alors la liste de ses cours est vide
            Assert.Empty(enseignant.CourseAssignments);
        }

        [Fact]
        public async Task Edit_AnnonceLEchecDeLEnregistrement()
        {
            //Etant donne une base qui refuse d'ecrire
            Enseignant();
            Formulaire.Poser(_controleur, new Dictionary<string, string> { ["LastName"] = "Abercrombie" });
            _contexte.EnregistrementImpossible(new DbUpdateException("refus"));

            //Lorsque
            IActionResult resultat = await _controleur.Edit(9, Array.Empty<string>());

            //Alors
            Assert.Single(_controleur.ModelState[string.Empty]!.Errors);
            Assert.IsType<RedirectToActionResult>(resultat);
        }

        [Fact]
        public async Task Edit_RetourneIntrouvableQuandLEnseignantNExistePas()
        {
            Assert.IsType<NotFoundResult>(await _controleur.Edit(null, Array.Empty<string>()));
            Assert.IsType<NotFoundResult>(await _controleur.Edit(404, Array.Empty<string>()));
        }

        [Fact]
        public async Task Delete_AnnonceLEchecDeLaSuppressionPrecedente()
        {
            //Etant donne un retour de suppression manquee
            Enseignant();
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Delete(9, saveChangesError: true);

            //Alors
            Assert.IsType<ViewResult>(resultat);
            Assert.Contains("Delete failed", _controleur.ViewData["ErrorMessage"]!.ToString());
        }

        [Fact]
        public async Task Delete_NAnnonceAucuneErreurEnTempsNormal()
        {
            //Etant donne une demande de suppression ordinaire
            Enseignant();
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Delete(9);

            //Alors
            Assert.IsType<ViewResult>(resultat);
            Assert.Null(_controleur.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task Delete_RetourneIntrouvableQuandLEnseignantNExistePas()
        {
            Formulaire.Poser(_controleur);

            Assert.IsType<NotFoundResult>(await _controleur.Delete(404));
            Assert.IsType<NotFoundResult>(await _controleur.Delete(null));
        }

        [Fact]
        public async Task DeleteConfirmed_SupprimeLEnseignant()
        {
            //Etant donne un enseignant trouve par sa cle
            Instructor enseignant = Enseignant();
            LaRechercheParCleTrouve(enseignant);
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.DeleteConfirmed(9);

            //Alors
            _contexte.EnsembleEnseignants.Verify(e => e.Remove(enseignant), Times.Once);
            _contexte.VerifierUnEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_RevientALaListeQuandLEnseignantADejaDisparu()
        {
            //Etant donne un enseignant supprime entre temps
            LaRechercheParCleTrouve(null);
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.DeleteConfirmed(9);

            //Alors
            _contexte.VerifierAucunEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_RameneALaConfirmationQuandLEnregistrementEchoue()
        {
            //Etant donne une base qui refuse la suppression
            LaRechercheParCleTrouve(Enseignant());
            Formulaire.Poser(_controleur);
            _contexte.EnregistrementImpossible(new DbUpdateException("refus"));

            //Lorsque
            IActionResult resultat = await _controleur.DeleteConfirmed(9);

            //Alors
            var redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal("Delete", redirection.ActionName);
            Assert.Equal(9, redirection.RouteValues!["id"]);
            Assert.True((bool)redirection.RouteValues!["saveChangesError"]!);
        }
    }
}
