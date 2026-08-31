using CurrencyWatchlist.Application.Dtos.Rates;
using CurrencyWatchlist.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyWatchlist.Api.Controllers;

/// <summary>Fetch and inspect exchange rates.</summary>
[ApiController]
[Route("api/rates")]
[Produces("application/json")]
public class RatesController : ControllerBase
{
    private readonly IRateService _rateService;

    public RatesController(IRateService rateService)
    {
        _rateService = rateService;
    }

    /// <summary>
    /// Fetch the latest rates from the external provider and store a snapshot for every affected pair.
    /// Scoped to a single watchlist's pairs when <paramref name="watchlistId"/> is provided; otherwise refreshes every distinct pair across all watchlists.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshRatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RefreshRatesResponse>> Refresh([FromQuery] int? watchlistId, CancellationToken cancellationToken) =>
        Ok(await _rateService.RefreshAsync(watchlistId, cancellationToken));

    /// <summary>Get the most recently stored rate snapshot for a currency pair.</summary>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(RateSnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RateSnapshotResponse>> GetLatest(
        [FromQuery] string @base, [FromQuery] string quote, CancellationToken cancellationToken) =>
        Ok(await _rateService.GetLatestAsync(@base, quote, cancellationToken));

    /// <summary>Get stored rate snapshots for a currency pair within a date range.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<RateSnapshotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RateSnapshotResponse>>> GetHistory(
        [FromQuery] string @base, [FromQuery] string quote, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken) =>
        Ok(await _rateService.GetHistoryAsync(@base, quote, from, to, cancellationToken));
}
