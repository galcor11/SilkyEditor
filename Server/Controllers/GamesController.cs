using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UsersManager.Server;

namespace AuthTemplate.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(AuthCheck))] //בדיקה שהמשתמש מחובר

    public class GamesController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<int>> GetLoginUser(int authUserId)
        {
            return Ok(authUserId);
        }
    }
}
