using CurrencyWatchlist.Application.Dtos.Watchlists;
using CurrencyWatchlist.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyWatchlist.Api.Controllers;

/// <summary>Manage currency watchlists.</summary>
[ApiController]
[Route("api/watchlists")]
[Produces("application/json")]
public class WatchlistsController : ControllerBase
{
    private readonly IWatchlistService _watchlistService;

    public WatchlistsController(IWatchlistService watchlistService)
    {
        _watchlistService = watchlistService;
    }

    /// <summary>Create a new watchlist.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WatchlistResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WatchlistResponse>> Create([FromBody] CreateWatchlistRequest request, CancellationToken cancellationToken)
    {
        var result = await _watchlistService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>List all watchlists.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WatchlistResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WatchlistResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _watchlistService.GetAllAsync(cancellationToken));

    /// <summary>Get a single watchlist, including its items and their latest known rate.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WatchlistDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchlistDetailResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _watchlistService.GetByIdAsync(id, cancellationToken));

    /// <summary>Delete a watchlist and all of its items/alert rules.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _watchlistService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
