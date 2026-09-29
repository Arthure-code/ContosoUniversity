using ContosoUniversity.Controllers;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Doubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContosoUniversity.Tests.Controleurs
{
    public class DepartmentsControllerTests
    {
        private static readonly DateTime Rentree = new DateTime(2007, 9, 1);

        private static readonly string[] EnseignantsAttendus = { "Abercrombie, Kim", "Fakhouri, Fadi" };

        private readonly ContexteFactice _contexte = new ContexteFactice();
        private readonly DepartmentsController _controleur;

        public DepartmentsControllerTests()
        {
            _controleur = new DepartmentsController(_contexte.Contexte);
            _contexte.Enseignants.Add(new Instructor { ID = 9, LastName = "Abercrombie", FirstMidName = "Kim" });
            _contexte.Enseignants.Add(new Instructor { ID = 10, LastName = "Fakhouri", FirstMidName = "Fadi" });
        }

        private Department Departement(int id = 1)
        {
            var departement = new Department
            {
                DepartmentID = id,
                Name = "English",
                Budget = 350000m,
                StartDate = Rentree,
                InstructorID = 9,
                RowVersion = new byte[] { 1 }
            };
            _contexte.Departements.Add(departement);
            return departement;
        }

        [Fact]
        public async Task Index_RetourneLesDepartementsAvecLeurAdministrateur()
        {
            //Etant donne un departement enregistre
            Departement();

            //Lorsque
            IActionResult resultat = await _controleur.Index();

            //Alors
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Single(Assert.IsAssignableFrom<IEnumerable<Department>>(vue.Model));
        }

        [Fact]
        public void Create_ProposeLesEnseignantsParLeurNomComplet()
        {
            //Etant donne deux enseignants
            Formulaire.Poser(_controleur);

            //Lorsque
            _controleur.Create();

            //Alors la liste porte le nom complet, pas le prenom seul
            var liste = Assert.IsType<SelectList>(_controleur.ViewData["InstructorID"]);
            Assert.Equal(EnseignantsAttendus, liste.Select(element => element.Text));
        }

        [Fact]
        public async Task Create_EnregistreLeDepartement()
        {
            //Etant donne un departement saisi
            var departement = new Department { Name = "History", Budget = 50000m, StartDate = Rentree, InstructorID = 10 };
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Create(departement);

            //Alors
            _contexte.Mock.Verify(c => c.Add(departement), Times.Once);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task Create_RemetLaListeQuandLeBudgetManque()
        {
            //Etant donne un budget absent
            Formulaire.Poser(_controleur);
            _controleur.ModelState.AddModelError("Budget", "Le budget est requis.");

            //Lorsque
            IActionResult resultat = await _controleur.Create(new Department { Name = "History", InstructorID = 10 });

            //Alors
            _contexte.VerifierAucunEnregistrement();
            Assert.IsType<ViewResult>(resultat);
            Assert.IsType<SelectList>(_controleur.ViewData["InstructorID"]);
        }

        [Fact]
        public async Task Edit_RetourneLeDepartementEtLaListeDesEnseignants()
        {
            //Etant donne un departement enregistre
            Departement();
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Edit(1);

            //Alors
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("English", Assert.IsType<Department>(vue.Model).Name);
            Assert.IsType<SelectList>(_controleur.ViewData["InstructorID"]);
        }

        [Fact]
        public async Task Edit_RetourneIntrouvableSansIdentifiant()
        {
            Assert.IsType<NotFoundResult>(await _controleur.Edit(null));
            Assert.IsType<NotFoundResult>(await _controleur.Edit(404));
        }

        [Fact]
        public async Task Edit_AnnonceQueLeDepartementAEteSupprimeParUnAutre()
        {
            //Etant donne un departement disparu entre l'ouverture et l'envoi
            Formulaire.Poser(_controleur, new Dictionary<string, string>
            {
                ["Name"] = "English",
                ["Budget"] = "350000",
                ["StartDate"] = "2007-09-01",
                ["InstructorID"] = "9"
            });

            //Lorsque
            IActionResult resultat = await _controleur.Edit(1, new byte[] { 1 });

            //Alors la page revient avec l'explication et les valeurs envoyees
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("English", Assert.IsType<Department>(vue.Model).Name);
            Assert.Contains("deleted by another user", _controleur.ModelState[string.Empty]!.Errors[0].ErrorMessage);
            _contexte.VerifierAucunEnregistrement();
        }

        [Fact]
        public async Task Edit_EnregistreLesChampsEnvoyesAvecLeJetonDeVersion()
        {
            //Etant donne un departement suivi, comme apres une lecture en base
            var contexte = new ContexteFactice(suiviReel: true);
            var departement = new Department
            {
                DepartmentID = 1,
                Name = "English",
                Budget = 350000m,
                StartDate = Rentree,
                InstructorID = 9,
                RowVersion = new byte[] { 1 }
            };
            contexte.Departements.Add(departement);
            contexte.Enseignants.Add(new Instructor { ID = 9, LastName = "Abercrombie", FirstMidName = "Kim" });
            contexte.Contexte.Attach(departement);

            var controleur = new DepartmentsController(contexte.Contexte);
            Formulaire.Poser(controleur, new Dictionary<string, string>
            {
                ["Name"] = "English",
                ["Budget"] = "360000",
                ["StartDate"] = "2007-09-01",
                ["InstructorID"] = "9"
            });

            //Lorsque le formulaire revient avec le jeton lu a l'ouverture
            IActionResult resultat = await controleur.Edit(1, new byte[] { 1 });

            //Alors le budget est enregistre, et le jeton envoye est celui que
            //la base comparera
            Assert.Equal(360000m, departement.Budget);
            Assert.Equal(
                new byte[] { 1 },
                contexte.Contexte.Entry(departement).Property(nameof(Department.RowVersion)).OriginalValue);
            contexte.VerifierUnEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task Edit_AnnonceLesValeursCourantesQuandUnAutreAEnregistreAvant()
        {
            //Etant donne un departement ouvert a l'ecran
            var contexte = new ContexteFactice(suiviReel: true);
            var departement = new Department
            {
                DepartmentID = 1,
                Name = "English",
                Budget = 350000m,
                StartDate = Rentree,
                InstructorID = 9,
                RowVersion = new byte[] { 1 }
            };
            contexte.Departements.Add(departement);
            contexte.Enseignants.Add(new Instructor { ID = 10, LastName = "Fakhouri", FirstMidName = "Fadi" });

            //Et la meme ligne, deja changee en base par quelqu'un d'autre
            using (var autre = new SchoolContext(contexte.Options))
            {
                autre.Departments.Add(new Department
                {
                    DepartmentID = 1,
                    Name = "Literature",
                    Budget = 400000m,
                    StartDate = new DateTime(2008, 9, 1),
                    InstructorID = 10,
                    RowVersion = new byte[] { 9 }
                });
                autre.SaveChanges();
            }

            contexte.SuitLaModificationDe(departement);

            var controleur = new DepartmentsController(contexte.Contexte);
            Formulaire.Poser(controleur, new Dictionary<string, string>
            {
                ["Name"] = "English",
                ["Budget"] = "350000",
                ["StartDate"] = "2007-09-01",
                ["InstructorID"] = "9"
            });

            //Lorsque l'enregistrement part avec le jeton lu a l'ouverture
            IActionResult resultat = await controleur.Edit(1, new byte[] { 1 });

            //Alors la page revient en annoncant, champ par champ, ce que
            //l'autre utilisateur a ecrit
            Assert.IsType<ViewResult>(resultat);
            Assert.Contains("Literature", controleur.ModelState["Name"]!.Errors[0].ErrorMessage);
            Assert.Contains("400", controleur.ModelState["Budget"]!.Errors[0].ErrorMessage);
            Assert.Contains("2008", controleur.ModelState["StartDate"]!.Errors[0].ErrorMessage);
            Assert.Contains("Fakhouri, Fadi", controleur.ModelState["InstructorID"]!.Errors[0].ErrorMessage);
            Assert.Contains("modified by another user", controleur.ModelState[string.Empty]!.Errors[0].ErrorMessage);

            //Et le jeton suit celui de la base, pour que le deuxieme envoi
            //soit accepte
            Assert.Equal(new byte[] { 9 }, departement.RowVersion);
        }

        [Fact]
        public async Task Edit_AnnonceLaDisparitionQuandLAutreUtilisateurASupprimeLaLigne()
        {
            //Etant donne un departement ouvert a l'ecran, et la ligne
            //supprimee en base entre temps
            var contexte = new ContexteFactice(suiviReel: true);
            var departement = new Department
            {
                DepartmentID = 1,
                Name = "English",
                Budget = 350000m,
                StartDate = Rentree,
                InstructorID = 9,
                RowVersion = new byte[] { 1 }
            };
            contexte.Departements.Add(departement);
            contexte.SuitLaModificationDe(departement);

            var controleur = new DepartmentsController(contexte.Contexte);
            Formulaire.Poser(controleur, new Dictionary<string, string>
            {
                ["Name"] = "English",
                ["Budget"] = "350000",
                ["StartDate"] = "2007-09-01",
                ["InstructorID"] = "9"
            });

            //Lorsque
            IActionResult resultat = await controleur.Edit(1, new byte[] { 1 });

            //Alors l'utilisateur apprend que la ligne n'existe plus
            Assert.IsType<ViewResult>(resultat);
            Assert.Contains("deleted by another user", controleur.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Edit_RemetLaListeQuandLeBudgetEnvoyeNEstPasUnNombre()
        {
            //Etant donne un departement suivi et un budget illisible
            var contexte = new ContexteFactice(suiviReel: true);
            var departement = new Department
            {
                DepartmentID = 1,
                Name = "English",
                Budget = 350000m,
                StartDate = Rentree,
                InstructorID = 9,
                RowVersion = new byte[] { 1 }
            };
            contexte.Departements.Add(departement);
            contexte.Enseignants.Add(new Instructor { ID = 9, LastName = "Abercrombie", FirstMidName = "Kim" });
            contexte.Contexte.Attach(departement);

            var controleur = new DepartmentsController(contexte.Contexte);
            Formulaire.Poser(controleur, new Dictionary<string, string>
            {
                ["Name"] = "English",
                ["Budget"] = "beaucoup",
                ["StartDate"] = "2007-09-01",
                ["InstructorID"] = "9"
            });

            //Lorsque
            IActionResult resultat = await controleur.Edit(1, new byte[] { 1 });

            //Alors rien n'est enregistre, et la page revient avec sa liste
            contexte.VerifierAucunEnregistrement();
            Assert.IsType<ViewResult>(resultat);
            Assert.IsType<SelectList>(controleur.ViewData["InstructorID"]);
        }

        [Fact]
        public async Task Edit_RetourneIntrouvableSansIdentifiantALEnvoi()
        {
            Assert.IsType<NotFoundResult>(await _controleur.Edit(null, new byte[] { 1 }));
        }

        [Fact]
        public async Task ShowDifferences_AnnonceChaqueChampChangeParLAutreUtilisateur()
        {
            //Etant donne ce qui a ete envoye et ce qui se trouve en base
            Department envoye = new Department
            {
                Name = "English",
                Budget = 350000m,
                StartDate = Rentree,
                InstructorID = 9
            };
            Department enBase = new Department
            {
                Name = "Literature",
                Budget = 400000m,
                StartDate = new DateTime(2008, 9, 1),
                InstructorID = 10,
                RowVersion = new byte[] { 9 }
            };
            Department aMettreAJour = Departement();
            Formulaire.Poser(_controleur);
            _controleur.ModelState.SetModelValue("RowVersion", new byte[] { 1 }, "AQ==");

            //Lorsque
            await _controleur.ShowDifferencesAsync(envoye, enBase, aMettreAJour);

            //Alors chaque champ porte la valeur courante, et le nom complet de
            //l'enseignant est celui lu en base
            Assert.Contains("Literature", _controleur.ModelState["Name"]!.Errors[0].ErrorMessage);
            Assert.Contains("400", _controleur.ModelState["Budget"]!.Errors[0].ErrorMessage);
            Assert.Contains("2008", _controleur.ModelState["StartDate"]!.Errors[0].ErrorMessage);
            Assert.Contains("Fakhouri, Fadi", _controleur.ModelState["InstructorID"]!.Errors[0].ErrorMessage);
            Assert.Contains("modified by another user", _controleur.ModelState[string.Empty]!.Errors[0].ErrorMessage);

            //Et le jeton de version suit celui de la base, pour que le
            //deuxieme envoi soit accepte
            Assert.Equal(new byte[] { 9 }, aMettreAJour.RowVersion);
            Assert.False(_controleur.ModelState.ContainsKey("RowVersion"));
        }

        [Fact]
        public async Task ShowDifferences_NAnnonceQueLExplicationQuandRienNeDiffere()
        {
            //Etant donne deux departements identiques
            Department envoye = new Department { Name = "English", Budget = 350000m, StartDate = Rentree, InstructorID = 9 };
            Department enBase = new Department { Name = "English", Budget = 350000m, StartDate = Rentree, InstructorID = 9 };
            Formulaire.Poser(_controleur);

            //Lorsque
            await _controleur.ShowDifferencesAsync(envoye, enBase, Departement());

            //Alors aucun champ n'est mis en cause
            Assert.Single(_controleur.ModelState);
            Assert.True(_controleur.ModelState.ContainsKey(string.Empty));
        }

        [Fact]
        public async Task Delete_AnnonceLeConflitDeLaSuppressionPrecedente()
        {
            //Etant donne un retour de suppression annulee
            Departement();
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Delete(1, concurrencyError: true);

            //Alors
            Assert.IsType<ViewResult>(resultat);
            Assert.Contains("was modified by another user", _controleur.ViewData["ConcurrencyErrorMessage"]!.ToString());
        }

        [Fact]
        public async Task Delete_RevientALaListeQuandLeDepartementADejaDisparu()
        {
            //Etant donne un departement supprime par un autre utilisateur
            Formulaire.Poser(_controleur);

            //Lorsque le retour de conflit arrive
            IActionResult resultat = await _controleur.Delete(1, concurrencyError: true);

            //Alors il n'y a plus rien a montrer
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task Delete_RetourneIntrouvableSansIdentifiant()
        {
            Formulaire.Poser(_controleur);

            Assert.IsType<NotFoundResult>(await _controleur.Delete((int?)null, (bool?)null));
            Assert.IsType<NotFoundResult>(await _controleur.Delete(404, (bool?)null));
        }

        [Fact]
        public async Task DeleteConfirme_SupprimeLeDepartement()
        {
            //Etant donne un departement enregistre
            Department departement = Departement();
            Formulaire.Poser(_controleur);

            //Lorsque la confirmation arrive sans jeton de version
            IActionResult resultat = await _controleur.Delete(1, (byte[]?)null);

            //Alors
            _contexte.EnsembleDepartements.Verify(e => e.Remove(departement), Times.Once);
            _contexte.VerifierUnEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task DeleteConfirme_RevientALaListeQuandLeDepartementADejaDisparu()
        {
            //Etant donne un departement deja supprime
            Formulaire.Poser(_controleur);

            //Lorsque
            IActionResult resultat = await _controleur.Delete(1, (byte[]?)null);

            //Alors
            _contexte.VerifierAucunEnregistrement();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task DeleteConfirme_RameneALaConfirmationQuandUnAutreAModifieLaLigne()
        {
            //Etant donne une suppression refusee par le jeton de version
            Departement();
            Formulaire.Poser(_controleur);
            _contexte.EnregistrementImpossible(new DbUpdateConcurrencyException("conflit"));

            //Lorsque
            IActionResult resultat = await _controleur.Delete(1, (byte[]?)null);

            //Alors la page de confirmation revient, avec le conflit annonce
            var redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal("Delete", redirection.ActionName);
            Assert.Equal(1, redirection.RouteValues!["id"]);
            Assert.True((bool)redirection.RouteValues!["concurrencyError"]!);
        }
    }
}
