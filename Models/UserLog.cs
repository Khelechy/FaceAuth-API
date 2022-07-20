using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAuth.Models
{
    public class UserLog
    {
        public int Id{ get; set; }
        public string Email { get; set; }
        public Guid UserId { get; set; }
        public DateTime LastLogTime { get; set; }
    }
}
