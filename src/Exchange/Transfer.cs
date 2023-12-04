using System.Collections.Immutable;
using Optional.Unsafe;

namespace Exchange;

public sealed class Transfer
{
    public sealed class Collection
    {
        private readonly object @lock = new();

        private sealed class Entry
        {
            public Entry(Transfer transfer)
            {
                Transfer = transfer;
            }

            public Transfer Transfer { get; }
            public bool Outdated { get; set; }
        }

        private ImmutableDictionary<string, Entry> index =
            ImmutableDictionary<string, Entry>.Empty.WithComparers(StringComparer.Ordinal);

        public IDisposable Get(string name, out Transfer transfer)
        {
            lock (@lock)
            {
                if (index.TryGetValue(name, out var value) && !value.Outdated)
                {
                    transfer = value.Transfer;
                    return new Cleanup(transfer, this);
                }

                transfer = new Transfer(name);
                var entry = new Entry(transfer);
                var cleanup = new Cleanup(transfer, this);
                index = index.Add(transfer.Name, entry);
                return cleanup;
            }
        }

        private void Clean(Transfer transfer)
        {
            lock (@lock)
            {
                if (index.TryGetValue(transfer.Name, out var current))
                {
                    if (current.Outdated || ReferenceEquals(current.Transfer, transfer))
                    {
                        current.Outdated = true;
                        index = index.Remove(transfer.Name);
                    }
                }
            }
        }

        private sealed class Cleanup : IDisposable
        {
            private readonly Collection collection;
            private readonly Transfer transfer;

            public Cleanup(Transfer transfer, Collection collection)
            {
                this.transfer = transfer;
                this.collection = collection;
            }

            public void Dispose()
            {
                collection.Clean(transfer);
            }
        }
    }

    private readonly Seat<object> sink = new();
    private readonly Seat<Source> source = new();

    public Transfer(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public async Task<ShowSourceResult> ShowSourceAsync(HttpContext context, CancellationToken token)
    {
        var newSource = new Source(context);
        var setResult = source.Take(newSource);
        switch (setResult)
        {
            case TakeSeatResult.Set:
            {
                sink.SetToken(token);
                var sinkResult = await sink.WaitAsync().ConfigureAwait(false);
                if (sinkResult.HasValue)
                {
                    return await newSource.WaitCopyAsync().ConfigureAwait(false);
                }

                return ShowSourceResult.NoMatch;
            }
            case TakeSeatResult.Cancelled:
                return ShowSourceResult.NoMatch;
            case TakeSeatResult.AlreadySet:
                return ShowSourceResult.Conflict;
            default:
                throw new Exception();
        }
    }

    public async Task<ShowSinkResult> ShowSinkAsync(CancellationToken token)
    {
        var newSink = new object();
        var setResult = sink.Take(newSink);
        switch (setResult)
        {
            case TakeSeatResult.Set:
            {
                source.SetToken(token);
                var sourceResult = await source.WaitAsync().ConfigureAwait(false);
                if (sourceResult.HasValue)
                {
                    return new ShowSinkResult.Match(sourceResult.ValueOrFailure());
                }

                return new ShowSinkResult.NoMatch();
            }
            case TakeSeatResult.Cancelled:
                return new ShowSinkResult.NoMatch();
            case TakeSeatResult.AlreadySet:
                return new ShowSinkResult.Conflict();
            default:
                throw new Exception();
        }
    }

    public sealed class Source
    {
        private readonly HttpContext context;
        private readonly TaskCompletionSource<ShowSourceResult> taskSource = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        public Source(HttpContext context)
        {
            this.context = context;
        }

        public Task<ShowSourceResult> WaitCopyAsync() => taskSource.Task;

        public async Task CopyAsync(Stream target)
        {
            try
            {
                await context.Request.Body.CopyToAsync(target);
                taskSource.TrySetResult(ShowSourceResult.Transferred);
            }
            catch
            {
                taskSource.TrySetResult(ShowSourceResult.TransferError);
                throw;
            }
        }

        public string? ContentType => context.Request.ContentType;
    }
}