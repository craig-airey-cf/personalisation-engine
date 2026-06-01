using Microsoft.EntityFrameworkCore;
using PersonalisationEngine.Api.Data;
using PersonalisationEngine.Api.DTOs.Players;
using PersonalisationEngine.Api.Middleware;
using PersonalisationEngine.Api.Models;

namespace PersonalisationEngine.Api.Services;

public class PlayerService(AppDbContext db) : IPlayerService
{
    public async Task<IReadOnlyList<PlayerResponse>> GetAllAsync()
    {
        var players = await db.Players.AsNoTracking().OrderBy(p => p.PlayerId).ToListAsync();
        return players.Select(PlayerResponse.From).ToList();
    }

    public async Task<PlayerResponse> GetByPlayerIdAsync(string playerId)
    {
        var player = await db.Players.AsNoTracking()
            .SingleOrDefaultAsync(p => p.PlayerId == playerId)
            ?? throw new NotFoundException($"Player '{playerId}' not found");
        return PlayerResponse.From(player);
    }

    public async Task<PlayerResponse> CreateAsync(PlayerRequest request)
    {
        if (await db.Players.AnyAsync(p => p.PlayerId == request.PlayerId))
            throw new ConflictException($"Player '{request.PlayerId}' already exists");

        var player = Map(new Player(), request);
        db.Players.Add(player);
        await db.SaveChangesAsync();
        return PlayerResponse.From(player);
    }

    public async Task<PlayerResponse> UpdateAsync(string playerId, PlayerRequest request)
    {
        if (request.PlayerId != playerId)
            throw new BadRequestException(
                $"PlayerId in body '{request.PlayerId}' does not match URL '{playerId}'");

        var player = await db.Players.SingleOrDefaultAsync(p => p.PlayerId == playerId)
            ?? throw new NotFoundException($"Player '{playerId}' not found");

        Map(player, request);
        await db.SaveChangesAsync();
        return PlayerResponse.From(player);
    }

    public async Task DeleteAsync(string playerId)
    {
        var player = await db.Players.SingleOrDefaultAsync(p => p.PlayerId == playerId)
            ?? throw new NotFoundException($"Player '{playerId}' not found");

        db.Players.Remove(player);
        await db.SaveChangesAsync();
    }

    private static Player Map(Player player, PlayerRequest r)
    {
        player.PlayerId = r.PlayerId;
        player.DaysSinceJoined = r.DaysSinceJoined;
        player.MostBetSport = r.MostBetSport;
        player.FavouriteTeam = r.FavouriteTeam;
        player.MostBetType = r.MostBetType;
        player.AverageStake = r.AverageStake;
        player.LastLoginDaysAgo = r.LastLoginDaysAgo;
        player.RiskLevel = r.RiskLevel;
        player.IsSelfExcluded = r.IsSelfExcluded;
        player.IsInCoolingOff = r.IsInCoolingOff;
        return player;
    }
}
