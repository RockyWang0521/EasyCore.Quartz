# EasyCore.Quartz Project Documentation

## Introduction

`EasyCore.Quartz` is a task scheduling framework encapsulation based on Quartz, designed to simplify the use of scheduled tasks in .NET projects. It supports triggering tasks via HTTP requests and provides database configuration support for MySQL and SQL Server.

This project is suitable for developers who want to quickly integrate scheduled task scheduling functionality and supports managing task creation, pausing, resuming, and deletion through simple APIs.

## Features

- Supports executing scheduled tasks via HTTP requests
- Provides dynamic task management: add, update, pause, resume, and delete
- Supports multiple database types (MySQL, SQL Server) for task storage
- Supports Cron expression configuration for scheduled tasks
- Simple and easy-to-use API controller for task management
- Supports task concurrency control and error handling

## Installation and Configuration

### 1. Project Structure

- `src/EasyCore.Quartz`: Core library code containing Quartz encapsulation logic
- `src/EasyCore.Quartz.MySql` and `src/EasyCore.Quartz.SqlServer`: Database support for MySQL and SQL Server respectively
- `demo/WebApp.Quartz`: Example web application demonstrating how to use EasyCore.Quartz

### 2. Configure Database Support

In `QuartzOptions`, you can set the database using the following methods:

```csharp
options.SetSqlServer("your_sqlserver_connection_string");
options.SetMySql("your_mysql_connection_string");
```

### 3. Configure Cron Expressions

Use the `EasyCoreCronAttribute` to set Cron expressions for tasks, for example:

```csharp
[EasyCoreCron("0/1 * * * * ?")]
public class QuartzTask : IEasyCoreJob
```

## Usage Examples

### Create a Scheduled Task

```csharp
[EasyCoreCron("0/1 * * * * ?")]
public class QuartzTask : IEasyCoreJob
{
    private readonly ILogger<QuartzTask> _logger;

    public QuartzTask(ILogger<QuartzTask> logger) => _logger = logger;

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Scheduled task is executing");
    }
}
```

### Manage Tasks Using the API

`QuartzController` provides RESTful APIs to perform the following operations:

- Get a list of all tasks
- Pause and resume tasks
- Update a task's Cron expression
- Manually trigger a task
- Add or update an HTTP task

Example: Add an HTTP task

```http
POST /quartz/addorupdate/httpjob
{
    "JobName": "HttpJob",
    "Url": "http://example.com/api/endpoint",
    "Method": "POST",
    "Cron": "0/1 * * * * ?"
}
```

### Disable a Task

Use the `EasyCoreDisableJobAttribute` to disable a specific task:

```csharp
[EasyCoreDisableJob]
public class DisableJobTask : IEasyCoreJob
```

## API Documentation

### Get All Tasks

```
GET /quartz/get/all/jobs
```

### Pause a Task

```
PUT /quartz/pause/job?jobName=JobName
```

### Resume a Task

```
PUT /quartz/resume/job?jobName=JobName
```

### Update Task Cron

```
PUT /quartz/update/cron?jobName=JobName&newCron=0/10 * * * * ?
```

### Delete a Task

```
DELETE /quartz/delete/job?jobName=JobName
```

### Manually Trigger a Task

```
POST /quartz/manualtrigger/job?jobName=JobName
```

### Add/Update an HTTP Task

```
POST /quartz/addorupdate/httpjob
{
    "JobName": "MyHttpJob",
    "Url": "http://your-endpoint.com",
    "Method": "GET",
    "Cron": "0/5 * * * * ?"
}
```

## Contribution Guidelines

We welcome the submission of Issues and Pull Requests. Please ensure that your code style matches the existing code and provide complete unit tests.

## License

This project uses the MIT License. For details, please refer to the [LICENSE](LICENSE) file.