import { useRef, useState, useCallback } from "react";

export type SseStatus = "idle" | "connecting" | "connected" | "error" | "closed";

type SseEventHandlers = {
  [eventName: string]: (event: MessageEvent) => void;
};

interface UseSseOptions {
  url: string;
  onOpen?: () => void;
  onError?: (error: Event) => void;
  events?: SseEventHandlers;
  eventNames: string[]; // explicit list so listeners are registered reliably
  onUnknownEvent?: (eventName: string, event: MessageEvent) => void; // NEW
}

export function useSse({ url, onOpen, onError, events = {}, eventNames }: UseSseOptions) {
  const [status, setStatus] = useState<SseStatus>("idle");
  const eventSourceRef = useRef<EventSource | null>(null);
  const eventsRef = useRef<SseEventHandlers>(events);
  eventsRef.current = events; // always up to date, no stale closures

  const reconnect = useCallback(() => {
    // Close existing connection inline to avoid stale ref issues
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
      eventSourceRef.current = null;
      setStatus("closed");
    }

    setStatus("connecting");
    const es = new EventSource(url, { withCredentials: true });
    eventSourceRef.current = es;

    es.onopen = () => {
      setStatus("connected");
      onOpen?.();
    };

    es.onerror = (error) => {
      setStatus("error");
      onError?.(error);
      if (es.readyState === EventSource.CLOSED) setStatus("closed");
    };

    // Use explicit eventNames so listeners are always registered
    // regardless of when eventsRef gets populated
    console.log("registering event listeners:", eventNames);
    eventNames.forEach((eventName) => {
      es.addEventListener(eventName, (event) => {
        console.log(`[useSse] fired: ${eventName}`);  // add this
        eventsRef.current[eventName]?.(event as MessageEvent);
      });
    });
  }, [url]);

  const close = useCallback(() => {
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
      eventSourceRef.current = null;
      setStatus("closed");
    }
  }, []);

  return { status, close, reconnect };
}