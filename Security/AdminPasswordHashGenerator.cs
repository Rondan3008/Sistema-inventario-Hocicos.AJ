using System.Text;
using HocicosAJ.Models.Database;
using Microsoft.AspNetCore.Identity;

namespace HocicosAJ.Security;

internal static class AdminPasswordHashGenerator
{
    private const int MinimumPasswordLength = 12;

    public static void Run()
    {
        Console.Write("Nueva contraseña de administrador (mínimo 12 caracteres): ");
        var password = ReadPassword();
        Console.WriteLine();

        if (password.Length < MinimumPasswordLength)
        {
            throw new InvalidOperationException(
                $"La contraseña debe tener al menos {MinimumPasswordLength} caracteres.");
        }

        Console.Write("Repita la contraseña: ");
        var confirmation = ReadPassword();
        Console.WriteLine();
        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Las contraseñas no coinciden.");
        }

        var hash = new PasswordHasher<Usuario>()
            .HashPassword(new Usuario(), password);

        if (hash.Length > 250)
        {
            throw new InvalidOperationException(
                "El hash excede la capacidad del campo contraseña.");
        }

        Console.WriteLine("Hash ASP.NET PasswordHasher generado. No contiene la contraseña.");
        Console.WriteLine("Transfiera el hash de forma segura y no lo registre en logs ni repositorios:");
        Console.WriteLine(hash);
    }

    private static string ReadPassword()
    {
        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
            }
        }

        var result = password.ToString();
        password.Clear();
        return result;
    }
}
