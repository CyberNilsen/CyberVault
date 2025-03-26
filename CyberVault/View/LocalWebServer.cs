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

        public LocalWebServer(string username, byte[] encryptionKey)
        {
            _username = username;
            _encryptionKey = encryptionKey;
            _accessToken = GenerateAccessToken();
        }

        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8765/");
            _listener.Start();

            Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    ProcessRequest(context);
                }
            });
        }

        public void Stop()
        {
            _listener?.Stop();
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

            if (context.Request.HttpMethod != "GET" ||
                context.Request.Url.LocalPath != "/passwords")
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            // Check Authorization
            string authHeader = context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) ||
                !authHeader.StartsWith("Bearer ") ||
                authHeader.Substring(7) != _accessToken)
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
                return;
            }

            // Fetch and decrypt passwords
            List<PasswordItem> passwords = PasswordStorage.LoadPasswords(_username, _encryptionKey);

            // Convert to JSON
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

        // Method to get the access token (to be used in the main app)
        public string GetAccessToken() => _accessToken;
    }
}