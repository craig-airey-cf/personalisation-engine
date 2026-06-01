using PersonalisationEngine.Api.DTOs.Players;

namespace PersonalisationEngine.Api.Services;

public interface IPlayerService
{
    Task<IReadOnlyList<PlayerResponse>> GetAllAsync();
    Task<PlayerResponse> GetByPlayerIdAsync(string playerId);
    Task<PlayerResponse> CreateAsync(PlayerRequest request);
    Task<PlayerResponse> UpdateAsync(string playerId, PlayerRequest request);
    Task DeleteAsync(string playerId);
}
