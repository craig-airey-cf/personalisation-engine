import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { mockPlayer, safeRecommendation, blockedRecommendation } from '../test/fixtures';
import * as client from '../api/client';
import PlayerDetail from './PlayerDetail';

function renderDetail(playerId = 'P001') {
  return render(
    <MemoryRouter initialEntries={[`/players/${playerId}`]}>
      <Routes>
        <Route path="/players/:playerId" element={<PlayerDetail />} />
      </Routes>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  vi.restoreAllMocks();
});

describe('PlayerDetail', () => {
  it('shows loading state initially', () => {
    vi.spyOn(client.api, 'getPlayer').mockResolvedValue(mockPlayer);
    renderDetail();
    expect(screen.getByText('Loading…')).toBeInTheDocument();
  });

  it('renders the player profile after loading', async () => {
    vi.spyOn(client.api, 'getPlayer').mockResolvedValue(mockPlayer);
    renderDetail();
    await waitFor(() => {
      expect(screen.getByText('Player: P001')).toBeInTheDocument();
    });
    expect(screen.getByText('Football')).toBeInTheDocument();
    expect(screen.getByText('Scotland')).toBeInTheDocument();
    expect(screen.getByText('£10.00')).toBeInTheDocument();
  });

  it('renders an error when player load fails', async () => {
    vi.spyOn(client.api, 'getPlayer').mockRejectedValue(new Error('Not found'));
    renderDetail('UNKNOWN');
    await waitFor(() => {
      expect(screen.getByText('Error: Not found')).toBeInTheDocument();
    });
  });

  it('shows the generate button', async () => {
    vi.spyOn(client.api, 'getPlayer').mockResolvedValue(mockPlayer);
    renderDetail();
    await waitFor(() => screen.getByText('Player: P001'));
    expect(screen.getByRole('button', { name: 'Generate Recommendation' })).toBeInTheDocument();
  });

  it('displays a safe recommendation result', async () => {
    vi.spyOn(client.api, 'getPlayer').mockResolvedValue(mockPlayer);
    vi.spyOn(client.api, 'generateRecommendation').mockResolvedValue(safeRecommendation);
    renderDetail();
    await waitFor(() => screen.getByText('Generate Recommendation'));

    await userEvent.click(screen.getByRole('button', { name: 'Generate Recommendation' }));

    await waitFor(() => {
      expect(screen.getByText('✓ Safe to show')).toBeInTheDocument();
      expect(screen.getByText('Great markets today')).toBeInTheDocument();
      expect(screen.getByText('Based on your history we picked these.')).toBeInTheDocument();
    });
    expect(screen.getByText('Football markets')).toBeInTheDocument();
  });

  it('displays a blocked recommendation result', async () => {
    vi.spyOn(client.api, 'getPlayer').mockResolvedValue(mockPlayer);
    vi.spyOn(client.api, 'generateRecommendation').mockResolvedValue(blockedRecommendation);
    renderDetail();
    await waitFor(() => screen.getByText('Generate Recommendation'));

    await userEvent.click(screen.getByRole('button', { name: 'Generate Recommendation' }));

    await waitFor(() => {
      expect(screen.getByText('✗ Blocked')).toBeInTheDocument();
      expect(screen.getByText('Guardrail triggered: High risk player')).toBeInTheDocument();
    });
  });

  it('shows an error when recommendation generation fails', async () => {
    vi.spyOn(client.api, 'getPlayer').mockResolvedValue(mockPlayer);
    vi.spyOn(client.api, 'generateRecommendation').mockRejectedValue(new Error('API unavailable'));
    renderDetail();
    await waitFor(() => screen.getByText('Generate Recommendation'));

    await userEvent.click(screen.getByRole('button', { name: 'Generate Recommendation' }));

    await waitFor(() => {
      expect(screen.getByText('Error: API unavailable')).toBeInTheDocument();
    });
  });

  it('disables the generate button while loading', async () => {
    vi.spyOn(client.api, 'getPlayer').mockResolvedValue(mockPlayer);
    let resolve: (v: typeof safeRecommendation) => void;
    vi.spyOn(client.api, 'generateRecommendation').mockReturnValue(
      new Promise(r => { resolve = r; }),
    );
    renderDetail();
    await waitFor(() => screen.getByText('Generate Recommendation'));

    await userEvent.click(screen.getByRole('button', { name: 'Generate Recommendation' }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Generating…' })).toBeDisabled();
    });

    resolve!(safeRecommendation);
  });
});
