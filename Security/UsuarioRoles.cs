namespace HocicosAJ.Security;

internal static class UsuarioRoles
{
    public const string Administrador = "Administrador";
    public const string Cliente = "Cliente";
    public const string ValidationTimestampClaim = "HocicosAJ.RoleValidatedAt";

    public static string? Normalizar(string? tipoUsuario)
    {
        return tipoUsuario?.Trim() switch
        {
            string value when value.Equals(Administrador, StringComparison.OrdinalIgnoreCase) => Administrador,
            string value when value.Equals(Cliente, StringComparison.OrdinalIgnoreCase) => Cliente,
            _ => null
        };
    }
}
