using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Image_Combinator.Models
{
    public class Shop
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "SiteID is required")]
        public int SiteID { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(40, ErrorMessage = "Description length should be <= 40")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Alias is required")]
        [StringLength(10, ErrorMessage = "Alias length should be <= 10")]
        public string Alias { get; set; }

        [StringLength(10, ErrorMessage = "PS length should be <= 10")]
        public string PS { get; set; }

        [Required(ErrorMessage = "SiteID is required")]
        public int LangID { get; set; }

        public Shop()
        {

        }

        public Shop(int siteID, string description, string alias, string ps, int langID)
        {
            SiteID = siteID;
            Description = description;
            Alias = alias;
            PS = ps;
            LangID = langID;
        }
    }
}
