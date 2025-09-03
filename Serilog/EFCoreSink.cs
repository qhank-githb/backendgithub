using Serilog;
using Serilog.Core;
using Serilog.Events;
using Microsoft.EntityFrameworkCore;
using ConsoleApp1.Data;
using MinioWebBackend.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Serilog.Configuration;

namespace MinioWebBackend.Serilog
{
    /// <summary>
    /// 自定义Serilog Sink：通过EF Core写入日志到数据库
    /// </summary>
    public class EFCoreSink : ILogEventSink, IDisposable
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ConcurrentQueue<SerilogLog> _logQueue = new ConcurrentQueue<SerilogLog>();
        private readonly Timer _flushTimer;
        private const int BatchSize = 100; // 批量写入阈值
        private const int FlushIntervalSeconds = 5; // 定时刷新间隔（秒）
        private bool _isDisposed;

        public EFCoreSink(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            // 定时刷新队列（避免日志堆积）
            _flushTimer = new Timer(FlushQueue, null, 
                TimeSpan.FromSeconds(FlushIntervalSeconds), 
                TimeSpan.FromSeconds(FlushIntervalSeconds));
        }

        /// <summary>
        /// 处理Serilog日志事件（核心方法）
        /// </summary>
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) return;

            // 1. 转换LogEvent为SerilogLog实体
            var logEntry = new SerilogLog
            {
                Timestamp = logEvent.Timestamp.UtcDateTime,
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage(), // 渲染带参数的消息
                Exception = logEvent.Exception?.ToString(),
                // 序列化结构化参数（保留{username}, {bucket}等键值对）
                Properties = JsonConvert.SerializeObject(logEvent.Properties, 
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
            };

            // 2. 加入队列（避免阻塞主线程）
            _logQueue.Enqueue(logEntry);

            // 3. 达到批量阈值时立即刷新
            if (_logQueue.Count >= BatchSize)
            {
                FlushQueue(null);
            }
        }

        /// <summary>
        /// 批量写入队列中的日志到数据库
        /// </summary>
        private void FlushQueue(object? state)
        {
            if (_logQueue.IsEmpty || _isDisposed) return;

            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var batch = new List<SerilogLog>();

                // 从队列取数据（最多BatchSize条）
                while (batch.Count < BatchSize && _logQueue.TryDequeue(out var log))
                {
                    batch.Add(log);
                }

                if (batch.Count == 0) return;

                // 批量插入
                dbContext.SerilogLogs.AddRange(batch);
                dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                // 日志写入失败时，可降级到控制台或文件（避免死循环）
                Console.WriteLine($"EF Core日志写入失败: {ex.Message}");
            }
        }

        // 释放资源时确保队列中所有日志被写入
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
                FlushQueue(null); // 程序退出前强制刷新
            }

            _isDisposed = true;
        }
    }

    /// <summary>
    /// Serilog扩展方法：简化配置
    /// </summary>
    public static class EFCoreSinkExtensions
    {
        /// <summary>
        /// 添加EF Core作为Serilog的日志输出目标
        /// </summary>
        /// <param name="sinkConfiguration">Serilog的Sink配置</param>
        /// <param name="dbContextFactory">EF Core的DbContext工厂</param>
        /// <returns>日志配置对象</returns>
        public static LoggerConfiguration EFCore(
            this LoggerSinkConfiguration sinkConfiguration,
            IDbContextFactory<AppDbContext> dbContextFactory)
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
            if (dbContextFactory == null) throw new ArgumentNullException(nameof(dbContextFactory));

            return sinkConfiguration.Sink(new EFCoreSink(dbContextFactory));
        }
    }
}
