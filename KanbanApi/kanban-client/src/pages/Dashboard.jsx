import { useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';

export default function Dashboard() {
  const navigate = useNavigate();

  const handleLogout = () => {
    authService.logout();
    navigate('/login');
  };

  return (
    <div style={{ padding: '20px' }}>
      <h1>Dashboard</h1>
      <p>Welcome! You are successfully logged in.</p>
      <button onClick={handleLogout}>Log Out</button>
    </div>
  );
}