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
    }
    public class UserServices : IUserServices
    {
        private readonly FaceAuthContext _context;

        public UserServices(FaceAuthContext context)
        {
            _context = context;
        }
        public async Task<User> GetUserById(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
