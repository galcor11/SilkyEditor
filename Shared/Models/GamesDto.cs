namespace AuthTemplate.Shared.Models;

public class GamesDto
{
    // מחלקת משחקים
    public bool canPublish { get; set; }
    public string gameName { get; set; }
    public int gameCode { get; set; }
    public int gameID { get; set; }
    public bool hasPotion { get; set; }
    public bool isPublish { get; set; }
    public int time { get; set; }
    public int userID { get; set; }
    public List<QuestionsDto> Questions { get; set; }

}