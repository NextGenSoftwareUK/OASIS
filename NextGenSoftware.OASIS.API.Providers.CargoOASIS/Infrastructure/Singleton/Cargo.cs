using System.Net.NetworkInformation;

namespace Infrastructure.Singleton
{
    public sealed class Cargo
    {
        private string _accessToken = string.Empty;
        private static Cargo _cargo;
        
        public static Cargo GetInstance()
        {
            return _cargo ??= new Cargo();
        }

        public void SetAccessToken(string token)
        {
            _accessToken = token;
        }
    }
}