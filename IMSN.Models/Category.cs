using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IMSNBook.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        [MaxLength(30)]
        public string Name { get; set; }

        [Display(Name = "Display Order")]
        [Range(1, 100, ErrorMessage = "Display Order must be within range 1 to 100!")]
        public int DisplayOrder { get; set; }
    }
}
