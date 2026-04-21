import api from './api.js';

export const boardService = {
  getBoards: async () => {
    const response = await api.get('/api/boards');
    return response.data;
  },

  createBoard: async (name) => {
    const response = await api.post('/api/boards', { name });
    return response.data;
  },

  getBoardById: async (id) => {
    const response = await api.get(`/api/boards/${id}`);
    return response.data;
  },

  createColumn: async (boardId, name) => {
    const response = await api.post(`/api/boards/${boardId}/columns`, { name });
    return response.data;
  },
  
  createCard: async (boardId, columnId, title) => {
    const response = await api.post(`/api/boards/${boardId}/cards`, { 
      columnId: columnId, 
      title: title, 
      description: '' 
    });
    return response.data;
  },

  moveCard: async (boardId, cardId, columnId, title, description) => {
    const response = await api.put(`/api/boards/${boardId}/cards/${cardId}`, { 
      columnId: columnId, 
      title: title, 
      description: description || '' 
    });
    return response.data;
  }
};