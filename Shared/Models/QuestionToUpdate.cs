namespace AuthTemplate.Shared.Models;

public class QuestionToUpdate
{
    //מחלקה שפתחנו לצורך עדכון של שאלה 
    public string instruction { get; set; }
    public int gameID { get; set; } 
    public string startLabel{ get; set; }
    public string endLabel{ get; set; }
}