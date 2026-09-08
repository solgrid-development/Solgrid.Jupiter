using System.Net;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class TriggerDcaTests
{
    private const string BaseUrl = "https://unit.test/trigger/v2";
    private const string Wallet = "BQ72nSv9f3PRyRKCBnHLVrerrv37CYTHm5h3s9VSGQDV";
    private const string Sol = "So11111111111111111111111111111111111111112";
    private const string Usdc = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
    private const string OrderId = "019f0530-36a7-77da-a38f-ed9414bb996b";

    private static JupiterTriggerClientOptions AuthedOptions() => new()
    {
        BaseUrl = BaseUrl,
        AuthToken = "test.jwt.token",
        MinRequestInterval = TimeSpan.Zero
    };

    private static CreateDcaOrderRequest TimeBasedOrder() => new()
    {
        DepositRequestId = "req-1",
        DepositSignedTx = "AQEN",
        UserPubkey = Wallet,
        InputMint = Usdc,
        OutputMint = Sol,
        InputAmount = "20000000",
        OrderCount = 2,
        IntervalSeconds = 3600,
        OrderType = DcaOrderType.TimeBased
    };

    [Fact]
    public async Task Create_SerializesTimeBasedBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_dca_created.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        await client.CreateDcaOrderAsync(TimeBasedOrder());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(BaseUrl + "/orders/dca", request.RequestUri!.ToString());
        Assert.Equal(
            "{\"depositRequestId\":\"req-1\",\"depositSignedTx\":\"AQEN\"," +
            "\"userPubkey\":\"" + Wallet + "\",\"inputMint\":\"" + Usdc + "\",\"outputMint\":\"" + Sol + "\"," +
            "\"inputAmount\":\"20000000\",\"orderCount\":2,\"intervalSeconds\":3600," +
            "\"orderType\":\"time_based\"}",
            handler.RequestBodies.Single());
    }

    [Fact]
    public async Task Create_OmitsOrderType_WhenNull()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_dca_created.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var request = TimeBasedOrder();
        request.OrderType = null;
        await client.CreateDcaOrderAsync(request);

        Assert.DoesNotContain("orderType", handler.RequestBodies.Single()!);
    }

    [Fact]
    public async Task Create_SerializesPriceConditionalBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_dca_created.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        await client.CreateDcaOrderAsync(new CreateDcaOrderRequest
        {
            DepositRequestId = "req-1",
            DepositSignedTx = "AQEN",
            UserPubkey = Wallet,
            InputMint = Usdc,
            OutputMint = Sol,
            InputAmount = "40000000",
            OrderCount = 4,
            IntervalSeconds = 86400,
            OrderType = DcaOrderType.PriceConditional,
            TriggerMint = Sol,
            MinPriceUsd = 120,
            MaxPriceUsd = 180
        });

        var body = handler.RequestBodies.Single()!;
        Assert.Contains("\"orderType\":\"price_conditional\"", body);
        Assert.Contains("\"triggerMint\":\"" + Sol + "\"", body);
        Assert.Contains("\"minPriceUsd\":120", body);
        Assert.Contains("\"maxPriceUsd\":180", body);
    }

    [Fact]
    public async Task Create_ParsesResponse()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_dca_created.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var response = await client.CreateDcaOrderAsync(TimeBasedOrder());

        Assert.Equal(OrderId, response.Id);
        Assert.False(string.IsNullOrEmpty(response.TxSignature));
    }

    [Fact]
    public async Task Create_RejectsOrderCountBelowTwo()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.CreateDcaOrderAsync(new CreateDcaOrderRequest
        {
            DepositRequestId = "req-1",
            DepositSignedTx = "AQEN",
            UserPubkey = Wallet,
            InputMint = Usdc,
            OutputMint = Sol,
            InputAmount = "20000000",
            OrderCount = 1,
            IntervalSeconds = 3600
        }));
    }

    [Fact]
    public async Task Create_RejectsIntervalOutOfRange()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.CreateDcaOrderAsync(new CreateDcaOrderRequest
        {
            DepositRequestId = "req-1",
            DepositSignedTx = "AQEN",
            UserPubkey = Wallet,
            InputMint = Usdc,
            OutputMint = Sol,
            InputAmount = "20000000",
            OrderCount = 2,
            IntervalSeconds = 30
        }));
    }

    [Fact]
    public async Task Create_RejectsSameMints()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateDcaOrderAsync(new CreateDcaOrderRequest
        {
            DepositRequestId = "req-1",
            DepositSignedTx = "AQEN",
            UserPubkey = Wallet,
            InputMint = Usdc,
            OutputMint = Usdc,
            InputAmount = "20000000",
            OrderCount = 2,
            IntervalSeconds = 3600
        }));
    }

    [Fact]
    public async Task Create_PriceConditionalRequiresTriggerMint()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        var request = TimeBasedOrder();
        request.OrderType = DcaOrderType.PriceConditional;
        request.MinPriceUsd = 120;

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateDcaOrderAsync(request));
    }

    [Fact]
    public async Task Create_PriceConditionalRequiresBand()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        var request = TimeBasedOrder();
        request.OrderType = DcaOrderType.PriceConditional;
        request.TriggerMint = Sol;

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateDcaOrderAsync(request));
    }

    [Fact]
    public async Task Create_JlEnabledRejectsPriceConditional()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        var request = TimeBasedOrder();
        request.OrderType = DcaOrderType.PriceConditional;
        request.TriggerMint = Sol;
        request.MinPriceUsd = 120;
        request.JlEnabled = true;
        request.JlMint = "9BEcn9aPEmhSPbPQeFGjidRiEKki46fVQDyPpSQXPA2D";

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateDcaOrderAsync(request));
    }

    [Fact]
    public async Task Create_JlEnabledRequiresJlMint()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        var request = TimeBasedOrder();
        request.JlEnabled = true;

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateDcaOrderAsync(request));
    }

    [Fact]
    public async Task Create_Throws_WithoutAuthToken()
    {
        using var client = new JupiterTriggerClient(
            new JupiterTriggerClientOptions { BaseUrl = BaseUrl, MinRequestInterval = TimeSpan.Zero },
            new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateDcaOrderAsync(TimeBasedOrder()));
    }

    [Fact]
    public async Task Cancel_BuildsUrlAndParsesRefund()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_dca_cancel.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var response = await client.CancelDcaOrderAsync(OrderId);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(BaseUrl + "/orders/dca/cancel/" + OrderId, request.RequestUri!.ToString());
        Assert.Null(handler.RequestBodies.Single());
        Assert.Equal(OrderId, response.Id);
        Assert.Equal(2, response.RoundsRemaining);
        Assert.Equal("20000000", response.RefundAmount);
        Assert.Equal("368645e8-7ac6-4c88-8b10-3a68812bdc1d", response.RequestId);
        Assert.False(string.IsNullOrEmpty(response.Transaction));
    }

    [Fact]
    public async Task ConfirmCancel_SerializesBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_dca_created.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        await client.ConfirmCancelDcaOrderAsync(OrderId, new ConfirmCancelRequest
        {
            SignedTransaction = "AQEN",
            CancelRequestId = "cancel-1"
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(BaseUrl + "/orders/dca/confirm-cancel/" + OrderId, request.RequestUri!.ToString());
        Assert.Equal("{\"signedTransaction\":\"AQEN\",\"cancelRequestId\":\"cancel-1\"}", handler.RequestBodies.Single());
    }

    [Fact]
    public async Task History_BuildsExpectedUrl()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_dca_history.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        await client.GetDcaHistoryAsync(new DcaHistoryQuery
        {
            State = TriggerHistoryState.Active,
            Limit = 10,
            Sort = DcaHistorySort.NextFillAt,
            Direction = TriggerSortDirection.Asc
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(BaseUrl + "/orders/history/dca?state=active&limit=10&sort=next_fill_at&dir=asc", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task History_ParsesResponse()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_dca_history.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var response = await client.GetDcaHistoryAsync();

        var order = Assert.Single(response.Orders!);
        Assert.Equal(OrderId, order.Id);
        Assert.Equal("price_conditional", order.OrderType);
        Assert.Equal(50, order.MinPriceUsd);
        Assert.Equal(1000, order.MaxPriceUsd);
        Assert.Equal(Sol, order.TriggerMint);
        Assert.Equal("20000000", order.InputAmountInitial);
        Assert.Equal("10000000", order.AmountPerRound);
        Assert.Equal(0, order.RoundsFilled);
        Assert.Equal("active", order.State);
        Assert.Equal("active", order.DisplayState);
        Assert.Equal("2026-06-26T18:27:53.665Z", order.CreatedAt);
        Assert.False(order.JlEnabled);

        var evt = Assert.Single(order.Events!);
        Assert.Equal("deposit", evt.Type);
        Assert.Equal("2026-06-26T18:27:54.529Z", evt.Timestamp);
        Assert.Equal("20000000", evt.InputAmount);

        Assert.Equal(1, response.Pagination!.Total);
    }

    [Fact]
    public async Task GetDcaOrder_BuildsUrlAndParsesFills()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_dca_order.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var order = await client.GetDcaOrderAsync("019f053d-3f0c-70ad-bb5e-76d633291f80");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(BaseUrl + "/orders/history/dca/019f053d-3f0c-70ad-bb5e-76d633291f80", request.RequestUri!.ToString());
        Assert.Equal("time_based", order.OrderType);
        Assert.Equal(1, order.RoundsFilled);
        Assert.Equal(0.5, order.FillPercent);
        Assert.Equal("10000000", order.InputAmountRemaining);
        Assert.Equal("137589159", order.OutputAmountTotal);
        Assert.Equal(1800, order.RetryWindowSeconds);

        Assert.Equal(2, order.Events!.Count);
        Assert.Equal("fill", order.Events[0].Type);
        Assert.Equal(1, order.Events[0].RoundNumber);
        Assert.Equal("137589159", order.Events[0].OutputAmount);
        Assert.Equal("deposit", order.Events[1].Type);
    }

    [Fact]
    public async Task History_RejectsLimitOutOfRange()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            client.GetDcaHistoryAsync(new DcaHistoryQuery { Limit = 0 }));
    }
}
