using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scrabble.Config
{
    [Serializable]
    public class LogEntry {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }
        public LogType Tipo { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}