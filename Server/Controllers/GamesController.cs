using AuthTemplate.Shared.Models;
using Data;
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
        //קישור ל-DBREPOSITORY
        private readonly DbRepository _db;
        public GamesController( DbRepository db) 
        { 
            _db = db;
            // בתוך הקיימות השיטות אל לפנות מאפשר DBRepository
        }
        [HttpGet]
        public async Task<ActionResult<int>> GetLoginUser(int authUserId)
        {
            return Ok(authUserId);
        }
        
        //שיטת קונטרולר שנועדה לשלוף את כל המשחקים של משתמש מסוים
        [HttpGet("GameList")]
        public async Task<IActionResult> GetUserGames(int authUserId)
        {
            if (authUserId > 0)
            {
                object param = new { ID = authUserId };
                string query =
                    "SELECT Games.*, count(Questions.questionID) AS questionCount FROM Games LEFT OUTER JOIN Questions on Games.gameCode = Questions.gameID WHERE Games.userID=@ID group by Games.gameCode";
                var record = await _db.GetRecordsAsync<GameToTableDto>(query, param);
                List<GameToTableDto> gameList = record.ToList();
                if (gameList.Count > 0)
                {
                    return Ok(gameList);
                }
                else
                {
                    return BadRequest("No games for this user");
                }
            }
            else
            {
                return Unauthorized("user is not authenticated");
            }
        
        }
    }
}