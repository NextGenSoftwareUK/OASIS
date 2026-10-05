using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.Utilities
{
    public static class URIHelper
    {
        public static async Task<bool> ValidateUrlWithHttpClientAsync(string url)
        {
            using var client = new HttpClient();

            try
            {
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException e)
                when (e.InnerException is SocketException
                { SocketErrorCode: SocketError.HostNotFound })
            {
                return false;
            }
            catch (HttpRequestException e)
                when (e.StatusCode.HasValue && (int)e.StatusCode.Value > 500)
            {
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}