<script setup lang="ts">
import { ref } from "vue";
import { useWebSocket } from "../utils/websocket";

const { wsUrl, isConnected, connect, disconnect, send } = useWebSocket();

const testText = ref('{"plugin": "ExamplePlugin", "method": "add", "parameters": [100, 200]}');
const result = ref("");

const handleConnect = () => {
  result.value = "连接中...";
  connect((data) => {
    result.value = data;
  });
};

const sendCommand = () => {
  if (!send(testText.value)) {
    result.value = "请先连接";
  }
};

const count = ref(0);
</script>

<template>
  <section id="center">
    <button type="button" class="counter" @click="count++">Count is {{ count }}</button>
    <div style="margin: 10px 0">
      <input type="text" v-model="wsUrl" placeholder="WebSocket URL" style="width: 40%" />
      <button type="button" class="counter" @click="handleConnect" :disabled="isConnected">
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
