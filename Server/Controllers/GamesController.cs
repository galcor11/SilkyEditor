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
        //שיטת קונטרולר שנועה לטפל בפרסום המשחק
        [HttpPost("publishGame")]
        public async Task<IActionResult> publishGame(int authUserId, PublishGame game){
            if (authUserId > 0) {
                object param = new {
                    userID = authUserId,
                    gameID = game.gameID
                };

                //שליפת שם המשחק לפי משתמש כדי לוודא שהמשחק המבוקש שייך למשתמש שמחובר
                string checkQuery = "SELECT gameName FROM Games WHERE userID = @userID AND gameID=@gameID";
                var checkRecords = await _db.GetRecordsAsync<string>(checkQuery, param);
                string gameName = checkRecords.FirstOrDefault(); 
                //שליפת שם המשחק כדי לוודא שהמשחק המבוקש שייך למשתמש המחובר
                if (gameName != null){
                    //אם יש רצון לפרסם את המשחק
                    if (game.isPublish == true) {
                        //נבדוק באמצעות פונקציית עזר שניתן לפרסם אותו
                        bool canPublish = await CanPublishFunc(game.gameID);
                        //אם לא ניתן לפרסם	
                        if (canPublish == false) {
                            //נחזיר הודעת שגיאה	
                            return BadRequest("This game cannot be published");
                        }
                    }
//המשך הקוד כאן - מחוץ לתנאי הראשון. אם רוצים להסיר פרסום, לא צריך לבדוק את הרשאת הפרסום
                    //אם ניתן לפרסם את המשחק או שרוצים להסיר אותו מפרסום
                    //נעדכן את בסיס הנתונים
                    object updateParam = new {
                        IsPublish = game.isPublish,
                        ID = game.gameID
                    };
                    string updateQuery = "UPDATE Games SET isPublish=@IsPublish WHERE gameID=@ID";
                    int isUpdate = await _db.SaveDataAsync(updateQuery, updateParam);
                    if (isUpdate == 1) {                   
                        return Ok();
                    }
                    return BadRequest("Update Failed");

 
                }
                return BadRequest("It's Not Your Game");
            }
            else 
            { 
                return Unauthorized("user is not authenticated");  
            }
        }

        //שיטה למחיקת רשומה חדשה של משחק
        [HttpDelete("deleteGame/{idToDelete}")]
        public async Task<IActionResult> GameDelete(int idToDelete)
        {
            //בדיקת תקינות
            if (idToDelete <= 0)
                return BadRequest("המזהה הייחודי של המשחק איננו תקין. חייב להיות מספר חיובי");

            //אובייקט הפרמטרים לשאילתה מכיל רק איי די
            object param = new
            {
                ID = idToDelete
            };

            string deleteQuery = "DELETE FROM Games WHERE gameID = @ID";
            int isDeleted = await _db.SaveDataAsync(deleteQuery, param);

            //אם המחיקה הצליחה
            if (isDeleted > 0)
            {
                return Ok();
            }
    
            return BadRequest("המחיקה נכשלה");
        }
        
        //שיטה שנועדה לשלוף לנו את הפרטים הרלוונטיים לעמוד של הגדרות הכלליות: שם המשחק, זמן ושיקוי 
        [HttpGet("byId/{id}")]
        public async Task<IActionResult> GetGameById(int id, int authUserId)
        {
            // אנחנו בודקים שה-ID של המשחק תקין ושיש משתמש מחובר
            if (id <= 0 || authUserId <= 0)
            {
                return BadRequest("Invalid request");
            }

            //אובייקט הפרמטרים לשאילתה שלנו
            object param = new {
                gameID = id,
                userId = authUserId
            };

            //  שאילתת SQL שבודקת גם את ה-ID של המשחק וגם שהוא שייך למשתמש
            string query = "SELECT gameName, hasPotion, time FROM Games WHERE gameID = @gameID AND userID = @userId";

            // אנחנו שולפים מבסיס הנתונים
            var records = await _db.GetRecordsAsync<GameToAddDto>(query, param);
            GameToAddDto game = records.FirstOrDefault();

            // אם המשחק נמצא והוא אכן של המשתמש, נחזיר אותו
            if(game != null) 
            {
                return Ok(game);
            }
    
            return BadRequest("Game not found or it's not your game");
        }
        //שיטה שנועדה לעדכן בבסיס הנתונים את הפרטים שמשנים בעמוד הגדרות כלליות: שם המשחק, זמן לשאלה, שיקויים
        [HttpPost("updateGame/{id}")]
        public async Task<IActionResult> UpdateGame(int id, int authUserId, GameToAddDto gameToUpdate)
        {
            // בדיקת תקינות מזהים: מזהה המשחק, המשתמש המחובר, ושהאובייקט עצמו לא ריק
            if (id <= 0 || authUserId <= 0 || gameToUpdate == null)
            {
                return BadRequest("Invalid request");
            }

            // יצירת אובייקט הפרמטרים לשאילתה
            object param = new {
                gameName = gameToUpdate.gameName,
                time = gameToUpdate.time, 
                hasPotion = gameToUpdate.hasPotion,
                gameID = id,
                userId = authUserId
            };

            //שאילתת העדכון שלנו
            string query = "UPDATE Games SET gameName = @gameName, time = @time, hasPotion = @hasPotion WHERE gameID = @gameID AND UserId = @userId";

            // שמירת הנתונים באמצעות השיטה הייעודית ב-DbRepository שמחזירה את כמות הרשומות שהשתנו
            int isUpdate = await _db.SaveDataAsync(query, param);
            // בדיקה אם רשומה אחת אכן עודכנה בהצלחה
            if (isUpdate == 1) 
            {
                return Ok(true);
            }

            // אם הגענו לפה, כנראה שהרשומה לא נמצאה או לא עודכנה
            return BadRequest("Update Failed");
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        //פונקציית עזר לפרסום, היא פרטית ואי אפשר לבדוק אותה בפוסטמן
        //שיטה שבודקת אם ניתן לפרסם את המשחק
        //אם נמצא שלא ניתן לפרסם - נוודא שהמשחק גם לא מפורסם
        private async Task<bool> CanPublishFunc(int gameId)
        {
            //במקרה שלנו - התנאי לפרסום משחק הוא לפחות שלוש שאלות
            //יש לשנות את השיטה בהתאם לתנאי הפרסום עליהם החלטתם
            int minQuestions = 3;
            //אצלנו יש מינימום 4 פריטים בשאלה כדי לפרסם את המשחק ומקסימום 8 פריטים בשאלה. 
            int minItemsPerQuestion = 4;
            int maxItemsPerQuestion = 8;

            //משתנה לשמירה של הסטטוס - האם ניתן לפרסום
            bool canPublish = false;

            object param = new{
                ID = gameId
            };

            //שאילתה שבודקת כמה שאלות יש במשחק
            string queryQuestionCount = "SELECT Count(questionID) FROM Questions WHERE gameID=@ID";
            var recordQuestionCount = await _db.GetRecordsAsync<int>(queryQuestionCount, param);
            int numberOfQuestions = recordQuestionCount.FirstOrDefault();

            //נשמור משתנה ריק שיכיל את שאילתת העדכון בהתאם למספר השאלות
            string updateQuery;
            //אם יש מספיק שאלות במשחק
            if (numberOfQuestions >= minQuestions) 
            {
                // אנחנו שולפים את המזהים של השאלות ששייכות למשחק הזה
                string queryGetQuestions = "SELECT questionID FROM Questions WHERE gameID=@ID";
                var questionIds = await _db.GetRecordsAsync<int>(queryGetQuestions, param);
    
                // נקודת המוצא היא שניתן לפרסם, אלא אם הלולאה תמצא שאלה לא תקינה
                canPublish = true;

                // לולאה שעוברת שאלה-שאלה ובודקת את הפריטים 
                foreach (int qId in questionIds)
                {
                    // אובייקט פרמטרים שמכיל לנו את המזהה של השאלה הנוכחית בלולאה
                    object itemParam = new { QID = qId };
        
                    // ספירת הפריטים ששייכים אך ורק לשאלה הספציפית הזו
                    string queryItemCount = "SELECT Count(answerID) FROM Items WHERE questionID=@QID";
                    var recordItemCount = await _db.GetRecordsAsync<int>(queryItemCount, itemParam);
                    int itemsInThisQuestion = recordItemCount.FirstOrDefault(); // משתנה שמכיל בתוכו את מספר הפריטים שיש בשאלה

                    // אנחנו בודקים אם כמות הפריטים חורגת מהטווח (פחות מהמינימום או יותר מהמקסימום שהגדרנו)
                    if (itemsInThisQuestion < minItemsPerQuestion || itemsInThisQuestion > maxItemsPerQuestion)
                    {
                        canPublish = false; // המשחק נפסל לפרסום
                        break; //יציאה מהלולאה
                    }
                }
            }
            // אנחנו מעדכנים את מצב הפרסום בטבלת המשחקים בהתאם לתוצאה 
            if (canPublish == true) 
            {
                updateQuery = "UPDATE Games SET canPublish=true WHERE gameID=@ID";
            }
            else
            {
                updateQuery = "UPDATE Games SET isPublish=false, canPublish=false WHERE gameID=@ID";
            }

            //מעדכנים את בסיס הנתונים
            int isUpdate = await _db.SaveDataAsync(updateQuery, param);
            
            //נחזיר משתנה בוליאני שאומר אם ניתן לפרסם את המשחק או לא
            return canPublish;
        }
        
    }
}