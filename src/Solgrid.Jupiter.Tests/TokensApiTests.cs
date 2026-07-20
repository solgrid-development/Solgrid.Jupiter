using System.Net;
using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class TokensApiTests
{
    private const string TokensApiUrl = "https://unit.test/tokens/v2";

    private static JupiterSwapClientOptions NoThrottleOptions() => new()
    {
        BaseUrl = "https://unit.test/swap/v2",
        TokensApiUrl = TokensApiUrl,
        MinRequestInterval = TimeSpan.Zero
    };

    private static JupiterSwapClient ClientWith(FakeHttpHandler handler) =>
        new(NoThrottleOptions(), new HttpClient(handler));

    [Fact]
    public void TokenInfo_MapsRichToken()
    {
        var tokens = JsonDefaults.Deserialize<List<TokenInfo>>(Fixtures.Read("tokens_search.json"));

        var sol = Assert.Single(tokens, t => t.Symbol == "SOL");
        Assert.Equal("So11111111111111111111111111111111111111112", sol.Id);
        Assert.Equal("Wrapped SOL", sol.Name);
        Assert.Equal(9, sol.Decimals);
        Assert.Equal("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA", sol.TokenProgram);
        Assert.Equal(3820662, sol.HolderCount);
        Assert.True(sol.OrganicScore > 98);
        Assert.Equal("high", sol.OrganicScoreLabel);
        Assert.True(sol.IsVerified);
        Assert.NotNull(sol.Tags);
        Assert.Contains("verified", sol.Tags);
        Assert.NotNull(sol.Apy);
        Assert.True(sol.Apy!.JupEarn > 0);

        Assert.NotNull(sol.Audit);
        Assert.True(sol.Audit!.MintAuthorityDisabled);
        Assert.True(sol.Audit.FreezeAuthorityDisabled);
        Assert.True(sol.Audit.TopHoldersPercentage > 0 && sol.Audit.TopHoldersPercentage < 100);

        Assert.NotNull(sol.FirstPool);
        Assert.Equal("58oQChx4yWmvKdwLLZzBi4ChoCc2fqCUWBkwMihLYQo2", sol.FirstPool!.Id);

        Assert.NotNull(sol.Stats5m);
        Assert.Equal(44472, sol.Stats5m!.NumBuys);
        Assert.NotNull(sol.Stats24h);
        Assert.Equal(935842, sol.Stats24h!.NumTraders);
    }

    [Fact]
    public void TokenInfo_NullableFieldsHandled()
    {
        var tokens = JsonDefaults.Deserialize<List<TokenInfo>>(Fixtures.Read("tokens_search.json"));

        var sol = Assert.Single(tokens, t => t.Symbol == "SOL");
        Assert.Null(sol.Twitter);
        Assert.Null(sol.Website);
        Assert.Null(sol.Dev);
        Assert.NotNull(sol.Audit);
        Assert.Null(sol.Audit!.DevBalancePercentage);

        var usdc = Assert.Single(tokens, t => t.Symbol == "USDC");
        Assert.Equal(6, usdc.Decimals);
        Assert.NotNull(usdc.MintAuthority);
        Assert.NotNull(usdc.FreezeAuthority);
        Assert.NotNull(usdc.Website);
    }

    [Fact]
    public async Task SearchTokens_BuildsUrlAndParsesArray()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("tokens_search.json"));
        using var client = ClientWith(handler);

        var tokens = await client.SearchTokensAsync("SOL");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(TokensApiUrl + "/search?query=SOL", request.RequestUri!.ToString());
        Assert.Equal(3, tokens.Count);
    }

    [Fact]
    public async Task SearchTokens_EmptyQueryThrows()
    {
        var handler = new FakeHttpHandler();
        using var client = ClientWith(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.SearchTokensAsync("  "));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetTokensByTag_BuildsUrl()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, "[]");
        using var client = ClientWith(handler);

        await client.GetTokensByTagAsync(TokenTag.Verified);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(TokensApiUrl + "/tag?query=verified", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetTopTokens_BuildsUrlWithCategoryIntervalAndLimit()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, "[]");
        using var client = ClientWith(handler);

        await client.GetTopTokensAsync(TokenCategory.TopOrganicScore, TokenInterval.FiveMinutes, 100);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(TokensApiUrl + "/toporganicscore/5m?limit=100", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetTopTokens_OmitsLimitWhenNotSet()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, "[]");
        using var client = ClientWith(handler);

        await client.GetTopTokensAsync(TokenCategory.TopTraded, TokenInterval.TwentyFourHours);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(TokensApiUrl + "/toptraded/24h", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetTopTokens_LimitOutOfRangeThrows()
    {
        var handler = new FakeHttpHandler();
        using var client = ClientWith(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetTopTokensAsync(TokenCategory.TopTraded, TokenInterval.OneHour, 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetTopTokensAsync(TokenCategory.TopTraded, TokenInterval.OneHour, 101));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetRecentTokens_BuildsUrl()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, "[]");
        using var client = ClientWith(handler);

        await client.GetRecentTokensAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(TokensApiUrl + "/recent", request.RequestUri!.ToString());
    }
}
