const API_BASE_URL = 'http://localhost:5007/api/forum';

export const forumService = {
  async getThreads(category = 'all', search = '') {
    try {
      const url = new URL(`${API_BASE_URL}/threads`);
      if (category !== 'all') url.searchParams.append('category', category);
      if (search) url.searchParams.append('search', search);

      console.log('🔍 Fetching threads from:', url.toString());
      
      const response = await fetch(url);
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      const data = await response.json();
      console.log('✅ Threads loaded:', data.length);
      return data;
    } catch (error) {
      console.error('❌ Error fetching threads:', error);
      throw error;
    }
  },

  // ... el resto de los métodos permanece igual
  async getThread(id) {
    const response = await fetch(`${API_BASE_URL}/threads/${id}`);
    if (!response.ok) throw new Error('Error fetching thread');
    return await response.json();
  },

  async createThread(threadData) {
    const response = await fetch(`${API_BASE_URL}/threads`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(threadData)
    });
    if (!response.ok) throw new Error('Error creating thread');
    return await response.json();
  },

  async createReply(replyData) {
    const response = await fetch(`${API_BASE_URL}/replies`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(replyData)
    });
    if (!response.ok) throw new Error('Error creating reply');
    return await response.json();
  },

  async getCategories() {
    const response = await fetch(`${API_BASE_URL}/categories`);
    if (!response.ok) throw new Error('Error fetching categories');
    return await response.json();
  }
};
