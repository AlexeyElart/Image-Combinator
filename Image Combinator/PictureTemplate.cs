using ImageMagick;
using Newtonsoft.Json;
using System.Text;

namespace Image_Combinator
{
    public class PictureTemplate
    {
        public TemplateElement Logo { get; set; }
        public TemplateElement Brand { get; set; }
        public TemplateElement GenericImage { get; set; }
        public TemplateElement Count { get; set; }
        public PartNumberType PartNumber { get; set; }
        public BaseImgTemplate BaseImage { get; set; }
        public WatermarkTemplate Watermark { get; set; }
        public int CellSize { get; set; }
        public int GridSize { get; set; }

        public static PictureTemplate GetTemplateFromByte(byte[] array)
        {
            string resStr = Encoding.UTF8.GetString(array);

            return JsonConvert.DeserializeObject<PictureTemplate>(resStr);
        }
    }

    public class TemplateElement
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public AnchorType Anchor { get; set; }
    }

    public class PartNumberType : TemplateElement
    {
        public MagickReadSettings readSettings { get; set; }
    }

    public class BaseImgTemplate
    {
        public int SizeKoef { get; set; }
        public double VerticalPositionKoef { get; set; }
    }

    public class WatermarkTemplate
    {
        public int TransparencyLevel { get; set; }
        public int PercentSizeBaseImage { get; set; }
    }  
}
