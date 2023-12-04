using Optional;

namespace Exchange;

public static class TaskExtensions
{
    public static IDisposable LinkTimeout(
        this CancellationToken token,
        TimeSpan timeout,
        out CancellationToken newToken)
    {
        var timeoutSource = new CancellationTokenSource(timeout);
        var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutSource.Token);
        newToken = linkedSource.Token;
        return new CompositeDisposable(timeoutSource, linkedSource);
    }

    public static async Task<Option<T>> NoneOnCancellationAsync<T>(this Task<T> task)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            return result.Some();
        }
        catch (OperationCanceledException)
        {
            return Option.None<T>();
        }
    }

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly CancellationTokenSource a;
        private readonly CancellationTokenSource b;

        public CompositeDisposable(CancellationTokenSource a, CancellationTokenSource b)
        {
            this.a = a;
            this.b = b;
        }

        public void Dispose()
        {
            b.Dispose();
            a.Dispose();
        }
    }
}