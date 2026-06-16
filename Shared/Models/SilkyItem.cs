using System.ComponentModel.DataAnnotations;

namespace AuthTemplate.Shared.Models;
//מחלקת DTO שפתחנו בשביל האזור להוספת פריט
public class SilkyItem
{
    // false = טקסט (ברירת מחדל), true = תמונה
    public bool isImage { get; set; } = false; 

    [StringLength(30, ErrorMessage = "התוכן יכול להכיל עד 30 תווים")]
    public string content { get; set; } 
}