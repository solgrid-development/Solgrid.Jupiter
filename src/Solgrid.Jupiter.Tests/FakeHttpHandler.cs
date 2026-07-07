using System.Net;
using System.Text;

namespace Solgrid.Jupiter.Tests;

internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string?> RequestBodies { get; } = [];

    public void Enqueue(HttpStatusCode status, string body, IDictionary<string, string>? headers = null)
    {
        _responses.Enqueue(_ =>
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            if (headers is not null)
            {
                foreach (var header in headers)
                    response.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return response;
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken));

        if (_responses.Count == 0)
            throw new InvalidOperationException("No response enqueued for request " + request.RequestUri);

        return _responses.Dequeue()(request);
    }
}
