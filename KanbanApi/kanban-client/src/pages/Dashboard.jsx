import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { boardService } from '../services/boardService.js';

export default function Dashboard() {
  const [boards, setBoards] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  const [isCreating, setIsCreating] = useState(false);
  const [newBoardName, setNewBoardName] = useState('');

  useEffect(() => {
    const loadBoards = async () => {
      try {
        setLoading(true);
        const data = await boardService.getBoards();
        setBoards(data);
      } catch (err) {
        console.error(err);
        setError('Failed to load boards. Please try again later.');
      } finally {
        setLoading(false);
      }
    };

    loadBoards();
  }, []);

  const handleCreateBoard = async (e) => {
    e.preventDefault();
    if (!newBoardName.trim()) return;

    try {
      const createdBoard = await boardService.createBoard(newBoardName);
      
      setBoards([...boards, createdBoard]); 
      
      setNewBoardName('');
      setIsCreating(false);
      setError('');
    } catch (err) {
      console.error(err);
      setError('Failed to create board. It might be a server issue.');
    }
  };

  if (loading) {
    return <div style={{ textAlign: 'center', marginTop: '50px' }}>Loading your boards...</div>;
  }

  return (
    <div style={{ padding: '20px', maxWidth: '1000px', margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>My Boards</h1>
        {boards.length > 0 && !isCreating && (
          <button onClick={() => setIsCreating(true)}>+ New Board</button>
        )}
      </div>

      {error && <p style={{ color: 'red' }}>{error}</p>}

      {isCreating && (
        <form onSubmit={handleCreateBoard} style={{ marginBottom: '20px', padding: '15px', backgroundColor: '#e9ecef', borderRadius: '8px' }}>
          <input 
            type="text" 
            placeholder="Enter board name..." 
            value={newBoardName}
            onChange={(e) => setNewBoardName(e.target.value)}
            autoFocus
            style={{ padding: '8px', marginRight: '10px' }}
          />
          <button type="submit" style={{ padding: '8px 15px', marginRight: '5px' }}>Save</button>
          <button type="button" onClick={() => setIsCreating(false)} style={{ padding: '8px 15px' }}>Cancel</button>
        </form>
      )}

      {boards.length === 0 && !isCreating ? (
        <div style={{ textAlign: 'center', padding: '40px', border: '2px dashed #ccc' }}>
          <p>You don't have any boards yet.</p>
          <button onClick={() => setIsCreating(true)}>Create your first board</button>
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '20px' }}>
          {boards.map(board => (
            <Link 
              key={board.id} 
              to={`/board/${board.id}`} 
              style={{ 
                textDecoration: 'none', 
                color: 'inherit',
                display: 'block',
                padding: '20px',
                backgroundColor: '#f4f5f7',
                borderRadius: '8px',
                boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
                fontWeight: 'bold',
                transition: 'background-color 0.2s'
              }}
            >
              {board.name}
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}