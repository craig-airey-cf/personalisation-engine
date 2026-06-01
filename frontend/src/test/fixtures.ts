import type { Player, RecommendationResponse } from '../api/types';

export const mockPlayer: Player = {
  id: 1,
  playerId: 'P001',
  daysSinceJoined: 100,
  mostBetSport: 'Football',
  favouriteTeam: 'Scotland',
  mostBetType: 'Match Winner',
  averageStake: 10.0,
  lastLoginDaysAgo: 1,
  riskLevel: 'Low',
  isSelfExcluded: false,
  isInCoolingOff: false,
  createdAt: '2025-01-01T00:00:00Z',
};

export const mockPlayer2: Player = {
  id: 2,
  playerId: 'P002',
  daysSinceJoined: 200,
  mostBetSport: 'Tennis',
  favouriteTeam: null,
  mostBetType: 'Set Betting',
  averageStake: 5.0,
  lastLoginDaysAgo: 3,
  riskLevel: 'Medium',
  isSelfExcluded: false,
  isInCoolingOff: true,
  createdAt: '2025-01-02T00:00:00Z',
};

export const safeRecommendation: RecommendationResponse = {
  id: 1,
  playerId: 'P001',
  safeToShow: true,
  blockReason: null,
  safeOptions: ['Football markets', 'Scotland specials'],
  recommendationType: 'Market',
  headline: 'Great markets today',
  message: 'Based on your history we picked these.',
  reason: 'Low risk, active player',
  createdAt: '2025-01-01T00:00:00Z',
};

export const blockedRecommendation: RecommendationResponse = {
  id: 2,
  playerId: 'P005',
  safeToShow: false,
  blockReason: 'High risk player',
  safeOptions: [],
  recommendationType: null,
  headline: null,
  message: null,
  reason: null,
  createdAt: '2025-01-01T00:00:00Z',
};
