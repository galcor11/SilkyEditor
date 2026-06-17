using AuthTemplate.Shared.Models;
using Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UsersManager.Server;

namespace AuthTemplate.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //כדי לוודא שיש משתמש מחובר
    [ServiceFilter(typeof(AuthCheck))] 

    //קונטרולר שפתחנו בשביל עמוד עריכת השאלות
    public class QuestionsController : ControllerBase
    {
        //קישור ל-DBREPOSITORY
        private readonly DbRepository _db;
        public QuestionsController( DbRepository db) 
        { 
            _db = db;
        }
        // [HttpGet]
        // public async Task<ActionResult<int>> GetLoginUser(int authUserId)
        // {
        //     return Ok(authUserId);
        // }
        
        //שיטת GET שנועדה לשלוף נתונים מטבלת השאלות
        [HttpGet("getQuestions/{gameId}")]
        public async Task<IActionResult> GetQuestionsByGame(int gameId, int authUserId)
        {
           //בדיקות תקינות
            if (authUserId <= 0 || gameId <= 0) 
            {
                return BadRequest("Invalid request");
            }

            // אובייקט פרמטרים שמשמש אותנו לצורך בדיקה שהמשחק שהתקבל בנתיב אכן שייך למשתמש המחובר
            object authParam = new { 
                UserId = authUserId, 
                GameID = gameId 
            };
    //שאילתת SQL ששולפת את שם המשחק, כדי לוודא שהמשחק שייך כרגע למשתמש שמחובר כעת למערכת
            string checkQuery = "SELECT gameName FROM Games WHERE userID = @UserId AND gameID = @GameID";
            var checkRecords = await _db.GetRecordsAsync<string>(checkQuery, authParam);
            // משתנה שנועד לשמור לתוכו את התוצאה הראשונה, כלומר את שם המשחק
            string gameName = checkRecords.FirstOrDefault();

//בדיקת תקינוּת              
            if (string.IsNullOrWhiteSpace(gameName)) 
            {
                return BadRequest("המשחק לא קיים או שאיננו שייך לך");
            }
           

           //שאילתת SQL ששולפת לנו את כל השאלות ששייכות לַמשחק. 
            string questionsQuery = "SELECT questionID, instruction, startLabel, endLabel FROM Questions WHERE gameID = @GameID";
            var questionsRecords = await _db.GetRecordsAsync<Question>(questionsQuery, authParam);
                //אנחנו ממירים את המידע שחזר למבנה של רשימה
            List<Question> questionsList = questionsRecords.ToList();

            
            //לולאה שעוברת לנו על כל שאלה ברשימת השאלות
            foreach (Question question in questionsList)
            {
                //אובייקט הפרמטרים לשאילתה
                object itemParam = new { questionID = question.questionID };
        
                //שאילתת SQL שנועדה לשלוף את התכונות הרלוונטיות של הפריטים. הוספנו את פקודת ORDERBY כדי למיין לפי הסדר של האינדקסים 
                string itemsQuery = "SELECT answerID, content, isImage, orderIndex FROM Items WHERE questionID = @questionID ORDER BY orderIndex";
                var itemsRecords = await _db.GetRecordsAsync<SilkyItem>(itemsQuery, itemParam);
        //אנחנו מכניסים את הפריטים ששלפנו אל הרשימה של השאלה הספציפית
                question.Items = itemsRecords.ToList(); 
            }
            //מופע חדש של ה-DTO (יכיל את שם המשחק ואת רשימת השאלות)
            GameEditDto gameData = new GameEditDto();
                //אנחנו מכניסים את הנתונים (השם והשאלות) אל תוך התכונות של המופע
            gameData.gameName = gameName; 
            gameData.Questions = questionsList; 
//אנחנו מחזירים את המופע אם סטטוס הרשת תקין
            return Ok(gameData);
          
        }
        
        //שיטת פוסט שתפקידה לעדכן את פרָטי השאלה
        [HttpPost("updateQuestion/{questionId}")]
public async Task<IActionResult> UpdateQuestion(int authUserId, int questionId, QuestionToUpdate questionToUpdate)
{
    // כדי לוודא שהמשתמש מחובר
    if (authUserId > 0)
    {
        //  בדיקת בעלות 
        // אנחנו שולפים את ה-gameID של השאלה כדי לבדוק למי היא שייכת
        object qParam = new { QId = questionId };
        string getGameQuery = "SELECT gameID FROM Questions WHERE questionID = @QId";
        var gameRecords = await _db.GetRecordsAsync<int>(getGameQuery, qParam);
        int gameId = gameRecords.FirstOrDefault();

        if (gameId > 0)
        {
            // נבדוק אם המשחק שייך למשתמש המחובר
            object authParam = new { GId = gameId, UId = authUserId };
            string authQuery = "SELECT gameID FROM Games WHERE gameID = @GId AND userID = @UId";
            var authRecords = await _db.GetRecordsAsync<int>(authQuery, authParam);
            int isOwner = authRecords.FirstOrDefault();

            if (isOwner > 0)
            {
                //  אובייקט פרמטרים לעדכון השאלה 
                object updateParam = new {
                    Instruction = questionToUpdate.instruction,
                    StartLabel = questionToUpdate.startLabel,
                    EndLabel = questionToUpdate.endLabel,
                    QId = questionId
                };

                // שאילתת העדכון מעדכנת רק את העמודות הרלוונטיות לשאלה
                string updateQuery = "UPDATE Questions SET instruction=@Instruction, startLabel=@StartLabel, endLabel=@EndLabel WHERE questionID=@QId";
                
                // SaveDataAsync מחזירה את כמות הרשומות שהושפעו
                int isUpdated = await _db.SaveDataAsync(updateQuery, updateParam);

                if (isUpdated > 0)
                {
                    return Ok(); // העדכון הצליח
                }
                return BadRequest("Update Failed");
            }
            return BadRequest("It's Not Your Game");
        }
        return BadRequest("Question not found");
    }
    return Unauthorized("user is not authenticated");
}

//שיטת פוסט שנועדה לעדכן פריט בודד
   [HttpPost("updateItem/{itemId}")]
public async Task<IActionResult> UpdateSilkyItem(int authUserId, int itemId, SilkyItemToUpdate itemToUpdate)
{
    if (authUserId > 0)
    {
        // בדיקות בעלות
        // אנחנו שולפים את ה-questionID שאליו שייך הפריט
        object iParam = new { Id = itemId };
        string getQuestionQuery = "SELECT questionID FROM Items WHERE answerID = @Id";
        var questionRecords = await _db.GetRecordsAsync<int>(getQuestionQuery, iParam);
        int questionId = questionRecords.FirstOrDefault();

        if (questionId > 0)
        {
            // אנחנו שולפים את ה-gameID שאליו שייכת השאלה
            object qParam = new { QId = questionId };
            string getGameQuery = "SELECT gameID FROM Questions WHERE questionID = @QId";
            var gameRecords = await _db.GetRecordsAsync<int>(getGameQuery, qParam);
            int gameId = gameRecords.FirstOrDefault();

            if (gameId > 0)
            {
                // אנחנו מוודאים שהמשחק שייך למשתמש
                object authParam = new { GId = gameId, UId = authUserId };
                string authQuery = "SELECT gameID FROM Games WHERE gameID = @GId AND userID = @UId";
                var authRecords = await _db.GetRecordsAsync<int>(authQuery, authParam);
                int isOwner = authRecords.FirstOrDefault();

                if (isOwner > 0)
                {
                    //  עדכון הפריט 
                    // אנחנו משתמשים בנתונים שהגיעו מה-DTO (התוכן, התמונה, האינדקס)
                    object updateParam = new {
                        Content = itemToUpdate.content,
                        IsImage = itemToUpdate.isImage,
                        OrderIndex = itemToUpdate.orderIndex, 
                        Id = itemId
                    };

                    string updateQuery = "UPDATE Items SET content=@Content, isImage=@IsImage, orderIndex=@OrderIndex WHERE answerID=@Id";
                    int isUpdated = await _db.SaveDataAsync(updateQuery, updateParam);

                    if (isUpdated > 0)
                    {
                        return Ok();
                    }
                    return BadRequest("Update Item Failed");
                }
                return BadRequest("It's Not Your Game");
            }
        }
        return BadRequest("Item or Question not found");
    }
    return Unauthorized("user is not authenticated");
}     
        

    }
}
