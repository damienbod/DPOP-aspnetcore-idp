using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;
using Serilog;

namespace Api;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        var deploySwaggerUI = builder.Environment.IsDevelopment();

        builder.Services.AddSecurityHeaderPolicies()
        .SetPolicySelector((PolicySelectorContext ctx) =>
        {
            // sum is weak security headers due to Swagger UI deployment
            // should only use in development
            if (deploySwaggerUI)
            {
                // Weakened security headers for Swagger UI
                if (ctx.HttpContext.Request.Path.StartsWithSegments("/swagger"))
                {
                    return SecurityHeadersDefinitionsSwagger.GetHeaderPolicyCollection(builder.Environment.IsDevelopment());
                }

                // Strict security headers
                return SecurityHeadersDefinitionsAPI.GetHeaderPolicyCollection(builder.Environment.IsDevelopment());
            }
            // Strict security headers for production
            else
            {
                return SecurityHeadersDefinitionsAPI.GetHeaderPolicyCollection(builder.Environment.IsDevelopment());
            }
        });

        var stsServer = configuration["StsServer"];

        services.AddAuthentication("dpoptokenscheme")
            .AddJwtBearer("dpoptokenscheme", options =>
            {
                options.Authority = stsServer;
                options.TokenValidationParameters.ValidateAudience = false;
                options.MapInboundClaims = false;

                options.TokenValidationParameters.ValidTypes = ["at+jwt"];
            });

        services.ConfigureDPoPTokensForScheme("dpoptokenscheme");

        services.AddAuthorizationBuilder()
            .AddPolicy("protectedScope", policy =>
            {
                policy.RequireClaim("scope", "scope-dpop");
            });

        builder.Services.AddOpenApi(options =>
        {
            //options.UseTransformer((document, context, cancellationToken) =>
            //{
            //    document.Info = new()
            //    {
            //        Title = "My API",
            //        Version = "v1",
            //        Description = "API for Damien"
            //    };
            //    return Task.CompletedTask;
            //});
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        services.AddControllers();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        IdentityModelEventSource.ShowPII = true;
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSecurityHeaders();

        //app.MapOpenApi(); // /openapi/v1.json
        app.MapOpenApi("/openapi/v1/openapi.json");
        //app.MapOpenApi("/openapi/{documentName}/openapi.json");

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1/openapi.json", "v1");
            });
        }

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers()
            .RequireAuthorization();

        return app;
    }
}