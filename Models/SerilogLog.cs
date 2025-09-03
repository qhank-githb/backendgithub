// Models/SerilogLog.cs（对应数据库表SerilogLogs）
using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinioWebBackend.Models
{
    [Table("SerilogLogs")] // 表名风格与OperationLogs一致
    public class SerilogLog
    {
        /// <summary>
        /// 自增主键（与OperationLogs的Id类型一致）
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// 日志级别（Info/Warn/Error等，对应OperationLogs的OperationType风格）
        /// </summary>
        [Required]
        public string Level { get; set; } // 对应longtext类型（允许长文本）

        /// <summary>
        /// 渲染后的日志消息（对应OperationLogs的Message风格）
        /// </summary>
        [Required]
        public string Message { get; set; } // 对应longtext类型

        /// <summary>
        /// 异常信息（ nullable，对应OperationLogs的Message可空场景）
        /// </summary>
        public string? Exception { get; set; } // 对应longtext类型

        /// <summary>
        /// 结构化参数（JSON格式，存储{username}, {bucket}等键值对）
        /// </summary>
        public string? Properties { get; set; } // 对应longtext类型

        /// <summary>
        /// 日志时间戳（与OperationLogs的Timestamp类型完全一致：datetime(6)）
        /// </summary>
        [Required]
        [Column(TypeName = "datetime2(6)")] // 精确到6位小数，与模板一致
        public DateTime Timestamp { get; set; }
    }
}
