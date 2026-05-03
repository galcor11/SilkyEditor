using Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using AuthTemplate.Server.Controllers;
using AuthTemplate.Shared.Models;

namespace AuthTemplate.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnityController : ControllerBase
    {
        private readonly DbRepository _db;

        public UnityController(DbRepository db)
        {
            _db = db;
        }
        
        //  שיטת קונטרולר שתפקידה לשלוף את הקוד מבסיס הנתונים

        [HttpGet ("GetCode/{gameCode}")]
        public async Task<IActionResult> GetCode(int gameCode)
        {
            
            object param = new 
                { GameCode = gameCode };
            // שאילתה ששולפת את ה-ID של המשחק, שיקוי, זמן, יכול להתפרסם, האם פורסם ואת שם המשחק (כאשר קוד המשחק שווה לקוד המשחק שהמשתמש הקליד) 
            string query = "SELECT gameID, hasPotion, time, canPublish, isPublish, gameName FROM Games WHERE gameCode=@GameCode";
            //משתנה ששומר בתוכו את הפנייה שהתקבלה מהשאילתה לתוך מבנה DTO של מחלקת המשחקים. 
            var records=await _db.GetRecordsAsync<GamesDto>(query,param);
            //מחזיר את הרשומה הראשונה שהתקבלה לתוך מופע של מחלקת המשחקים.
            GamesDto myGame = records?.FirstOrDefault();
           
            //בדיקות תקינוּת             
            if (myGame == null)
            {
                return NotFound("לא נמצא משחק עם הקוד שהוזן."); 
            }

            if (myGame.isPublish == false)
            {
                return BadRequest("המשחק קיים, אך טרם פורסם"); 
            }

            object questionParam = new 
                {GameID=myGame.gameID};
            string questionsQuery = "SELECT questionID, instruction, startLabel, endLabel FROM Questions WHERE gameID = @GameID"; 
            var questionRecords = await _db.GetRecordsAsync<QuestionsDto>(questionsQuery,questionParam);
            List<QuestionsDto> Questions = questionRecords?.ToList(); //המרה לתוך מבנה של רשימה

            if (Questions != null)
            {
                //לולאת FOREACH שעוברת לנו על כל שאלה ברשימת השאלות כדי לשלוף את הפריטים שלה
                foreach (QuestionsDto question in Questions)
                {
                  object itemParam=new 
                      {QuestionID=question.questionID};
                  string itemQuery = "SELECT answerID,content, isImage, orderIndex FROM Items WHERE questionID =@QuestionID"; 
                  var itemRecords = await _db.GetRecordsAsync<ItemsDto>(itemQuery,itemParam);
                  //מכניסים את הרשימה שחזרה אל השאלה הנוכחית
                  question.Items = itemRecords?.ToList();
                }
                //מכניסים את כל השאלות לתוך רשימת השאלות שהגדרנו ב-DTO של המשחק
                myGame.Questions = Questions; 
            }
            return Ok(myGame); 
        }  
    }
}
