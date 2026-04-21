import api from './api.js';

export const authService = {
  login: async (email, password) => {
    const response = await api.post('/login', { email, password });
    // .NET Identity API zwraca token w polu accessToken
    localStorage.setItem('token', response.data.accessToken);
    return response.data;
  },

  register: async (email, password) => {
    const response = await api.post('/register', { email, password });
    return response.data;
  },

  logout: () => {
    localStorage.removeItem('token');
  }
};