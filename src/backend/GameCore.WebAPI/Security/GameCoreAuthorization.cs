namespace GameCore.WebAPI.Security;

/// <summary>
/// Role names stored in <c>dbo.AppUsers.Role</c> (see CK_AppUsers_Role).
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Viewer = "Viewer";
}

/// <summary>
/// Authorization policies used by the API.
/// <list type="bullet">
/// <item><see cref="Read"/>: any authenticated GameCore role (Viewer, Manager, Admin).</item>
/// <item><see cref="Write"/>: roles allowed to create, update or deactivate business data (Manager, Admin).</item>
/// </list>
/// There are no Admin-only endpoints because user administration is out of scope for v1.
/// </summary>
public static class Policies
{
    public const string Read = "GameCore.Read";
    public const string Write = "GameCore.Write";

    public static IServiceCollection AddGameCoreAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Read, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Admin, Roles.Manager, Roles.Viewer))
            .AddPolicy(Write, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Admin, Roles.Manager));
        return services;
    }
}
