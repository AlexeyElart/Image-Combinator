using System;


namespace Image_Combinator.Models
{
    class PictureException
    {
        public string EbayItemID { get; set; }
        public int ShopID { get; set; }
        public string URL { get; set; }
        public DateTime UpdateDate {get; set;}
    }
}
