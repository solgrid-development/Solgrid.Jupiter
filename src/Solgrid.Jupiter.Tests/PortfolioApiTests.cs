using System.Net;
using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class PortfolioApiTests
{
    private const string PortfolioApiUrl = "https://unit.test/portfolio/v1";
    private const string Wallet = "BQ72nSv9f3PRyRKCBnHLVrerrv37CYTHm5h3s9VSGQDV";

    private static JupiterSwapClientOptions NoThrottleOptions() => new()
    {
        BaseUrl = "https://unit.test/swap/v2",
        PortfolioApiUrl = PortfolioApiUrl,
        MinRequestInterval = TimeSpan.Zero
    };

    private static JupiterSwapClient ClientWith(FakeHttpHandler handler) =>
        new(NoThrottleOptions(), new HttpClient(handler));

    [Fact]
    public void PortfolioResponse_MapsPositions()
    {
        var portfolio = JsonDefaults.Deserialize<PortfolioResponse>(Fixtures.Read("portfolio_positions.json"));

        Assert.Equal(Wallet, portfolio.Owner);
        Assert.Equal(1710000000000, portfolio.Date);
        Assert.Equal(450, portfolio.Duration);

        Assert.NotNull(portfolio.FetcherReports);
        Assert.Equal(2, portfolio.FetcherReports!.Count);
        Assert.Equal("succeeded", portfolio.FetcherReports[0].Status);
        Assert.Equal("timeout", portfolio.FetcherReports[1].Error);

        Assert.NotNull(portfolio.Elements);
        Assert.Equal(2, portfolio.Elements!.Count);

        var wallet = portfolio.Elements[0];
        Assert.Equal("multiple", wallet.Type);
        Assert.Equal("Wallet", wallet.Label);
        Assert.Equal("native-stake", wallet.PlatformId);
        Assert.Equal(1713.4, wallet.Value);
        Assert.NotNull(wallet.Data);
        Assert.True(wallet.Data!.Value.TryGetProperty("assets", out _));

        var order = portfolio.Elements[1];
        Assert.Equal("trade", order.Type);
        Assert.Equal("LimitOrder", order.Label);

        Assert.NotNull(portfolio.TokenInfo);
        var sol = portfolio.TokenInfo!["solana"]["So11111111111111111111111111111111111111112"];
        Assert.Equal("SOL", sol.Symbol);
        Assert.Equal(9, sol.Decimals);
        Assert.Equal("https://example.com/sol.png", sol.LogoUri);
        Assert.NotNull(sol.Tags);
        Assert.Contains("verified", sol.Tags);
    }

    [Fact]
    public async Task GetPortfolio_BuildsUrl()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("portfolio_positions.json"));
        using var client = ClientWith(handler);

        var portfolio = await client.GetPortfolioAsync(Wallet);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(PortfolioApiUrl + "/positions/" + Wallet, request.RequestUri!.ToString());
        Assert.Equal(Wallet, portfolio.Owner);
    }

    [Fact]
    public async Task GetPortfolio_WithPlatforms_BuildsQuery()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("portfolio_positions.json"));
        using var client = ClientWith(handler);

        await client.GetPortfolioAsync(Wallet, new[] { "jupiter-exchange", "jupiter-governance" });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            PortfolioApiUrl + "/positions/" + Wallet + "?platforms=jupiter-exchange%2Cjupiter-governance",
            request.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetPortfolio_EmptyAddressThrows()
    {
        var handler = new FakeHttpHandler();
        using var client = ClientWith(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetPortfolioAsync("  "));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetPlatforms_BuildsUrlAndParses()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("portfolio_platforms.json"));
        using var client = ClientWith(handler);

        var platforms = await client.GetPlatformsAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(PortfolioApiUrl + "/platforms", request.RequestUri!.ToString());

        Assert.Equal(8, platforms.Count);

        var jup = platforms[0];
        Assert.Equal("jupiter-exchange", jup.Id);
        Assert.Equal("Jupiter", jup.Name);
        Assert.Equal("jupiter", jup.DefiLlamaId);
        Assert.False(jup.IsDeprecated);
        Assert.NotNull(jup.Tokens);
        Assert.Equal(3, jup.Tokens!.Count);
        Assert.NotNull(jup.Tags);
        Assert.Contains("trading", jup.Tags);
        Assert.NotNull(jup.Links);
        Assert.Equal("https://jup.ag/", jup.Links!.Website);
        Assert.Null(jup.Links.Telegram);

        var dao = platforms[1];
        Assert.Equal("jupiter-governance", dao.Id);
        Assert.Null(dao.DefiLlamaId);
        Assert.Null(dao.Tokens);
    }

    [Fact]
    public async Task GetStakedJup_BuildsUrlAndParses()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("portfolio_staked_jup.json"));
        using var client = ClientWith(handler);

        var staked = await client.GetStakedJupAsync(Wallet);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(PortfolioApiUrl + "/staked-jup/" + Wallet, request.RequestUri!.ToString());

        Assert.Equal(15000.5, staked.StakedAmount);
        var entry = Assert.Single(staked.Unstaking!);
        Assert.Equal(500.0, entry.Amount);
        Assert.Equal(1711000000000, entry.Until);
    }
}
