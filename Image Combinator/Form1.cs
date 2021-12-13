using ImageMagick;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using Dapper;
using Image_Combinator.Models;
using System.Linq.Dynamic;
using Image_Combinator.Utils;
using System.Net;

namespace Image_Combinator
{
    public partial class Form1 : Form
    {
        int CELL_SIZE = 32;
        int GRID_SIZE = 16;
        int IMAGE_SIZE;
        MagickImage result;
        MagickReadSettings readSettings;
        string connectionString = "SERVER=172.16.10.48;DATABASE=Elart;UID=ImageCombinator;PASSWORD=?user4Image;SslMode=none;Charset=utf8;Default Command Timeout = 3000;Connect Timeout=600;Pooling=false;";
        List<TemplateDB> rowsInDb;
        List<(int, byte[], int)> templates;
        List<Shop> shops;
        private bool sortAscending = false;
        private bool sortAscendingExc = false;
        List<TemplateDB> picturesTemplatesFull;
        List<PictureException> pictureExceptions = new List<PictureException>();

        public Form1()
        {
            InitializeComponent();
            IMAGE_SIZE = CELL_SIZE * GRID_SIZE;
            comboBox1.DataSource = Enum.GetValues(typeof(AnchorType));
            comboBox2.DataSource = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            comboBox3.DataSource = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            comboBox6.DataSource = Enum.GetValues(typeof(AnchorType));
            comboBox5.DataSource = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            comboBox4.DataSource = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            comboBox9.DataSource = Enum.GetValues(typeof(AnchorType));
            comboBox8.DataSource = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            comboBox7.DataSource = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            comboBox12.DataSource = Enum.GetValues(typeof(AnchorType));
            comboBox11.DataSource = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            comboBox10.DataSource = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            comboBox15.DataSource = Enum.GetValues(typeof(AnchorType));
            comboBox14.DataSource = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            comboBox13.DataSource = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

            label22.Text = trackBar1.Value + "%";
            label25.Text = trackBar2.Value + "%";


#if DEBUG
            label22.Text = trackBar1.Value + "%";
            label25.Text = trackBar2.Value + "%";
            label28.Text = trackBar3.Value.ToString();
            label2.Visible = true;
            label3.Visible = true;
            label5.Visible = true;
            label7.Visible = true;
            label20.Visible = true;
            label31.Visible = true;
            comboBox2.SelectedIndex = 4;
            comboBox3.SelectedIndex = 2;
            comboBox5.SelectedIndex = 5;
            comboBox4.SelectedIndex = 5;
            comboBox8.SelectedIndex = 7;
            comboBox7.SelectedIndex = 2;
            comboBox11.SelectedIndex = 4;
            comboBox10.SelectedIndex = 1;
            comboBox14.SelectedIndex = 2;
            comboBox13.SelectedIndex = 2;
            comboBox1.SelectedIndex = 0;
            comboBox6.SelectedIndex = 1;
            comboBox9.SelectedIndex = 3;
            comboBox12.SelectedIndex = 2;
            comboBox15.SelectedIndex = 6;
#endif
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                while (conn.State != System.Data.ConnectionState.Open)
                {
                    try
                    {
                        conn.Open();
                    }
                    catch
                    {
                        Task.Delay(50);
                    }

                    rowsInDb = GetTemplatesFromDB(conn);

                }
                conn.Close();
            }


        }

        private List<TemplateDB> GetTemplatesFromDB(MySqlConnection conn)
        {
            string queSelect = $"select * from Elart.BrandPicrureTemplates";

            picturesTemplatesFull = conn.Query<TemplateDB>(queSelect).ToList();
            dataGridView1.DataSource = picturesTemplatesFull;
            queSelect = "SELECT DISTINCT SI.EbayItemID, SI.ShopID, VP.URL, VP.UpdateDate FROM Elart.ItemsValidPictureReplacement VP " +
                        "JOIN Elart.ShopItems SI ON VP.ItemID = SI.ItemID AND SI.EbayListingStatus = 0; ";
            pictureExceptions = conn.Query<PictureException>(queSelect).ToList();
            dataGridViewException.DataSource = pictureExceptions;

            //foreach (DataGridViewColumn column in dataGridView1.Columns)
            //{

            //    column.SortMode = DataGridViewColumnSortMode.Automatic;
            //}

            string queSelect2 = $"select TecdocSupplierID, Template, ShopID from Elart.BrandPicrureTemplates";

            templates = conn.Query<(int, byte[], int)>(queSelect2).ToList();
            return picturesTemplatesFull;
        }

        private void GeneratePicture(List<string> fileNames)
        {
            var files = Directory.GetFiles("Base Images");

            string brandPath = "204.png";
            string logoPath = "logo3.png";
            string generic = "Generic_text.png";
            string SaveImagePath = "\\result3\\";

            List<PictureElement> elements = new List<PictureElement>();
            elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, logoPath, (int)comboBox2.SelectedItem, (int)comboBox3.SelectedItem, AnchorType.TopLeft));
            elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, brandPath, (int)comboBox5.SelectedItem, (int)comboBox4.SelectedItem, AnchorType.TopRight));
            elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, generic, (int)comboBox8.SelectedItem, (int)comboBox7.SelectedItem, AnchorType.BottomRight));
            elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, GetTextPictureElement("label:9889734569"), (int)comboBox11.SelectedItem, (int)comboBox10.SelectedItem, AnchorType.BottomLeft));

            List<Point> boundingPoints = GetBoundingPoints(elements);

            Point center = new Point(IMAGE_SIZE / 2, (int)Math.Round(IMAGE_SIZE / 2 * trackBar2.Value / 100.0, 0));

            double minDistanceToBoundingPoint = GetMinDistanceToPoint(center, boundingPoints);
            foreach (string fileName in fileNames)
            {
                List<PictureElement> tmpElements = new List<PictureElement>();
                foreach (var element in elements)
                {
                    tmpElements.Add((PictureElement)element.Clone());
                }
                PictureElement tmpBase = new PictureElement(label7.Text, center, minDistanceToBoundingPoint, trackBar1.Value, label31.Text, trackBar3.Value, Convert.ToDouble(textBox3.Text) / 100);
                tmpElements.Add(tmpBase);
                using (MagickImage result = CompositeResult(tmpElements))
                {
                    string path = Environment.CurrentDirectory + SaveImagePath + fileName.Split('\\')[1].Split('.')[0] + ".webp";
                    result.Write(path);
                }
            }

        }

        private MagickImage CompositeResult(List<PictureElement> elements)
        {
            MagickImage image = new MagickImage(MagickColor.FromRgb(255, 255, 255), IMAGE_SIZE, IMAGE_SIZE);

            PictureElement baseImg = elements.Where(e => e.Anchor == AnchorType.Center).FirstOrDefault();
            image.Composite(baseImg.Image, baseImg.StartPoint.X, baseImg.StartPoint.Y);

            foreach (PictureElement element in elements)
            {
                if (element.Anchor == AnchorType.Center)
                    continue;
                if (element.Anchor == AnchorType.AfterBottomLeft)
                {
                    PictureElement bottomLeftElement = elements.Where(e => e.Anchor == AnchorType.BottomLeft).FirstOrDefault();
                    element.StartPoint.X = bottomLeftElement.Image.Width + CELL_SIZE / 2;
                    element.EndPoint.X = element.StartPoint.X + element.Image.Width;
                }
                image.Alpha(AlphaOption.Opaque);
                image.Composite(element.Image, element.StartPoint.X, element.StartPoint.Y, CompositeOperator.Over);
            }



            return image;
        }

        private double GetMinDistanceToPoint(Point center, List<Point> boundingPoints)
        {
            double minDistanceToBoundingPoint = Point.GetDistance(new Point(0, 0), center);
            foreach (Point p in boundingPoints)
            {
                Math.Min(minDistanceToBoundingPoint, Point.GetDistance(center, p));
            }
            return minDistanceToBoundingPoint;
        }

        private List<Point> GetBoundingPoints(List<PictureElement> elements)
        {
            List<Point> boundingPoints = new List<Point>();
            foreach (PictureElement element in elements)
            {
                Point newPoint = new Point(0, 0);
                switch (element.Anchor)
                {
                    case AnchorType.TopLeft:
                        newPoint.X = element.EndPoint.X;
                        newPoint.Y = element.EndPoint.Y;
                        break;
                    case AnchorType.TopRight:
                        newPoint.X = element.StartPoint.X;
                        newPoint.Y = element.EndPoint.Y;
                        break;
                    case AnchorType.BottomLeft:
                        newPoint.X = element.EndPoint.X;
                        newPoint.Y = element.StartPoint.Y;
                        break;
                    case AnchorType.BottomRight:
                        newPoint.X = element.StartPoint.X;
                        newPoint.Y = element.StartPoint.Y;
                        break;
                }
                boundingPoints.Add(newPoint);
            }
            return boundingPoints;
        }

        private MagickImage GetTextPictureElement(string v)
        {
            readSettings = new MagickReadSettings
            {
                FillColor = MagickColors.DarkSlateGray,
                BackgroundColor = MagickColors.Transparent,
                Font = "Arial",
                FontPointsize = 20,
                Width = 11 * (v.Length - 6),
                Height = CELL_SIZE,
                TextGravity = Gravity.Center
                
            };
            MagickImage label = new MagickImage(v, readSettings);
            return label;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo";
                openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    label2.Text = openFileDialog.FileName;
                    label2.Visible = true;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo";
                openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    label3.Text = openFileDialog.FileName;
                    label3.Visible = true;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "E:\\Generic Images";
                openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    label5.Text = openFileDialog.FileName;
                    label5.Visible = true;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "E:\\TecDocs\\2104\\PIC_FILES";
                openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    label7.Text = openFileDialog.FileName;
                    label7.Visible = true;
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "E:\\Generic Images";
                openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    label20.Text = openFileDialog.FileName;
                    label20.Visible = true;
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string brandPath = label3.Text;
            string logoPath = label2.Text;
            string generic = label5.Text;
            string SaveImagePath = "\\tmp\\";

                List<PictureElement> elements = new List<PictureElement>();
                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, label2.Text, (int)comboBox2.SelectedItem, (int)comboBox3.SelectedItem, (AnchorType)comboBox1.SelectedItem));
                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, label3.Text, (int)comboBox5.SelectedItem, (int)comboBox4.SelectedItem, (AnchorType)comboBox6.SelectedItem));
                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, label5.Text, (int)comboBox8.SelectedItem, (int)comboBox7.SelectedItem, (AnchorType)comboBox9.SelectedItem));
                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, label20.Text, (int)comboBox14.SelectedItem, (int)comboBox13.SelectedItem, (AnchorType)comboBox15.SelectedItem));
                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, GetTextPictureElement("label:" + textBox1.Text), (int)comboBox11.SelectedItem, (int)comboBox10.SelectedItem, (AnchorType)comboBox12.SelectedItem));

                List<Point> boundingPoints = GetBoundingPoints(elements);

                Point center = new Point(IMAGE_SIZE / 2, (int)Math.Round(IMAGE_SIZE / 2 * trackBar2.Value / 100.0, 0));

                double minDistanceToBoundingPoint = GetMinDistanceToPoint(center, boundingPoints);

                PictureElement tmpBase = new PictureElement(/*pathLoc*/label7.Text, center, minDistanceToBoundingPoint, trackBar1.Value, label31.Text, trackBar3.Value, Convert.ToDouble(textBox3.Text) / 100);
                elements.Add(tmpBase);
                //string path = "N:\\Alexey\\Image Combinator\\Image Combinator\\bin\\Debug\\UM_picture\\forSite2\\";

                result = CompositeResult(elements);
                //result.Write(pathLoc.Replace("\\Generic", "\\forSite2"), MagickFormat.WebP);
            //}

            pictureBox1.Width = 512;
            pictureBox1.Height = 512;
            ImageConverter converter = new ImageConverter();
            pictureBox1.Image = ((Image)converter.ConvertFrom(result.ToByteArray(MagickFormat.Jpg)));

            button6.Enabled = true;
            button8.Enabled = true;
            button10.Enabled = true;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.DefaultExt = "jpg";
                saveFileDialog.AddExtension = true;
                if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
                    return;

                string filename = saveFileDialog.FileName;

                result.Write(filename);
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label22.Text = trackBar1.Value + "%";
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            label25.Text = trackBar2.Value + "%";
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            label28.Text = trackBar3.Value.ToString();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo";
                openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    label31.Text = openFileDialog.FileName;
                    label31.Visible = true;
                }
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            int numb;
            if (Int32.TryParse(textBox.Text, out numb))
            {
                button8.Enabled = true;
            }
            else
            {
                button8.Enabled = false;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            int numb;
            if (Int32.TryParse(textBox.Text, out numb) && numb <= 100 && numb > 0)
            {
                button8.Enabled = true;
                button5.Enabled = true;
            }
            else
            {
                button8.Enabled = false;
                button5.Enabled = false;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            byte[] template = TemplateToByteArray();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                while (conn.State != System.Data.ConnectionState.Open)
                {
                    try
                    {
                        conn.Open();
                    }
                    catch
                    {
                        Task.Delay(50);
                    }
                }
                int brandID;
                if (!Int32.TryParse(textBox2.Text, out brandID))
                {
                    MessageBox.Show("Please, enter brandID!");
                    return;
                }
                int shopID = shops.Where(s => s.Alias == comboBox16.SelectedItem as string).Select(s => s.ID).FirstOrDefault();
                string queSelect = $"select ID from Elart.BrandPicrureTemplates where TecdocSupplierID = {brandID} and ShopID = {shopID}";

                int rowInDb = conn.Query<int>(queSelect).FirstOrDefault();

                if (rowInDb == 0)
                {
                    DateTime currentDate = DateTime.Now;

                    string queInsert = $"INSERT INTO Elart.BrandPicrureTemplates (TecdocSupplierID, Template, CreateDate, ShopID) VALUES (@brandID, @template, @currentDate, @shopID)";
                    conn.Execute(queInsert, new { brandID, template, currentDate, shopID });

                    MessageBox.Show(
                        "Template added to DB",
                        "Added",
                        MessageBoxButtons.OK
                        );
                }
                else
                {
                    DialogResult result = MessageBox.Show(
                        "Template for this brand already exist. Do you want to rewrite it?",
                        "Template already exist!",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2
                        );
                    if (result == DialogResult.Yes)
                    {
                        DateTime currentDate = DateTime.Now;
                        string queInsert = $"UPDATE Elart.BrandPicrureTemplates set Template = @template, UpdateDate = @currentDate where TecdocSupplierID = {brandID} and ShopID = {shopID}";
                        conn.Execute(queInsert, new { template, currentDate });
                        MessageBox.Show(
                       "Template update in DB",
                       "Updated",
                       MessageBoxButtons.OK
                       );
                    }
                }
                rowsInDb = GetTemplatesFromDB(conn);


                //string queSelect = $"select TecdocSupplierID, Template from Elart.BrandPicrureTemplates where TecdocSupplierID = {brandID}";
                //var res = conn.Query<(int, byte[])>(queSelect).FirstOrDefault();

                //string resStr = Encoding.UTF8.GetString(res.Item2);

                //PictureTemplate pictureTemplate = JsonConvert.DeserializeObject<PictureTemplate>(resStr);
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            DrawMarkup();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                while (conn.State != System.Data.ConnectionState.Open)
                {
                    try
                    {
                        conn.Open();
                    }
                    catch
                    {
                        Task.Delay(50);
                    }
                }
                shops = conn.Query<Shop>($"SELECT * FROM Elart.Shops;").ToList();
                conn.Close();
            }
            comboBox16.Items.AddRange(shops.Select(s => s.Alias).ToArray());
            comboBox16.SelectedItem = comboBox16.Items[0];
        }

        public void DrawMarkup()
        {
            Graphics g = tabPage1.CreateGraphics();
            int lineLength = 5;
            for (int i = 1; i < 16; i++)
            {
                g.DrawLine(new Pen(Color.DarkBlue), 31 + CELL_SIZE * i, 32 - lineLength, 31 + CELL_SIZE * i, 32);
                g.DrawLine(new Pen(Color.DarkBlue), 30 - lineLength, 33 + CELL_SIZE * i, 30, 33 + CELL_SIZE * i);
                g.DrawLine(new Pen(Color.DarkBlue), 31 + CELL_SIZE * i, 545, 31 + CELL_SIZE * i, 545 + lineLength);
                g.DrawLine(new Pen(Color.DarkBlue), 541, 33 + CELL_SIZE * i, 541 + lineLength, 33 + CELL_SIZE * i);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            byte[] template = TemplateToByteArray();
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.DefaultExt = "ptp";
                saveFileDialog.AddExtension = true;
                saveFileDialog.InitialDirectory = Environment.CurrentDirectory;
                if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
                    return;

                string filename = saveFileDialog.FileName;

                File.WriteAllBytes(filename, template);
            }
        }

        private byte[] TemplateToByteArray()
        {
            PictureTemplate template = new PictureTemplate
            {
                Logo = new TemplateElement
                {
                    Width = (int)comboBox2.SelectedItem,
                    Height = (int)comboBox3.SelectedItem,
                    Anchor = (AnchorType)comboBox1.SelectedItem
                },
                Brand = new TemplateElement
                {
                    Width = (int)comboBox5.SelectedItem,
                    Height = (int)comboBox4.SelectedItem,
                    Anchor = (AnchorType)comboBox6.SelectedItem
                },
                GenericImage = new TemplateElement
                {
                    Width = (int)comboBox8.SelectedItem,
                    Height = (int)comboBox7.SelectedItem,
                    Anchor = (AnchorType)comboBox9.SelectedItem
                },
                Count = new TemplateElement
                {
                    Width = (int)comboBox14.SelectedItem,
                    Height = (int)comboBox13.SelectedItem,
                    Anchor = (AnchorType)comboBox15.SelectedItem
                },
                PartNumber = new PartNumberType
                {
                    Width = (int)comboBox11.SelectedItem,
                    Height = (int)comboBox10.SelectedItem,
                    Anchor = (AnchorType)comboBox12.SelectedItem,
                    readSettings = readSettings
                },
                BaseImage = new BaseImgTemplate
                {
                    SizeKoef = trackBar1.Value,
                    VerticalPositionKoef = trackBar2.Value
                },
                Watermark = new WatermarkTemplate
                {
                    PercentSizeBaseImage = Convert.ToInt32(textBox3.Text),
                    TransparencyLevel = trackBar3.Value
                },
                CellSize = CELL_SIZE,
                GridSize = GRID_SIZE
            };

            string json = JsonConvert.SerializeObject(template);
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
            return byteArray;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.CurrentDirectory;
                openFileDialog.Filter = "Picture Template Files(*.PTP)|*.PTP";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    byte[] byteArr = File.ReadAllBytes(openFileDialog.FileName);
                    PictureTemplate template = PictureTemplate.GetTemplateFromByte(byteArr);

                    FillForm(template);
                }
            }
        }

        private void FillForm(PictureTemplate template)
        {
            comboBox2.SelectedItem = template.Logo.Width;
            comboBox3.SelectedItem = template.Logo.Height;
            comboBox1.SelectedItem = template.Logo.Anchor;

            comboBox5.SelectedItem = template.Brand.Width;
            comboBox4.SelectedItem = template.Brand.Height;
            comboBox6.SelectedItem = template.Brand.Anchor;

            comboBox8.SelectedItem = template.GenericImage.Width;
            comboBox7.SelectedItem = template.GenericImage.Height;
            comboBox9.SelectedItem = template.GenericImage.Anchor;

            comboBox14.SelectedItem = template.Count.Width;
            comboBox13.SelectedItem = template.Count.Height;
            comboBox15.SelectedItem = template.Count.Anchor;

            comboBox11.SelectedItem = template.PartNumber.Width;
            comboBox10.SelectedItem = template.PartNumber.Height;
            comboBox12.SelectedItem = template.PartNumber.Anchor;
            readSettings = template.PartNumber.readSettings;

            trackBar1.Value = template.BaseImage.SizeKoef;
            trackBar2.Value = (int)template.BaseImage.VerticalPositionKoef;

            textBox3.Text = template.Watermark.PercentSizeBaseImage.ToString();
            trackBar3.Value = template.Watermark.TransparencyLevel;

            CELL_SIZE = template.CellSize;
            GRID_SIZE = template.GridSize;
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            byte[] byteArr = (from tmplt in templates
                              where tmplt.Item1 == (int)(sender as DataGridView)[0, e.RowIndex].Value && tmplt.Item3 == (int)(sender as DataGridView)[1, e.RowIndex].Value
                              select tmplt.Item2).FirstOrDefault();
            PictureTemplate template = PictureTemplate.GetTemplateFromByte(byteArr);
            FillForm(template);
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if(sortAscending)
                dataGridView1.DataSource = picturesTemplatesFull.OrderBy(dataGridView1.Columns[e.ColumnIndex].DataPropertyName).ToList();
            else
                dataGridView1.DataSource = picturesTemplatesFull.OrderBy(dataGridView1.Columns[e.ColumnIndex].DataPropertyName).Reverse().ToList();
            sortAscending = !sortAscending;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "E:\\Generic Images\\Images\\Revise_Images";
                openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    label34.Text = openFileDialog.FileName;
                    label34.Visible = true;
                    pictureBox2.Image = Image.FromFile(openFileDialog.FileName);

                }
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (textBox4.Text.Length != 12)
            {
                MessageBox.Show("Incorrect EbayItemID!");
                return;
            }
            FtpOperation ftpOperation = new FtpOperation();

            byte[] byteArr = File.ReadAllBytes(label34.Text);

            string pathToFtp = ftpOperation.Upload(byteArr, textBox4.Text + DateTime.Now.ToString(), 9999, 4);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                while (conn.State != System.Data.ConnectionState.Open)
                {
                    try
                    {
                        conn.Open();
                    }
                    catch
                    {
                        Task.Delay(50);
                    }
                }
                DateTime currentDate = DateTime.Now;
                int itemID = conn.Query<int>($"SELECT ItemID FROM Elart.ShopItems where EbayItemID = '{textBox4.Text}';").FirstOrDefault();
                int idInExceptions = conn.Query<int>($"select ID from Elart.ItemsValidPictureReplacement where ItemID = {itemID};").FirstOrDefault();

                if (idInExceptions == 0)
                {
                    conn.Execute($"insert into Elart.ItemsValidPictureReplacement (ItemID, URL, UpdateDate) values (@itemID, @pathToFtp, @currentDate);", new { itemID, pathToFtp, currentDate});
                }
                else
                {
                    conn.Execute($"update Elart.ItemsValidPictureReplacement set URL = @pathToFtp, UpdateDate = @currentDate where ID = {idInExceptions};", new { pathToFtp, currentDate});
                }
                conn.Execute($"update Elart.ShopItemPicturesStatuses set IsActual = 0 where ItemID = {itemID};");
                conn.Execute($"update Elart.ShopItemMainPictures set IsActual = 0 where ItemID = {itemID};");

                string queSelect = "SELECT DISTINCT SI.EbayItemID, SI.ShopID, VP.URL, VP.UpdateDate FROM Elart.ItemsValidPictureReplacement VP " +
                        "JOIN Elart.ShopItems SI ON VP.ItemID = SI.ItemID; ";
                pictureExceptions = conn.Query<PictureException>(queSelect).ToList();
                dataGridViewException.DataSource = pictureExceptions;
                conn.Close();
            }
        }

        private void dataGridViewException_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (sortAscendingExc)
                dataGridViewException.DataSource = pictureExceptions.OrderBy(dataGridViewException.Columns[e.ColumnIndex].DataPropertyName).ToList();
            else
                dataGridViewException.DataSource = pictureExceptions.OrderBy(dataGridViewException.Columns[e.ColumnIndex].DataPropertyName).Reverse().ToList();
            sortAscendingExc = !sortAscendingExc;
        }

        private void dataGridViewException_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            WebClient webClient = new WebClient();
            MemoryStream stream = new MemoryStream(webClient.DownloadData((string)(sender as DataGridView)[2, e.RowIndex].Value ));

            pictureBox3.Image = Image.FromStream(stream);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.ShowDialog();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (textBox4.Text.Length != 12)
            {
                MessageBox.Show("Incorrect EbayItemID!");
                return;
            }
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                while (conn.State != System.Data.ConnectionState.Open)
                {
                    try
                    {
                        conn.Open();
                    }
                    catch
                    {
                        Task.Delay(50);
                    }
                }
                var tmpPair = conn.Query<(int, int)>($"SELECT ItemID, ShopID FROM Elart.ShopItems where EbayItemID = '{textBox4.Text}';").FirstOrDefault();
                int statusesCount = conn.Execute($"update Elart.ShopItemPicturesStatuses set IsActual = 0 where ItemID = {tmpPair.Item1};");
                int picturesCount = conn.Execute($"update Elart.ShopItemMainPictures set IsActual = 0 where ItemID = {tmpPair.Item1};");
                MessageBox.Show($"Succes. Status of ({statusesCount}) listing's pictures and ({picturesCount}) pictures is not actual now");
                conn.Close();
            }
        }

        private void buttonPictWithWatermark_Click(object sender, EventArgs e)
        {
            var files = Directory.GetFiles("E:\\Images\\Cars's_Models");

            string logoPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\logo1.PNG";

            foreach (string filePath in files)
            {
                MagickImage image = new MagickImage(filePath);

                MagickImage watermark = new MagickImage(logoPath);

                watermark.Alpha(AlphaOption.Set);
                //watermark.Rotate(45);
                watermark.ColorFuzz = new Percentage(20);
                watermark.Opaque(MagickColors.White, MagickColor.FromRgba(0, 0, 0, 0));
                

                if (image.Width / (double)image.Height > 1)
                {
                    watermark.Resize((int)(image.Width * 0.5), watermark.Height * (int)(image.Width * 0.5) / watermark.Width);
                }
                else
                {
                    watermark.Resize(image.Width * (int)(image.Height * 0.5) / watermark.Height, (int)(watermark.Height * 0.5));
                }

                watermark.Evaluate(Channels.Alpha, EvaluateOperator.Divide, 1.5);
                image.Composite(watermark, Gravity.Southwest, CompositeOperator.Over);
                image.Composite(watermark, Gravity.Northeast, CompositeOperator.Over);

                string resultPath = filePath.Replace("Cars's_Models", "Cars's_Models _Waternark");

                image.Write(resultPath);
            }
        }
    }
}
