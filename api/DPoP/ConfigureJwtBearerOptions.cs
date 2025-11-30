using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Api;

public class ConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly string _configScheme;

    public ConfigureJwtBearerOptions(string configScheme)
    {
        _configScheme = configScheme;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (_configScheme == name)
        {
            if (options.EventsType != null && !typeof(DPoPJwtBearerEvents).IsAssignableFrom(options.EventsType))
            {
                throw new Exception("EventsType on JwtBearerOptions must derive from DPoPJwtBearerEvents to work with the DPoP support.");
            }
            
            // In ASP.NET Core 10.0+, the framework auto-creates a default JwtBearerEvents instance.
            // If Events is set but is just the default JwtBearerEvents (not a custom derived type),
            // we replace it with DPoPJwtBearerEvents. If it's a custom type that doesn't derive
            // from DPoPJwtBearerEvents, we throw an error.
            if (options.Events != null)
            {
                var eventsType = options.Events.GetType();
                
                // If it's exactly JwtBearerEvents (the default), replace it with DPoPJwtBearerEvents
                if (eventsType == typeof(JwtBearerEvents))
                {
                    options.Events = null!;
                    options.EventsType = typeof(DPoPJwtBearerEvents);
                }
                // If it's a custom type that doesn't derive from DPoPJwtBearerEvents, throw error
                else if (!typeof(DPoPJwtBearerEvents).IsAssignableFrom(eventsType))
                {
                    throw new Exception("Events on JwtBearerOptions must derive from DPoPJwtBearerEvents to work with the DPoP support.");
                }
            }
            else if (options.EventsType == null)
            {
                options.EventsType = typeof(DPoPJwtBearerEvents);
            }
        }
    }
}
