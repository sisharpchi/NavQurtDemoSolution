namespace NavQurt.Server.App.Services
{
    public class DevHttpHandler : HttpClientHandler
    {
        public DevHttpHandler()
        {
            // ⚠️ DEV UCHUN! PROD’da olib tashlang.
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }
    }
}
