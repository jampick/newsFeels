using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(newsFeelsWeb.Startup))]
namespace newsFeelsWeb
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
