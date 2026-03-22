import type { ChatRoom } from '../types/ChatRoom'
import type { ChatMessage } from '../types/ChatMessage'
import { api } from "../../utils/api";

const endpoint = '/api/chat';

export const chatApi = {
  sendMessage: async (roomId: string, content: string): Promise<void> => {
    try {
      await api(`${endpoint}/messages`, {
        init: {
          method: 'POST',
          body: JSON.stringify({ roomId, content }),
        },
      });
      console.log('Message sent successfully', roomId, content);
    } catch (err: any) {
      console.error('Failed to send message:', err);
      throw err;
    }
  },

  getMyRooms: async (): Promise<ChatRoom[]> => {
    try {
      const rooms = await api(`${endpoint}/my-rooms`) as ChatRoom[];
      console.log('Fetched chat rooms:', rooms);
      return rooms;
    } catch (err: any) {
      console.error('Failed to fetch chat rooms:', err);
      throw err;
    }
  },

  searchRooms: async (name: string): Promise<ChatRoom[]> => {
    try {
      const rooms = await api(`${endpoint}/rooms/search?name=${encodeURIComponent(name)}`) as ChatRoom[];
      console.log('Fetched search results:', rooms);
      return rooms;
    } catch (err: any) {
      console.error('Failed to search chat rooms:', err);
      throw err;
    }
  },

  getAllRooms: async (): Promise<ChatRoom[]> => {
    try {
      const rooms = await api(`${endpoint}/get-all-rooms`) as ChatRoom[];
      console.log('Fetched all rooms:', rooms);
      return rooms;
    } catch (err: any) {
      console.error('Failed to fetch all rooms:', err);
      throw err;
    }
  },

  joinRoom: async (roomId: string): Promise<void> => {
    await api(`/api/chat/rooms/${roomId}/join`, { init: { method: "POST" }});
  },

  leaveRoom: async (roomId: string): Promise<void> => {
    await api(`/api/chat/rooms/${roomId}/leave`, { init: { method: "POST" }});
  },

  getRoomMessages: async (roomId: string): Promise<ChatMessage[]> => {
    const result = await api<any>(`${endpoint}/rooms/${roomId}/messages`);
    return (result?.dto ?? result) as ChatMessage[];
  },
};