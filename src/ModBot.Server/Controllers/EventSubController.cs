using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModBot.Database.Models;
using ModBot.Server.Config;
using ModBot.Server.Helpers;
using RestSharp;

namespace ModBot.Server.Controllers;

[ApiController]
[Authorize]
[Route("eventsub/callback")]
public class EventSubController : BaseController
{
    private const string TwitchMessageSignatureHeader = "Twitch-Eventsub-Message-Signature";
    private const string TwitchMessageIdHeader = "Twitch-Eventsub-Message-Id";
    private const string TwitchMessageTimestampHeader = "Twitch-Eventsub-Message-Timestamp";

    private const string Secret = "your-secret-string"; // Store securely in environment variables

    [HttpPost("create-subscription")]
    public async Task<IActionResult> CreateEventSub(string channelId)
    {
        User? currentUser = User.User();
        if (currentUser == null) return Unauthorized("Moderator not authenticated.");

        string callbackUrl = $"{Globals.EventSubCallbackUri}/callback";

        var payload = new
        {
            type = "channel.chat_moderator_actions",
            version = "1",
            condition = new { broadcaster_user_id = channelId },
            transport = new
            {
                method = "webhook",
                callback = callbackUrl,
                secret = Secret
            }
        };

        RestClient client = new($"{Globals.TwitchApiUrl}/eventsub/subscriptions");
        RestRequest request = new("", Method.Post);
        request.AddHeader("Authorization", $"Bearer {currentUser.AccessToken}");
        request.AddHeader("Client-Id", Globals.TwitchClientId);
        request.AddHeader("Content-Type", "application/json");

        request.AddJsonBody(payload.ToJson());

        RestResponse response = await client.ExecuteAsync(request);

        if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
            return Ok(new
            {
                Message = "Subscription created successfully",
                Response = response.Content.FromJson<object>()
            });

        Console.WriteLine($"Failed to create subscription: {response.ErrorMessage}");
        return BadRequest(new
        {
            Message = "Failed to create subscription",
            Error = response.Content?.FromJson<object>()
        });
    }

    [HttpPost]
    public async Task<IActionResult> HandleEventSubCallback()
    {
        string body = await new StreamReader(Request.Body).ReadToEndAsync();

        // Handle Twitch subscription challenge
        if (Request.Headers["Twitch-Eventsub-Message-Type"] == "webhook_callback_verification")
        {
            dynamic? message = body.FromJson<dynamic>();
            string? challenge = message?.challenge;
            if (challenge != null) return Content(challenge, "text/plain");
        }

        // Verify Twitch signature
        if (!VerifyTwitchSignature(Request.Headers, body)) return Unauthorized("Invalid Twitch signature.");

        // Process EventSub notifications
        dynamic? messageData = body.FromJson<dynamic>();
        if (messageData?.subscription?.type == "channel.chat_moderator_actions")
        {
            dynamic? eventData = messageData.ToJson();
            Console.WriteLine($"Moderator action received: {eventData}");
        }

        return Ok();
    }

    private static bool VerifyTwitchSignature(IHeaderDictionary headers, string body)
    {
        if (!headers.TryGetValue(TwitchMessageSignatureHeader, out StringValues signature)) return false;

        StringValues messageId = headers[TwitchMessageIdHeader];
        StringValues timestamp = headers[TwitchMessageTimestampHeader];

        string hmacMessage = $"{messageId}{timestamp}{body}";
        string hash = ComputeHmacSha256(hmacMessage, Secret);

        return signature == $"sha256={hash}";
    }

    private static string ComputeHmacSha256(string message, string secret)
    {
        using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}