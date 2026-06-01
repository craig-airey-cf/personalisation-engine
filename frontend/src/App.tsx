import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import PlayerList from './pages/PlayerList';
import PlayerDetail from './pages/PlayerDetail';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/players" replace />} />
        <Route path="/players" element={<PlayerList />} />
        <Route path="/players/:playerId" element={<PlayerDetail />} />
      </Routes>
    </BrowserRouter>
  );
}
