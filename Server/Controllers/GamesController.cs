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
        //שיטת פוסט שתקבל מידע מסוג הDTO שיצרנו GameToAddDto. הוספנו לה כבר את בדיקת ההתחברות
        [HttpPost("addGame")]
        public async Task<IActionResult> AddGames(int authUserId, GameToAddDto gameToAdd)
        {
            if (authUserId > 0)
            {
                //ניצור משחק חדש בבסיס הנתונים
                object newGameParam = new
                {
                    GameName = gameToAdd.gameName,
                    GameCode = 0,
                    IsPublish = false,
                    TimePerItem = 60,
                    UserId = authUserId,
                    CanPublish = false,
                    HasPotion = true
                };
                string insertGameQuery = "INSERT INTO Games (time, gameName, userID, canPublish, isPublish, hasPotion, gameCode) " +
                                         "VALUES (@TimePerItem, @GameName, @UserId, @CanPublish, @IsPublish, @HasPotion, @GameCode)";
                int newGameId = await _db.InsertReturnIdAsync(insertGameQuery, newGameParam);
                if (newGameId != 0)
                {
                    //אם המשחק נוצר בהצלחה, נחשב את הקוד עבורו
                    int gameCode = newGameId + 1000;
                    object updateParam = new
                    {
                        ID = newGameId,
                        GameCode = gameCode
                    };
                    string updateCodeQuery = "UPDATE Games SET gameCode = @GameCode WHERE gameID=@ID";
                    int isUpdate = await _db.SaveDataAsync(updateCodeQuery, updateParam);
                    if (isUpdate > 0)
                    {
                        //אם המשחק עודכן בהצלחה - נחזיר את הפרטים שלו לעורך
                        object param2 = new
                        {
                            ID = newGameId
                        };
                        string gameQuery = "SELECT gameID, gameName, gameCode, isPublish, canPublish FROM Games WHERE gameID = @ID";
                        var gameRecord = await _db.GetRecordsAsync<GameToTableDto>(gameQuery, param2);
                        GameToTableDto newGame = gameRecord.FirstOrDefault();
                        return Ok(newGame);
                    }
                    return BadRequest("Game code not created");
                }
                return BadRequest("Game not created");
            }
            else
            {
                return Unauthorized("user is not authenticated");
            }
        }
        
        
    }
}