export interface Player {
  id: number;
  playerId: string;
  daysSinceJoined: number;
  mostBetSport: string;
  favouriteTeam: string | null;
  mostBetType: string;
  averageStake: number;
  lastLoginDaysAgo: number;
  riskLevel: 'Low' | 'Medium' | 'High';
  isSelfExcluded: boolean;
  isInCoolingOff: boolean;
  createdAt: string;
}

export interface RecommendationResponse {
  id: number;
  playerId: string;
  safeToShow: boolean;
  blockReason: string | null;
  safeOptions: string[];
  recommendationType: string | null;
  headline: string | null;
  message: string | null;
  reason: string | null;
  createdAt: string;
}
