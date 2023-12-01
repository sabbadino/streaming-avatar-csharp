using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Azure.Communication.Identity;
using Azure.Communication.NetworkTraversal;
using Azure;

namespace streaming_avatar_csharp.Controllers;

[ApiController]
[Route("api")]
public class AvatarController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AvatarController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost(template: "getIceServerToken", Name = "getIceServerToken")]
    public async Task<GetIceServerTokenResponse> GetIceServerToken()
    {
        try
        {

            var cnString = GetIceServerConfigData();
            var client = new CommunicationIdentityClient(cnString);


            var identityResponse = await client.CreateUserAsync();
            var identity = identityResponse.Value;
            var relayClient = new CommunicationRelayClient(cnString);


            Response<CommunicationRelayConfiguration> turnTokenResponse =
                await relayClient.GetRelayConfigurationAsync(identity);
            DateTimeOffset turnTokenExpiresOn = turnTokenResponse.Value.ExpiresOn;
            var iceServers = turnTokenResponse.Value.IceServers;
            Console.WriteLine($"Expires On: {turnTokenExpiresOn}");
            foreach (CommunicationIceServer iceServer in iceServers)
            {
                foreach (string url in iceServer.Urls)
                {
                    Console.WriteLine($"TURN Url: {url}");
                }

                return new GetIceServerTokenResponse {Username = iceServer.Username, Credential = iceServer.Credential};
            }

            return new GetIceServerTokenResponse();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new GetIceServerTokenResponse
            {
                Credential = e.Message, 
                Username = "Username"
            };
            //throw;
        }   
    }

    

    [HttpPost(template: "message", Name = "message")]
    public async Task<List<Message>> Message(List<Message> messages)
    {
        var ret = messages;
        ret.Add(new Controllers.Message { Role = "bot", Content = "hello from Emma"});
        return ret;
    }
    [HttpPost(template: "detectLanguage", Name = "detectLanguage")]
    public async Task<string> DetectLanguage([FromQuery] string text)
    {
        //return "en-US";
        return "en";
    }
    

    [HttpPost(template: "getSpeechToken", Name = "getSpeechToken")]
    public async Task<string> GetSpeechToken()
    {
     
            var configData = GetSpeechConfigData();
            var url = $"https://{configData.Region}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
            var client = _httpClientFactory.CreateClient(url);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", configData.SubscriptionKey);
            UriBuilder uriBuilder = new UriBuilder(url);

            var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception($"could not get speech token url {url} statusCode {result.StatusCode} content {await result.Content.ReadAsStringAsync()}");
            }
    }

    private string GetIceServerConfigData()
    {
        var iceConnectionString = _configuration["iceConnectionString"];
        return iceConnectionString;
    }

    private (string SubscriptionKey, string Region) GetSpeechConfigData()
    {
        var subscriptionKey = _configuration["speechKey"];
        ArgumentException.ThrowIfNullOrEmpty(subscriptionKey);
        var speechRegion = _configuration["speechRegion"];
        ArgumentException.ThrowIfNullOrEmpty(speechRegion);
        return (subscriptionKey, speechRegion);
    }
}

public class Message
{
    public string Role{ get; init; } = "";
    public string Content { get; init; } = "";
}

public class GetIceServerTokenResponse
{
    public string Username { get; init; } = "";
    public string Credential { get; init; } = "";
}




