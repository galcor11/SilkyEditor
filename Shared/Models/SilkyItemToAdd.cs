namespace AuthTemplate.Shared.Models;
// מחלקת DTO שפתחנו כדי לשלוח פרטי סדר חדשים שהמשתמש יוצר 
public class SilkyItemToAdd
{
    public string content { get; set; } 
    public bool isImage { get; set; }
    
    //מפתח זר שמקשר את הפריט לשאלה הספציפית
    public int questionID { get; set; }   
}