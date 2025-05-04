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
    private Task? _processingTask;
    private CancellationTokenSource _cancellationTokenSource;

    public delegate void LogoutRequestedEventHandler(object sender, EventArgs e);
    public event LogoutRequestedEventHandler? LogoutRequested;

    private DateTime _lastRequestTime;
    private readonly TimeSpan _idleTimeout = TimeSpan.FromMinutes(30);
    private Timer? _idleCheckTimer;

    public LocalWebServer(string username, byte[] encryptionKey)
    {
        _username = username;
        _encryptionKey = encryptionKey;
        _accessToken = GenerateAccessToken();
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:8765/");
        _cancellationTokenSource = new CancellationTokenSource();
        _lastRequestTime = DateTime.Now;
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
            catch (HttpListenerException)
            {
                _listener.Close();
                _listener = new HttpListener();
                _listener.Prefixes.Clear();
                _listener.Prefixes.Add("http://localhost:8766/");
                _listener.Start();
            }

            _isRunning = true;
            _lastRequestTime = DateTime.Now;

            _idleCheckTimer = new Timer(CheckIdleTimeout, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

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

                        _lastRequestTime = DateTime.Now;

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

    private void CheckIdleTimeout(object? state)
    {
        if (!_isRunning) return;

        var idleTime = DateTime.Now - _lastRequestTime;
        if (idleTime > _idleTimeout)
        {
            Console.WriteLine("Web server idle timeout reached. Initiating logout.");
            OnLogoutRequested();
        }
    }

    protected virtual void OnLogoutRequested()
    {
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (!_isRunning) return;

        try
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();

            _idleCheckTimer?.Dispose();
            _idleCheckTimer = null;

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

            _accessToken = null;
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
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS, HEAD");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Authorization");

            if (context.Request.HttpMethod == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                context.Response.Close();
                return;
            }

            if (context.Request.HttpMethod == "GET" && context.Request.Url!.LocalPath == "/logout")
            {
                string authHeader = context.Request.Headers["Authorization"] ?? string.Empty;
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ") || authHeader.Substring(7) != _accessToken)
                {
                    context.Response.StatusCode = 401;
                    context.Response.Close();
                    return;
                }

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = Encoding.UTF8.GetBytes("{\"success\":true}").Length;
                context.Response.OutputStream.Write(Encoding.UTF8.GetBytes("{\"success\":true}"), 0, Encoding.UTF8.GetBytes("{\"success\":true}").Length);
                context.Response.Close();

                Task.Run(() => OnLogoutRequested());
                return;
            }

            if (context.Request.HttpMethod == "HEAD" && context.Request.Url!.LocalPath == "/passwords")
            {
                string authHeader = context.Request.Headers["Authorization"] ?? string.Empty;
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ") || authHeader.Substring(7) != _accessToken)
                {
                    context.Response.StatusCode = 401;
                    context.Response.Close();
                    return;
                }

                context.Response.StatusCode = 200;
                context.Response.Close();
                return;
            }

            if ((context.Request.HttpMethod != "GET" && context.Request.HttpMethod != "HEAD") || context.Request.Url!.LocalPath != "/passwords")
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            string authHeaderValue = context.Request.Headers["Authorization"] ?? string.Empty;
            if (string.IsNullOrEmpty(authHeaderValue) || !authHeaderValue.StartsWith("Bearer ") || authHeaderValue.Substring(7) != _accessToken)
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
                return;
            }

            if (context.Request.HttpMethod == "HEAD")
            {
                context.Response.StatusCode = 200;
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
        byte[] tokenData = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenData);
    }

    public string GetAccessToken() => _accessToken ?? string.Empty;

    public bool IsRunning => _isRunning;
}