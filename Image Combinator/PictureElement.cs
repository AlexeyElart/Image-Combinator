using ImageMagick;
using System;

namespace Image_Combinator
{
    class PictureElement : ICloneable
    {
        public PictureElement()
        {

        }

        public PictureElement(string path, Point center, double minDistanceToBoundingPoint, int correctionKoef, string logoPath, int watermarkTransparencyLevel, double watermarkSizePercentOfBaseImage)
        {
            Anchor = AnchorType.Center;
            MagickImage image;
            if (path.Contains("http"))
            {
                System.Net.WebClient webCl = new System.Net.WebClient();
                image = new MagickImage(webCl.DownloadData(path));
            }
            else
            {                 
                image = new MagickImage(path);
                //image.Alpha(AlphaOption.Set);
                //image.ColorFuzz = new Percentage(10);
                //image.Opaque(MagickColor.FromRgba(0, 0, 0, 0), MagickColors.White);
            }


            //image.Write("1.jpg");
            Image = image;

            double koeff = minDistanceToBoundingPoint / Point.GetDistance(new Point(Image.Width, Image.Height), new Point(0, 0)) * (correctionKoef / 100.0);
            Image.Resize((int)Math.Round(Image.Width * koeff, 0), (int)Math.Round(Image.Height * koeff, 0));
            StartPoint = new Point(center.X - Image.Width / 2, center.Y - Image.Height / 2);
            EndPoint = new Point(StartPoint.X + Image.Width, StartPoint.Y + Image.Height);

            if (logoPath != "")
            {
                MagickImage watermark = new MagickImage(logoPath);
                watermark.Alpha(AlphaOption.Set);
                watermark.ColorFuzz = new Percentage(60);
                watermark.Opaque(MagickColors.White, MagickColor.FromRgba(0, 0, 0, 0));
                if (Image.Width / (double)Image.Height > 1)
                {
                    watermark.Resize((int)(Image.Width * watermarkSizePercentOfBaseImage), watermark.Height * (int)(Image.Width * watermarkSizePercentOfBaseImage) / watermark.Width);
                }
                else
                {
                    watermark.Resize(Image.Width * (int)(Image.Height * watermarkSizePercentOfBaseImage) / watermark.Height, (int)(watermark.Height * watermarkSizePercentOfBaseImage));
                }
                watermark.Evaluate(Channels.Alpha, EvaluateOperator.Divide, watermarkTransparencyLevel);
                Image.Composite(watermark, Gravity.Southeast, CompositeOperator.Over);

                Image.Composite(watermark, Gravity.Northwest, CompositeOperator.Over);
            }
            //image.Write("2.jpg");
        }

        public PictureElement(int gridSize, int cellSize, string path, int cellWidth, int cellHeight, AnchorType anchor)
        {
            Anchor = anchor;
            MagickImage image = new MagickImage(path);
            Image = image;
            CellWidth = cellWidth * cellSize;
            CellHeight = cellHeight * cellSize;
            StartPoint = new Point(0, 0);
            EndPoint = new Point(0, 0);

            SetSizeElement();

            switch (Anchor)
            {
                case AnchorType.TopLeft:
                    StartPoint.X = 0;
                    EndPoint.X = StartPoint.X + Image.Width;
                    StartPoint.Y = 0;
                    EndPoint.Y = StartPoint.Y + Image.Height;
                    break;
                case AnchorType.TopRight:
                    EndPoint.X = gridSize * cellSize - 1;
                    StartPoint.X = EndPoint.X - Image.Width;
                    StartPoint.Y = 0;
                    EndPoint.Y = StartPoint.Y + Image.Height;
                    break;
                case AnchorType.BottomLeft:
                    StartPoint.X = 0;
                    EndPoint.X = StartPoint.X + Image.Width;
                    EndPoint.Y = gridSize * cellSize - 1;
                    StartPoint.Y = EndPoint.Y - Image.Height;
                    break;
                case AnchorType.BottomRight:
                    EndPoint.X = gridSize * cellSize - 1;
                    StartPoint.X = EndPoint.X - Image.Width;
                    EndPoint.Y = gridSize * cellSize - 1;
                    StartPoint.Y = EndPoint.Y - Image.Height;
                    break;
                case AnchorType.CenterLeft:
                    StartPoint.X = 0;
                    EndPoint.X = EndPoint.X + Image.Width;
                    StartPoint.Y = ((gridSize * cellSize - 1) - Image.Height) / 2;
                    EndPoint.Y = EndPoint.Y + Image.Height;
                    break;
                case AnchorType.CenterRight:
                    EndPoint.X = gridSize * cellSize - 1;
                    StartPoint.X = EndPoint.X - Image.Width;
                    StartPoint.Y = ((gridSize * cellSize - 1) - Image.Height) / 2;
                    EndPoint.Y = EndPoint.Y + Image.Height;
                    break;
                case AnchorType.AfterBottomLeft:
                    StartPoint.X = ((gridSize * cellSize - 1) - Image.Width) / 2; ;
                    EndPoint.X = StartPoint.X + Image.Width;
                    EndPoint.Y = gridSize * cellSize - 1;
                    StartPoint.Y = EndPoint.Y - Image.Height;
                    break;
                case AnchorType.CenterTop:
                    StartPoint.X = ((gridSize * cellSize - 1) - Image.Width) / 2; ;
                    EndPoint.X = StartPoint.X + Image.Width;
                    StartPoint.Y = 0;
                    EndPoint.Y = EndPoint.Y + Image.Height;
                    break;
                case AnchorType.CenterBottom:
                    StartPoint.X = ((gridSize * cellSize - 1) - Image.Width) / 2; ;
                    EndPoint.X = StartPoint.X + Image.Width;
                    EndPoint.Y = gridSize * cellSize - 1;
                    StartPoint.Y = EndPoint.Y - Image.Height;
                    break;
            }
        }

        public PictureElement(int gridSize, int cellSize, MagickImage image, int cellWidth, int cellHeight, AnchorType anchor)
        {
            Anchor = anchor;
            Image = image;
            CellWidth = cellWidth * cellSize;
            CellHeight = cellHeight * cellSize;
            StartPoint = new Point(0, 0);
            EndPoint = new Point(0, 0);

            SetSizeElement();

            switch (Anchor)
            {
                case AnchorType.TopLeft:
                    StartPoint.X = 0;
                    EndPoint.X = StartPoint.X + Image.Width;
                    StartPoint.Y = 0;
                    EndPoint.Y = StartPoint.Y + Image.Height;
                    break;
                case AnchorType.TopRight:
                    EndPoint.X = gridSize * cellSize - 1;
                    StartPoint.X = EndPoint.X - Image.Width;
                    StartPoint.Y = 0;
                    EndPoint.Y = StartPoint.Y + Image.Height;
                    break;
                case AnchorType.BottomLeft:
                    StartPoint.X = 0;
                    EndPoint.X = StartPoint.X + Image.Width;
                    EndPoint.Y = gridSize * cellSize - 1;
                    StartPoint.Y = EndPoint.Y - Image.Height;
                    break;
                case AnchorType.BottomRight:
                    EndPoint.X = gridSize * cellSize - 1;
                    StartPoint.X = EndPoint.X - Image.Width;
                    EndPoint.Y = gridSize * cellSize - 1;
                    StartPoint.Y = EndPoint.Y - Image.Height;
                    break;
                case AnchorType.CenterLeft:
                    StartPoint.X = 0;
                    EndPoint.X = EndPoint.X + Image.Width;
                    StartPoint.Y = ((gridSize * cellSize - 1) - Image.Height) / 2;
                    EndPoint.Y = EndPoint.Y + Image.Height;
                    break;
                case AnchorType.CenterRight:
                    EndPoint.X = gridSize * cellSize - 1;
                    StartPoint.X = EndPoint.X - Image.Width;
                    StartPoint.Y = ((gridSize * cellSize - 1) - Image.Height) / 2;
                    EndPoint.Y = EndPoint.Y + Image.Height;
                    break;
                case AnchorType.AfterBottomLeft:
                    StartPoint.X = ((gridSize * cellSize - 1) - Image.Width) / 2; ;
                    EndPoint.X = StartPoint.X + Image.Width;
                    EndPoint.Y = gridSize * cellSize - 1;
                    StartPoint.Y = EndPoint.Y - Image.Height;
                    break;
                case AnchorType.CenterTop:
                    StartPoint.X = ((gridSize * cellSize - 1) - Image.Width) / 2; ;
                    EndPoint.X = StartPoint.X + Image.Width;
                    StartPoint.Y = 0;
                    EndPoint.Y = EndPoint.Y + Image.Height;
                    break;
                case AnchorType.CenterBottom:
                    StartPoint.X = ((gridSize * cellSize - 1) - Image.Width) / 2; ;
                    EndPoint.X = StartPoint.X + Image.Width;
                    EndPoint.Y = gridSize * cellSize - 1;
                    StartPoint.Y = EndPoint.Y - Image.Height;
                    break;
            }
        }

        private void SetSizeElement()
        {
            if (CellWidth == 0 || CellHeight == 0)
            {
                Image.Resize(0, 0);
            }
            if (Image.Width / (double)Image.Height > CellWidth / (double)CellHeight)
            {
                Image.Resize(new Percentage(CellWidth / (double)Image.Width * 100));
            }
            else
            {
                Percentage percent = new Percentage(CellHeight / (double)Image.Height * 100);
                Image.Resize(percent);
            }
        }

        public int Width()
        {
            return EndPoint.X - StartPoint.X;
        }

        public int Height()
        {
            return EndPoint.Y - StartPoint.Y;
        }

        public object Clone()
        {
            return new PictureElement()
            {
                StartPoint = this.StartPoint,
                EndPoint = this.EndPoint,
                Image = (MagickImage)this.Image.Clone(),
                Anchor = this.Anchor
            };
        }


        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public MagickImage Image { get; set; }
        public AnchorType Anchor { get; set; }
        public int CellWidth { get; set; }
        public int CellHeight { get; set; }
    }

    public enum AnchorType
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        CenterLeft,
        CenterRight,
        AfterBottomLeft,
        CenterTop,
        CenterBottom,
        Center
    }

    public class Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public static double GetDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow((p1.X - p2.X), 2) + Math.Pow((p1.Y - p2.Y), 2));
        }
    }
}
