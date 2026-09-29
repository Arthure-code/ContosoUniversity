using ContosoUniversity.Controllers;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Doubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContosoUniversity.Tests.Controleurs
{
    public class StudentsControllerTests
    {
        private readonly ContexteFactice _contexte = new ContexteFactice();
        private readonly StudentsController _controleur;

        public StudentsControllerTests()
        {
            _controleur = new StudentsController(_contexte.Contexte);
        }

        private Student Etudiant(int id = 1)
        {
            var etudiant = new Student
            {
                ID = id,
                LastName = "Alexander",
                FirstMidName = "Carson",
                EnrollmentDate = new DateTime(2010, 9, 1)
            };
            _contexte.Etudiants.Add(etudiant);
            return etudiant;
        }

        [Fact]
        public async Task Details_RetourneLEtudiantDemande()
        {
            //Etant donne un etudiant enregistre
            Etudiant();

            //Lorsque
            IActionResult resultat = await _controleur.Details(1);

            //Alors
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("Alexander", Assert.IsType<Student>(vue.Model).LastName);
        }

        [Fact]
        public async Task Details_RetourneIntrouvableSansIdentifiant()
        {
            //Lorsque
            IActionResult resultat = await _controleur.Details(null);

            //Alors
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task Details_RetourneIntrouvableQuandLEtudiantNExistePas()
        {
            //Lorsque
            IActionResult resultat = await _controleur.Details(404);

            //Alors
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task Create_EnregistreLEtudiantEtRevientALaListe()
        {
            //Etant donne un etudiant saisi
            var etudiant = new Student
            {
                LastName = "Tremblay",
                FirstMidName = "Jeanne",
                EnrollmentDate = new DateTime(2026, 9, 1)
            };
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Create(etudiant);

            //Alors
            _contexte.Mock.Verify(c => c.Add(etudiant), Times.Once);
            _contexte.VerifierUnEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task Create_AnnonceLEchecDeLEnregistrement()
        {
            //Etant donne une base qui refuse d'ecrire
            Formulaire.Poser(_controleur);
            _contexte.EnregistrementImpossible(new DbUpdateException("refus"));

            //Lorsque
            IActionResult resultat = await _controleur.Create(new Student { LastName = "Tremblay", FirstMidName = "Jeanne" });

            //Alors la page revient avec le message, sans exception
            Assert.IsType<ViewResult>(resultat);
            Assert.Single(_controleur.ModelState[string.Empty]!.Errors);
        }

        [Fact]
        public async Task Edit_EnregistreLesChampsDuFormulaire()
        {
            //Etant donne un etudiant et un formulaire qui change son nom
            Student etudiant = Etudiant();
            Formulaire.Poser(_controleur, new Dictionary<string, string>
            {
                ["LastName"] = "Alexandre",
                ["FirstMidName"] = "Carson",
                ["EnrollmentDate"] = "2010-09-01"
            });

            //Lorsque
            IActionResult resultat = await _controleur.Edit(1);

            //Alors
            Assert.Equal("Alexandre", etudiant.LastName);
            _contexte.VerifierUnEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task Edit_NEnregistreRienQuandLeModeleEstInvalide()
        {
            //Etant donne un formulaire deja tenu pour invalide
            Etudiant();
            Formulaire.Poser(_controleur, new Dictionary<string, string> { ["LastName"] = "Alexandre" });
            _controleur.ModelState.AddModelError("LastName", "invalide");

            //Lorsque
            IActionResult resultat = await _controleur.Edit(1);

            //Alors
            _contexte.VerifierAucunEnregistrement();
            Assert.IsType<ViewResult>(resultat);
        }

        [Fact]
        public async Task Edit_AnnonceLEchecDeLEnregistrement()
        {
            //Etant donne une base qui refuse d'ecrire
            Etudiant();
            Formulaire.Poser(_controleur, new Dictionary<string, string> { ["LastName"] = "Alexandre" });
            _contexte.EnregistrementImpossible(new DbUpdateException("refus"));

            //Lorsque
            IActionResult resultat = await _controleur.Edit(1);

            //Alors
            Assert.IsType<ViewResult>(resultat);
            Assert.Single(_controleur.ModelState[string.Empty]!.Errors);
        }

        [Fact]
        public async Task Edit_RetourneIntrouvableQuandLEtudiantNExistePas()
        {
            //Lorsque
            IActionResult resultat = await _controleur.Edit(404);

            //Alors
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task Delete_AnnonceLEchecDeLaSuppressionPrecedente()
        {
            //Etant donne un retour de suppression manquee
            Etudiant();
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Delete(1, saveChangesError: true);

            //Alors
            Assert.IsType<ViewResult>(resultat);
            Assert.Contains("Delete failed", _controleur.ViewData["ErrorMessage"]!.ToString());
        }

        [Fact]
        public async Task Delete_NAnnonceAucuneErreurEnTempsNormal()
        {
            //Etant donne une demande de suppression ordinaire
            Etudiant();
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Delete(1);

            //Alors
            Assert.IsType<ViewResult>(resultat);
            Assert.Null(_controleur.ViewData["ErrorMessage"]);
        }
    }
}
