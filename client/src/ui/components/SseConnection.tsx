import { useEffect } from "react";
import { useAtomValue, useSetAtom } from "jotai";
import { authAtom } from "../../core/atoms/authAtom";
import { notificationsAtom } from "../../core/atoms/notificationAtom";
import { allchatRoomsAtom } from "../../core/atoms/chatAtoms";
import { useSse } from "../../core/hooks/useSse";
import { notificationApi } from "../../core/controllers/notificationApi";
import type { Notification } from "../../core/types/Notification";
import { resolveNotificationDisplay } from "../../utils/resolveNotificationDisplay";

const SseConnection: React.FC = () => {
  const auth = useAtomValue(authAtom);
  const setNotifications = useSetAtom(notificationsAtom);
  const allRooms = useAtomValue(allchatRoomsAtom);

  const getRoomName = (roomId: string) =>
      allRooms.find((r) => r.id === roomId)?.name;

  // Initial load of persisted unread notifications
  useEffect(() => {
    if (auth.status !== "authenticated") return;
    notificationApi.getUnread()
        .then(async (raw) => {
          const resolved = await Promise.all(
              raw.map((n) => resolveNotificationDisplay(n, getRoomName))
          );
          setNotifications(resolved);
        })
        .catch(console.error);
  }, [auth.status]);

  const sse = useSse({
    url: "/api/chat/stream",
    onError: (error) => console.error("SSE error:", error),
    events: {
      ping: () => {},
      connected: (event) => console.log("SSE connected:", event.data),

      notification: async (event) => {
        const data = JSON.parse(event.data);

        const raw: Notification = {
          id: data.notificationId,
          type: data.type === "poke" ? 0 : 1,
          payload: event.data,
          isRead: false,
          createdAt: data.createdAt ?? new Date().toISOString(),
          displayTitle: "",
        };

        const resolved = await resolveNotificationDisplay(raw, getRoomName);
        setNotifications((prev) => [resolved, ...prev]);
      },
    },
  });

  useEffect(() => {
    if (auth.status === "authenticated") {
      sse.connect();
    } else {
      sse.close();
    }
  }, [auth.status]);

  return null;
};

export default SseConnection;