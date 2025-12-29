using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace IstasyonDemo.Api.Services
{
    public interface IFcmService
    {
        Task<string> SendNotificationAsync(string token, string title, string body, Dictionary<string, string>? data = null);
    }

    public class FcmService : IFcmService
    {
        private readonly ILogger<FcmService> _logger;

        public FcmService(ILogger<FcmService> logger)
        {
            _logger = logger;
        }

        public async Task<string> SendNotificationAsync(string token, string title, string body, Dictionary<string, string>? data = null)
        {
            try
            {
                // FirebaseApp'in Program.cs'de initialize edildiğini varsayıyoruz.
                // Eğer initialize edilmemişse burada kontrol edilebilir ama best practice startup'ta yapmaktır.
                
                var message = new Message()
                {
                    Token = token,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation($"Successfully sent message: {response}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending FCM notification");
                throw;
            }
        }
    }
}
