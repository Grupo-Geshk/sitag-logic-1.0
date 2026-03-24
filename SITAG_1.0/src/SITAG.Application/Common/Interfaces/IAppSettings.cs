namespace SITAG.Application.Common.Interfaces;

/// <summary>
/// Provides application-level configuration values to the Application layer
/// without taking a direct dependency on Microsoft.Extensions.Configuration.
/// Implemented in Infrastructure and registered as a singleton.
/// </summary>
public interface IAppSettings
{
    /// <summary>Base URL of the frontend app (e.g. https://sitag.app).</summary>
    string FrontendUrl { get; }
}
