﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
            schemes.DefaultAuthenticateScheme = OpenIdConnectDefaults.AuthenticationScheme;
            schemes.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Name = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Events.OnRedirectToLogin = (ctx) => { 
                ctx.Response.StatusCode = 401;
                return Task.CompletedTask; 
            };
            options.Events.OnRedirectToAccessDenied = (ctx) => {
                ctx.Response.StatusCode = 403;
                return Task.CompletedTask;
            };
            options.LoginPath = "/User/NotAuthorized";
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = configurationManager["Authentication:OIDC:Authority"];            
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.ClientId = "backend-for-frontend";
            options.ClientSecret = "49C1A7E1-0C79-4A89-A3D6-A37998FB86B0";
            options.ResponseType = "code";

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("email");
            options.Scope.Add("user");
            options.Scope.Add("offline_access");

            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = AsgOpenIdConnectEvents.OnTokenValidated,
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

        services.AddIdpRedirect(options => {
            options.RedirectFrom = configurationManager["AUTH:LOCAL:IDP:REDIRECTFROM"] ?? string.Empty;
            options.RedirectTo = configurationManager["AUTH:LOCAL:IDP:REDIRECTTO"] ?? string.Empty;
        });

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
