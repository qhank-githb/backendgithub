using System;
using System.Collections.Generic;
using System.Text.Json;
using MinioWebBackend.Models;
using Serilog.Events;

namespace MinioWebBackend.Dtos.LogDtos
{
    /// <summary>
    /// 单条日志DTO
    /// </summary>
    public class LogItemDto
    {
        public long Id { get; set; }
        public LogEventLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 从数据库实体转换为DTO
        /// </summary>
        public static LogItemDto FromEntity(SerilogLog log)
        {
            return new LogItemDto
            {
                Id = log.Id,
                Level = ParseLogLevel(log.Level),
                Message = log.Message,
                Exception = log.Exception,
                Properties = ParseProperties(log.Properties),
                Timestamp = log.Timestamp
            };
        }

        /// <summary>
        /// 解析日志级别
        /// </summary>
        private static LogEventLevel ParseLogLevel(string level)
        {
            if (Enum.TryParse<LogEventLevel>(level, true, out var result))
                return result;
            return LogEventLevel.Information;
        }

        /// <summary>
        /// 解析Properties JSON
        /// </summary>
        private static Dictionary<string, object> ParseProperties(string? propertiesJson)
        {
            if (string.IsNullOrEmpty(propertiesJson))
                return new Dictionary<string, object>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson) 
                    ?? new Dictionary<string, object>();
            }
            catch (JsonException)
            {
                return new Dictionary<string, object>();
            }
        }
    }
}
