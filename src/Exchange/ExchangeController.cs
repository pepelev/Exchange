using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Exchange;

[ApiController]
[Route("contents")]
public class ExchangeController : Controller
{
    [JsonConverter(typeof(JsonStringEnumConverter<PostResult>))]
    public enum PostResult
    {
        Conflict,
        NoMatch,
        Transferred,
        TransferError
    }

    private static readonly Transfer.Collection transfers = new();

    [Route("{name}")]
    [HttpGet]
    public async Task<IResult> Get(string name, ushort timeout = 30)
    {
        using (transfers.Get(name, out var transfer))
        using (HttpContext.RequestAborted.LinkTimeout(TimeSpan.FromSeconds(timeout), out var token))
        {
            var result = await transfer.ShowSinkAsync(token).ConfigureAwait(false);
            return result.Accept(new ResultVisitor());
        }
    }

    [Route("{name}")]
    [HttpPost]
    public async Task<PostResult> Post(string name, ushort timeout = 30)
    {
        using (transfers.Get(name, out var transfer))
        using (HttpContext.RequestAborted.LinkTimeout(TimeSpan.FromSeconds(timeout), out var token))
        {
            var result = await transfer.ShowSourceAsync(HttpContext, token).ConfigureAwait(false);
            return result switch
            {
                ShowSourceResult.Conflict => PostResult.Conflict,
                ShowSourceResult.NoMatch => PostResult.NoMatch,
                ShowSourceResult.Transferred => PostResult.Transferred,
                ShowSourceResult.TransferError => PostResult.TransferError,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    private sealed class ResultVisitor : ShowSinkResult.Visitor<IResult>
    {
        public override IResult Visit(ShowSinkResult.NoMatch noMatch) => Results.NotFound();
        public override IResult Visit(ShowSinkResult.Conflict conflict) => Results.Conflict();

        public override IResult Visit(ShowSinkResult.Match match) =>
            Results.Stream(match.Source.CopyAsync, match.Source.ContentType);
    }
}