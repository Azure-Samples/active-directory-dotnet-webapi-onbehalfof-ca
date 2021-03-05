using Microsoft.Identity.Web.Aspnet;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.ActiveDirectory;
using Owin;

namespace TodoListDownstreamService
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            app.ProtectWebApiWithMicrosoftIdentity(new JwtBearerOptions());
        }
    }
}
