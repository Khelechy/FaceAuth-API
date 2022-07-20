
using FaceAuth.Services;
using FaceAuth.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaceAuthController : ControllerBase
    {
        private readonly IAzureServices _azureServices;
        private readonly IUserServices _userServices;

        public FaceAuthController(IAzureServices azureServices, IUserServices userServices)
        {
            _azureServices = azureServices;
            _userServices = userServices;
        }


        [HttpGet("get-staffs")]
        public async Task<IActionResult> GetUser()
        {
            var users = await _userServices.GetUsers();
            if (users == null)
                return BadRequest(new { error = "No users" });
            return Ok(users);
        }

        [HttpPost("add-person")]
        public async Task<IActionResult> AddPerson([FromBody]AddPersonViewModel model)
        {
            var (isError, error, user) = await _azureServices.AddPerson(model);
            if (isError)
                return BadRequest(new { error = error });
            else
                return Ok(user);


        }

        [HttpGet("recognize-person")]
        public async Task<IActionResult> RecognizePerson([FromBody]RecognizePersonViewModel model)
        {
            Guid personId = await _azureServices.Recognize(model);
            if(personId == Guid.Empty)
            {
                return BadRequest(new { error = "Cant Recognize Face" });
            }
            var user = await _userServices.GetUserById(personId);
            if(user == null)
            {
                return BadRequest(new { error = "User not found in Database" });
            }
            await _userServices.AddLog(user.Email, $"{user.FirstName + " " + user.LastName}", user.Id);
            return Ok(user);
        }

        [HttpGet("get-logs")]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _userServices.GetUserLogs();
            return Ok(logs);
        }

        [HttpGet("get-user-log")]
        public async Task<IActionResult> GetUserLogs(string email)
        {
            var (success, logs) = await _userServices.GetUserLogForUser(email);
            if (!success)
            {
                return BadRequest(new { error = "no user found with such email" });

            }
            return Ok(logs);
        }
    }
}
