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
                request.MaxResults = 10;
                request.LabelIds = new[] { "INBOX" };
                request.IncludeSpamTrash = false;

                var messages = await request.ExecuteAsync();

                if (messages.Messages == null || messages.Messages.Count == 0)
                {
                    _logger.LogWarning("📭 No matching Gmail messages found.");
                    return null;
                }

                // Ordenar los mensajes por internalDate (más reciente primero)
                var detailedMessages = new List<Message>();
                foreach (var messageMeta in messages.Messages)
                {
                    var msg = await service.Users.Messages.Get("me", messageMeta.Id).ExecuteAsync();
                    detailedMessages.Add(msg);
                }

                var latestMessage = detailedMessages
                    .OrderByDescending(m => m.InternalDate)
                    .FirstOrDefault();

                if (latestMessage == null)
                {
                    _logger.LogWarning("⚠️ No message could be loaded after metadata fetch.");
                    return null;
                }

                // Intentar extraer desde el snippet
                if (!string.IsNullOrWhiteSpace(latestMessage.Snippet))
                {
                    var match = Regex.Match(latestMessage.Snippet, @"\b\d{6}\b");
                    if (match.Success)
                    {
                        _logger.LogInformation("✅ Code found in snippet: {Code}", match.Value);
                        return match.Value;
                    }
                }

                // Intentar extraer desde el cuerpo si snippet falla
                var body = GetPlainTextFromMessage(latestMessage.Payload);
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
