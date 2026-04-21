import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { boardService } from '../services/boardService.js';

const Avatar = ({ name }) => {
  if (!name) return null;
  const initials = name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  return (
    <div style={{
      width: '28px', height: '28px', borderRadius: '50%',
      backgroundColor: '#0052cc', color: 'white',
      display: 'flex', justifyContent: 'center', alignItems: 'center',
      fontSize: '12px', fontWeight: 'bold', title: name
    }}>
      {initials}
    </div>
  );
};

export default function Board() {
  const { boardId } = useParams(); 
  const [board, setBoard] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [isAddingColumn, setIsAddingColumn] = useState(false);
  const [newColumnName, setNewColumnName] = useState('');

  useEffect(() => {
    const fetchBoardDetails = async () => {
      try {
        setLoading(true);
        const data = await boardService.getBoardById(boardId);
        setBoard(data);
      } catch (err) {
        console.error(err);
        setError('Failed to load board details.');
      } finally {
        setLoading(false);
      }
    };

    fetchBoardDetails();
  }, [boardId]);

  const handleAddColumn = async (e) => {
    e.preventDefault();
    if (!newColumnName.trim()) return;

    try {
      const newCol = await boardService.createColumn(boardId, newColumnName);
      
      const columnToAdd = { ...newCol, cards: [] };
      
      setBoard(prevBoard => ({
        ...prevBoard,
        columns: [...(prevBoard.columns || []), columnToAdd]
      }));

      setNewColumnName('');
      setIsAddingColumn(false);
    } catch (err) {
      console.error(err);
      alert('Wystąpił błąd podczas dodawania kolumny.');
    }
  };

  if (loading) return <div style={{ padding: '20px', textAlign: 'center' }}>Loading board...</div>;
  if (error) return <div style={{ padding: '20px', color: 'red', textAlign: 'center' }}>{error}</div>;
  if (!board) return <div style={{ padding: '20px', textAlign: 'center' }}>Board not found.</div>;

  return (
    <div style={{ padding: '20px', display: 'flex', flexDirection: 'column', height: '100vh', backgroundColor: '#fff' }}>
      
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: '20px' }}>
        <Link to="/dashboard" style={{ marginRight: '20px', textDecoration: 'none', color: '#0052cc', fontWeight: 'bold', padding: '8px 12px', backgroundColor: '#ebecf0', borderRadius: '4px' }}>
          &larr; Back to Dashboard
        </Link>
        <h1 style={{ margin: 0, color: '#172b4d' }}>{board.name}</h1>
      </div>

      <div style={{ 
        display: 'flex', gap: '20px', flexGrow: 1, overflowX: 'auto', paddingBottom: '20px', alignItems: 'flex-start' 
      }}>
        
        {board.columns && board.columns.map(column => (
          <div key={column.id} style={{ 
            minWidth: '280px', maxWidth: '280px', backgroundColor: '#f4f5f7', padding: '12px', borderRadius: '8px', boxShadow: '0 1px 2px rgba(0,0,0,0.1)'
          }}>
            <h3 style={{ marginTop: 0, color: '#172b4d', fontSize: '16px', padding: '4px 8px' }}>{column.name}</h3>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {column.cards && column.cards.length > 0 ? (
                column.cards.map(card => (
                  <div key={card.id} style={{ backgroundColor: 'white', padding: '12px', borderRadius: '6px', boxShadow: '0 1px 3px rgba(0,0,0,0.15)', cursor: 'pointer' }}>
                    <div style={{ marginBottom: card.assigneeName ? '12px' : '0' }}>{card.title}</div>
                    <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                      {card.assigneeName ? <Avatar name={card.assigneeName} /> : <div style={{ width: '28px', height: '28px', borderRadius: '50%', backgroundColor: '#dfe1e6' }}></div>}
                    </div>
                  </div>
                ))
              ) : (
                <div style={{ color: '#8c9bab', fontSize: '14px', fontStyle: 'italic', padding: '8px' }}>No cards yet.</div>
              )}
            </div>
          </div>
        ))}

        <div style={{ minWidth: '280px', maxWidth: '280px' }}>
          {isAddingColumn ? (
            <form onSubmit={handleAddColumn} style={{ backgroundColor: '#f4f5f7', padding: '12px', borderRadius: '8px', boxShadow: '0 1px 2px rgba(0,0,0,0.1)' }}>
              <input 
                type="text" 
                placeholder="Enter column name..." 
                value={newColumnName}
                onChange={(e) => setNewColumnName(e.target.value)}
                autoFocus
                style={{ width: '100%', padding: '8px', marginBottom: '8px', boxSizing: 'border-box', borderRadius: '4px', border: '2px solid #0052cc' }}
              />
              <div style={{ display: 'flex', gap: '8px' }}>
                <button type="submit" style={{ padding: '6px 12px', backgroundColor: '#0052cc', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}>
                  Save
                </button>
                <button type="button" onClick={() => setIsAddingColumn(false)} style={{ padding: '6px 12px', backgroundColor: 'transparent', border: 'none', cursor: 'pointer', color: '#172b4d' }}>
                  Cancel
                </button>
              </div>
            </form>
          ) : (
            <button 
              onClick={() => setIsAddingColumn(true)}
              style={{ width: '100%', padding: '12px', backgroundColor: 'rgba(9, 30, 66, 0.04)', border: 'none', borderRadius: '8px', cursor: 'pointer', textAlign: 'left', fontWeight: 'bold', color: '#172b4d', transition: 'background-color 0.2s' }}
            >
              + Add another list
            </button>
          )}
        </div>

      </div>
    </div>
  );
}