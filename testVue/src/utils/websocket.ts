import { ref } from "vue";

const wsUrl = ref("ws://localhost:8761");
const isConnected = ref(false);
let ws: WebSocket | null = null;
let messageCallback: ((data: string) => void) | null = null;

export function useWebSocket() {
  const connect = (onMessage?: (data: string) => void) => {
    if (ws) {
      ws.close();
    }

    messageCallback = onMessage || null;
    ws = new WebSocket(wsUrl.value);

    ws.onopen = () => {
      isConnected.value = true;
    };

    ws.onmessage = (event) => {
      if (messageCallback) {
        messageCallback(event.data);
      }
    };

    ws.onerror = () => {
      isConnected.value = false;
    };

    ws.onclose = () => {
      isConnected.value = false;
    };
  };

  const disconnect = () => {
    if (ws) {
      ws.close();
      ws = null;
    }
  };

  const send = (data: string) => {
    if (ws && ws.readyState === WebSocket.OPEN) {
      ws.send(data);
      return true;
    }
    return false;
  };

  return {
    wsUrl,
    isConnected,
    connect,
    disconnect,
    send,
  };
}