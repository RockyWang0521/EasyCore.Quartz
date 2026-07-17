# ⏱️ EasyCore.Quartz

> **EasyCore.Quartz** 是面向 .NET 8 的生产级任务调度封装库。基于 [Quartz.NET](https://www.quartz-scheduler.net/)，提供特性驱动任务、英文可视化 Dashboard、REST 管理接口、HTTP 动态任务，以及 MySQL / SQL Server / PostgreSQL / Oracle 持久化与集群能力。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![Quartz](https://img.shields.io/badge/Quartz.NET-3.14-orange)
![Dashboard](https://img.shields.io/badge/Dashboard-English-blueviolet)
![DB](https://img.shields.io/badge/DB-MySQL%20%7C%20SQLServer%20%7C%20PG%20%7C%20Oracle-green)
![License](https://img.shields.io/badge/License-MIT-yellow)
![Version](https://img.shields.io/badge/Version-8.0.0-blue)

---

## 🌍 Language

- **中文（当前文档）**
- English: [README.en.md](README.en.md)

---

## 📚 目录

### 第一部分：总览与架构
- [1. 项目定位](#1-项目定位)
- [2. 架构与模块关系](#2-架构与模块关系)
- [3. NuGet / 项目清单](#3-nuget--项目清单)
- [4. 数据库选型对比](#4-数据库选型对比)

### 第二部分：快速上手
- [5. 环境要求](#5-环境要求)
- [6. 安装](#6-安装)
- [7. 三分钟快速开始](#7-三分钟快速开始)
- [8. 特性与属性完整说明](#8-特性与属性完整说明)

### 第三部分：Dashboard · REST · HTTP Job
- [9. Dashboard（英文可视化面板）](#9-dashboard英文可视化面板)
- [10. REST API](#10-rest-api)
- [11. HTTP 动态任务](#11-http-动态任务)

### 第四部分：持久化与生产
- [12. 数据库配置详解](#12-数据库配置详解)
- [13. 集群与并发](#13-集群与并发)
- [14. Demo 项目](#14-demo-项目)
- [15. 从旧版迁移](#15-从旧版迁移)
- [16. 生产清单](#16-生产清单)
- [17. FAQ](#17-faq)
- [18. License](#18-license)

---

## 1. 项目定位

EasyCore.Quartz 解决「在 ASP.NET Core 里快速、安全、可运维地使用 Quartz」的问题：

| 痛点 | EasyCore.Quartz 做法 |
|---|---|
| 手写 Job 注册繁琐 | `IEasyCoreJob` + `[EasyCoreCron]` 自动发现 |
| 缺少可视化运维面 | 独立包 `EasyCore.Quartz.Dashboard`（`/easy-quartz`） |
| 管理接口分散 | 统一 `IJobManagementService`（Dashboard + REST） |
| 多库持久化麻烦 | MySQL / SQL Server / PostgreSQL / Oracle 独立包 |
| 异常被吞导致“假成功” | `JobWrapper` 记录后 **重新抛出** |
| 公网误暴露管理面 | Dashboard Basic Auth，账号密码必填 |

### 1.1 设计原则

| 原则 | 说明 |
|---|---|
| **低摩擦接入** | 一个扩展方法 + 一个特性即可跑通 |
| **运维可视化** | Dashboard 覆盖 Overview / Jobs / History 等完整页面 |
| **存储可插拔** | 核心与数据库包分离，按需引用 |
| **失败可感知** | 异常传播到 Quartz，History 记录成败 |
| **默认安全** | Dashboard 强制 Basic Auth（账号密码必填） |

### 1.2 解决方案目录

```text
EasyCore.Quartz/
├── src/
│   ├── EasyCore.Quartz/                 # 核心：发现、管理、REST、History
│   ├── EasyCore.Quartz.Dashboard/       # 英文可视化 Dashboard
│   ├── EasyCore.Quartz.MySql/
│   ├── EasyCore.Quartz.SqlServer/
│   ├── EasyCore.Quartz.PostgreSql/
│   └── EasyCore.Quartz.Oracle/
├── demo/
│   ├── WebApp.Quartz.InMemory/          # :5101 — 各自自带 SampleJob
│   ├── WebApp.Quartz.MySql/             # :5102
│   ├── WebApp.Quartz.SqlServer/         # :5103
│   ├── WebApp.Quartz.PostgreSql/        # :5104
│   └── WebApp.Quartz.Oracle/            # :5105
├── tests/EasyCore.Quartz.Tests/
└── docs/svg/                            # README 架构图
```

---

## 2. 架构与模块关系

### 2.1 组件关系图

![architecture-cn](https://gitee.com/wzhy-0521/easy-core.-quartz/tree/master/docs/svg/architecture-cn.svg)

### 2.2 任务生命周期

![sequence-cn](https://gitee.com/wzhy-0521/easy-core.-quartz/tree/master/docs/svg/sequence-cn.svg)

### 2.3 数据流（文字版）

```text
[EasyCoreCron Job]
       │
       ▼
 JobTypeDiscovery ──► JobWrapper<T> ──► Quartz Scheduler
       │                                      │
       │                                      ▼
       │                            JobExecutionHistoryListener
       │                                      │
       └──────── IJobManagementService ◄──────┘
                      │
           ┌──────────┴──────────┐
           ▼                     ▼
     Dashboard UI           REST api/quartz
```

---

## 3. NuGet / 项目清单

| 包名 | 职责 | 是否必须 |
|---|---|---|
| `EasyCore.Quartz` | 核心、REST、History | ✅ |
| `EasyCore.Quartz.Dashboard` | 英文可视化 Dashboard + Basic Auth | 可选 |
| `EasyCore.Quartz.MySql` | MySQL 持久化 + 自动建表 | 可选 |
| `EasyCore.Quartz.SqlServer` | SQL Server 持久化 + 自动建表 | 可选 |
| `EasyCore.Quartz.PostgreSql` | PostgreSQL 持久化 + 自动建表 | 可选 |
| `EasyCore.Quartz.Oracle` | Oracle 持久化 + 自动建表 | 可选 |

---

## 4. 数据库选型对比

| 能力 | In-Memory | MySQL | SQL Server | PostgreSQL | Oracle |
|---|---|---|---|---|---|
| 包 | 核心即可 | `.MySql` | `.SqlServer` | `.PostgreSql` | `.Oracle` |
| 持久化 | ❌ | ✅ | ✅ | ✅ | ✅ |
| 集群 | ❌ | ✅ | ✅ | ✅ | ✅ |
| AutoCreateSchema | — | ✅ | ✅ | ✅ | ✅ |
| 表前缀 | — | `QRTZ_` | `QRTZ_` | `QRTZ_` | `QRTZ_` |
| 典型场景 | 本地试用 | Linux 常见栈 | Windows / 企业库 | 云原生 / 开源栈 | 传统企业库 |

### 4.1 选型决策树

```text
是否需要跨重启保留任务 / 多节点？
├── 否 → In-Memory（WebApp.Quartz.InMemory）
└── 是 → 选已有数据库
        ├── MySQL / MariaDB → EasyCore.Quartz.MySql
        ├── SQL Server → EasyCore.Quartz.SqlServer
        ├── PostgreSQL → EasyCore.Quartz.PostgreSql
        └── Oracle → EasyCore.Quartz.Oracle
```

---

## 5. 环境要求

| 项 | 要求 |
|---|---|
| .NET | 8.0+ |
| 宿主 | ASP.NET Core（Web / API） |
| Quartz.NET | 3.14（由核心包引入） |
| 数据库 | 可选；持久化时需对应驱动可达 |

---

## 6. 安装

```bash
dotnet add package EasyCore.Quartz
dotnet add package EasyCore.Quartz.Dashboard

# 按需选择其一
dotnet add package EasyCore.Quartz.MySql
dotnet add package EasyCore.Quartz.SqlServer
dotnet add package EasyCore.Quartz.PostgreSql
dotnet add package EasyCore.Quartz.Oracle
```

---

## 7. 三分钟快速开始

### 7️⃣.1️⃣ 定义任务

```csharp
using EasyCore.Quartz;
using Quartz;

[EasyCoreCron("0/10 * * * * ?")]
[EasyCoreDisallowConcurrentExecution]
public sealed class SampleJob : IEasyCoreJob
{
    private readonly ILogger<SampleJob> _logger;
    public SampleJob(ILogger<SampleJob> logger) => _logger = logger;

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("SampleJob running at {Time}", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
}
```

禁用但不删除代码：

```csharp
[EasyCoreDisableJob]
[EasyCoreCron("0 0 * * * ?")]
public sealed class DisabledJob : IEasyCoreJob
{
    public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
}
```

### 7️⃣.2️⃣ 注册服务（含 Dashboard）

```csharp
// 引用 EasyCore.Quartz.Dashboard
builder.Services.EasyCoreQuartz(options =>
{
    options.AddAssemblyFrom<SampleJob>();
    options.TimeZoneOffsetHours = +8;
    options.AutoCreateSchema = true;

    // 默认内存。持久化时取消注释其一：
    // options.UseMySql(m => m.ConnectionString = "...");
    // options.UseSqlServer(s => s.ConnectionString = "...");
    // options.UsePostgreSql(p => p.ConnectionString = "...");
    // options.UseOracle(o => o.ConnectionString = "...");

    // Dashboard：访问地址 = 当前 App 根地址 + PathMatch
    // 无需再写 app.UseEasyCoreQuartzDashboard(...)
    options.EasyCoreQuartzDashboard(dash =>
    {
        dash.PathMatch = "/easy-quartz";
        dash.Username = "admin";
        dash.Password = "admin123";
    });
});
```

打开：`http://localhost:<port>/easy-quartz/`（浏览器弹窗输入账号密码）

---

## 8. 特性与属性完整说明

| 特性 / 配置 | 说明 |
|---|---|
| `IEasyCoreJob` | 任务标记接口（继承 Quartz `IJob`） |
| `[EasyCoreCron]` | Cron、JobKey、JobGroup、Misfire、RequestRecovery |
| `[EasyCoreDisableJob]` | 排除自动注册 |
| `[EasyCoreDisallowConcurrentExecution]` | 禁止同一 Job 重叠执行 |
| `EasyCoreQuartzOptions.AddAssembly` | 显式扫描程序集 |
| `AutoCreateSchema` | 启动时幂等建表（生产建议关闭） |
| `HistoryCapacity` | 进程内历史环形缓冲容量（默认 200）；Overview 成功/失败数为**窗口内**统计 |
| `HttpJobTimeout` | HTTP Job 超时（默认 30s） |
| `HttpJobBlockPrivateNetworks` | 默认 `true`，拦截本机/私网/元数据地址 |
| `HttpJobAllowedHosts` | HTTP Job 主机白名单（可放行 localhost 等） |
| `TablePrefix` | 默认 `QRTZ_`，需与 DDL 一致 |
| `MaxConcurrency` | 线程池上限；`0` 表示自动 |

---

## 9. Dashboard（英文可视化面板）

### 9.1 预览

![dashboard-preview](https://gitee.com/wzhy-0521/easy-core.-quartz/tree/master/docs/svg/dashboard-preview.svg)

### 9.2 页面能力

| 页面 | 图标含义 | 功能 |
|---|---|---|
| **Overview** | 📊 | 调度器状态、任务/触发器数、失败统计 |
| **Jobs** | 📋 | 列表；Pause / Resume / Trigger / Delete / Edit Cron / Detail |
| **Recurring** | 🔁 | 仅 Cron 任务 |
| **Executing** | ⚡ | 当前执行中 |
| **HTTP Jobs** | 🌐 | 创建 / 更新 HTTP 调用任务 |
| **History** | 📜 | 近期执行历史（节点本地内存） |
| **Servers** | 🖥️ | Scheduler 名称 / InstanceId / Store |

> ⚠️ History 为**进程内环形缓冲**，Overview 成功/失败数是缓冲窗口内计数（非进程终身累计），且不跨节点共享。

### 9.3 鉴权（HTTP Basic Auth）

```csharp
options.EasyCoreQuartzDashboard(dash =>
{
    dash.PathMatch = "/easy-quartz"; // 完整 URL = App 根地址 + 该路径
    dash.Username = "admin";         // 必填
    dash.Password = "admin123";      // 必填
    // dash.DashboardTitle = "EasyCore Quartz";
    // dash.AppPath = "/";
});
```

| 配置 | 说明 |
|---|---|
| `PathMatch` | 相对路径，默认 `/easy-quartz` |
| `Username` / `Password` | Basic Auth 账号密码（必填，中间件自动挂载） |
| `BasicAuthAuthorizationFilter` | 默认启用 |
| `LocalRequestsOnlyAuthorizationFilter` | 可选，可追加到 `dash.Authorization` |
| 自定义 `IEasyCoreQuartzAuthorizationFilter` | 生产可替换或叠加 |

---

## 10. REST API

前缀：`api/quartz`

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/overview` | 总览 |
| GET | `/jobs` | 全部任务 |
| GET | `/jobs/{group}/{name}` | 详情 |
| GET | `/recurring` | Cron 任务 |
| GET | `/executing` | 执行中 |
| GET | `/history?take=100` | 历史 |
| PUT | `/jobs/{group}/{name}/pause` | 暂停 |
| PUT | `/jobs/{group}/{name}/resume` | 恢复 |
| PUT | `/jobs/{group}/{name}/cron?cron=...` | 更新 Cron |
| DELETE | `/jobs/{group}/{name}` | 删除 |
| POST | `/jobs/{group}/{name}/trigger` | 立即触发 |
| POST | `/http-jobs` | 添加/更新 HTTP 任务 |

---

## 11. HTTP 动态任务

通过 Dashboard「HTTP Jobs」或 REST 创建：

```json
{
  "jobName": "PingApi",
  "jobGroup": "DEFAULT",
  "url": "https://example.com/demo/ping",
  "method": "GET",
  "cron": "0/30 * * * * ?",
  "headers": { "X-Trace": "demo" },
  "body": "",
  "description": "Health ping"
}
```

| 能力 | 说明 |
|---|---|
| Method | `GET` / `POST` / `PUT` / `DELETE` / `PATCH`（大小写不敏感） |
| Body | POST/PUT/PATCH 时校验 JSON |
| 超时 | `HttpJobTimeout`（默认 30 秒） |
| SSRF | 默认拦截本机/私网/链路本地/云元数据；可用 `HttpJobAllowedHosts` 放行 |
| 失败语义 | 非 2xx ⇒ 抛异常 ⇒ History 记失败 |

调用本机 Demo API 时需显式放行：

```csharp
options.HttpJobAllowedHosts.Add("localhost");
options.HttpJobAllowedHosts.Add("127.0.0.1");
```

---

## 12. 数据库配置详解

所有库统一表前缀 **`QRTZ_`**。

### 🐬 MySQL

```csharp
options.UseMySql(mysql =>
{
    mysql.ConnectionString =
        "server=localhost;port=3306;user id=root;password=***;database=EasyCoreQuartz;";
});
```

### 🟦 SQL Server

```csharp
options.UseSqlServer(sql =>
{
    sql.ConnectionString =
        "Server=.;Database=EasyCoreQuartz;User Id=sa;Password=***;TrustServerCertificate=True;";
});
```

### 🐘 PostgreSQL

```csharp
options.UsePostgreSql(pg =>
{
    pg.ConnectionString =
        "Host=localhost;Port=5432;Database=EasyCoreQuartz;Username=postgres;Password=***";
});
```

### 🔶 Oracle

```csharp
options.UseOracle(ora =>
{
    ora.ConnectionString =
        "User Id=quartz;Password=***;Data Source=localhost:1521/ORCL";
});
```

### 生产建表建议

```csharp
options.AutoCreateSchema = false; // 生产关闭自动 DDL
```

通过迁移流水线执行官方 Quartz DDL，或先在预发用 `AutoCreateSchema=true` 生成后固化脚本。

---

## 13. 集群与并发

| 配置 | 默认 | 说明 |
|---|---|---|
| `CheckinInterval` | 5s | 节点心跳间隔 |
| `CheckinMisfireThreshold` | 10s | 心跳 misfire 阈值 |
| `MaxConcurrency` | 20 | 线程池；`0` = 自动 |

启用任意持久化提供程序后，Quartz **自动开启 Clustering**。

禁止重叠执行：

```csharp
[EasyCoreDisallowConcurrentExecution]
[EasyCoreCron("0/5 * * * * ?")]
public sealed class ExclusiveJob : IEasyCoreJob { /* ... */ }
```

---

## 14. Demo 项目

| 项目 | 存储 | 端口 | 命令 |
|---|---|---|---|
| [`WebApp.Quartz.InMemory`](demo/WebApp.Quartz.InMemory) | 内存 | 5101 | `dotnet run --project demo/WebApp.Quartz.InMemory` |
| [`WebApp.Quartz.MySql`](demo/WebApp.Quartz.MySql) | MySQL | 5102 | `dotnet run --project demo/WebApp.Quartz.MySql` |
| [`WebApp.Quartz.SqlServer`](demo/WebApp.Quartz.SqlServer) | SQL Server | 5103 | `dotnet run --project demo/WebApp.Quartz.SqlServer` |
| [`WebApp.Quartz.PostgreSql`](demo/WebApp.Quartz.PostgreSql) | PostgreSQL | 5104 | `dotnet run --project demo/WebApp.Quartz.PostgreSql` |
| [`WebApp.Quartz.Oracle`](demo/WebApp.Quartz.Oracle) | Oracle | 5105 | `dotnet run --project demo/WebApp.Quartz.Oracle` |

每个 demo 项目内都有独立的 `Jobs/SampleJob.cs`，打开即可改，无需跨项目引用。

```bash
# 最快体验
dotnet run --project demo/WebApp.Quartz.InMemory
# 浏览器打开
# http://localhost:5101/easy-quartz
```

数据库 Demo 请先修改对应 `appsettings.json` 中的 `ConnectionStrings:Quartz`。

---

## 15. 从旧版迁移

**8.0.0** 为破坏性升级（相对早期 `Quarzt*` 命名版本）：

| 旧版 | 8.0 |
|---|---|
| `QuarztOptions` | `EasyCoreQuartzOptions` |
| `api/Quarzt` | `api/quartz` |
| 拼写 `Quarzt` | 全面纠正 |
| 扫描 BaseDirectory 全部 DLL | EntryAssembly + `AddAssembly` |
| 吞掉任务异常 | 记录后重抛 |
| 表前缀 `qrtz_` | 统一 `QRTZ_` |
| License 混用 | **MIT** |

---

## 16. 生产清单

- [ ] Dashboard 使用强密码（公网勿用 demo 账号）
- [ ] `AutoCreateSchema = false`，使用评审后的迁移脚本
- [ ] 连接串放入配置中心 / 密钥库
- [ ] 监控日志与 History **窗口内**失败数（非终身累计）
- [ ] HTTP Job：确认 `HttpJobAllowedHosts` / 出站网络策略，勿随意关闭私网拦截
- [ ] 高负载显式设置 `MaxConcurrency`
- [ ] 上线前校验 Cron 表达式
- [ ] 多节点必须使用同一持久化库与表前缀

---

## 17. FAQ

**Q: Dashboard 打开是 401？**  
A: 浏览器会弹出 Basic Auth。使用配置的 `Username` / `Password` 登录。未配置账号密码时启用 Dashboard 会在启动时抛错。

**Q: 内存模式有 Dashboard 吗？**  
A: 有。引用 `EasyCore.Quartz.Dashboard` 并调用 `options.EasyCoreQuartzDashboard(...)` 即可。

**Q: History 为什么节点间不一致？**  
A: History 是进程内环形缓冲；Overview 成功/失败数只统计当前窗口内的记录。跨节点请依赖日志系统或自建审计。

**Q: HTTP Job 调本机 API 被拒绝？**  
A: 默认开启 SSRF 防护。将主机加入 `options.HttpJobAllowedHosts`，或仅在受控环境关闭 `HttpJobBlockPrivateNetworks`。

**Q: HTTP Job 默认 Method=`GET` 会失败吗？**  
A: 不会。8.0 已按大小写不敏感校验。

**Q: 如何只扫描业务程序集？**  
A: `options.AddAssemblyFrom<YourJob>()` 或 `options.AddAssembly(asm)`。

---

## 18. License

MIT — 详见 [LICENSE](LICENSE)。

---

## 🤝 贡献

1. Fork 并创建特性分支  
2. 在 `tests/EasyCore.Quartz.Tests` 补充测试  
3. 执行 `dotnet test` 与 `dotnet build EasyCore.Quartz.sln`  
4. 提交 Pull Request  

欢迎 Issue / PR 🚀
