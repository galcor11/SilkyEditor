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
        
        
        
    }
}
