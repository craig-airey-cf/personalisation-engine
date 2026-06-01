import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { Player, RecommendationResponse } from '../api/types';

export default function PlayerDetail() {
  const { playerId } = useParams<{ playerId: string }>();
  const navigate = useNavigate();
  const [player, setPlayer] = useState<Player | null>(null);
  const [recommendation, setRecommendation] = useState<RecommendationResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!playerId) return;
    api.getPlayer(playerId)
      .then(setPlayer)
      .catch((e: Error) => setError(e.message));
  }, [playerId]);

  const generate = async () => {
    if (!playerId) return;
    setLoading(true);
    setError(null);
    try {
      const rec = await api.generateRecommendation(playerId);
      setRecommendation(rec);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  if (error && !player) return <p style={{ color: 'red', padding: '2rem' }}>Error: {error}</p>;
  if (!player) return <p style={{ padding: '2rem' }}>Loading…</p>;

  return (
    <div style={{ padding: '2rem', fontFamily: 'sans-serif', maxWidth: '860px' }}>
      <button onClick={() => navigate('/')} style={{ marginBottom: '1rem', cursor: 'pointer' }}>
        ← Back to player list
      </button>

      <h1>Player: {player.playerId}</h1>

      <section style={{ background: '#f5f5f5', padding: '1rem', borderRadius: '8px', marginBottom: '1.5rem' }}>
        <h2 style={{ marginTop: 0 }}>Profile</h2>
        <table style={{ borderCollapse: 'collapse' }}>
          <tbody>
            {[
              ['Most Bet Sport', player.mostBetSport],
              ['Favourite Team', player.favouriteTeam ?? '—'],
              ['Bet Type', player.mostBetType],
              ['Average Stake', `£${player.averageStake.toFixed(2)}`],
              ['Days Since Joined', player.daysSinceJoined],
              ['Last Login', `${player.lastLoginDaysAgo} days ago`],
              ['Risk Level', player.riskLevel],
              ['Self Excluded', player.isSelfExcluded ? 'Yes' : 'No'],
              ['Cooling Off', player.isInCoolingOff ? 'Yes' : 'No'],
            ].map(([label, value]) => (
              <tr key={String(label)}>
                <td style={{ padding: '4px 12px 4px 0', fontWeight: 'bold', color: '#555' }}>{label}</td>
                <td style={{ padding: '4px 0' }}>{value}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <button
        onClick={generate}
        disabled={loading}
        style={{
          background: '#1a3a5c',
          color: '#fff',
          border: 'none',
          padding: '10px 24px',
          borderRadius: '6px',
          fontSize: '1rem',
          cursor: loading ? 'not-allowed' : 'pointer',
          marginBottom: '1.5rem',
        }}
      >
        {loading ? 'Generating…' : 'Generate Recommendation'}
      </button>

      {error && <p style={{ color: 'red' }}>Error: {error}</p>}

      {recommendation && (
        <section style={{
          border: `2px solid ${recommendation.safeToShow ? '#2e7d32' : '#c62828'}`,
          borderRadius: '8px',
          padding: '1rem',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1rem' }}>
            <h2 style={{ margin: 0 }}>Recommendation Result</h2>
            <span style={{
              background: recommendation.safeToShow ? '#2e7d32' : '#c62828',
              color: '#fff',
              borderRadius: '4px',
              padding: '4px 12px',
              fontWeight: 'bold',
            }}>
              {recommendation.safeToShow ? '✓ Safe to show' : '✗ Blocked'}
            </span>
          </div>

          {recommendation.safeToShow ? (
            <>
              {recommendation.headline && (
                <h3 style={{ marginTop: 0, color: '#1a3a5c' }}>{recommendation.headline}</h3>
              )}
              {recommendation.message && <p>{recommendation.message}</p>}
              <table style={{ borderCollapse: 'collapse', marginTop: '0.5rem' }}>
                <tbody>
                  {recommendation.recommendationType && (
                    <tr>
                      <td style={{ fontWeight: 'bold', paddingRight: '12px', color: '#555' }}>Type</td>
                      <td>{recommendation.recommendationType}</td>
                    </tr>
                  )}
                  {recommendation.reason && (
                    <tr>
                      <td style={{ fontWeight: 'bold', paddingRight: '12px', color: '#555' }}>Reason</td>
                      <td>{recommendation.reason}</td>
                    </tr>
                  )}
                </tbody>
              </table>
              <div style={{ marginTop: '1rem' }}>
                <strong>Rules that passed:</strong>
                <ul style={{ marginTop: '0.5rem' }}>
                  {recommendation.safeOptions.map(opt => (
                    <li key={opt}>{opt}</li>
                  ))}
                </ul>
              </div>
            </>
          ) : (
            <p style={{ color: '#c62828', fontWeight: 'bold' }}>
              Guardrail triggered: {recommendation.blockReason}
            </p>
          )}
        </section>
      )}
    </div>
  );
}
