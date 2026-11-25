using System.Net;
using System.Text.Json;

namespace Switchyard.Robot;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            Console.Error.WriteLine("Uso: Switchyard.Robot <ID> [URL del servidor]");
            Console.Error.WriteLine("Ejemplo: Switchyard.Robot R01 http://localhost:5080");
            return 1;
        }

        var serverAddress = args.Length == 2 ? args[1] : "http://localhost:5080";
        if (!Uri.TryCreate(serverAddress, UriKind.Absolute, out var serverUri)
            || (serverUri.Scheme != Uri.UriSchemeHttp && serverUri.Scheme != Uri.UriSchemeHttps)
            || serverUri.AbsolutePath != "/"
            || serverUri.Query.Length != 0
            || serverUri.Fragment.Length != 0
            || serverUri.UserInfo.Length != 0)
        {
            Console.Error.WriteLine("Indica una URL HTTP o HTTPS del servidor, sin rutas, parámetros ni credenciales.");
            return 1;
        }

        using var httpClient = new HttpClient
        {
            BaseAddress = serverUri,
            Timeout = TimeSpan.FromSeconds(10)
        };
        var api = new RobotApiClient(httpClient);

        try
        {
            var robot = await api.RegisterAsync(args[0]);
            Console.WriteLine($"Robot {robot.Id} registrado en {serverUri}.");
            Console.WriteLine($"Fecha de alta (UTC): {robot.RegisteredAt:O}");
            return 0;
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            Console.Error.WriteLine($"El robot {args[0]} ya está registrado. Puedes consultar su registro en /api/robots/{args[0]}.");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            Console.Error.WriteLine("La API ha rechazado el identificador. Usa 1–32 caracteres: A–Z, 0–9, guion o guion bajo, empezando por una letra o un número.");
        }
        catch (HttpRequestException exception)
        {
            Console.Error.WriteLine(exception.StatusCode is null
                ? $"No se pudo conectar con {serverUri}. Comprueba que la API está arrancada y la dirección es correcta."
                : $"La API devolvió un error HTTP {(int)exception.StatusCode}.");
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("La API no respondió dentro del tiempo de espera de 10 segundos. Consulta el registro antes de repetir el alta.");
        }
        catch (JsonException)
        {
            Console.Error.WriteLine("La API devolvió una respuesta JSON que el cliente no puede interpretar.");
        }

        return 1;
    }
}
