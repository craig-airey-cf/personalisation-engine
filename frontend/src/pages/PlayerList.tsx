import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import type { Player } from '../api/types';

const riskColour: Record<string, string> = {
  Low: '#2e7d32',
  Medium: '#f57c00',
  High: '#c62828',
};

export default function PlayerList() {
  const [players, setPlayers] = useState<Player[]>([]);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    api.getPlayers()
      .then(setPlayers)
      .catch((e: Error) => setError(e.message));
  }, []);

  if (error) return <p style={{ color: 'red' }}>Error: {error}</p>;

  return (
    <div style={{ padding: '2rem', fontFamily: 'sans-serif' }}>
      <h1>Personalisation Engine — Admin</h1>
      <p>Select a player to generate a recommendation.</p>
      <table style={{ borderCollapse: 'collapse', width: '100%' }}>
        <thead>
          <tr style={{ background: '#1a3a5c', color: '#fff' }}>
            {['Player ID', 'Sport', 'Favourite Team', 'Avg Stake', 'Last Login', 'Risk', 'Excluded', 'Cooling Off'].map(h => (
              <th key={h} style={{ padding: '8px 12px', textAlign: 'left' }}>{h}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {players.map((p, i) => (
            <tr
              key={p.playerId}
              onClick={() => navigate(`/players/${p.playerId}`)}
              style={{
                background: i % 2 === 0 ? '#f9f9f9' : '#fff',
                cursor: 'pointer',
              }}
              onMouseEnter={e => (e.currentTarget.style.background = '#e3f2fd')}
              onMouseLeave={e => (e.currentTarget.style.background = i % 2 === 0 ? '#f9f9f9' : '#fff')}
            >
              <td style={{ padding: '8px 12px' }}><strong>{p.playerId}</strong></td>
              <td style={{ padding: '8px 12px' }}>{p.mostBetSport}</td>
              <td style={{ padding: '8px 12px' }}>{p.favouriteTeam ?? '—'}</td>
              <td style={{ padding: '8px 12px' }}>£{p.averageStake.toFixed(2)}</td>
              <td style={{ padding: '8px 12px' }}>{p.lastLoginDaysAgo}d ago</td>
              <td style={{ padding: '8px 12px' }}>
                <span style={{
                  color: '#fff',
                  background: riskColour[p.riskLevel] ?? '#555',
                  borderRadius: '4px',
                  padding: '2px 8px',
                  fontSize: '0.85em',
                }}>
                  {p.riskLevel}
                </span>
              </td>
              <td style={{ padding: '8px 12px' }}>{p.isSelfExcluded ? '🚫 Yes' : 'No'}</td>
              <td style={{ padding: '8px 12px' }}>{p.isInCoolingOff ? '⏸ Yes' : 'No'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
