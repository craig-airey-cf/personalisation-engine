import type { Player, RecommendationResponse } from './types';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(path, options);
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(err.error ?? 'Request failed');
  }
  return res.json();
}

export const api = {
  getPlayers: () => request<Player[]>('/api/players'),
  getPlayer: (playerId: string) => request<Player>(`/api/players/${playerId}`),
  generateRecommendation: (playerId: string) =>
    request<RecommendationResponse>(`/api/recommendations/${playerId}`, { method: 'POST' }),
};
