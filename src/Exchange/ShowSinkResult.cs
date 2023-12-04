using Variety;

namespace Exchange;

[Vary]
public abstract partial record ShowSinkResult
{
    public sealed partial record NoMatch;

    public sealed partial record Conflict;

    public sealed partial record Match(Transfer.Source Source);
}