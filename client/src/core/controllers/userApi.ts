import { api } from "../../utils/api";
import type { User } from "../types/User";

export const userApi = {
    getById: async (userId: string): Promise<User> => {
        return await api<User>(`/api/users/${userId}`);
    },
};