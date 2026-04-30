<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from "vue";
import { useWebSocket } from "../utils/websocket";

const { wsUrl, isConnected, connect, disconnect, send, onMessage, offMessage } = useWebSocket();

const result = ref("");
const reportType = ref("Simple List.frx");
const reportTypes = [
  "Simple List.frx",
  "Master-Detail.frx",
  "Barcode.frx",
  "Groups.frx",
  "Simple Matrix.frx",
];

// 预览弹窗
const showPreview = ref(false);
const previewPdfData = ref("");

const msgFunc = (data) => {
  result.value = data;
  try {
    const res = JSON.parse(data);
    if (res.command === "generateReport") {
      // result.value 已在上面赋值，无需额外处理
    } else if (res.command === "previewReport") {
      // 假设你的 base64 字符串（不含 data: 前缀）
      const base64String = res.data;

      // 转为二进制
      const binaryString = atob(base64String);
      const bytes = new Uint8Array(binaryString.length);
      for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
      }

      // 创建 Blob URL
      const blob = new Blob([bytes], { type: "application/pdf" });
      const blobUrl = URL.createObjectURL(blob);

      // 设置到 iframe
      // document.querySelector("iframe").src = blobUrl;

      previewPdfData.value = blobUrl;
      showPreview.value = true;
    }
  } catch {
    // 非 JSON 或非预览数据，忽略
  }
};

const handleConnect = () => {
  result.value = "连接中...";
  connect();
};

const sendCommand = (cmd: object) => {
  const json = JSON.stringify(cmd);
  if (!send(json)) {
    result.value = "请先连接";
  }
};

// 生成报表
const generateReport = () => {
  const cmd = {
    command: "generateReport",
    plugin: "ReportPlugin",
    method: "GenerateReport",
    params: [reportType.value, "pdf"],
  };
  sendCommand(cmd);
};

// 预览报表（生成PDF并在弹窗中展示）
const previewReport = () => {
  const cmd = {
    command: "previewReport",
    plugin: "ReportPlugin",
    method: "PreviewReport",
    params: [reportType.value],
  };
  sendCommand(cmd);
};

// 设计报表
const designReport = () => {
  const cmd = {
    command: "designReport",
    plugin: "ReportPlugin",
    method: "DesignReport",
    params: [reportType.value],
  };
  sendCommand(cmd);
};

const closePreview = () => {
  showPreview.value = false;
  previewPdfData.value = "";
};

onMounted(() => {
  onMessage(msgFunc);
});

onBeforeUnmount(() => {
  offMessage(msgFunc);
});
</script>

<template>
  <section id="center">
    <div style="margin: 10px 0">
      <input type="text" v-model="wsUrl" placeholder="WebSocket URL" style="width: 180px" />
      <button type="button" class="counter" @click="handleConnect" :disabled="isConnected">
        {{ isConnected ? "已连接" : "连接" }}
      </button>
      <button type="button" class="counter" @click="disconnect" :disabled="!isConnected">
        断开
      </button>
    </div>

    <hr style="margin: 20px 0; width: 60%" />

    <h3>报表操作</h3>
    <div style="margin: 10px 0">
      <select v-model="reportType" style="padding: 5px; margin-right: 10px; width: 160px">
        <option v-for="t in reportTypes" :key="t" :value="t">{{ t }}</option>
      </select>
    </div>
    <div style="margin: 10px 0; display: flex; gap: 10px; justify-content: center">
      <button type="button" class="counter" @click="generateReport">生成报表</button>
      <button type="button" class="counter" @click="previewReport">预览报表</button>
      <button type="button" class="counter" @click="designReport">设计报表</button>
    </div>

    <hr style="margin: 20px 0; width: 60%" />

    <div style="margin-top: 10px; width: 60%">
      <textarea readonly v-model="result" rows="6" style="width: 80%"></textarea>
    </div>
  </section>

  <!-- PDF 预览弹窗 -->
  <div v-if="showPreview" class="modal-overlay" @click.self="closePreview">
    <div class="modal-content">
      <div class="modal-header">
        <h3>报表预览 - {{ reportType }}</h3>
        <button type="button" class="counter" @click="closePreview">关闭</button>
      </div>
      <div class="modal-body">
        <iframe
          v-if="showPreview"
          :src="previewPdfData"
          style="width: 100%; height: 100%; border: none"
        ></iframe>
      </div>
    </div>
  </div>

  <div class="ticks"></div>
  <section id="spacer"></section>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  background: var(--bg-color, #fff);
  border-radius: 8px;
  width: 90%;
  height: 90%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 20px;
  border-bottom: 1px solid var(--border-color, #ccc);
}

.modal-body {
  flex: 1;
  padding: 10px;
  overflow: hidden;
}
</style>
