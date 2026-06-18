using Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

    //קונטרולר שמשמש לצורך העלאת תמונה
namespace AuthTemplate.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        //משתנה פנימי בשביל מחלקת העזר
        private readonly FilesManage _filesManage;   
        
       //מאפשר לפנות דרך הקונטרולר לשיטת במחלקת העזר
        public MediaController(FilesManage filesManage)
        {
            _filesManage = filesManage;
        } 
        //שיטת פוסט שמקבלת תמונה מהבלייזור ושומרת אותה בַתיקייה
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromBody] string imageBase64)
        {
            // הפונקציה מקבלת את התמונה מממשק המשתמש, קובעת את הסיומת (png), 
            // ושומרת אותה בַּתיקייה uploadedFiles
            string fileName = await _filesManage.SaveFile(imageBase64, "png", "uploadedFiles");
    
            // מחזירים לבלייזור את השם הֶחדש שהתמונה קיבלה בתיקיית הקבצים
            return Ok(fileName);
        }
        
    }
}
