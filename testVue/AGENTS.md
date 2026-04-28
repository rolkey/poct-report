# testVue

**前端：** Vue 3 + TypeScript + Vite WebSocket 客户端，用于与 TrayApp 交互。

## 目录结构
```
testVue/
├── src/
│   ├── main.ts              # Vue 应用入口
│   ├── App.vue              # 根组件
│   ├── components/
│   │   └── HelloWorld.vue   # 主界面：WS 连接、报表命令
│   ├── utils/
│   │   └── websocket.ts     # WebSocket 组合式函数 (useWebSocket)
│   ├── style.css            # 全局样式（亮色/暗色）
│   └── assets/              # 静态图片
├── index.html
├── vite.config.ts
└── package.json
```

## 关键代码索引
| 关注点 | 文件 | 关键 |
|--------|------|------|
| WS 连接 | `src/utils/websocket.ts` | `useWebSocket()` 组合式函数 |
| 报表界面 | `src/components/HelloWorld.vue` | 生成/预览/打印/配置 |
| 命令格式 | `src/components/HelloWorld.vue:29-97` | JSON 载荷构造 |

## 约定
- `<script setup lang="ts">` SFC 风格
- 组合式函数模式管理共享状态（`useWebSocket`）
- WS 消息格式：`{"type":"invoke","plugin":"...","method":"...","params":[...]}`
- 开发服务器：`npm run dev`（Vite）

## 反模式（禁止）
- 不要使用 Options API — 仅使用 Composition API + `<script setup>`
- 除非需要多页面，否则不要添加路由（当前为单页应用）
