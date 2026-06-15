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
                var itemsRecords = await _db.GetRecordsAsync<Item>(itemsQuery, itemParam);
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
    }
}
