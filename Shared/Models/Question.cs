namespace AuthTemplate.Shared.Models;
//מחלקת שאלות שפתחנו בשביל עריכת השאלות
public class Question
{
    public int questionID { get; set; }
    public string instruction { get; set; } 
    public string startLabel { get; set; }
    public string endLabel { get; set; }

    //רשימה שמכילה את הפריטים
    public List<Item> Items { get; set; } 

}