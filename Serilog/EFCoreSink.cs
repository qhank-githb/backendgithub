using Serilog;
using Serilog.Core;
using Serilog.Events;
using Microsoft.Extensions.DependencyInjection;
using MinioWebBackend.Models; // 你的 SerilogLog 模型所在命名空间
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Serilog.Configuration;

namespace MinioWebBackend.Serilog
{
    /// <summary>
    /// 依赖 IServiceScopeFactory 的 Serilog Sink（复用已注册的 AppDbContext）
    /// </summary>
    public class EFCoreSink : ILogEventSink, IDisposable
    {
        // 改为依赖 IServiceScopeFactory（不再用 IDbContextFactory）
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConcurrentQueue<SerilogLog> _logQueue = new ConcurrentQueue<SerilogLog>();
        private readonly Timer _flushTimer;
        private const int BatchSize = 100; // 批量写入阈值
        private const int FlushIntervalSeconds = 5; // 定时刷新间隔
        private bool _isDisposed;

        // 构造函数：注入 IServiceScopeFactory（与扩展方法参数匹配）
        public EFCoreSink(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _flushTimer = new Timer(FlushQueue, null,
                TimeSpan.FromSeconds(FlushIntervalSeconds),
                TimeSpan.FromSeconds(FlushIntervalSeconds));
        }

        // 处理日志事件（不变）
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) return;

            var logEntry = new SerilogLog
            {
                Timestamp = logEvent.Timestamp.UtcDateTime,
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage(),
                Exception = logEvent.Exception?.ToString(),
                Properties = JsonConvert.SerializeObject(logEvent.Properties,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
            };

            _logQueue.Enqueue(logEntry);
            if (_logQueue.Count >= BatchSize) FlushQueue(null);
        }

        // 批量写入：通过 IServiceScopeFactory 创建临时 Scope 获取 AppDbContext
        private void FlushQueue(object? state)
        {
            if (_logQueue.IsEmpty || _isDisposed) return;

            try
            {
                // 1. 创建临时服务作用域（自动释放，避免内存泄漏）
                using var scope = _scopeFactory.CreateScope();
                // 2. 从 Scope 中获取你已注册的 AppDbContext（复用原有注册）
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 3. 批量读取队列中的日志
                var batch = new List<SerilogLog>();
                while (batch.Count < BatchSize && _logQueue.TryDequeue(out var log))
                {
                    batch.Add(log);
                }

                // 4. 写入数据库
                if (batch.Count > 0)
                {
                    dbContext.SerilogLogs.AddRange(batch);
                    dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // 日志写入失败时降级到控制台（避免影响主程序）
                Console.WriteLine($"EF Core 日志写入失败: {ex.Message}");
            }
        }

        // 释放资源（不变）
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _flushTimer.Dispose();
                FlushQueue(null); // 程序退出前强制刷新剩余日志
            }
            _isDisposed = true;
        }
    }

    /// <summary>
    /// 扩展方法：接收 IServiceScopeFactory（与 Sink 构造函数匹配）
    /// </summary>
    public static class EFCoreSinkExtensions
    {
        public static LoggerConfiguration EFCore(
            this LoggerSinkConfiguration sinkConfig,
            IServiceScopeFactory scopeFactory) // 参数改为 IServiceScopeFactory
        {
            if (sinkConfig == null) throw new ArgumentNullException(nameof(sinkConfig));
            if (scopeFactory == null) throw new ArgumentNullException(nameof(scopeFactory));

            // 创建 Sink 实例时传入 IServiceScopeFactory
            return sinkConfig.Sink(new EFCoreSink(scopeFactory));
        }
    }
}
