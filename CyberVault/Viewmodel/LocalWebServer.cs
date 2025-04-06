using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace CyberVault.WebExtension
{
    public class LocalWebServer
    {
        private HttpListener _listener;
        private string _username;
        private byte[] _encryptionKey;
        private static string _accessToken;
        private static bool _isRunning = false;

        public LocalWebServer(string username, byte[] encryptionKey)
        {
            _username = username;
            _encryptionKey = encryptionKey;
            _accessToken = GenerateAccessToken();
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8765/");
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener.Start();
                _isRunning = true;

                Task.Run(async () =>
                {
                    while (_isRunning)
                    {
                        try
                        {
                            HttpListenerContext context = await _listener.GetContextAsync();
                            ProcessRequest(context);
                        }
                        catch (HttpListenerException) { }
                    }
                });
            }
            catch (HttpListenerException)
            {
                _isRunning = false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _isRunning = false;
                _listener.Stop();
                _listener.Close();
            }
            catch (ObjectDisposedException) { }
        }

        private void ProcessRequest(HttpListenerContext context)
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

            if (context.Request.HttpMethod != "GET" || context.Request.Url.LocalPath != "/passwords")
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            string authHeader = context.Request.Headers["Authorization"];
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
            context.Response.Close();
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

        public string GetAccessToken() => _accessToken;
    }
}
