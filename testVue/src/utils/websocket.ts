import { ref, onUnmounted } from "vue";

const wsUrl = ref("ws://localhost:8761");
const isConnected = ref(false);
let ws: WebSocket | null = null;
const messageCallbacks: Set<(data: string) => void> = new Set();

export function useWebSocket() {
  const connect = () => {
    if (ws) {
      ws.close();
    }

    ws = new WebSocket(wsUrl.value);

    ws.onopen = () => {
      isConnected.value = true;
    };

    ws.onmessage = (event) => {
      for (const cb of messageCallbacks) {
        cb(event.data);
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

  const onMessage = (cb: (data: string) => void) => {
    messageCallbacks.add(cb);
    // 组件卸载时自动取消注册
    onUnmounted(() => {
      messageCallbacks.delete(cb);
    });
  };

  const offMessage = (cb: (data: string) => void) => {
    messageCallbacks.delete(cb);
  };

  return {
    wsUrl,
    isConnected,
    connect,
    disconnect,
    send,
    onMessage,
    offMessage,
  };
}
