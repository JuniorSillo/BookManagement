

using System.ComponentModel.DataAnnotations;

namespace BookMgr.Models
{
    public class Book
    {
        public int Id { get; set; }
        [Required]
        public string ?Title { get; set; }
        [Required]
        public string ?ISBN { get; set; }
        public DateTime PublicationDate { get; set; }
        public int PublisherId { get; set; }
        public Publisher ?Publisher { get; set; }
        public ICollection<BookAuthor> ?BookAuthors { get; set; }
    }
}
