import type { ChatRoom } from '../types/ChatRoom'
import type { ChatMessage } from '../types/ChatMessage'
import { api } from "../../utils/api";
import {RoomMember} from "../types/RoomMember";

const endpoint = '/api/chat';

export const chatApi = {
  sendMessage: async (roomId: string, content: string): Promise<void> => {
    return await api(`${endpoint}/messages`, {
      init: {
        method: 'POST',
        body: JSON.stringify({ roomId, content }),
      },
    }).then(() => {
      console.log('Message sent successfully', roomId, content);
    }).catch((err: any) => {
      console.error('Failed to send message:', err);
      throw new Error(err.message || 'Failed to send message');
    });
  },

  getMyRooms: async (): Promise<ChatRoom[]> => {
    return await api(`${endpoint}/my-rooms`).then((value) => {
      const rooms = value as ChatRoom[];
      console.log('Fetched chat rooms:', rooms);
      return rooms;
    }).catch((err: any) => {
      console.error('Failed to fetch chat rooms:', err);
      throw new Error(err.message || 'Failed to fetch chat rooms');
    });
  },

  searchRooms: async (name: string): Promise<ChatRoom[]> => {
    return await api(`${endpoint}/rooms/search?name=${encodeURIComponent(name)}`).then((value) => {
      const rooms = value as ChatRoom[];
      console.log('Fetched search results:', rooms);
      return rooms;
    }).catch((err: any) => {
      console.error('Failed to search chat rooms:', err);
      throw new Error(err.message || 'Failed to search chat rooms');
    });
  },

  // Additional chat-related API methods can be added here
  getAllRooms: async (): Promise<ChatRoom[]> => {
    return await api(`${endpoint}/get-all-rooms`).then((value) => {
      const rooms = value as ChatRoom[];
      console.log('Fetched all rooms:', rooms);
      return rooms;
    }).catch((err: any) => {
      console.error('Failed to fetch all rooms:', err);
      throw new Error(err.message || 'Failed to fetch all rooms');
    });
  },

  joinRoom: async (roomId: string) => {
    return api(`/api/chat/rooms/${roomId}/join`, { init: { method: "POST" }});
  },
  leaveRoom: async (roomId: string) => {
    return api(`/api/chat/rooms/${roomId}/leave`, { init: { method: "POST" }});
  },

  getRoomMessages: async (roomId: string): Promise<ChatMessage[]> => {
    const result = await api<any>(`${endpoint}/rooms/${roomId}/messages`)
    return (result?.dto ?? result) as ChatMessage[]
  },

  editMessage: async (messageId: string, newContent: string): Promise<void> => {
    await api(`${endpoint}/messages/${messageId}`, {
      init: {
        method: 'PATCH',
        body: JSON.stringify({ newContent }),
      },
    })
  },

  deleteMessage: async (messageId: string): Promise<void> => {
    await api(`${endpoint}/messages/${messageId}`, {
      init: { method: 'DELETE' },
    })
  },

  getRoomMembers: async (roomId: string): Promise<RoomMember[]> => {
    return await api<RoomMember[]>(`${endpoint}/rooms/${roomId}/members`);
  },
};