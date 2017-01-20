using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SentimentAnalyzer.Startup))]
namespace SentimentAnalyzer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
