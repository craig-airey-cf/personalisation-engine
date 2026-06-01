using Microsoft.AspNetCore.Mvc;
using PersonalisationEngine.Api.DTOs.Players;
using PersonalisationEngine.Api.Services;

namespace PersonalisationEngine.Api.Controllers;

[ApiController]
[Route("api/players")]
public class PlayersController(IPlayerService playerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await playerService.GetAllAsync());

    [HttpGet("{playerId}")]
    public async Task<IActionResult> GetById(string playerId) =>
        Ok(await playerService.GetByPlayerIdAsync(playerId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PlayerRequest request)
    {
        var result = await playerService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { playerId = result.PlayerId }, result);
    }

    [HttpPut("{playerId}")]
    public async Task<IActionResult> Update(string playerId, [FromBody] PlayerRequest request) =>
        Ok(await playerService.UpdateAsync(playerId, request));

    [HttpDelete("{playerId}")]
    public async Task<IActionResult> Delete(string playerId)
    {
        await playerService.DeleteAsync(playerId);
        return NoContent();
    }
}
