using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Switch
{
    public class DatabaseOptions
    {
        [Required]
        public string APIKey { get; set; }

        [Required]
        public string APISecret { get; set; }

        [Required]
        public ConnectionType ConnectionType { get; set; }

        [Required]
        public DateTime ConnectionExpire { get; set; }
    }

    public enum ConnectionType
    {
        HTTP, HTTPS, WebSocket
    }
}