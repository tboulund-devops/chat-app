import { atom } from "jotai";
import type {ChatRoom} from "../types/ChatRoom";

export const chatRoomsAtom = atom<ChatRoom[]>([]);
chatRoomsAtom.debugLabel = 'Chat Rooms Atom';

export const allchatRoomsAtom = atom<ChatRoom[]>([]);
allchatRoomsAtom.debugLabel = 'All Chat Rooms Atom';