namespace AuthTemplate.Shared.Models;
//מחלקת שאלות שפתחנו בשביל עריכת השאלות
public class Question
{
    public int questionID { get; set; }
    public string instruction { get; set; } 
    //רשימה שמכילה את הפריטים
    public List<Item> Items { get; set; } 

}