using System.ComponentModel.DataAnnotations;

namespace CodeBuddy.Data
{
    public class SearchModel
    {
        [Required] public string Prompt { get; set; }
    }
}
