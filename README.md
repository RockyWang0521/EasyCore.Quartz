

# EasyCore.Quartz 项目说明

## 简介

`EasyCore.Quartz` 是一个基于 Quartz 的任务调度框架封装，旨在简化在 .NET 项目中使用定时任务的流程。它支持通过 HTTP 请求触发任务，并提供对 MySQL 和 SQL Server 的数据库配置支持。

该项目适合希望快速集成定时任务调度功能的开发者，且支持通过简单的 API 管理任务的创建、暂停、恢复和删除。

## 特性

- 支持通过 HTTP 请求执行定时任务
- 提供任务的动态管理：添加、更新、暂停、恢复和删除
- 支持多种数据库类型（MySQL、SQL Server）进行任务存储
- 支持 Cron 表达式配置定时任务
- 简单易用的 API 控制器进行任务管理
- 支持任务并发控制和错误处理

## 安装与配置

### 1. 项目结构

- `src/EasyCore.Quartz`: 核心库代码，包含 Quartz 的封装逻辑
- `src/EasyCore.Quartz.MySql` 和 `src/EasyCore.Quartz.SqlServer`: 分别用于 MySQL 和 SQL Server 的数据库支持
- `demo/WebApp.Quartz`: 示例 Web 应用，展示如何使用 EasyCore.Quartz

### 2. 配置数据库支持

在 `QuarztOptions` 中，可以使用以下方法设置数据库：

```csharp
options.SetSqlServer("your_sqlserver_connection_string");
options.SetMySql("your_mysql_connection_string");
```

### 3. 配置 Cron 表达式

通过 `EasyCoreCronAttribute` 特性为任务设置 Cron 表达式，如：

```csharp
[EasyCoreCron("0/1 * * * * ?")]
public class QuarztTask : IEasyCoreJob
```

## 使用示例

### 创建一个定时任务

```csharp
[EasyCoreCron("0/1 * * * * ?")]
public class QuarztTask : IEasyCoreJob
{
    private readonly ILogger<QuarztTask> _logger;

    public QuarztTask(ILogger<QuarztTask> logger) => _logger = logger;

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("定时任务执行中");
    }
}
```

### 使用 API 管理任务

`QuarztController` 提供了 RESTful API，可以执行以下操作：

- 获取所有任务列表
- 暂停和恢复任务
- 更新任务的 Cron 表达式
- 手动触发任务
- 添加或更新 HTTP 任务

示例：添加一个 HTTP 任务

```http
POST /quarzt/addorupdate/httpjob
{
    "JobName": "HttpJob",
    "Url": "http://example.com/api/endpoint",
    "Method": "POST",
    "Cron": "0/1 * * * * ?"
}
```

### 禁用任务

通过 `EasyCoreDisableJobAttribute` 可以禁用特定任务：

```csharp
[EasyCoreDisableJob]
public class DisableJobTask : IEasyCoreJob
```

## API 文档

### 获取所有任务

```
GET /quarzt/get/all/jobs
```

### 暂停任务

```
PUT /quarzt/pause/job?jobName=JobName
```

### 恢复任务

```
PUT /quarzt/resume/job?jobName=JobName
```

### 更新任务 Cron

```
PUT /quarzt/update/cron?jobName=JobName&newCron=0/10 * * * * ?
```

### 删除任务

```
DELETE /quarzt/delete/job?jobName=JobName
```

### 手动触发任务

```
POST /quarzt/manualtrigger/job?jobName=JobName
```

### 添加/更新 HTTP 任务

```
POST /quarzt/addorupdate/httpjob
{
    "JobName": "MyHttpJob",
    "Url": "http://your-endpoint.com",
    "Method": "GET",
    "Cron": "0/5 * * * * ?"
}
```

## 贡献指南

欢迎提交 Issue 和 Pull Request。请确保代码风格与现有代码一致，并提供完整的单元测试。

## 许可证

本项目采用 MIT 许可证。详情请参阅 [LICENSE](LICENSE) 文件。