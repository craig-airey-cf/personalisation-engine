import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import PlayerList from './PlayerList';
import * as client from '../api/client';

const mockPlayers = [
  {
    id: 1,
    playerId: 'P001',
    daysSinceJoined: 100,
    mostBetSport: 'Football',
    favouriteTeam: 'Scotland',
    mostBetType: 'Match Winner',
    averageStake: 10.5,
    lastLoginDaysAgo: 1,
    riskLevel: 'Low' as const,
    isSelfExcluded: false,
    isInCoolingOff: false,
    createdAt: '2025-01-01T00:00:00Z',
  },
  {
    id: 2,
    playerId: 'P002',
    daysSinceJoined: 200,
    mostBetSport: 'Tennis',
    favouriteTeam: null,
    mostBetType: 'Set Betting',
    averageStake: 5.0,
    lastLoginDaysAgo: 3,
    riskLevel: 'Medium' as const,
    isSelfExcluded: false,
    isInCoolingOff: true,
    createdAt: '2025-01-02T00:00:00Z',
  },
];

function renderPlayerList() {
  return render(
    <MemoryRouter initialEntries={['/']}>
      <Routes>
        <Route path="/" element={<PlayerList />} />
        <Route path="/players/:playerId" element={<div data-testid="player-detail" />} />
      </Routes>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  vi.restoreAllMocks();
});

describe('PlayerList', () => {
  it('renders the page heading', async () => {
    vi.spyOn(client.api, 'getPlayers').mockResolvedValue(mockPlayers);
    renderPlayerList();
    expect(screen.getByText('Personalisation Engine — Admin')).toBeInTheDocument();
  });

  it('renders a row for each player', async () => {
    vi.spyOn(client.api, 'getPlayers').mockResolvedValue(mockPlayers);
    renderPlayerList();
    await waitFor(() => {
      expect(screen.getByText('P001')).toBeInTheDocument();
      expect(screen.getByText('P002')).toBeInTheDocument();
    });
  });

  it('renders player sport and formatted stake', async () => {
    vi.spyOn(client.api, 'getPlayers').mockResolvedValue(mockPlayers);
    renderPlayerList();
    await waitFor(() => {
      expect(screen.getByText('Football')).toBeInTheDocument();
      expect(screen.getByText('£10.50')).toBeInTheDocument();
    });
  });

  it('renders — for null favouriteTeam', async () => {
    vi.spyOn(client.api, 'getPlayers').mockResolvedValue(mockPlayers);
    renderPlayerList();
    await waitFor(() => {
      expect(screen.getByText('—')).toBeInTheDocument();
    });
  });

  it('renders an error message when the API fails', async () => {
    vi.spyOn(client.api, 'getPlayers').mockRejectedValue(new Error('Network error'));
    renderPlayerList();
    await waitFor(() => {
      expect(screen.getByText('Error: Network error')).toBeInTheDocument();
    });
  });

  it('navigates to player detail on row click', async () => {
    vi.spyOn(client.api, 'getPlayers').mockResolvedValue(mockPlayers);
    const { container } = renderPlayerList();
    await waitFor(() => screen.getByText('P001'));

    const rows = container.querySelectorAll('tbody tr');
    await userEvent.click(rows[0]);

    await waitFor(() => {
      expect(screen.getByTestId('player-detail')).toBeInTheDocument();
    });
  });
});
