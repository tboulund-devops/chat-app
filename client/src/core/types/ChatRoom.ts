import {ChatMessage} from "../../core/types/ChatMessage";
import {User} from "../../core/types/User";

export type ChatRoom = {
    id: string;
    name: string;
    owner: User;
    description?: string;
    messages: ChatMessage[];
}