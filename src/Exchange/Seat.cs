using Optional;

namespace Exchange;

public sealed class Seat<T>
{
    private readonly TaskCompletionSource<T> source = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void SetToken(CancellationToken token)
    {
        token.Register(() => source.TrySetCanceled(token));
    }

    public TakeSeatResult Take(T value)
    {
        var set = source.TrySetResult(value);
        if (set)
        {
            return TakeSeatResult.Set;
        }

        return source.Task.Status == TaskStatus.RanToCompletion
            ? TakeSeatResult.AlreadySet
            : TakeSeatResult.Cancelled;
    }

    public Task<Option<T>> WaitAsync()
    {
        return source.Task.NoneOnCancellationAsync();
    }
}