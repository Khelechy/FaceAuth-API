using FaceAuth.Data;
using FaceAuth.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAuth.Services
{
    public interface IUserServices
    {
        Task<User> GetUserById(Guid id);
        Task<IEnumerable<User>> GetUsers();
        Task<IEnumerable<UserLog>> GetUserLogs();
        Task AddLog(string email, Guid userId);
        Task<Tuple<bool,IEnumerable<UserLog>>> GetUserLogForUser(string email);
    }
    public class UserServices : IUserServices
    {
        private readonly FaceAuthContext _context;

        public UserServices(FaceAuthContext context)
        {
            _context = context;
        }

        public async Task AddLog(string email, Guid userId)
        {
            var userLog = new UserLog
            {
                Email = email,
                UserId = userId,
                LastLogTime = DateTime.Now
            };

            await _context.UserLogs.AddAsync(userLog);
            await _context.SaveChangesAsync();
        }

        public async Task<User> GetUserById(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Tuple<bool, IEnumerable<UserLog>>> GetUserLogForUser(string email)
        {
            var user = _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
            if(user == null){
                return new Tuple<bool, IEnumerable<UserLog>>(false, null);
            }
            return new Tuple<bool, IEnumerable<UserLog>>(true, await _context.UserLogs.Where(x => x.Email.ToLower() == email.ToLower()).ToListAsync());
        }

        public async Task<IEnumerable<UserLog>> GetUserLogs()
        {
            return await _context.UserLogs.ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }
    }
}
