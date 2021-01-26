using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Aspnet;
using System.Configuration;

namespace TodoListWebApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            string[] scopes = new[] { ConfigurationManager.AppSettings["ida:TodoListServiceScope"] };
            app.AddMicrosoftIdentityWebAppAuthentication(new AuthenticationConfig());
            app.EnableTokenAcquisitionToCallDownstreamApi(new AuthenticationConfig(), scopes);

            // This makes any middleware defined above this line run before the Authorization rule is applied in web.config
            app.UseStageMarker(PipelineStage.Authenticate);
        }
    }
}