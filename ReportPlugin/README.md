# 补充

## 安装组件

```powershell
$env:HTTP_PROXY = "http://192.168.168.128:8118"
$env:HTTPS_PROXY = "http://192.168.168.128:8118"
dotnet add package Microsoft.CodeAnalysis --version 4.0.1
dotnet add package Microsoft.Chart.Controls --version 4.7.2046
```

## 完整打包

```bash
dotnet publish -c Release -o ..\TrayApp\plugins
```
