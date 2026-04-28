# 需求

## 产生BUG

* 当函数声明为：

```C#
public string GenerateReport(string reportName, string format = "pdf", string? outputPath = null)
```

* 调用参数为GenerateReport("Test1", "pdf")时出错

```
找不到函数
```

## 建议

* 修改PluginManager.Invoke模块，if (pis.Length != (parameters?.Length ?? 0)) continue;这个处理方法

