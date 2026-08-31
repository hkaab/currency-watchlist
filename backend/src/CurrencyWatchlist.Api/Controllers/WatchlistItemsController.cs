using CurrencyWatchlist.Application.Dtos.Items;
using CurrencyWatchlist.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyWatchlist.Api.Controllers;

/// <summary>Manage currency pairs within a watchlist.</summary>
[ApiController]
[Route("api/watchlists/{watchlistId:int}/items")]
[Produces("application/json")]
public class WatchlistItemsController : ControllerBase
{
    private readonly IWatchlistItemService _itemService;

    public WatchlistItemsController(IWatchlistItemService itemService)
    {
        _itemService = itemService;
    }

    /// <summary>Add a currency pair (e.g. USD -> AUD) to a watchlist.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WatchlistItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchlistItemResponse>> Add(
        int watchlistId, [FromBody] CreateWatchlistItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _itemService.AddItemAsync(watchlistId, request, cancellationToken);
        return CreatedAtAction(nameof(WatchlistsController.GetById), "Watchlists", new { id = watchlistId }, result);
    }

    /// <summary>Remove a currency pair from a watchlist.</summary>
    [HttpDelete("{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int watchlistId, int itemId, CancellationToken cancellationToken)
    {
        await _itemService.RemoveItemAsync(watchlistId, itemId, cancellationToken);
        return NoContent();
    }
}
