import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { boardService } from '../services/boardService.js';
import { DragDropContext, Droppable, Draggable } from '@hello-pangea/dnd';

const Avatar = ({ name }) => {
  if (!name) return null;
  const initials = name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  return (
    <div style={{
      width: '28px', height: '28px', borderRadius: '50%', backgroundColor: '#0052cc', color: 'white',
      display: 'flex', justifyContent: 'center', alignItems: 'center', fontSize: '12px', fontWeight: 'bold', title: name
    }}>
      {initials}
    </div>
  );
};

export default function Board() {
  const { boardId } = useParams(); 
  const [board, setBoard] = useState(null);
  const [loading, setLoading] = useState(true);
  
  const [isAddingColumn, setIsAddingColumn] = useState(false);
  const [newColumnName, setNewColumnName] = useState('');
  
  const [addingCardToColumn, setAddingCardToColumn] = useState(null);
  const [newCardTitle, setNewCardTitle] = useState('');

  useEffect(() => {
    const fetchBoardDetails = async () => {
      try {
        const data = await boardService.getBoardById(boardId);
        setBoard(data);
      } catch (err) {
        console.error(err);
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
      setBoard(prev => ({ ...prev, columns: [...(prev.columns || []), { ...newCol, cards: [] }] }));
      setNewColumnName('');
      setIsAddingColumn(false);
    } catch (err) { console.error(err); }
  };

  const handleAddCard = async (e, columnId) => {
    e.preventDefault();
    if (!newCardTitle.trim()) return;
    try {
      const newCard = await boardService.createCard(boardId, columnId, newCardTitle);
      
      setBoard(prev => {
        const updatedColumns = prev.columns.map(col => {
          if (col.id === columnId) {
            return { ...col, cards: [...(col.cards || []), newCard] };
          }
          return col;
        });
        return { ...prev, columns: updatedColumns };
      });
      
      setNewCardTitle('');
      setAddingCardToColumn(null);
    } catch (err) { console.error(err); }
  };

  const handleDragEnd = async (result) => {
    const { destination, source, draggableId } = result;

    if (!destination) return;
    if (destination.droppableId === source.droppableId && destination.index === source.index) return;

    const sourceColIndex = board.columns.findIndex(c => c.id.toString() === source.droppableId);
    const destColIndex = board.columns.findIndex(c => c.id.toString() === destination.droppableId);

    const sourceCol = board.columns[sourceColIndex];
    const destCol = board.columns[destColIndex];

    const boardCopy = JSON.parse(JSON.stringify(board));
    const previousBoardState = JSON.parse(JSON.stringify(board));

    const sourceCards = Array.from(boardCopy.columns[sourceColIndex].cards || []);
    const destCards = sourceCol.id === destCol.id ? sourceCards : Array.from(boardCopy.columns[destColIndex].cards || []);

    const [movedCard] = sourceCards.splice(source.index, 1);
    destCards.splice(destination.index, 0, movedCard);

    boardCopy.columns[sourceColIndex].cards = sourceCards;
    if (sourceCol.id !== destCol.id) boardCopy.columns[destColIndex].cards = destCards;

    setBoard(boardCopy);

    try {
      await boardService.moveCard(
        boardId, 
        draggableId, 
        destCol.id,
        movedCard.title, 
        movedCard.description
      );
    } catch (err) {
      console.error(err);
      setBoard(previousBoardState);
      alert("Failed to move card. Network error.");
    }
  };

  if (loading) return <div style={{ padding: '20px' }}>Loading board...</div>;
  if (!board) return <div style={{ padding: '20px' }}>Board not found.</div>;

  return (
    <div style={{ padding: '20px', display: 'flex', flexDirection: 'column', height: '100vh', backgroundColor: '#fff' }}>
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: '20px' }}>
        <Link to="/dashboard" style={{ marginRight: '20px', textDecoration: 'none', color: '#0052cc', fontWeight: 'bold', padding: '8px 12px', backgroundColor: '#ebecf0', borderRadius: '4px' }}>
          &larr; Back to Dashboard
        </Link>
        <h1 style={{ margin: 0, color: '#172b4d' }}>{board.name}</h1>
      </div>

      <DragDropContext onDragEnd={handleDragEnd}>
        <div style={{ display: 'flex', gap: '20px', flexGrow: 1, overflowX: 'auto', paddingBottom: '20px', alignItems: 'flex-start' }}>
          
          {board.columns && board.columns.map(column => (
            <div key={column.id} style={{ minWidth: '280px', maxWidth: '280px', backgroundColor: '#f4f5f7', padding: '12px', borderRadius: '8px' }}>
              <h3 style={{ marginTop: 0, color: '#172b4d', fontSize: '16px', padding: '4px 8px' }}>{column.name}</h3>
              
              <Droppable droppableId={column.id.toString()}>
                {(provided) => (
                  <div 
                    ref={provided.innerRef} 
                    {...provided.droppableProps}
                    style={{ minHeight: '50px' }}
                  >
                    {column.cards && column.cards.map((card, index) => (
                      
                      <Draggable key={card.id.toString()} draggableId={card.id.toString()} index={index}>
                        {(provided) => (
                          <div 
                            ref={provided.innerRef}
                            {...provided.draggableProps}
                            {...provided.dragHandleProps}
                            style={{ 
                              ...provided.draggableProps.style, // Ważne dla animacji
                              backgroundColor: 'white', padding: '12px', borderRadius: '6px', 
                              boxShadow: '0 1px 3px rgba(0,0,0,0.15)', cursor: 'grab', marginBottom: '8px'
                            }}
                          >
                            <div style={{ marginBottom: card.assigneeName ? '12px' : '0' }}>{card.title}</div>
                            <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                              {card.assigneeName ? <Avatar name={card.assigneeName} /> : <div style={{ width: '28px', height: '28px', borderRadius: '50%', backgroundColor: '#dfe1e6' }}></div>}
                            </div>
                          </div>
                        )}
                      </Draggable>

                    ))}
                    {provided.placeholder}
                  </div>
                )}
              </Droppable>

              {addingCardToColumn === column.id ? (
                <form onSubmit={(e) => handleAddCard(e, column.id)} style={{ marginTop: '8px' }}>
                  <input type="text" placeholder="What needs to be done?" value={newCardTitle} onChange={e => setNewCardTitle(e.target.value)} autoFocus style={{ width: '100%', padding: '8px', marginBottom: '8px', boxSizing: 'border-box', borderRadius: '4px', border: 'none' }} />
                  <div>
                    <button type="submit" style={{ padding: '6px 12px', backgroundColor: '#0052cc', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>Add</button>
                    <button type="button" onClick={() => setAddingCardToColumn(null)} style={{ padding: '6px 12px', backgroundColor: 'transparent', border: 'none', cursor: 'pointer' }}>Cancel</button>
                  </div>
                </form>
              ) : (
                <button onClick={() => setAddingCardToColumn(column.id)} style={{ width: '100%', padding: '8px', marginTop: '8px', backgroundColor: 'transparent', border: 'none', textAlign: 'left', color: '#5e6c84', cursor: 'pointer', borderRadius: '4px' }}>
                  + Add a card
                </button>
              )}
            </div>
          ))}

          <div style={{ minWidth: '280px', maxWidth: '280px' }}>
            {isAddingColumn ? (
              <form onSubmit={handleAddColumn} style={{ backgroundColor: '#f4f5f7', padding: '12px', borderRadius: '8px' }}>
                <input type="text" placeholder="Enter list title..." value={newColumnName} onChange={(e) => setNewColumnName(e.target.value)} autoFocus style={{ width: '100%', padding: '8px', marginBottom: '8px', boxSizing: 'border-box', borderRadius: '4px', border: '2px solid #0052cc' }} />
                <div>
                  <button type="submit" style={{ padding: '6px 12px', backgroundColor: '#0052cc', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>Save</button>
                  <button type="button" onClick={() => setIsAddingColumn(false)} style={{ padding: '6px 12px', backgroundColor: 'transparent', border: 'none', cursor: 'pointer' }}>Cancel</button>
                </div>
              </form>
            ) : (
              <button onClick={() => setIsAddingColumn(true)} style={{ width: '100%', padding: '12px', backgroundColor: 'rgba(9, 30, 66, 0.04)', border: 'none', borderRadius: '8px', cursor: 'pointer', textAlign: 'left', fontWeight: 'bold', color: '#172b4d' }}>+ Add another list</button>
            )}
          </div>
        </div>
      </DragDropContext>
    </div>
  );
}