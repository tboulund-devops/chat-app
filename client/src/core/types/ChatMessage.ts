import {User} from "../../core/types/User";

export type ChatMessage = {
    id: string;
    roomId: string;
    sender: User;
    content: string;
    createdAt: string;
    isDeleted: boolean;
};