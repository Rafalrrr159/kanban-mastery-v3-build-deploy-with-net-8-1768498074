import api from './api.js';

export const boardService = {
  getBoards: async () => {
    const response = await api.get('/api/boards');
    return response.data;
  },

  createBoard: async (name) => {
    const response = await api.post('/api/boards', { name });
    return response.data;
  }
};