using System.Text;
using System.Text.RegularExpressions;
using Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Services
{
    public class GmailCodeReader : IGmailCodeReader
    {
        private readonly ILogger<GmailCodeReader> _logger;
        private static readonly string[] Scopes = { GmailService.Scope.GmailReadonly };
        private const string ApplicationName = "ICBC Road Test Verifier";
        private readonly AppConfig _config;

        public GmailCodeReader(ILogger<GmailCodeReader> logger, AppConfig config)
        {
            _logger = logger;
            _config = config;
        }

        private async Task<UserCredential> LoadGoogleCredentialAsync()
        {
            using var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);

            string tokenPath = "token.json";

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user", // This can be changed to an identifier if you support multiple accounts
                CancellationToken.None,
                new FileDataStore(tokenPath, true)
            );

            return credential;
        }


        public async Task<string?> GetVerificationCodeAsync()
        {
            try
            {
                var credential = await LoadGoogleCredentialAsync();

                var service = new GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                var request = service.Users.Messages.List("me");
                request.Q = "from:roadtests-donotreply@icbc.com";
                request.MaxResults = 5;
                request.LabelIds = new[] { "INBOX" }; // Opcional: limitar a bandeja de entrada
                request.IncludeSpamTrash = false;

                var messages = await request.ExecuteAsync();

                if (messages.Messages is null || messages.Messages.Count == 0)
                {
                    _logger.LogWarning("📭 No matching Gmail messages found.");
                    return null;
                }

                // Tomar solo el mensaje más reciente
                var latestMessage = messages.Messages.First();
                var email = await service.Users.Messages.Get("me", latestMessage.Id).ExecuteAsync();

                // Primero intentar extraer desde el snippet
                if (!string.IsNullOrWhiteSpace(email.Snippet))
                {
                    var match = Regex.Match(email.Snippet, @"\b\d{6}\b");
                    if (match.Success)
                    {
                        _logger.LogInformation("✅ Code found in snippet: {Code}", match.Value);
                        return match.Value;
                    }
                }

                // Fallback: intentar decodificar cuerpo (por si snippet falla en el futuro)
                var body = GetPlainTextFromMessage(email.Payload);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    var match = Regex.Match(body, @"\b\d{6}\b");
                    if (match.Success)
                    {
                        _logger.LogInformation("✅ Code found in body: {Code}", match.Value);
                        return match.Value;
                    }
                }

                _logger.LogWarning("⚠️ No verification code found in snippet or body.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting verification code from Gmail.");
                return null;
            }
        }


        private static string? GetPlainTextFromMessage(MessagePart part)
        {
            if (part.MimeType == "text/plain" && part.Body?.Data != null)
            {
                var decodedBytes = Convert.FromBase64String(part.Body.Data.Replace("-", "+").Replace("_", "/"));
                return Encoding.UTF8.GetString(decodedBytes);
            }

            if (part.Parts != null)
            {
                foreach (var subPart in part.Parts)
                {
                    var result = GetPlainTextFromMessage(subPart);
                    if (!string.IsNullOrEmpty(result))
                        return result;
                }
            }

            return null;
        }
    }
}
