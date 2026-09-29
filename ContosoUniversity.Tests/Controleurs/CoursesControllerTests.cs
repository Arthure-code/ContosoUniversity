using ContosoUniversity.Controllers;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Doubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContosoUniversity.Tests.Controleurs
{
    public class CoursesControllerTests
    {
        private readonly ContexteFactice _contexte = new ContexteFactice();
        private readonly CoursesController _controleur;

        public CoursesControllerTests()
        {
            _controleur = new CoursesController(_contexte.Contexte);
            _contexte.Departements.Add(new Department { DepartmentID = 3, Name = "Engineering" });
            _contexte.Departements.Add(new Department { DepartmentID = 1, Name = "English" });
        }

        private Course Cours(int numero = 1050)
        {
            var cours = new Course { CourseID = numero, Title = "Chemistry", Credits = 3, DepartmentID = 3 };
            _contexte.Cours.Add(cours);
            return cours;
        }

        private void LaRechercheParCleTrouve(Course? cours)
        {
            _contexte.EnsembleCours
                .Setup(e => e.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync(cours);
        }

        [Fact]
        public async Task Index_RetourneLesCours()
        {
            //Etant donne un cours enregistre
            Cours();

            //Lorsque
            IActionResult resultat = await _controleur.Index();

            //Alors
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Single(Assert.IsAssignableFrom<IEnumerable<Course>>(vue.Model));
        }

        [Fact]
        public async Task Details_RetourneLeCoursDemande()
        {
            //Etant donne un cours enregistre
            Cours();

            //Lorsque
            IActionResult resultat = await _controleur.Details(1050);

            //Alors
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("Chemistry", Assert.IsType<Course>(vue.Model).Title);
        }

        [Fact]
        public async Task Details_RetourneIntrouvableSansIdentifiant()
        {
            Assert.IsType<NotFoundResult>(await _controleur.Details(null));
        }

        [Fact]
        public void Create_ProposeLesDepartementsParOrdreAlphabetique()
        {
            //Etant donne deux departements
            Formulaire.Poser(_controleur);

            //Lorsque
            _controleur.Create();

            //Alors
            object? propose = _controleur.ViewBag.DepartmentID;
            var liste = Assert.IsType<SelectList>(propose);
            Assert.Equal(new[] { "Engineering", "English" }, liste.Select(element => element.Text));
        }

        [Fact]
        public async Task Create_EnregistreLeCours()
        {
            //Etant donne un cours saisi
            var cours = new Course { CourseID = 5001, Title = "Astronomy", Credits = 3, DepartmentID = 3 };
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Create(cours);

            //Alors
            _contexte.Mock.Verify(c => c.Add(cours), Times.Once);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task Create_RevientAuFormulaireQuandLeNumeroEstRefuse()
        {
            //Etant donne un numero hors des bornes
            Formulaire.Poser(_controleur);
            _controleur.ModelState.AddModelError("CourseID", "Le numero doit se situer entre 1 et 9999.");

            //Lorsque
            IActionResult resultat = await _controleur.Create(new Course { CourseID = 0, DepartmentID = 3 });

            //Alors la liste des departements est de retour avec le formulaire
            _contexte.VerifierAucunEnregistrement();
            Assert.IsType<ViewResult>(resultat);
            object? propose = _controleur.ViewBag.DepartmentID;
            Assert.NotNull(propose);
        }

        [Fact]
        public async Task Edit_EnregistreLesChampsDuFormulaire()
        {
            //Etant donne un cours et un formulaire qui change ses credits
            Course cours = Cours();
            Formulaire.Poser(_controleur, new Dictionary<string, string>
            {
                ["Title"] = "Chemistry",
                ["Credits"] = "4",
                ["DepartmentID"] = "3"
            });

            //Lorsque
            IActionResult resultat = await _controleur.EditPost(1050);

            //Alors
            Assert.Equal(4, cours.Credits);
            _contexte.VerifierUnEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task Edit_RetourneIntrouvableQuandLeCoursNExistePas()
        {
            Assert.IsType<NotFoundResult>(await _controleur.EditPost(404));
            Assert.IsType<NotFoundResult>(await _controleur.EditPost(null));
        }

        [Fact]
        public async Task Edit_AnnonceLEchecDeLEnregistrement()
        {
            //Etant donne une base qui refuse d'ecrire
            Cours();
            Formulaire.Poser(_controleur, new Dictionary<string, string> { ["Credits"] = "4" });
            _contexte.EnregistrementImpossible(new DbUpdateException("refus"));

            //Lorsque
            IActionResult resultat = await _controleur.EditPost(1050);

            //Alors
            Assert.Single(_controleur.ModelState[string.Empty]!.Errors);
            Assert.IsType<RedirectToActionResult>(resultat);
        }

        [Fact]
        public async Task Delete_AnnonceLEchecDeLaSuppressionPrecedente()
        {
            //Etant donne un retour de suppression manquee
            Cours();
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Delete(1050, saveChangesError: true);

            //Alors
            Assert.IsType<ViewResult>(resultat);
            Assert.Contains("Delete failed", _controleur.ViewData["ErrorMessage"]!.ToString());
        }

        [Fact]
        public async Task Delete_NAnnonceAucuneErreurEnTempsNormal()
        {
            //Etant donne une demande de suppression ordinaire
            Cours();
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Delete(1050);

            //Alors
            Assert.IsType<ViewResult>(resultat);
            Assert.Null(_controleur.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task Delete_RetourneIntrouvableQuandLeCoursNExistePas()
        {
            Formulaire.Poser(_controleur);

            Assert.IsType<NotFoundResult>(await _controleur.Delete(404));
            Assert.IsType<NotFoundResult>(await _controleur.Delete(null));
        }

        [Fact]
        public async Task DeleteConfirmed_SupprimeLeCours()
        {
            //Etant donne un cours trouve par sa cle
            Course cours = Cours();
            LaRechercheParCleTrouve(cours);
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.DeleteConfirmed(1050);

            //Alors
            _contexte.EnsembleCours.Verify(e => e.Remove(cours), Times.Once);
            _contexte.VerifierUnEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_RevientALaListeQuandLeCoursADejaDisparu()
        {
            //Etant donne un cours supprime entre temps
            LaRechercheParCleTrouve(null);
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.DeleteConfirmed(1050);

            //Alors
            _contexte.VerifierAucunEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_RameneALaConfirmationQuandLEnregistrementEchoue()
        {
            //Etant donne une base qui refuse la suppression
            LaRechercheParCleTrouve(Cours());
            Formulaire.Poser(_controleur);
            _contexte.EnregistrementImpossible(new DbUpdateException("refus"));

            //Lorsque
            IActionResult resultat = await _controleur.DeleteConfirmed(1050);

            //Alors l'utilisateur revient sur la page de confirmation, avertie
            var redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal("Delete", redirection.ActionName);
            Assert.Equal(1050, redirection.RouteValues!["id"]);
            Assert.Equal(true, redirection.RouteValues!["saveChangesError"]);
        }
    }
}
