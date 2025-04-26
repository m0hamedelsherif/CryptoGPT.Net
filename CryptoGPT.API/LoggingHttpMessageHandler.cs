using Serilog;

namespace CryptoGPT.API
{
    public class LoggingHttpMessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Log.Information("[HTTP OUT] {Method} {Uri}", request.Method, request.RequestUri);
            //if (request.Content != null)
            //{
            //    var requestBody = await request.Content.ReadAsStringAsync();
            //    Log.Information("[HTTP OUT] RequestBody: {Body}", requestBody);
            //}
            var response = await base.SendAsync(request, cancellationToken);
            Log.Information("[HTTP IN] {StatusCode} {Uri} ResponseHeaders", (int)response.StatusCode, response.RequestMessage?.RequestUri);
            if (response.Content != null)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Log.Information("[HTTP IN] ResponseBody: {Body}", responseBody);
            }
            return response;
        }
    }
}