using System.Net;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class TriggerPriceOrderTests
{
    private const string BaseUrl = "https://unit.test/trigger/v2";
    private const string Wallet = "BQ72nSv9f3PRyRKCBnHLVrerrv37CYTHm5h3s9VSGQDV";
    private const string Sol = "So11111111111111111111111111111111111111112";
    private const string Usdc = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
    private const string OrderId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

    private static JupiterTriggerClientOptions AuthedOptions() => new()
    {
        BaseUrl = BaseUrl,
        AuthToken = "test.jwt.token",
        MinRequestInterval = TimeSpan.Zero
    };

    private static CreatePriceOrderRequest SingleOrder() => new()
    {
        OrderType = TriggerOrderType.Single,
        DepositRequestId = "req-1",
        DepositSignedTx = "AQEN",
        UserPubkey = Wallet,
        InputMint = Sol,
        InputAmount = "1000000000",
        OutputMint = Usdc,
        TriggerMint = Sol,
        TriggerCondition = TriggerCondition.Above,
        TriggerPriceUsd = 200,
        SlippageBps = 100,
        ExpiresAt = 1735689600000
    };

    [Fact]
    public async Task Create_SerializesSingleBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_order_created.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        await client.CreatePriceOrderAsync(SingleOrder());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(BaseUrl + "/orders/price", request.RequestUri!.ToString());
        Assert.Equal(
            "{\"orderType\":\"single\",\"depositRequestId\":\"req-1\",\"depositSignedTx\":\"AQEN\"," +
            "\"userPubkey\":\"" + Wallet + "\",\"inputMint\":\"" + Sol + "\",\"inputAmount\":\"1000000000\"," +
            "\"outputMint\":\"" + Usdc + "\",\"triggerMint\":\"" + Sol + "\"," +
            "\"triggerCondition\":\"above\",\"triggerPriceUsd\":200,\"slippageBps\":100," +
            "\"expiresAt\":1735689600000}",
            handler.RequestBodies.Single());
    }

    [Fact]
    public async Task Create_SerializesOcoBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_order_created.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        await client.CreatePriceOrderAsync(new CreatePriceOrderRequest
        {
            OrderType = TriggerOrderType.Oco,
            DepositRequestId = "req-1",
            DepositSignedTx = "AQEN",
            UserPubkey = Wallet,
            InputMint = Sol,
            InputAmount = "1000000000",
            OutputMint = Usdc,
            TriggerMint = Sol,
            TpPriceUsd = 250,
            SlPriceUsd = 150,
            TpSlippageBps = 100,
            SlSlippageBps = 100,
            ExpiresAt = 1735689600000
        });

        var body = handler.RequestBodies.Single()!;
        Assert.Contains("\"orderType\":\"oco\"", body);
        Assert.Contains("\"tpPriceUsd\":250", body);
        Assert.Contains("\"slPriceUsd\":150", body);
        Assert.DoesNotContain("triggerCondition", body);
        Assert.DoesNotContain("\"slippageBps\"", body);
    }

    [Fact]
    public async Task Create_ParsesResponse()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_order_created.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var response = await client.CreatePriceOrderAsync(SingleOrder());

        Assert.Equal(OrderId, response.Id);
        Assert.True(response.DepositConfirmed);
        Assert.False(string.IsNullOrEmpty(response.TxSignature));
    }

    [Fact]
    public async Task Create_SingleRejectsPriceAndTrailingTogether()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        var request = SingleOrder();
        request.TrailingBps = 1000;

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreatePriceOrderAsync(request));
    }

    [Fact]
    public async Task Create_SingleRejectsNeitherPriceNorTrailing()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        var request = SingleOrder();
        request.TriggerPriceUsd = null;

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreatePriceOrderAsync(request));
    }

    [Fact]
    public async Task Create_SingleRequiresCondition()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        var request = SingleOrder();
        request.TriggerCondition = null;

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreatePriceOrderAsync(request));
    }

    [Fact]
    public async Task Create_RejectsTrailingBpsOutOfRange()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        var request = SingleOrder();
        request.TriggerPriceUsd = null;
        request.TrailingBps = 40;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.CreatePriceOrderAsync(request));
    }

    [Fact]
    public async Task Create_OcoRequiresTpAboveSl()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreatePriceOrderAsync(new CreatePriceOrderRequest
        {
            OrderType = TriggerOrderType.Oco,
            DepositRequestId = "req-1",
            DepositSignedTx = "AQEN",
            UserPubkey = Wallet,
            InputMint = Sol,
            InputAmount = "1000000000",
            OutputMint = Usdc,
            TriggerMint = Sol,
            TpPriceUsd = 150,
            SlPriceUsd = 250,
            ExpiresAt = 1735689600000
        }));
    }

    [Fact]
    public async Task Create_OtocoRequiresParentTrigger()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreatePriceOrderAsync(new CreatePriceOrderRequest
        {
            OrderType = TriggerOrderType.Otoco,
            DepositRequestId = "req-1",
            DepositSignedTx = "AQEN",
            UserPubkey = Wallet,
            InputMint = Usdc,
            InputAmount = "200000000",
            OutputMint = Sol,
            TriggerMint = Sol,
            TpPriceUsd = 220,
            SlPriceUsd = 160,
            ExpiresAt = 1735689600000
        }));
    }

    [Fact]
    public async Task Create_Throws_WithoutAuthToken()
    {
        using var client = new JupiterTriggerClient(
            new JupiterTriggerClientOptions { BaseUrl = BaseUrl, MinRequestInterval = TimeSpan.Zero },
            new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreatePriceOrderAsync(SingleOrder()));
    }

    [Fact]
    public async Task Update_SendsPatchWithBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_order_updated.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var response = await client.UpdatePriceOrderAsync(OrderId, new UpdatePriceOrderRequest
        {
            OrderType = TriggerOrderType.Single,
            TriggerPriceUsd = 210,
            SlippageBps = 150
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal(BaseUrl + "/orders/price/" + OrderId, request.RequestUri!.ToString());
        Assert.Equal("{\"orderType\":\"single\",\"triggerPriceUsd\":210,\"slippageBps\":150}", handler.RequestBodies.Single());
        Assert.Equal(OrderId, response.Id);
    }

    [Fact]
    public async Task Cancel_BuildsUrlAndParsesWithdrawal()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_cancel_response.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var response = await client.CancelPriceOrderAsync(OrderId);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(BaseUrl + "/orders/price/cancel/" + OrderId, request.RequestUri!.ToString());
        Assert.Null(handler.RequestBodies.Single());
        Assert.Equal(OrderId, response.Id);
        Assert.Equal("368645e8-7ac6-4c88-8b10-3a68812bdc1d", response.RequestId);
        Assert.False(string.IsNullOrEmpty(response.Transaction));
    }

    [Fact]
    public async Task ConfirmCancel_SerializesBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_confirm_cancel.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var response = await client.ConfirmCancelPriceOrderAsync(OrderId, new ConfirmCancelRequest
        {
            SignedTransaction = "AQEN",
            CancelRequestId = "cancel-1"
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(BaseUrl + "/orders/price/confirm-cancel/" + OrderId, request.RequestUri!.ToString());
        Assert.Equal("{\"signedTransaction\":\"AQEN\",\"cancelRequestId\":\"cancel-1\"}", handler.RequestBodies.Single());
        Assert.Equal(OrderId, response.Id);
    }

    [Fact]
    public async Task History_BuildsExpectedUrl()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_history_page.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        await client.GetOrderHistoryAsync(new TriggerHistoryQuery
        {
            State = TriggerHistoryState.Active,
            Mint = Sol,
            Limit = 20,
            Offset = 0,
            Sort = TriggerHistorySort.CreatedAt,
            Direction = TriggerSortDirection.Asc
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            BaseUrl + "/orders/history?state=active&mint=" + Sol + "&limit=20&offset=0&sort=created_at&dir=asc",
            request.RequestUri!.ToString());
    }

    [Fact]
    public async Task History_ParsesResponse()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("trigger_history_page.json"));
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(handler));

        var response = await client.GetOrderHistoryAsync();

        var order = Assert.Single(response.Orders!);
        Assert.Equal(OrderId, order.Id);
        Assert.Equal("single", order.OrderType);
        Assert.Equal("open", order.OrderState);
        Assert.Equal("open", order.RawState);
        Assert.Equal("1000000000", order.RemainingInputAmount);
        Assert.Equal(200.0, order.TriggerPriceUsd);
        Assert.Equal(1704067200000, order.ExpiresAt);
        Assert.Null(order.TriggeredAt);

        var evt = Assert.Single(order.Events!);
        Assert.Equal("deposit", evt.Type);
        Assert.Equal("success", evt.State);
        Assert.Equal("1000000000", evt.Amount);

        Assert.Equal(50, response.Pagination!.Total);
        Assert.Equal(20, response.Pagination.Limit);
    }

    [Fact]
    public async Task History_RejectsLimitOutOfRange()
    {
        using var client = new JupiterTriggerClient(AuthedOptions(), new HttpClient(new FakeHttpHandler()));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            client.GetOrderHistoryAsync(new TriggerHistoryQuery { Limit = 101 }));
    }
}
