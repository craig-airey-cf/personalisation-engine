import { describe, it, expect, vi, beforeEach } from 'vitest';
import { api } from './client';

const mockPlayer = {
  id: 1,
  playerId: 'P001',
  daysSinceJoined: 100,
  mostBetSport: 'Football',
  favouriteTeam: 'Scotland',
  mostBetType: 'Match Winner',
  averageStake: 10.0,
  lastLoginDaysAgo: 1,
  riskLevel: 'Low' as const,
  isSelfExcluded: false,
  isInCoolingOff: false,
  createdAt: '2025-01-01T00:00:00Z',
};

const mockRecommendation = {
  id: 1,
  playerId: 'P001',
  safeToShow: true,
  blockReason: null,
  safeOptions: ['Football markets', 'Scotland specials'],
  recommendationType: 'Market',
  headline: 'Check out today\'s markets',
  message: 'Great markets available.',
  reason: 'Based on your betting history',
  createdAt: '2025-01-01T00:00:00Z',
};

function mockFetch(body: unknown, ok = true, status = 200) {
  return vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
    ok,
    status,
    statusText: ok ? 'OK' : 'Not Found',
    json: () => Promise.resolve(body),
  } as Response);
}

beforeEach(() => {
  vi.restoreAllMocks();
});

describe('api.getPlayers', () => {
  it('returns players on success', async () => {
    mockFetch([mockPlayer]);
    const result = await api.getPlayers();
    expect(result).toEqual([mockPlayer]);
  });

  it('calls the correct endpoint', async () => {
    const spy = mockFetch([]);
    await api.getPlayers();
    expect(spy).toHaveBeenCalledWith('/api/players', undefined);
  });

  it('throws with server error message on failure', async () => {
    mockFetch({ error: 'Database unavailable' }, false, 500);
    await expect(api.getPlayers()).rejects.toThrow('Database unavailable');
  });

  it('falls back to statusText when error body has no message', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: false,
      status: 503,
      statusText: 'Service Unavailable',
      json: () => Promise.reject(new Error('not json')),
    } as Response);
    await expect(api.getPlayers()).rejects.toThrow('Service Unavailable');
  });
});

describe('api.getPlayer', () => {
  it('returns a single player on success', async () => {
    mockFetch(mockPlayer);
    const result = await api.getPlayer('P001');
    expect(result).toEqual(mockPlayer);
  });

  it('calls the correct endpoint', async () => {
    const spy = mockFetch(mockPlayer);
    await api.getPlayer('P001');
    expect(spy).toHaveBeenCalledWith('/api/players/P001', undefined);
  });

  it('throws on 404', async () => {
    mockFetch({ error: 'Player not found' }, false, 404);
    await expect(api.getPlayer('UNKNOWN')).rejects.toThrow('Player not found');
  });
});

describe('api.generateRecommendation', () => {
  it('returns a recommendation on success', async () => {
    mockFetch(mockRecommendation);
    const result = await api.generateRecommendation('P001');
    expect(result).toEqual(mockRecommendation);
  });

  it('calls POST to the correct endpoint', async () => {
    const spy = mockFetch(mockRecommendation);
    await api.generateRecommendation('P001');
    expect(spy).toHaveBeenCalledWith('/api/recommendations/P001', { method: 'POST' });
  });

  it('throws on error response', async () => {
    mockFetch({ error: 'Player blocked' }, false, 400);
    await expect(api.generateRecommendation('P005')).rejects.toThrow('Player blocked');
  });
});
