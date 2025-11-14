using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler.Encoder;
using Microsoft.Owin.Security.Jwt;
using Owin;
using System.Configuration;
using System.Text;

[assembly: OwinStartup(typeof(AppOwnsData.Startup))]

namespace AppOwnsData
{
    public class Startup
    {
        string secret = ConfigurationManager.AppSettings["applicationSecret"].ToString();
        public void Configuration(IAppBuilder app)
        {
            var key = Encoding.UTF8.GetBytes(secret);

            app.UseJwtBearerAuthentication(new JwtBearerAuthenticationOptions
            {
                AuthenticationMode = AuthenticationMode.Active,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = System.TimeSpan.Zero
                }
            });
        }
    }
}
