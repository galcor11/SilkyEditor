namespace AuthTemplate.Shared.Models;
//מחלקת DTO שפתחנו לצורך יצירה של שאלה חדשה
public class QuestionToAdd
{
    public string instruction { get; set; }
    public int gameID { get; set; } // מפתח זר שמקשר את השאלה למשחק
    public string startLabel{ get; set; }
    public string endLabel{ get; set; }
//  הרשימה של הפריטים שמרכיבים את השאלה
    public List<SilkyItemToAdd> Items { get; set; }
 
}