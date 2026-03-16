import { User } from "../types/User";
import { api } from "../../utils/api";

const endpoint = "/api/auth";
export const authApi = { 
    login: async (email: string, password: string): Promise<User> => {
        return await api(`${endpoint}/login`, {
            init: {
                method: 'POST',
                body: JSON.stringify({ email, password }),
            },
        }).then((value) => {
            const user = value as User;
            console.log('Login successful:', user);
            return user;
        }).catch((err: any) => {
            console.error('Login failed:', err);
            throw new Error(err.message || 'Login failed');
        });
    },

    logout: async (): Promise<void> => {
        await api(`${endpoint}/logout`, {
            init: {
                method: 'POST',
            },
        }).then(() => {
            console.log('Logout successful');
        }).catch((err: any) => {
            console.error('Logout failed:', err);
            throw new Error(err.message || 'Logout failed');
        });
    },

    me: async (): Promise<User> => {
        return await api(`${endpoint}/me`, {
            init: {
                method: 'GET',
            },
        }).then((value) => {
            const user = value as User;
            console.log('Fetched current user:', user);
            return user;
        }).catch((err: any) => {
            console.error('Failed to fetch current user:', err);
            throw new Error(err.message || 'Failed to fetch current user');
        });
    }
};