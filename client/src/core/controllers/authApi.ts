import { User } from "../types/User";
import { api } from "../../utils/api";

const endpoint = "/api/auth";

export const authApi = { 
    login: async (email: string, password: string): Promise<User> => {
        try {
            const user = await api(`${endpoint}/login`, {
                init: {
                    method: 'POST',
                    body: JSON.stringify({ email, password }),
                },
            }) as User;
            console.log('Login successful:', user);
            return user;
        } catch (err: any) {
            console.error('Login failed:', err);
            throw err;
        }
    },

    logout: async (): Promise<void> => {
        try {
            await api(`${endpoint}/logout`, {
                init: {
                    method: 'POST',
                },
            });
            console.log('Logout successful');
        } catch (err: any) {
            console.error('Logout failed:', err);
            throw err;
        }
    },

    me: async (): Promise<User> => {
        try {
            const user = await api(`${endpoint}/me`, {
                init: {
                    method: 'GET',
                },
            }) as User;
            console.log('Fetched current user:', user);
            return user;
        } catch (err: any) {
            console.error('Failed to fetch current user:', err);
            throw err;
        }
    }
};