import { atom } from 'jotai';
import type { User } from '../types/User';

export type AuthState =
  | { status: "unknown" }
  | { status: "authenticated"; user: User }
  | { status: "unauthenticated" }

export const authAtom = atom<AuthState>({
  status: "unknown"
})
authAtom.debugLabel = 'Authentication State Atom';