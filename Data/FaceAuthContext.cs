using FaceAuth.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAuth.Data
{
    public class FaceAuthContext : DbContext
    {
        public FaceAuthContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
    }
}
