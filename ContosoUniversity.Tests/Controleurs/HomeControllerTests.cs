using ContosoUniversity.Controllers;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Doubles;
using Microsoft.AspNetCore.Mvc;

namespace ContosoUniversity.Tests.Controleurs
{
    public class HomeControllerTests
    {
        private readonly ContexteFactice _contexte = new ContexteFactice();
        private readonly HomeController _controleur;

        public HomeControllerTests()
        {
            _controleur = new HomeController(_contexte.Contexte);
            Formulaire.Poser(_controleur);
        }

        [Fact]
        public void Index_RetourneLaPageDAccueil()
        {
            //Lorsque
            IActionResult resultat = _controleur.Index();

            //Alors
            Assert.IsType<ViewResult>(resultat);
        }

        [Fact]
        public void Privacy_RetourneLaPage()
        {
            //Lorsque
            IActionResult resultat = _controleur.Privacy();

            //Alors
            Assert.IsType<ViewResult>(resultat);
        }

        [Fact]
        public void Error_PorteLIdentifiantDeLaRequete()
        {
            //Etant donne une requete en cours
            _controleur.HttpContext.TraceIdentifier = "requete-42";

            //Lorsque
            IActionResult resultat = _controleur.Error();

            //Alors l'utilisateur recoit de quoi nommer l'incident
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("requete-42", Assert.IsType<ErrorViewModel>(vue.Model).RequestId);
        }
    }
}
