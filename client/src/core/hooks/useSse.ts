import { useRef, useState, useCallback } from "react";

export type SseStatus =
  | "idle"
  | "connecting"
  | "connected"
  | "error"
  | "closed";

interface UseSseOptions {
  url: string;
  withCredentials?: boolean;
  onMessage?: (event: MessageEvent) => void;
  onOpen?: () => void;
  onError?: (error: Event) => void;
}

export function useSse({
  url,
  onMessage,
  onOpen,
  onError,
}: UseSseOptions) {
  const [status, setStatus] = useState<SseStatus>("idle");
  const eventSourceRef = useRef<EventSource | null>(null);

  const connect = useCallback(() => {
    if (eventSourceRef.current) return;

    setStatus("connecting");

    const es = new EventSource(url, { withCredentials: true });

    eventSourceRef.current = es;

    es.onopen = () => {
      setStatus("connected");
      onOpen?.();
    };

    es.onmessage = (event) => {
      onMessage?.(event);
    };

    es.onerror = (error) => {
      setStatus("error");
      onError?.(error);

      if (es.readyState === EventSource.CLOSED) {
        setStatus("closed");
      }
    };
  }, [url, onMessage, onOpen, onError]);

  const close = useCallback(() => {
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
      eventSourceRef.current = null;
      setStatus("closed");
    }
  }, []);

  return {
    status,
    close,
    reconnect: () => {
      close();
      connect();
    },
  };
}