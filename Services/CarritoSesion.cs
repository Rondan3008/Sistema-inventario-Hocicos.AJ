using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HocicosAJ.Services;

internal static class CarritoSesion
{
    private const string SessionKey = "HocicosAJ.Carrito";
    private const string CheckoutKeySessionKey = "HocicosAJ.CheckoutKey";
    private const string CheckoutFingerprintSessionKey = "HocicosAJ.CheckoutFingerprint";

    public static Dictionary<int, int> Leer(ISession session)
    {
        var contenido = session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return [];
        }

        var lineas = JsonSerializer.Deserialize<List<CarritoEntrada>>(contenido) ?? [];
        return lineas
            .Where(linea => linea.ProductoId > 0 && linea.Cantidad > 0)
            .GroupBy(linea => linea.ProductoId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(linea => linea.Cantidad));
    }

    public static void Guardar(ISession session, Dictionary<int, int> carrito)
    {
        InvalidarClaveCheckout(session);
        if (carrito.Count == 0)
        {
            session.Remove(SessionKey);
            return;
        }

        var lineas = carrito
            .Select(par => new CarritoEntrada(par.Key, par.Value))
            .ToList();
        session.SetString(SessionKey, JsonSerializer.Serialize(lineas));
    }

    public static Guid ObtenerClaveCheckout(ISession session, IReadOnlyDictionary<int, int> carrito)
    {
        var fingerprint = CrearHuella(carrito);
        var savedKey = session.GetString(CheckoutKeySessionKey);
        var savedFingerprint = session.GetString(CheckoutFingerprintSessionKey);
        if (Guid.TryParseExact(savedKey, "D", out var key)
            && string.Equals(savedFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return key;
        }

        key = Guid.NewGuid();
        session.SetString(CheckoutKeySessionKey, key.ToString("D"));
        session.SetString(CheckoutFingerprintSessionKey, fingerprint);
        return key;
    }

    public static bool ClaveCheckoutVigente(
        ISession session,
        IReadOnlyDictionary<int, int> carrito,
        Guid key)
    {
        return Guid.TryParseExact(session.GetString(CheckoutKeySessionKey), "D", out var savedKey)
            && savedKey == key
            && string.Equals(
                session.GetString(CheckoutFingerprintSessionKey),
                CrearHuella(carrito),
                StringComparison.Ordinal);
    }

    public static bool VaciarSiCoincide(
        ISession session,
        IReadOnlyDictionary<int, int> carrito,
        Guid key)
    {
        if (!ClaveCheckoutVigente(session, carrito, key)
            || !CarritosCoinciden(Leer(session), carrito))
        {
            return false;
        }

        Guardar(session, []);
        return true;
    }

    private static void InvalidarClaveCheckout(ISession session)
    {
        session.Remove(CheckoutKeySessionKey);
        session.Remove(CheckoutFingerprintSessionKey);
    }

    private static string CrearHuella(IReadOnlyDictionary<int, int> carrito)
    {
        return string.Join(
            ";",
            carrito.OrderBy(linea => linea.Key)
                .Select(linea => $"{linea.Key}:{linea.Value}"));
    }

    private static bool CarritosCoinciden(
        IReadOnlyDictionary<int, int> actual,
        IReadOnlyDictionary<int, int> esperado)
    {
        return actual.Count == esperado.Count
            && actual.All(linea =>
                esperado.TryGetValue(linea.Key, out var cantidad)
                && cantidad == linea.Value);
    }

    private sealed record CarritoEntrada(int ProductoId, int Cantidad);
}
