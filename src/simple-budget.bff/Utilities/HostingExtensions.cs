using asg.redirect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Serilog;
using Yarp.ReverseProxy.Transforms;

namespace simple_budget.bff.Utilities;

public static class HostingExtensions
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, ConfigurationManager configurationManager)
    {
        // Add services to the container.
        services
        .AddAuthentication(schemes =>
        {            
            schemes.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            schemes.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            schemes.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            schemes.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.ForwardChallenge = OpenIdConnectDefaults.AuthenticationScheme;
            options.ForwardAuthenticate = JwtBearerDefaults.AuthenticationScheme;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Name = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Events.OnRedirectToLogin = CookieEvents.OnRedirectToLogin;
            options.Events.OnRedirectToAccessDenied = CookieEvents.OnRedirectToAccessDenied;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => {
            options.Events = new JwtBearerEvents() {
                OnMessageReceived = AsgJwtBearerEvents.OnMessageReceived
            };
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = configurationManager["Authentication:OIDC:Authority"];            
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = false;
            options.ClientId = configurationManager["Authentication:OIDC:ClientID"];
            options.ClientSecret = configurationManager["Authentication:OIDC:ClientSecret"];
            options.ResponseType = "code";
            
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("offline_access");
            options.Scope.Add("user");
            options.Scope.Add("email");

            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = AsgOpenIdConnectEvents.OnTokenValidated
            };
        });

        services.AddHealthChecks();
        services.AddHttpLogging(o => { });
        services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        services.AddReverseProxy()
                        .LoadFromConfig(configurationManager.GetSection("ReverseProxy"))
                        .AddTransforms(builderContext =>
                        {
                            // Conditionally add a transform for routes that require auth.
                            if (!string.IsNullOrEmpty(builderContext.Route.AuthorizationPolicy))
                            {
                                builderContext.AddRequestTransform(async transformContext =>
                                {
                                    string accessToken = await transformContext.HttpContext.GetTokenAsync("access_token") ?? string.Empty;
                                    transformContext.ProxyRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
                                });
                            }
                        });
        
        services.AddAuthorization(opt => {
            opt.AddPolicy("RequireAuthenticatedUserPolicy", policy => {
                policy.RequireAuthenticatedUser();
            });
        });

        services.AddRedirect(options => {
            options.RedirectFrom = configurationManager["AUTH:LOCAL:IDP:REDIRECTFROM"] ?? string.Empty;
            options.RedirectTo = configurationManager["AUTH:LOCAL:IDP:REDIRECTTO"] ?? string.Empty;
        });
        
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<ITokenManagementService, TokenManagementService>();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddDataProtection();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient();

        return services;
    }

    public static ConfigureHostBuilder ConfigureHost(this ConfigureHostBuilder host)
    {
        host.UseSerilog((context, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration);
        });
        return host;
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseIdpRedirect();
        }

        app.UseHealthChecks("/health");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpLogging();
        app.MapControllers();
        app.MapReverseProxy();
        return app;
    }
}
