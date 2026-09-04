using CurrencyWatchlist.Application.Dtos.Alerts;
using CurrencyWatchlist.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyWatchlist.Api.Controllers;

/// <summary>Create and evaluate alert rules on watchlist items.</summary>
[ApiController]
[Route("api/alerts")]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    /// <summary>Create an alert rule (e.g. notify when USD-&gt;AUD goes above 1.60).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AlertRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertRuleResponse>> Create([FromBody] CreateAlertRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await _alertService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { watchlistId = (int?)null }, result);
    }

    /// <summary>List alert rules, optionally scoped to a single watchlist.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertRuleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertRuleResponse>>> GetAll([FromQuery] int? watchlistId, CancellationToken cancellationToken) =>
        Ok(await _alertService.GetAllAsync(watchlistId, cancellationToken));

    /// <summary>
    /// Fetch the latest rate for this rule's pair, check its threshold condition, and return the result.
    /// An AlertEvent is persisted (and pushed live) when the condition is met.
    /// </summary>
    [HttpPost("{id:int}/evaluate")]
    [ProducesResponseType(typeof(AlertEvaluationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<AlertEvaluationResult>> Evaluate(int id, CancellationToken cancellationToken) =>
        Ok(await _alertService.EvaluateAsync(id, cancellationToken));
}
