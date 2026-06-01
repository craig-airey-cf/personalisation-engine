import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { mockPlayer, mockPlayer2 } from '../test/fixtures';
import * as client from '../api/client';
import PlayerList from './PlayerList';

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
    vi.spyOn(client.api, 'getPlayers').mockResolvedValue([mockPlayer, mockPlayer2]);
    renderPlayerList();
    expect(screen.getByText('Personalisation Engine — Admin')).toBeInTheDocument();
  });

  it('renders a row for each player', async () => {
    vi.spyOn(client.api, 'getPlayers').mockResolvedValue([mockPlayer, mockPlayer2]);
    renderPlayerList();
    await waitFor(() => {
      expect(screen.getByText('P001')).toBeInTheDocument();
      expect(screen.getByText('P002')).toBeInTheDocument();
    });
  });

  it('renders player sport and formatted stake', async () => {
    vi.spyOn(client.api, 'getPlayers').mockResolvedValue([mockPlayer, mockPlayer2]);
    renderPlayerList();
    await waitFor(() => {
      expect(screen.getByText('Football')).toBeInTheDocument();
      expect(screen.getByText('£10.00')).toBeInTheDocument();
    });
  });

  it('renders — for null favouriteTeam', async () => {
    vi.spyOn(client.api, 'getPlayers').mockResolvedValue([mockPlayer, mockPlayer2]);
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
    vi.spyOn(client.api, 'getPlayers').mockResolvedValue([mockPlayer, mockPlayer2]);
    const { container } = renderPlayerList();
    await waitFor(() => screen.getByText('P001'));

    const rows = container.querySelectorAll('tbody tr');
    await userEvent.click(rows[0]);

    await waitFor(() => {
      expect(screen.getByTestId('player-detail')).toBeInTheDocument();
    });
  });
});
