<script setup lang="ts">
import { ref } from "vue";
import { useWebSocket } from "../utils/websocket";

const { wsUrl, isConnected, connect, disconnect, send } = useWebSocket();

const testText = ref('{"plugin": "ExamplePlugin", "method": "add", "parameters": [100, 200]}');
const result = ref("");

// 报表相关
const reportType = ref("SimpleList");
const previewImage = ref("");
const reportTypes = ["SimpleList", "Group"];

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

// 生成报表
const generateReport = () => {
  const cmd = {
    type: "invoke",
    plugin: "ReportPlugin",
    method: "GenerateReport",
    params: [reportType.value, "pdf"],
  };
  testText.value = JSON.stringify(cmd);
  sendCommand();
};

// 打印报表
const printReport = () => {
  const cmd = {
    type: "invoke",
    plugin: "ReportPlugin",
    method: "PrintReport",
    params: [reportType.value],
  };
  testText.value = JSON.stringify(cmd);
  sendCommand();
};

// 预览报表
const previewReport = () => {
  const cmd = {
    type: "invoke",
    plugin: "ReportPlugin",
    method: "PreviewReport",
    params: [reportType.value, "png"],
  };
  testText.value = JSON.stringify(cmd);
  connect((data) => {
    result.value = data;
    try {
      const res = JSON.parse(data);
      if (res.result && res.result.startsWith("data:image")) {
        previewImage.value = res.result;
      }
    } catch (e) {
      console.error("解析预览结果失败", e);
    }
  });
  sendCommand();
};

// 获取配置
const configureReport = () => {
  const cmd = {
    type: "invoke",
    plugin: "ReportPlugin",
    method: "ConfigureReport",
    params: [reportType.value],
  };
  testText.value = JSON.stringify(cmd);
  sendCommand();
};

// 获取报表类型
const getReportTypes = () => {
  const cmd = {
    type: "invoke",
    plugin: "ReportPlugin",
    method: "GetReportTypes",
    params: [],
  };
  testText.value = JSON.stringify(cmd);
  sendCommand();
};

const count = ref(0);
</script>

<template>
  <section id="center">
    <button type="button" class="counter" @click="count++">Count is {{ count }}</button>
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

    <h3>报表测试</h3>
    <div style="margin: 10px 0">
      <select v-model="reportType" style="padding: 5px; margin-right: 10px; width: 140px">
        <option v-for="t in reportTypes" :key="t" :value="t">{{ t }}</option>
      </select>
    </div>
    <div style="margin: 10px 0">
      <button type="button" class="counter" @click="generateReport">生成报表</button>
      <button type="button" class="counter" @click="printReport">打印报表</button>
      <button type="button" class="counter" @click="previewReport">预览报表</button>
      <button type="button" class="counter" @click="configureReport">获取配置</button>
      <button type="button" class="counter" @click="getReportTypes">获取报表类型</button>
    </div>

    <div v-if="previewImage" style="margin: 10px 0">
      <h4>预览效果</h4>
      <img :src="previewImage" alt="报表预览" style="max-width: 80%; border: 1px solid #ccc" />
    </div>

    <hr style="margin: 20px 0; width: 60%" />

    <h3>通用命令测试</h3>
    <input type="text" placeholder="测试文本" v-model="testText" style="width: 60%" />
    <button type="button" class="counter" @click="sendCommand">发送命令</button>
    <div style="margin-top: 10px; width: 60%">
      <textarea readonly v-model="result" rows="4" style="width: 80%"></textarea>
    </div>
  </section>

  <div class="ticks"></div>
  <section id="spacer"></section>
</template>
