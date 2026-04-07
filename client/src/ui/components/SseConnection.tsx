// src/ui/components/SseConnection.tsx
import { useEffect } from "react";
import { useAtomValue, useSetAtom } from "jotai";
import { authAtom } from "../../core/atoms/authAtom";
import { notificationsAtom } from "../../core/atoms/notificationAtom";
import { useSse } from "../../core/hooks/useSse";
import { notificationApi } from "../../core/controllers/notificationApi";
import type { Notification } from "../../core/types/Notification";
import { resolveNotificationDisplay } from "../../utils/resolveNotificationDisplay";

const SseConnection: React.FC = () => {
  const auth = useAtomValue(authAtom);
  const setNotifications = useSetAtom(notificationsAtom);

  // Load persisted unread notifications on login
  useEffect(() => {
    if (auth.status !== "authenticated") return;
    notificationApi.getUnread()
        .then((raw) => setNotifications(raw.map(resolveNotificationDisplay)))
        .catch(console.error);
  }, [auth.status]);

  const sse = useSse({
    url: "/api/chat/stream",
    eventNames: ["ping", "connected", "notification"],
    onError: (error) => console.error("SSE error:", error),
    events: {
      ping: () => {},
      connected: (event) => console.log("SSE connected:", event.data),
      notification: (event) => {
        const data = JSON.parse(event.data);
        const raw: Notification = {
          id: data.notificationId,
          type: data.type === "poke" ? 0 : 1,
          payload: event.data,
          isRead: false,
          createdAt: data.createdAt ?? new Date().toISOString(),
          displayTitle: "",
        };
        setNotifications((prev) => [resolveNotificationDisplay(raw), ...prev]);
      },
    },
  });

  useEffect(() => {
    if (auth.status === "authenticated") {
      sse.reconnect();
    } else if (auth.status === "unauthenticated") {
      sse.close();
    }
  }, [auth.status]);

  return null;
};

export default SseConnection;