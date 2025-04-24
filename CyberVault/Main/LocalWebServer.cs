using CyberVault;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

namespace CyberVault.WebExtension;
public class LocalWebServer
{
    private HttpListener _listener;
    private string _username;
    private byte[] _encryptionKey;
    private static string? _accessToken;
    private static bool _isRunning = false;
    private Task _processingTask;
    private CancellationTokenSource _cancellationTokenSource;

    public LocalWebServer(string username, byte[] encryptionKey)
    {
        _username = username;
        _encryptionKey = encryptionKey;
        _accessToken = GenerateAccessToken();
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:8765/");
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            try
            {
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {
                _listener.Close();
                _listener = new HttpListener();
                _listener.Prefixes.Clear();
                _listener.Prefixes.Add("http://localhost:8766/");
                _listener.Start();
            }

            _isRunning = true;

            _processingTask = Task.Run(async () =>
            {
                var token = _cancellationTokenSource.Token;

                while (_isRunning && !token.IsCancellationRequested)
                {
                    try
                    {
                        var getContextTask = _listener.GetContextAsync();
                        HttpListenerContext context = await getContextTask.ConfigureAwait(false);

                        if (token.IsCancellationRequested)
                            break;

                        ProcessRequest(context);
                    }
                    catch (HttpListenerException)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WebServer error: {ex.Message}");
                    }
                }
            }, _cancellationTokenSource.Token);
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"Failed to start web server: {ex.Message}");
            _isRunning = false;
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;

        try
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();

            try
            {
                if (_processingTask != null)
                {
                    Task.WaitAny(new[] { _processingTask }, 1000);
                }
            }
            catch { }

            _listener.Stop();
            _listener.Close();
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping web server: {ex.Message}");
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        try
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Authorization");

            if (context.Request.HttpMethod == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                context.Response.Close();
                return;
            }

            if (context.Request.HttpMethod != "GET" || context.Request.Url!.LocalPath != "/passwords")
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            string authHeader = context.Request.Headers["Authorization"] ?? string.Empty;
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ") || authHeader.Substring(7) != _accessToken)
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
                return;
            }

            List<PasswordItem> passwords = PasswordStorage.LoadPasswords(_username, _encryptionKey);
            string jsonPasswords = JsonSerializer.Serialize(passwords);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonPasswords);

            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing request: {ex.Message}");
            try
            {
                context.Response.StatusCode = 500;
            }
            catch { }
        }
        finally
        {
            try
            {
                context.Response.Close();
            }
            catch { }
        }
    }

    private static string GenerateAccessToken()
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] tokenData = new byte[32];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData);
        }
    }

    public string GetAccessToken() => _accessToken!;
}