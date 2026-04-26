<script setup lang="ts">
import { ref, onUnmounted } from "vue";

const wsUrl = ref("ws://localhost:8761");
const testText = ref('{"plugin": "ExamplePlugin", "method": "Execute", "parameters": ["World"]}');
const result = ref("");
const isConnected = ref(false);
let ws: WebSocket | null = null;

const connect = () => {
  if (ws) {
    ws.close();
  }

  result.value = "连接中...";
  ws = new WebSocket(wsUrl.value);

  ws.onopen = () => {
    isConnected.value = true;
    result.value = "已连接";
  };

  ws.onmessage = (event) => {
    result.value = event.data;
  };

  ws.onerror = () => {
    result.value = "连接错误";
    isConnected.value = false;
  };

  ws.onclose = () => {
    isConnected.value = false;
    result.value = "已断开";
  };
};

const sendCommand = () => {
  if (ws && ws.readyState === WebSocket.OPEN) {
    ws.send(testText.value);
  } else {
    result.value = "请先连接";
  }
};

const disconnect = () => {
  if (ws) {
    ws.close();
    ws = null;
  }
};

onUnmounted(() => {
  if (ws) {
    ws.close();
  }
});

const count = ref(0);
</script>

<template>
  <section id="center">
    <button type="button" class="counter" @click="count++">Count is {{ count }}</button>
    <div style="margin: 10px 0">
      <input type="text" v-model="wsUrl" placeholder="WebSocket URL" style="width: 40%" />
      <button type="button" class="counter" @click="connect" :disabled="isConnected">
        {{ isConnected ? "已连接" : "连接" }}
      </button>
      <button type="button" class="counter" @click="disconnect" :disabled="!isConnected">
        断开
      </button>
    </div>
    <input type="text" placeholder="测试文本" v-model="testText" style="width: 60%" />
    <button type="button" class="counter" @click="sendCommand">发送命令</button>
    <div style="margin-top: 10px; width: 60%">
      <textarea readonly v-model="result" rows="4" style="width: 80%"></textarea>
    </div>
  </section>

  <div class="ticks"></div>
  <section id="spacer"></section>
</template>
