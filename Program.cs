using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

internal static class Program
{
    const int Port = 47821;
    static string PairingCode = "";

    static async Task Main()
    {
        Console.Title = "Authorized Recovery Agent";
        PairingCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        Console.WriteLine("======================================");
        Console.WriteLine("     AUTHORIZED RECOVERY AGENT");
        Console.WriteLine("======================================");
        Console.WriteLine();
        Console.WriteLine($"Pairing code: {PairingCode}");
        Console.WriteLine($"Listening port: {Port}");
        Console.WriteLine();
        Console.WriteLine("Keep this window open while using");
        Console.WriteLine("the authorized recovery controller.");
        Console.WriteLine();

        TcpListener listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine("Agent is running.");
        Console.WriteLine();

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(async () =>
            {
                try { await HandleClient(client); }
                catch (Exception ex) { Console.WriteLine($"Client error: {ex.Message}"); }
                finally { client.Close(); }
            });
        }
    }

    static async Task HandleClient(TcpClient client)
    {
        using NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[8192];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead <= 0) return;

        string requestText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        JsonDocument request;
        try { request = JsonDocument.Parse(requestText); }
        catch
        {
            await SendResponse(stream, new { success = false, message = "Invalid JSON." });
            return;
        }

        JsonElement root = request.RootElement;
        string code = root.TryGetProperty("code", out JsonElement codeElement)
            ? codeElement.GetString() ?? "" : "";

        if (code != PairingCode)
        {
            await SendResponse(stream, new { success = false, message = "Invalid pairing code." });
            return;
        }

        string action = root.TryGetProperty("action", out JsonElement actionElement)
            ? actionElement.GetString() ?? "" : "";

        switch (action.ToLowerInvariant())
        {
            case "status":
                await SendResponse(stream, new
                {
                    success = true,
                    computer = Environment.MachineName,
                    operatingSystem = Environment.OSVersion.ToString(),
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
                break;

            case "restart":
                await SendResponse(stream, new { success = true, message = "Restart requested." });
                await Task.Delay(1000);
                ProcessStart("shutdown.exe", "/r /t 5");
                break;

            case "shutdown":
                await SendResponse(stream, new { success = true, message = "Shutdown requested." });
                await Task.Delay(1000);
                ProcessStart("shutdown.exe", "/s /t 5");
                break;

            default:
                await SendResponse(stream, new { success = false, message = "Unknown action." });
                break;
        }
    }

    static void ProcessStart(string fileName, string arguments)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    static async Task SendResponse(NetworkStream stream, object response)
    {
        string json = JsonSerializer.Serialize(response);
        byte[] data = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(data, 0, data.Length);
        await stream.FlushAsync();
    }
}
