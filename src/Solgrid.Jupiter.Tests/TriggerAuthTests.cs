using System.Net;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class TriggerAuthTests
{
    private const string BaseUrl = "https://unit.test/trigger/v2";
    private const string Wallet = "BQ72nSv9f3PRyRKCBnHLVrerrv37CYTHm5h3s9VSGQDV";

    private static JupiterTriggerClientOptions NoThrottleOptions(string? apiKey = null, string? authToken = null) => new()
    {
        BaseUrl = BaseUrl,
        ApiKey = apiKey,
        AuthToken = authToken,
        MinRequestInterval = TimeSpan.Zero
    };

    [Fact]
    public async Task GetChallenge_BuildsExpectedRequest()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_challenge_message.json"));
        using var client = new JupiterTriggerClient(NoThrottleOptions(), new HttpClient(handler));

        await client.GetChallengeAsync(Wallet);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(BaseUrl + "/auth/challenge", request.RequestUri!.ToString());
        Assert.Equal($"{{\"walletPubkey\":\"{Wallet}\",\"type\":\"message\"}}", handler.RequestBodies.Single());
    }

    [Fact]
    public async Task GetChallenge_SerializesTransactionType()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_challenge_transaction.json"));
        using var client = new JupiterTriggerClient(NoThrottleOptions(), new HttpClient(handler));

        var response = await client.GetChallengeAsync(Wallet, TriggerChallengeType.Transaction);

        Assert.Equal($"{{\"walletPubkey\":\"{Wallet}\",\"type\":\"transaction\"}}", handler.RequestBodies.Single());
        Assert.Equal("transaction", response.Type);
        Assert.False(string.IsNullOrEmpty(response.Transaction));
        Assert.Null(response.Challenge);
    }

    [Fact]
    public async Task GetChallenge_ParsesMessageResponse()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_challenge_message.json"));
        using var client = new JupiterTriggerClient(NoThrottleOptions(), new HttpClient(handler));

        var response = await client.GetChallengeAsync(Wallet);

        Assert.Equal("message", response.Type);
        Assert.Contains("Sign this message to authenticate with Jupiter", response.Challenge);
    }

    [Fact]
    public async Task GetChallenge_SendsApiKeyHeader_WhenConfigured()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_challenge_message.json"));
        using var client = new JupiterTriggerClient(NoThrottleOptions("jup_test_key"), new HttpClient(handler));

        await client.GetChallengeAsync(Wallet);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("jup_test_key", request.Headers.GetValues("x-api-key").Single());
    }

    [Fact]
    public async Task Verify_SerializesMessageBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_verify_token.json"));
        using var client = new JupiterTriggerClient(NoThrottleOptions(), new HttpClient(handler));

        await client.VerifyAsync(new TriggerVerifyRequest
        {
            Type = TriggerChallengeType.Message,
            WalletPubkey = Wallet,
            Signature = "5eykt4UsFv8P8NJdTREpY1vzqKqZKvdpKuc147dw2N9dXGfJw8Lf3FQ6oQvS9nN2tS1vV7uVe3XyYz9pL2mK4rT1"
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(BaseUrl + "/auth/verify", request.RequestUri!.ToString());
        Assert.Equal(
            "{\"type\":\"message\",\"walletPubkey\":\"" + Wallet + "\",\"signature\":\"5eykt4UsFv8P8NJdTREpY1vzqKqZKvdpKuc147dw2N9dXGfJw8Lf3FQ6oQvS9nN2tS1vV7uVe3XyYz9pL2mK4rT1\"}",
            handler.RequestBodies.Single());
    }

    [Fact]
    public async Task Verify_SerializesTransactionBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_verify_token.json"));
        using var client = new JupiterTriggerClient(NoThrottleOptions(), new HttpClient(handler));

        await client.VerifyAsync(new TriggerVerifyRequest
        {
            Type = TriggerChallengeType.Transaction,
            WalletPubkey = Wallet,
            SignedTransaction = "AQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAEN"
        });

        Assert.Equal(
            "{\"type\":\"transaction\",\"walletPubkey\":\"" + Wallet + "\",\"signedTransaction\":\"AQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAEN\"}",
            handler.RequestBodies.Single());
    }

    [Fact]
    public async Task Verify_ParsesToken()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_verify_token.json"));
        using var client = new JupiterTriggerClient(NoThrottleOptions(), new HttpClient(handler));

        var response = await client.VerifyAsync(new TriggerVerifyRequest
        {
            Type = TriggerChallengeType.Message,
            WalletPubkey = Wallet,
            Signature = "sig"
        });

        Assert.StartsWith("eyJhbGciOiJIUzI1NiIs", response.Token);
    }

    [Fact]
    public async Task Verify_Throws_WhenMessageSignatureMissing()
    {
        using var client = new JupiterTriggerClient(NoThrottleOptions(), new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<ArgumentException>(() => client.VerifyAsync(new TriggerVerifyRequest
        {
            Type = TriggerChallengeType.Message,
            WalletPubkey = Wallet
        }));
    }

    [Fact]
    public async Task Verify_Throws_WhenTransactionMissing()
    {
        using var client = new JupiterTriggerClient(NoThrottleOptions(), new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<ArgumentException>(() => client.VerifyAsync(new TriggerVerifyRequest
        {
            Type = TriggerChallengeType.Transaction,
            WalletPubkey = Wallet,
            Signature = "sig"
        }));
    }

    [Fact]
    public async Task Verify_ThrowsJupiterApiException_On500WithRawMessage()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.InternalServerError, "{\"error\":\"Expected base58-encoded signature to decode to a byte array of length 64. Actual length: 32.\"}");
        using var client = new JupiterTriggerClient(NoThrottleOptions(), new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<JupiterApiException>(() => client.VerifyAsync(new TriggerVerifyRequest
        {
            Type = TriggerChallengeType.Message,
            WalletPubkey = Wallet,
            Signature = "tooshort"
        }));

        Assert.Equal(500, exception.StatusCode);
        Assert.Contains("base58-encoded signature", exception.Error);
    }
}
