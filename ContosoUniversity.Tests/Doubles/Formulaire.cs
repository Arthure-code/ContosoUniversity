using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace ContosoUniversity.Tests.Doubles
{
    /// <summary>
    /// Pose un formulaire sur un controleur, avec les services dont la
    /// liaison de modele a besoin. Sans cela, TryUpdateModelAsync n'a ni
    /// valeurs a lire ni validation a executer.
    /// </summary>
    internal static class Formulaire
    {
        public static void Poser(Controller controleur, Dictionary<string, string> champs)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMvcCore().AddDataAnnotations().AddViews();
            ServiceProvider fournisseur = services.BuildServiceProvider();

            var requete = new DefaultHttpContext { RequestServices = fournisseur };
            requete.Request.ContentType = "application/x-www-form-urlencoded";
            requete.Request.Form = new FormCollection(
                champs.ToDictionary(champ => champ.Key, champ => new StringValues(champ.Value)));

            // RouteData et le descripteur ne servent pas a la liaison, mais
            // RedirectToAction construit une adresse et les exige.
            controleur.ControllerContext = new ControllerContext(
                new ActionContext(requete, new RouteData(), new ControllerActionDescriptor()));
            controleur.ControllerContext.ValueProviderFactories.Add(new FormValueProviderFactory());
            controleur.MetadataProvider = fournisseur.GetRequiredService<IModelMetadataProvider>();
            controleur.ModelBinderFactory = fournisseur.GetRequiredService<IModelBinderFactory>();
            controleur.ObjectValidator = fournisseur.GetRequiredService<IObjectModelValidator>();
        }

        /// <summary>
        /// Le meme appareillage, sans aucun champ : ce qu'il faut a une
        /// action qui ne lit pas de formulaire mais ecrit dans ViewData.
        /// </summary>
        public static void Poser(Controller controleur)
        {
            Poser(controleur, new Dictionary<string, string>());
        }
    }
}
