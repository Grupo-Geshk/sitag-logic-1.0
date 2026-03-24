using Microsoft.Extensions.Configuration;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Infrastructure.Identity;

/// <summary>
/// Reads application-level config values from IConfiguration and exposes them
/// to the Application layer through IAppSettings.
/// </summary>
public sealed class AppSettings : IAppSettings
{
    public string FrontendUrl { get; }

    public AppSettings(IConfiguration config)
    {
        FrontendUrl = config["FRONTEND_URL"] ?? "https://sitag.app";
    }
}
