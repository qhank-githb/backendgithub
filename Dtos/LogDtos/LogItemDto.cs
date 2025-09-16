using System;
using System.Collections.Generic;
using System.Text.Json;
using MinioWebBackend.Models;
using Serilog.Events;
using System.Text.Json.Serialization;

namespace MinioWebBackend.Dtos.LogDtos
{
    /// <summary>
    /// 单条日志的DTO（数据传输对象），用于展示日志的详细信息
    /// </summary>
    public class LogItemDto
    {
        /// <summary>
        /// 日志记录的唯一标识（自增ID）
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 日志级别（如 Verbose、Debug、Information、Warning、Error、Fatal 等）
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogEventLevel Level { get; set; }

        /// <summary>
        /// 日志的具体消息内容
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息（如果存在），否则为 null
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// 日志的结构化属性（键值对集合），包含额外的上下文信息
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 日志记录的时间戳（UTC时间）
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 将数据库中的日志实体（SerilogLog）转换为当前DTO对象
        /// </summary>
        /// <param name="log">数据库中的日志实体对象</param>
        /// <returns>转换后的日志DTO实例</returns>
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

        // private 方法按要求不添加注释
        private static LogEventLevel ParseLogLevel(string level)
        {
            if (Enum.TryParse<LogEventLevel>(level, true, out var result))
                return result;
            return LogEventLevel.Information;
        }

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
        
public static LogItemDto FromESDto(SerilogLogESDto esDto)
{
    var level = Enum.TryParse<LogEventLevel>(esDto.Level, true, out var parsedLevel)
        ? parsedLevel
        : LogEventLevel.Information;

    return new LogItemDto
    {
        Level = level,
        Message = esDto.Message,
        Exception = esDto.Exception,
        Timestamp = esDto.AtTimestamp,  // ✅ 使用正确的属性名
        Properties = esDto.Fields       // ⚠️ 确保类型一致
    };
}


    }
}
