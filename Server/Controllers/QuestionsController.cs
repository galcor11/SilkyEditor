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

//שיטת פוסט לצורך יצירה של שאלה חדשה
[HttpPost("addQuestion")]
public async Task<IActionResult> AddQuestion([FromBody] QuestionToAdd questionToAdd) 
{
    // בדיקות תקינות
    if (questionToAdd == null || string.IsNullOrWhiteSpace(questionToAdd.instruction))
    {
        return BadRequest("יש להזין הנחיה לשאלה");
    }

    // 2. הכנת האובייקט לשאילתת ההוספה נטו לפי חומרי הקורס
    object insertParam = new {
        instruction = questionToAdd.instruction,
        gameID = questionToAdd.gameID,
        startLabel = questionToAdd.startLabel,
        endLabel = questionToAdd.endLabel
    };
        
    // שאילתת ההוספה
    string insertQuery = "INSERT INTO Questions (instruction, gameID, startLabel, endLabel) VALUES (@instruction, @gameID, @startLabel, @endLabel)";
        
    // ה-ID החדש שנוצר
    int newId = await _db.InsertReturnIdAsync(insertQuery, insertParam);
        
    if (newId > 0)
    {
        //אנחנו שולפים את השאלה החדשה שנוצרה כדי להחזיר אותה לעמוד
        object selectParam = new { id = newId };
        string selectQuery = "SELECT * FROM Questions WHERE questionID = @id"; 
        var records = await _db.GetRecordsAsync<Question>(selectQuery, selectParam); 
        var createdQuestion = records.FirstOrDefault();
            
        return Ok(createdQuestion);
    }
        
    return BadRequest("שגיאה ביצירת השאלה בבסיס הנתונים");
}










// שיטת פוסט שנועדה להוסיף פריט חדש לשאלה
[HttpPost("addItem")]
public async Task<IActionResult> AddSilkyItem(int authUserId,  SilkyItemToAdd itemToAdd)
{
    if (authUserId > 0)
    {
        //בדיקת אבטחה ובעלות: שולפים את ה-gameID של השאלה כדי לוודא שהיא קיימת
        object qParam = new { QId = itemToAdd.questionID };
        string getGameQuery = "SELECT gameID FROM Questions WHERE questionID = @QId";
        var gameRecords = await _db.GetRecordsAsync<int>(getGameQuery, qParam);
        int gameId = gameRecords.FirstOrDefault();

        if (gameId > 0)
        {
            //  מוודאים שהמשחק שייך למשתמש המחובר
            object authParam = new { GId = gameId, UId = authUserId };
            string authQuery = "SELECT gameID FROM Games WHERE gameID = @GId AND userID = @UId";
            var authRecords = await _db.GetRecordsAsync<int>(authQuery, authParam);
            int isOwner = authRecords.FirstOrDefault();

            if (isOwner > 0)
            {
                //  אובייקט הפרמטרים 
                //  אנחנו מקשרים את הפריט ל-questionID שהגיע מהנתיב
                object insertParam = new {
                    Content = itemToAdd.content,
                    IsImage = itemToAdd.isImage,
                    OrderIndex = itemToAdd.orderIndex,
                    QId = itemToAdd.questionID
                };

                // שאילתת ה-INSERT שמוסיפה את השורה החדשה לטבלת Items
                string insertQuery = "INSERT INTO Items (content, isImage, orderIndex, questionID) VALUES (@Content, @IsImage, @OrderIndex, @QId)";
                
                int newAnswerId = await _db.InsertReturnIdAsync(insertQuery, insertParam);
//בדיקה אם ההוספה הצליחה               
                if (newAnswerId > 0)
                {
                    SilkyItem createdItem = new SilkyItem()
                    {
                        answerID = newAnswerId,
                        content = itemToAdd.content,
                        isImage = itemToAdd.isImage,
                        orderIndex = itemToAdd.orderIndex
                    };
                    return Ok(createdItem); 
                }
                return BadRequest("Insert Item Failed");
            }
            return BadRequest("It's Not Your Game");
        }
        return BadRequest("Question not found");
    }
    return Unauthorized("user is not authenticated");
}

// שיטה שמיועדת למחוק שאלה שלמה
[HttpDelete("deleteQuestion/{questionId}")]
public async Task<IActionResult> DeleteQuestion(int questionId)
{
    // בדיקת תקינות בסיסית
    if (questionId <= 0)
    {
        return BadRequest("מזהה שאלה לא תקין");
    }

    //אובייקט הפרמטרים
    object param = new { id = questionId };

   //שאילתת SQL למחיקה
    string query = "DELETE FROM Questions WHERE questionID=@id";

    // הפעלת השאילתה מול בסיס הנתונים 
    int isDeleted = await _db.SaveDataAsync(query, param);
//בדיקה אם המחיקה הצליחה     
    if (isDeleted > 0)
    {
        return Ok();
    }
        
    return BadRequest("המחיקה נכשלה. ייתכן שהמזהה איננו קיים");
}


// שיטת מחיקה שתפקידה למחוק פריט בודד מגוף התולעת מתוך טבלת הפריטים שבבסיס הנתונים 
        [HttpDelete("deleteItem/{itemId}")]
        public async Task<IActionResult> DeleteItem(int authUserId, int itemId)
        {
            // בדיקה שהמשתמש מחובר למערכת
            if (authUserId > 0)
            {
                //  אנחנו שולפים את ה-questionID של הפריט כדי לדעת לאיזו שאלה הוא שייך 
                object itemOwnerParam = new { ItemId = itemId };
                string getQuestionQuery = "SELECT questionID FROM Items WHERE answerID = @ItemId"; 
                var questionRecords = await _db.GetRecordsAsync<int>(getQuestionQuery, itemOwnerParam);
                int questionId = questionRecords.FirstOrDefault();

                // אם הפריט נמצא ויש לו שאלה מקושרת
                if (questionId > 0)
                {
                    // אנחנו שולפים את ה-gameID של השאלה כדי לדעת לאיזה משחק היא שייכת 
                    object qParam = new { QId = questionId };
                    string getGameQuery = "SELECT gameID FROM Questions WHERE questionID = @QId";
                    var gameRecords = await _db.GetRecordsAsync<int>(getGameQuery, qParam);
                    int gameId = gameRecords.FirstOrDefault();

                    // אם השאלה נמצאה ויש לה משחק מקושר
                    if (gameId > 0)
                    {
                        //  בודקים אם המשחק שייך למשתמש המחובר 
                        object authParam = new { GId = gameId, UId = authUserId };
                        string authQuery = "SELECT gameID FROM Games WHERE gameID = @GId AND userID = @UId";
                        var authRecords = await _db.GetRecordsAsync<int>(authQuery, authParam);
                        int isOwner = authRecords.FirstOrDefault();

                        // אם המשחק אכן שייך למשתמש
                        if (isOwner > 0)
                        {
                            // אובייקט הפרמטרים למחיקה
                            object itemParam = new { ItemId = itemId };

                            // שאילתת SQL שמוחקת לנו את הפריט הספציפי מטבלת Items 
                            string deleteQuery = "DELETE FROM Items WHERE answerID=@ItemId"; 
                
                            // ביצוע הפעולה מול בסיס הנתונים ושמירת מספר השורות שהושפעו
                            int isDeleted = await _db.SaveDataAsync(deleteQuery, itemParam);

                            // בדיקה אם המחיקה הצליחה 
                            if (isDeleted > 0)
                            {
                                return Ok(); 
                            }
                
                            return BadRequest("Delete Failed"); 
                        }
                        return BadRequest("It's Not Your Game");
                    }
                    return BadRequest("Game not found");
                }
                return BadRequest("Item not found"); 
            }
            return Unauthorized("user is not authenticated");
        }






    }
}
