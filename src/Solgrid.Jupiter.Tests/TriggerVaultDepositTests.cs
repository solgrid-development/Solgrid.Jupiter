using System.Net;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class TriggerVaultDepositTests
{
    private const string BaseUrl = "https://unit.test/trigger/v2";
    private const string Wallet = "BQ72nSv9f3PRyRKCBnHLVrerrv37CYTHm5h3s9VSGQDV";

    private static JupiterTriggerClientOptions AuthedOptions(string? apiKey = null) => new()
    {
        BaseUrl = BaseUrl,
        ApiKey = apiKey,
        AuthToken = "test.jwt.token",
        MinRequestInterval = TimeSpan.Zero
    };

    private static CraftDepositRequest SampleDeposit() => new()
    {
        InputMint = "So11111111111111111111111111111111111111112",
        OutputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
        UserAddress = Wallet,
        Amount = "1000000000",
        OrderType = TriggerDepositOrderType.Price,
        OrderSubType = TriggerOrderType.Single
    };

    [Fact]
    public async Task GetVault_SendsBearerAndApiKey()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_vault.json"));
        using var client = new JupiterTriggerClient(AuthedOptions("jup_test_key"), new HttpClient(handler));

        var vault = await client.GetVaultAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(BaseUrl + "/vault", request.RequestUri!.ToString());
        Assert.Equal("Bearer test.jwt.token", request.Headers.Authorization!.ToString());
        Assert.Equal("jup_test_key", request.Headers.GetValues("x-api-key").Single());
        Assert.Equal("7nE9GJoYHNmtaQvTQpota3KV2oz4pQ2dA6nvYK8EUJHV", vault!.VaultPubkey);
        Assert.Equal(Wallet, vault.UserPubkey);
    }

    [Fact]
    public async Task GetVault_ReturnsNull_On404()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.NotFound, "{\"error\":\"No vault found\"}");
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var vault = await client.GetVaultAsync();

        Assert.Null(vault);
    }

    [Fact]
    public async Task GetVault_Throws_WithoutAuthToken()
    {
        using var client = new JupiterTriggerClient(
            new JupiterTriggerClientOptions { BaseUrl = BaseUrl, MinRequestInterval = TimeSpan.Zero },
            new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetVaultAsync());
    }

    [Fact]
    public async Task RegisterVault_BuildsExpectedUrl()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.Created, Fixtures.Read("trigger_vault.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var vault = await client.RegisterVaultAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(BaseUrl + "/vault/register", request.RequestUri!.ToString());
        Assert.Equal("x7qm2p9rk4vt8wz1nb3jc5yd6e", vault.PrivyVaultId);
    }

    [Fact]
    public async Task RegisterVault_ThrowsJupiterApiException_On409()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.Conflict, "{\"error\":\"Vault already exists\"}");
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<JupiterApiException>(() => client.RegisterVaultAsync());

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("Vault already exists", exception.Error);
    }

    [Fact]
    public async Task CraftDeposit_SerializesBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_deposit_craft.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        await client.CraftDepositAsync(SampleDeposit());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(BaseUrl + "/deposit/craft", request.RequestUri!.ToString());
        Assert.Equal(
            "{\"inputMint\":\"So11111111111111111111111111111111111111112\"," +
            "\"outputMint\":\"EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v\"," +
            "\"userAddress\":\"" + Wallet + "\"," +
            "\"amount\":\"1000000000\"," +
            "\"orderType\":\"price\"," +
            "\"orderSubType\":\"single\"}",
            handler.RequestBodies.Single());
    }

    [Fact]
    public async Task CraftDeposit_ParsesResponse()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_deposit_craft.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var response = await client.CraftDepositAsync(SampleDeposit());

        Assert.True(response.HasTransaction);
        Assert.Equal("0c7f7fb7-96a5-4d65-84f9-15e943d742b0", response.RequestId);
        Assert.Equal("7nE9GJoYHNmtaQvTQpota3KV2oz4pQ2dA6nvYK8EUJHV", response.ReceiverAddress);
        Assert.Equal("110000000", response.Amount);
        Assert.Equal(9, response.TokenDecimals);
        Assert.False(string.IsNullOrEmpty(response.InputTokenAccount));
    }

    [Fact]
    public async Task CraftDeposit_DcaBody_OmitsSubTypeAndKeepsDcaType()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_deposit_craft.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        await client.CraftDepositAsync(new CraftDepositRequest
        {
            InputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
            OutputMint = "So11111111111111111111111111111111111111112",
            UserAddress = Wallet,
            Amount = "20000000",
            OrderType = TriggerDepositOrderType.Dca
        });

        var body = handler.RequestBodies.Single()!;
        Assert.Contains("\"orderType\":\"dca\"", body);
        Assert.DoesNotContain("orderSubType", body);
    }

    [Fact]
    public async Task CraftDeposit_Throws_WhenSubTypeOnDca()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        var request = new CraftDepositRequest
        {
            InputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
            OutputMint = "So11111111111111111111111111111111111111112",
            UserAddress = Wallet,
            Amount = "20000000",
            OrderType = TriggerDepositOrderType.Dca,
            OrderSubType = TriggerOrderType.Single
        };

        await Assert.ThrowsAsync<ArgumentException>(() => client.CraftDepositAsync(request));
    }

    [Fact]
    public async Task CraftDeposit_Throws_WhenSubTypeMissingOnPrice()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        var request = SampleDeposit();
        request.OrderSubType = null;

        await Assert.ThrowsAsync<ArgumentException>(() => client.CraftDepositAsync(request));
    }

    [Fact]
    public async Task Vault_ThrowsJupiterApiException_On401()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized, Fixtures.Read("trigger_error_401.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<JupiterApiException>(() => client.GetVaultAsync());

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("Unauthorized", exception.Error);
    }

    [Fact]
    public async Task CraftDeposit_ThrowsJupiterApiException_On400WithDetails()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, Fixtures.Read("trigger_error_400_details.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<JupiterApiException>(() => client.CraftDepositAsync(SampleDeposit()));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("Request validation failed", exception.Error);
        Assert.Contains("Invalid option", exception.RawBody);
    }

    [Fact]
    public async Task CraftDeposit_RetriesOnce_On429()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.TooManyRequests, "{\"error\":\"Rate limit exceeded\"}",
            new Dictionary<string, string> { ["x-ratelimit-reset"] = "1" });
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_deposit_craft.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var response = await client.CraftDepositAsync(SampleDeposit());

        Assert.Equal(2, handler.Requests.Count);
        Assert.True(response.HasTransaction);
    }
}
