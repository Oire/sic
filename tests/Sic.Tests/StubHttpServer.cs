using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Oire.Sic.Tests;

/// <summary>
/// A minimal HTTP/1.1 server on the loopback address, for tests that download images. Each path
/// is mapped to a canned response; any other path gets a 404. Every connection is closed after
/// one response. Responses can claim a <c>Content-Length</c> they never deliver and then stall,
/// which is how tests exercise cancellation and the download size limit without sending
/// megabytes.
/// </summary>
internal sealed class StubHttpServer: IDisposable {
    private static readonly StubResponse NotFound = new(404, "text/plain", Encoding.ASCII.GetBytes("Not found"), null, false);

    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stopping = new();
    private readonly ConcurrentDictionary<string, StubResponse> _responses = new(StringComparer.Ordinal);
    private readonly ConcurrentBag<Task> _handlers = [];
    private readonly Task _acceptLoop;

    public StubHttpServer() {
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        BaseUrl = $"http://127.0.0.1:{port}";
        _acceptLoop = AcceptLoopAsync(_stopping.Token);
    }

    /// <summary>The server's root, such as <c>http://127.0.0.1:50123</c>, without a trailing slash.</summary>
    public string BaseUrl { get; }

    /// <summary>Serves <paramref name="body"/> with status 200 at <paramref name="path"/> and returns its full URL.</summary>
    public string Serve(string path, byte[] body, string contentType) {
        return Map(path, new StubResponse(200, contentType, body, null, false));
    }

    /// <summary>
    /// Sends the headers of a 200 response claiming <paramref name="declaredLength"/> bytes, then
    /// <paramref name="body"/>, then nothing more until the server is disposed. Returns the full URL.
    /// </summary>
    public string ServeStalled(string path, long declaredLength, byte[] body) {
        return Map(path, new StubResponse(200, "application/octet-stream", body, declaredLength, true));
    }

    /// <summary>The full URL of <paramref name="path"/>, whether or not anything is served there.</summary>
    public string Url(string path) => BaseUrl + path;

    private string Map(string path, StubResponse response) {
        _responses[path] = response;
        return Url(path);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken) {
        try {
            while (true) {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _handlers.Add(HandleAsync(client, cancellationToken));
            }
        } catch (OperationCanceledException) {
            // Disposed.
        } catch (ObjectDisposedException) {
            // Same as above.
        } catch (SocketException) {
            // Same as above.
        }
    }

    private async Task HandleAsync(TcpClient client, CancellationToken cancellationToken) {
        using (client) {
            try {
                var stream = client.GetStream();
                var path = await ReadRequestPathAsync(stream, cancellationToken).ConfigureAwait(false);
                var response = path is not null && _responses.TryGetValue(path, out var mapped) ? mapped : NotFound;
                var reason = response.Status == 200 ? "OK" : "Not Found";
                var header = $"HTTP/1.1 {response.Status} {reason}\r\n"
                    + $"Content-Type: {response.ContentType}\r\n"
                    + $"Content-Length: {response.DeclaredLength ?? response.Body.Length}\r\n"
                    + "Connection: close\r\n\r\n";

                await stream.WriteAsync(Encoding.ASCII.GetBytes(header), cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(response.Body, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                if (response.Stall) {
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                }
            } catch (OperationCanceledException) {
                // Disposed while stalling or reading.
            } catch (IOException) {
                // The client hung up, which tests of cancellation and size limits do on purpose.
            } catch (SocketException) {
                // Same as above.
            }
        }
    }

    private static async Task<string?> ReadRequestPathAsync(NetworkStream stream, CancellationToken cancellationToken) {
        var received = new List<byte>();
        var buffer = new byte[4096];

        while (!EndsHeaders(received)) {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) {
                return null;
            }

            received.AddRange(buffer.AsSpan(0, read));
        }

        var requestLine = Encoding.ASCII.GetString([.. received]).Split("\r\n")[0];
        var parts = requestLine.Split(' ');
        if (parts.Length < 2) {
            return null;
        }

        var target = parts[1];
        var query = target.IndexOf('?', StringComparison.Ordinal);
        return query >= 0 ? target[..query] : target;
    }

    private static bool EndsHeaders(List<byte> received) {
        var count = received.Count;
        return count >= 4
            && received[count - 4] == '\r' && received[count - 3] == '\n'
            && received[count - 2] == '\r' && received[count - 1] == '\n';
    }

    public void Dispose() {
        _stopping.Cancel();
        _listener.Stop();

        try {
            Task.WaitAll([_acceptLoop, .. _handlers], TimeSpan.FromSeconds(5));
        } catch (AggregateException) {
            // Every handler swallows its own expected failures; nothing else should surface here.
        }

        _listener.Dispose();
        _stopping.Dispose();
    }

    private sealed record StubResponse(int Status, string ContentType, byte[] Body, long? DeclaredLength, bool Stall);
}
