import { authAtom } from "../../core/atoms/authAtom";
import { useSse } from "../../core/hooks/useSse";
import { useAtom } from "jotai";
import { useEffect } from "react";

const SseConnection: React.FC = () => {
const [auth] = useAtom(authAtom);

  const sse = useSse({
    url: "/api/chat/stream",
    onMessage: (event) => {
      console.log("Received SSE message:", event.data);
      // Here you would typically update your state with the new incident data
    },
    onError: (error) => {
      console.error("SSE error:", error);
    },
  });

  useEffect(() => {
    if(auth.status === "authenticated") {
      sse.reconnect();
    }else {
      sse.close();
    }
  }, [auth.status]);
  

  return (
    <div>

    </div>
  );
};

export default SseConnection;