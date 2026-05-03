namespace AuthTemplate.Shared.Models;

public class QuestionsDto
{
    public string endLabel { get; set; }
    public int gameID { get; set; }
    public string instruction { get; set; }
    public int questionID { get; set; }
    public string startLabel { get; set; }
    public List<ItemsDto> Items { get; set; }

}