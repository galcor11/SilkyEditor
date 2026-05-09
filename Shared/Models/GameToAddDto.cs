using System.ComponentModel.DataAnnotations;

namespace AuthTemplate.Shared.Models;
// מחלקה שאנחנו מעבירים בה את הפרטים של המשחק לצורך יצירת משחק חדש
public class GameToAddDto
{
    [Required(ErrorMessage = "שדה חובה")]
    [MinLength(1, ErrorMessage = "יש להזין לפחות תו אחד")]
    [StringLength(20, ErrorMessage = "מספר התווים המקסימלי הוא 20")]
    public string gameName { get; set; } 
   
    
    [Required(ErrorMessage = "חובה לבחור זמן לשאלה")]
    public int time { get; set; }
    
    public bool hasPotion { get; set; }
    
}