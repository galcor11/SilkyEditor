using System.ComponentModel.DataAnnotations;

namespace AuthTemplate.Shared.Models;
//מחלקת DTO שפתחנו בשביל האזור להוספת פריט
public class SilkyItem
{
    // המזהה הייחודי של הפריט כדי שנדע איזה פריט לעדכן  
    public int answerID { get; set; } 
    // false = טקסט (ברירת מחדל), true = תמונה
    public bool isImage { get; set; }  

    public string content { get; set; } 
    public int orderIndex { get; set; } 
}