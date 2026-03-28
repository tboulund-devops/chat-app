import { atom } from "jotai";
import type {ChatRoom} from "../types/ChatRoom";
import {ChatMessage} from "../types/ChatMessage";
export const incomingMessageAtom = atom<{ roomId: string; message: ChatMessage } | null>(null);

export const chatRoomsAtom = atom<ChatRoom[]>([]);
chatRoomsAtom.debugLabel = 'Chat Rooms Atom';

export const allchatRoomsAtom = atom<ChatRoom[]>([]);
allchatRoomsAtom.debugLabel = 'All Chat Rooms Atom';