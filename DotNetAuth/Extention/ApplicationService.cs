using DotNetAuth.Domain.Constracts;
using DotNetAuth.Infrsstructure.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DotNetAuth.Extention
{
    public static partial class ApplicationService
    {
        //Allow Any Origin method and header
         public static void ConfigureCors(this IServiceCollection services)
         {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });
         }

        public static void ConfigureIdentity(this IServiceCollection service)
        {
            service.AddIdentityCore<IdentityUser>(o =>
            {
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 8;
            }).AddEntityFrameworkStores<ApplicationDbContext>()
              .AddDefaultTokenProviders();
        }

        public static void ConfigureJwt(this IServiceCollection services, IConfiguration configuration) 
        {
            var jwtSetting = configuration.GetSection("JwtSettings").Get<JwtSettings>(); 
            if (jwtSetting == null || string.IsNullOrEmpty(jwtSetting?.Key))
            {
                throw new InvalidOperationException("JWT Secret key is not configured");
            }
            var secretkey =  new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting.Key));
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSetting.ValidIssuer,
                    ValidAudience = jwtSetting.ValidAudience,
                    IssuerSigningKey = secretkey
                };
                o.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = "You are not authorize to access this resource. please authenticaate"
                        });
                        return context.Response.WriteAsync(result);
                    }
                };
            });

        }
    }
}
