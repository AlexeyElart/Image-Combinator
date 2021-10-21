using Dapper;
using Image_Combinator.Models;
using Image_Combinator.Utils;
using ImageMagick;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Image_Combinator
{
    //public Delegate TextBoxLinesInvoke;
    public partial class Form2 : Form
    {
        string connectionString = "SERVER=172.16.10.48;DATABASE=Elart;UID=ImageCombinator;PASSWORD=?user4Image;SslMode=none;Charset=utf8;Default Command Timeout = 3000;Connect Timeout=600;Pooling=false;";
        int IMAGE_SIZE;
        int GRID_SIZE;
        int CELL_SIZE;
        List<SaveErrData> badFiles;
        int shopID;
        List<string> groupSetIDs;
        List<PictureInDB> pictureInDB;
        static object locker = new object();
        int threadsCount = Environment.ProcessorCount / 4;
        Dictionary<string, int> skuCyl_HUST_Pair = new Dictionary<string, int>();
        Dictionary<int, string> picturesException = new Dictionary<int, string>();
        List<Shop> shops;
        List<int> brandIDs;
        //ProgressBar progressBar;
        private delegate void SafeCallDelegate();


        public Form2()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.CurrentDirectory;
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;


                List<string> skuFromFiles = new List<string>();
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    skuFromFiles = File.ReadAllLines(openFileDialog.FileName).ToList();
                }

                skuFromFiles = skuFromFiles.Select(s => s.Split(';')[0]).ToList();


                button2.Enabled = false;

                textBox1.AppendText(new string('-', 10) + "\n");
                textBox1.AppendText($"Downloaded SKU from file: {skuFromFiles.Count}\n");

                FillCylindPair();

                shopID = shops.Where(p => p.Alias == (string)comboBoxShop.SelectedItem).Select(p => p.ID).FirstOrDefault();

                backgroundWorker2.RunWorkerAsync(skuFromFiles);
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            shopID = shops.Where(p => p.Alias == (string)comboBoxShop.SelectedItem).Select(p => p.ID).FirstOrDefault();
            button1.Enabled = false;

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

                pictureInDB = conn.Query<PictureInDB>($"select * from Elart.ShopItemMainPictures where ShopID = {shopID};").ToList();

                List<int> brandForAnalize = new List<int>();
                foreach (var item in checkedListBoxBrands.CheckedItems)
                {
                    brandForAnalize.Add(Convert.ToInt32(item.ToString()));
                }

                if (checkBoxAllBrands.Checked == true)
                {
                    brandForAnalize = brandIDs;
                }

                backgroundWorker1.RunWorkerAsync(new DoWorkArguments { BrandForAnalize = brandForAnalize, connection = conn });

            }
        }

        private void CreateDigPicturesForSite(List<string> lstr, string brandPath, PictureTemplate pictureTemplate, Dictionary<string, string> pairForReplace)
        {
            foreach (string pathImage in lstr)
            {

                GC.Collect(2);

                string name = pathImage.Substring(pathImage.LastIndexOf('\\') + 1);
                name = name.Substring(0, name.LastIndexOf('.'));
                KeyValuePair<string, string> replacement = pairForReplace.Where(t => name.Contains(t.Key)).Select(t => t).FirstOrDefault();
                if (replacement.Key != null && replacement.Key != ""/*pairForReplace.ContainsKey(name)*/)
                {
                    name = name.Replace(replacement.Key, replacement.Value);
                }
                else
                {
                    continue;
                }

                List<PictureElement> elements = new List<PictureElement>();
                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, brandPath, pictureTemplate.Brand.Width, pictureTemplate.Brand.Height, pictureTemplate.Brand.Anchor));

                List<Point> boundingPoints = GetBoundingPoints(elements);

                Point center = new Point(IMAGE_SIZE / 2, (int)Math.Round(IMAGE_SIZE / 2 * pictureTemplate.BaseImage.VerticalPositionKoef / 100.0, 0));

                double minDistanceToBoundingPoint = GetMinDistanceToPoint(center, boundingPoints);

                PictureElement tmpBase = new PictureElement(pathImage, center, minDistanceToBoundingPoint, pictureTemplate.BaseImage.SizeKoef, "", pictureTemplate.Watermark.TransparencyLevel, pictureTemplate.Watermark.PercentSizeBaseImage / 100.0);
                elements.Add(tmpBase);
                MagickImage result = CompositeResult(elements);

                result.Write($"UM_picture\\{name}.webp", MagickFormat.WebP);

            }
        }

        private void FillCylindPair()
        {
            List<string> hustCyl = File.ReadAllLines("cylinders.csv").ToList();
            skuCyl_HUST_Pair.Clear();
            foreach (string s in hustCyl)
            {
                string[] pair = s.Split(';');
                if (!skuCyl_HUST_Pair.ContainsKey(pair[0]))
                {
                    skuCyl_HUST_Pair.Add(pair[0], Convert.ToInt32(pair[1]));
                }
            }
        }

        private void CreatePictureInThread(MySqlConnection conn, List<string> dataFromDB)
        {
            FtpOperation ftpClient = new FtpOperation();
            //if (Task.CurrentId.HasValue)
            //{
            //    this.Invoke(new Action(() => SendText($"{Task.CurrentId.Value} \n")));
            //}
            foreach (string sku in dataFromDB)
            {
                CreateMainPicture(conn, sku, ftpClient);
                //int id = Task.CurrentId.Value;
                this.Invoke(new Action(() => SendText($"{sku} \n")));
                backgroundWorker1.ReportProgress(10);
            }
        }


        private void CreatePictureInThread(MySqlConnection conn, List<string> dataFromDB, PictureTemplate pictureTemplate)
        {
            FtpOperation ftpClient = new FtpOperation();
            foreach (string ebayItemID in dataFromDB)
            {
                CreateMainPictureByEbayItenID(conn, ebayItemID, pictureTemplate, ftpClient);
                this.Invoke(new Action(() => SendText($"Created {ebayItemID}\n")));
                backgroundWorker1.ReportProgress(10);
            }
        }

        private void CreateMainPicture(MySqlConnection conn, string sku, FtpOperation ftpClient)
        {
            byte[] res = conn.Query<byte[]>("SELECT distinct BPT.Template FROM asvela.supplier_data SD " +
                                            "join Elart.BrandPicrureTemplates BPT on SD.supplier_id = BPT.TecdocSupplierID " +
                                            $"where SD.asvela_sku = '{sku}' and BPT.ShopID = {shopID}; ").FirstOrDefault();

            if (res == null)
            {
                this.Invoke(new Action(() => SendText($"template not found for {sku}\n")));
                return;
            }
            PictureTemplate pictureTemplate = PictureTemplate.GetTemplateFromByte(res);

            CreateMainPictureBySku(conn, sku, pictureTemplate, ftpClient);
        }

        private void CreateMainPictureBySku(MySqlConnection conn, string sku, PictureTemplate pictureTemplate, FtpOperation ftpClient)
        {
            int brandID;
            long itemID;
            string nameForFtp;

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
            try
            {
                string pathImage = GetPathImageBySku(conn, sku, out brandID, out nameForFtp, out itemID);

                if (nameForFtp == "" || brandID == 0)
                    return;

                GRID_SIZE = pictureTemplate.GridSize;
                CELL_SIZE = pictureTemplate.CellSize;
                IMAGE_SIZE = CELL_SIZE * GRID_SIZE;

                string partNumber = GetPartNumberBySku(conn, sku);

                string brandPath;
                if (brandID == 4460 && (partNumber.Contains("XP") || partNumber.Contains("HP")))
                {
                    brandPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\" + brandID + "_2.png";
                }
                else
                {
                    if (brandID == 432 && (sku.Contains("MS") || sku.Contains("CB") || sku.Contains("TW")))
                    {
                        brandPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\" + brandID + "_1.png";
                    }
                    else
                    {
                        brandPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\" + brandID + ".png";
                    }
                }

                int logoNumb = 0;
                string watermarkPath = "";
                switch (shopID)
                {
                    case 1:
                        logoNumb = 1;
                        watermarkPath = "E:\\Generic Images\\watermark.png";
                        break;
                    case 2:
                        logoNumb = 2;
                        watermarkPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\logo2.png";
                        break;
                    case 3:
                        logoNumb = 3;
                        watermarkPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\Com_W.png";
                        break;
                    case 4:
                        logoNumb = 4;
                        watermarkPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\logo4_w.png";
                        break;
                }

                string logoPath = $"E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\logo{logoNumb}.png";



                //string SaveImagePath = "\\tmp\\";


                List<PictureElement> elements = new List<PictureElement>();
                pictureTemplate.PartNumber.readSettings.FillColor = MagickColors.DarkGray;
                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, logoPath, pictureTemplate.Logo.Width, pictureTemplate.Logo.Height, pictureTemplate.Logo.Anchor));
                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, brandPath, pictureTemplate.Brand.Width, pictureTemplate.Brand.Height, pictureTemplate.Brand.Anchor));
                if (brandID == 4865)
                {
                    string queSize = "select ac.criterion_value from asvela.supplier_data sd " +
                            "join Tecdoc_2019_11_22.Article_Criteria ac on ac.man_article_number = sd.tecdoc_number and ac.supplier_id = sd.supplier_id " +
                            $"where sd.asvela_sku = '{sku}' and ac.criterion_id = 1292;";
                    string size = conn.Query<string>(queSize).FirstOrDefault();
                    if (size == "" || size == null)
                    {
                        partNumber += " /STD";
                    }
                    else
                    {
                        partNumber += $" /+" + size + "mm";
                    }
                }
                if (shopID == 1)
                {
                    elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, GetTextPictureElement("label:" + partNumber, pictureTemplate.PartNumber.readSettings), pictureTemplate.PartNumber.Width, pictureTemplate.PartNumber.Height, pictureTemplate.PartNumber.Anchor));
                }
                if (brandID == 4865)
                {
                    int countInSet = 0;

                    {
                        countInSet = skuCyl_HUST_Pair[sku];
                    }
                    string cylindersCountPath = $"E:\\Generic Images\\Hastings_Gen_Img\\{countInSet}_cyl.png";

                    elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, cylindersCountPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));
                }
                else
                {
                    if (brandID == 4460)
                    {
                        int pairCount = Convert.ToInt32(partNumber.Replace(" ", "").Substring(2, 1));
                        string pairCountPath = $"E:\\Generic Images\\King\\{pairCount}_pib.png";
                        elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, pairCountPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));
                        if (!File.Exists(pathImage))
                        {
                            pathImage = "E:\\Done Programm\\KING\\Images_ger\\Pictures\\none.png";
                        }
                    }
                    else
                    {
                        if (File.Exists("E:\\TecDocs\\2104" + pathImage))
                        {
                            pathImage = "E:\\TecDocs\\2104" + pathImage;

                            string actualImgPath;
                            if (shopID == 4)
                            {
                                actualImgPath = "E:\\Generic Images\\Actual_Image_DE.png";
                            }
                            else
                            {
                                actualImgPath = "E:\\Generic Images\\Actual image_3.png";
                            }
                            elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, actualImgPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));
                        }
                        else
                        {
                            if (File.Exists("E:\\TecAlliance Data Package" + pathImage))
                            {
                                pathImage = "E:\\TecAlliance Data Package" + pathImage;

                                string actualImgPath;
                                if (shopID == 4)
                                {
                                    actualImgPath = "E:\\Generic Images\\Actual_Image_DE.png";
                                }
                                else
                                {
                                    actualImgPath = "E:\\Generic Images\\Actual image_3.png";
                                }
                                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, actualImgPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));
                            }
                            else
                            {
                                if (!pathImage.Contains("http"))
                                {
                                    string genericPath = "";
                                    switch (shopID)
                                    {
                                        case 1:
                                            genericPath = "E:\\Generic Images\\Generic_image_couk.png";
                                            break;
                                        case 2:
                                            genericPath = "E:\\Generic Images\\Generic_image_couk2.png";
                                            break;
                                        case 3:
                                            genericPath = "E:\\Generic Images\\Generic image_com.png";
                                            break;
                                        case 4:
                                            genericPath = "E:\\Generic Images\\Generic_DE.png";
                                            break;
                                    }


                                    elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, genericPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));

                                    string partType;
                                    pathImage = GetPathGenericImageBySku(conn, sku, out partType);
                                    if (pathImage == "")
                                    {
                                        return;
                                    }

                                    nameForFtp += partType + "_GenImg";
                                }
                                else
                                {
                                    string actualImgPath;
                                    if (shopID == 4)
                                    {
                                        actualImgPath = "E:\\Generic Images\\Actual_Image_DE.png";
                                    }
                                    else
                                    {
                                        actualImgPath = "E:\\Generic Images\\Actual image_3.png";
                                    }
                                    elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, actualImgPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));
                                }

                            }
                        }

                    }

                }


                List<Point> boundingPoints = GetBoundingPoints(elements);

                Point center = new Point(IMAGE_SIZE / 2, (int)Math.Round(IMAGE_SIZE / 2 * pictureTemplate.BaseImage.VerticalPositionKoef / 100.0, 0));

                double minDistanceToBoundingPoint = GetMinDistanceToPoint(center, boundingPoints);

                PictureElement tmpBase = new PictureElement(pathImage, center, minDistanceToBoundingPoint, pictureTemplate.BaseImage.SizeKoef, watermarkPath, pictureTemplate.Watermark.TransparencyLevel, pictureTemplate.Watermark.PercentSizeBaseImage / 100.0);
                elements.Add(tmpBase);

                MagickImage result = CompositeResult(elements);

                byte[] byteArr = result.ToByteArray(MagickFormat.Jpg);


                if (nameForFtp.Contains("_ver"))
                {
                    string pathForDelete = pictureInDB.Where(p => p.ItemID == itemID && p.ShopID == shopID).Select(p => p.URL).FirstOrDefault();
                    pathForDelete = "/var/www/html" + pathForDelete.Replace("https://shop.elartcom.eu", "");
                    ftpClient.Delete(pathForDelete);
                }
                string pathFTP = ftpClient.Upload(byteArr, nameForFtp, brandID, shopID);
                SavePictureToDB(conn, itemID, shopID, pathFTP);
                //}
            }
            catch (Exception exc)
            {
                lock (locker)
                {
                    this.Invoke(new Action(() => SendText(exc.Message + "\n")));
                }
            }
        }

        private string GetPathGenericImageBySku(MySqlConnection conn, string sku, out string partType)
        {
            string que = "select distinct LD.description from asvela.supplier_data as SD " +
                         "join Temp.Article_to_Generic_Article_Allocation as AGAA on SD.tecdoc_number = AGAA.man_article_number and SD.supplier_id = AGAA.supplier_id " +
                         "join Temp.Generic_Articles ga on ga.gen_article_id = AGAA.gen_article_id " +
                         "join Temp.Language_Descriptions as LD on ga.description_id = LD.description_id " +
                         $"where SD.asvela_sku = \"{sku}\" and LD.lang_id = 4 " +
                         "UNION " +
                         "select distinct LD.description from asvela.supplier_data as SD " +
                         "join ElartTecDoc.Article_to_Generic_Article_Allocation as AGAA on SD.tecdoc_number = AGAA.man_article_number and SD.supplier_id = AGAA.supplier_id " +
                         "join Temp.Generic_Articles ga on ga.gen_article_id = AGAA.gen_article_id " +
                         "join Temp.Language_Descriptions as LD on ga.description_id = LD.description_id " +
                         $"where SD.asvela_sku = \"{sku}\" and LD.lang_id = 4 ";
            partType = conn.Query<string>(que).FirstOrDefault();

            if (!String.IsNullOrEmpty(partType))
            {
                partType = RemoveExtraCharacters(partType).ToUpper();
                DirectoryInfo dirInfo = new DirectoryInfo("E:\\Generic Images\\Images");
                string partTypeLocal = partType;
                FileInfo fileInfo = dirInfo.GetFiles().Where(n => n.FullName.Contains("\\" + partTypeLocal + ".")).FirstOrDefault();

                if (fileInfo == null)
                {
                    this.Invoke(new Action(() => SendText("Not found " + partTypeLocal + " generic image \n")));
                    return "";
                }
                else
                {
                    return fileInfo.FullName;
                }
            }
            else
            {
                this.Invoke(new Action(() => SendText("Not found partType\n")));
                return "";
            }
        }

        private string GetPartNumberBySku(MySqlConnection conn, string sku)
        {
            string que = $"select distinct tecdoc_number from asvela.supplier_data where asvela_sku = \"{sku}\"";
            string partNumber = conn.Query<string>(que).FirstOrDefault();
            partNumber = partNumber == null ? partNumber = "" : partNumber;
            return partNumber;
        }

        private string GetPathImageBySku(MySqlConnection conn, string sku, out int brandID, out string nameForFtp, out long itemID)
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

            itemID = conn.Query<int>($"select ID from Elart.Items where SKU = \"{sku}\";").FirstOrDefault();
            if (itemID == 0)
            {
                conn.Execute($"insert into Elart.Items (SKU) values (\"{sku}\");");
                itemID = conn.Query<int>($"select ID from Elart.Items where SKU = \"{sku}\";").FirstOrDefault();
            }

            int localItemID = (int)itemID;


            nameForFtp = sku + "_";

            Regex regularExpressionForGroupSetsSKU = new Regex("(_){1}[0-9]{1,2}(X|x){1}$", RegexOptions.Compiled);
            var match = regularExpressionForGroupSetsSKU.Match(sku);
            if (match.Success)
            {
                sku = sku.Replace(match.Value, "");
            }

            string que = "select GD.graphic_file_number, SD.supplier_id, GD.doc_key from asvela.supplier_data SD " +
                    "join Temp.Allocation_of_Graphics_to_Article_Numbers AGAN on SD.tecdoc_number = AGAN.man_article_number and SD.supplier_id = AGAN.supplier_id and AGAN.sort_key = 1 " +
                    "join Temp.Graphics_Documents GD on AGAN.graphic_number = GD.graphic_number and (GD.doc_key = 1 or GD.doc_key = 3) " +
                    $"where SD.asvela_sku = '{sku}'; ";
            var res2 = conn.Query<(string, int, int)>(que).FirstOrDefault();

            if (res2.Item1 == null && res2.Item2 == 0 && res2.Item3 == 0)
            {
                que = "select GD.graphic_file_number, SD.supplier_id, GD.doc_key from asvela.supplier_data SD " +
                    "join ElartTecDoc.Allocation_of_Graphics_to_Article_Numbers AGAN on SD.tecdoc_number = AGAN.man_article_number and SD.supplier_id = AGAN.supplier_id and AGAN.sort_key = 1 " +
                    "join ElartTecDoc.Graphics_and_Documents GD on AGAN.graphic_number = GD.graphic_number and (GD.doc_key = 1 or GD.doc_key = 3 or GD.doc_key = 6) " +
                    $"where SD.asvela_sku = '{sku}'; ";
                res2 = conn.Query<(string, int, int)>(que).FirstOrDefault();
            }

            brandID = res2.Item2 != 0 ? res2.Item2 : conn.Query<int>($"select SD.supplier_id from asvela.supplier_data SD where SD.asvela_sku = '{sku}';").FirstOrDefault();

            if (brandID == 30 && res2.Item1 != null && !res2.Item1.Contains(sku.Replace("BOSCH", "")))
            {
                que = "select GD.graphic_file_number, SD.supplier_id, GD.doc_key from asvela.supplier_data SD " +
                    "join Temp.Allocation_of_Graphics_to_Article_Numbers AGAN on SD.tecdoc_number = AGAN.man_article_number and SD.supplier_id = AGAN.supplier_id and AGAN.sort_key = 2 " +
                    "join Temp.Graphics_Documents GD on AGAN.graphic_number = GD.graphic_number and (GD.doc_key = 1 or GD.doc_key = 3) " +
                    $"where SD.asvela_sku = '{sku}' order by AGAN.sort_key; ";
                res2 = conn.Query<(string, int, int)>(que).FirstOrDefault();

                if (res2.Item1 != null && !res2.Item1.Contains(sku.Replace("BOSCH", "")))
                {
                    que = "select GD.graphic_file_number, SD.supplier_id, GD.doc_key from asvela.supplier_data SD " +
                        "join Temp.Allocation_of_Graphics_to_Article_Numbers AGAN on SD.tecdoc_number = AGAN.man_article_number and SD.supplier_id = AGAN.supplier_id and AGAN.sort_key = 3 " +
                        "join Temp.Graphics_Documents GD on AGAN.graphic_number = GD.graphic_number and (GD.doc_key = 1 or GD.doc_key = 3) " +
                        $"where SD.asvela_sku = '{sku}' order by AGAN.sort_key; ";
                    res2 = conn.Query<(string, int, int)>(que).FirstOrDefault();

                    if (res2.Item1 != null && !res2.Item1.Contains(sku.Replace("BOSCH", "")))
                    {
                        res2.Item1 = "";
                    }
                }
            }

            string folder = brandID.ToString();
            while (folder.Length < 4)
            {
                folder = folder.Insert(0, "0");
            }
            string fileExtention = "";
            string fileName = "";

            try
            {

                nameForFtp = res2.Item1 == null ? nameForFtp : nameForFtp + res2.Item1.Trim();
                if (pictureInDB.Any(p => p.ItemID == localItemID && p.ShopID == shopID))
                {
                    int version = pictureInDB.Where(p => p.ItemID == localItemID && p.ShopID == shopID).Select(p => p.Version).FirstOrDefault();
                    nameForFtp += $"_ver{version + 1}";
                }

                fileName = res2.Item1 == null || res2.Item1 == "MS_120000316001" ? "" : res2.Item1.Trim();

                switch (res2.Item3)
                {
                    case 1:
                        fileExtention = ".BMP";
                        break;
                    case 3:
                        fileExtention = ".JPG";
                        break;
                    case 5:
                        fileExtention = ".JPG";
                        break;
                    case 6:
                        fileExtention = ".PNG";
                        break;
                    case 7:
                        fileExtention = ".GIF";
                        break;
                    default:
                        fileExtention = ".JPG";
                        break;
                }



                if (brandID == 0)
                {
                    this.Invoke(new Action(() => SendText($"Brand not found for {sku} \n")));
                    //lock (locker)
                    //{
                    File.AppendAllText("error.txt", $"Brand not found for {sku} \n");
                    //}
                    return "";
                }

                if (brandID == 4865)
                {
                    int cylCount = skuCyl_HUST_Pair[sku];
                    if (cylCount != 1 && cylCount != 2 && cylCount != 3 && cylCount != 4 && cylCount != 6 && cylCount != 8)
                        return $"E:\\Generic Images\\HASTINGS\\none.jpg";
                    return $"E:\\Generic Images\\HASTINGS\\{cylCount}c_1.jpg";
                }

                if (brandID == 4460)
                {
                    string partNumber = GetPartNumber(conn, (int)itemID);
                    partNumber = partNumber.Replace(" ", "");
                    string kingFileName = partNumber.Substring(0, 3);
                    if (partNumber.Contains("XP"))
                    {
                        kingFileName += "XP";
                    }
                    return "E:\\Done Programm\\KING\\Images_ger\\Pictures\\" + kingFileName + ".png";
                }

                if (picturesException.ContainsKey((int)itemID))
                {
                    string excURL = picturesException[(int)itemID];
                    return excURL;
                }

                string localSku = sku;
                int localBtandID = brandID;

                if (brandID != 30)
                {
                    if (badFiles.Any(f => f.brandID == localBtandID && f.sku == localSku))
                    {
                        fileName = "";
                    }
                }


            }
            catch (Exception exc)
            {
                this.Invoke(new Action(() => SendText($"{exc.Message}\n")));
            }
            return "\\PIC_FILES\\" + folder + "\\PIC.7z.001\\" + fileName + fileExtention;
        }

        private void CreateMainPictureByEbayItenID(MySqlConnection conn, string ebayItemID, PictureTemplate pictureTemplate, FtpOperation ftpClient)
        {
            int brandID;
            long itemID;
            string nameForFtp;
            string sku;

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
            try
            {
                string partNumber = GetPartNumber(conn, ebayItemID);
                string pathImage = GetPathImage(conn, ebayItemID, partNumber, out brandID, out nameForFtp, out itemID, out sku);

                if (nameForFtp == "" || brandID == 0)
                    return;

                GRID_SIZE = pictureTemplate.GridSize;
                CELL_SIZE = pictureTemplate.CellSize;
                IMAGE_SIZE = CELL_SIZE * GRID_SIZE;

                string brandPath;

                if (brandID == 4460 && (partNumber.Contains("XP") || partNumber.Contains("HP")))
                {
                    brandPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\" + brandID + "_2.png";
                }
                else
                {
                    if (brandID == 432 && (sku.Contains("MS") || sku.Contains("CB") || sku.Contains("TW")))
                    {
                        brandPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\" + brandID + "_1.png";
                    }
                    else
                    {
                        brandPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\" + brandID + ".png";
                    }
                }

                int logoNumb = 0;
                string watermarkPath = "";
                switch (shopID)
                {
                    case 1:
                        logoNumb = 1;
                        watermarkPath = "E:\\Generic Images\\watermark.png";
                        break;
                    case 2:
                        logoNumb = 2;
                        watermarkPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\logo2.png";
                        break;
                    case 3:
                        logoNumb = 3;
                        watermarkPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\Com_W.png";
                        break;
                    case 4:
                        logoNumb = 4;
                        watermarkPath = "E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\logo4_w.png";
                        break;
                }

                string logoPath = $"E:\\tecdoc1q2018\\TecDoc\\images\\Logo\\logo{logoNumb}.png";


                List<PictureElement> elements = new List<PictureElement>();
                pictureTemplate.PartNumber.readSettings.FillColor = MagickColors.DarkGray;
                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, logoPath, pictureTemplate.Logo.Width, pictureTemplate.Logo.Height, pictureTemplate.Logo.Anchor));
                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, brandPath, pictureTemplate.Brand.Width, pictureTemplate.Brand.Height, pictureTemplate.Brand.Anchor));
                if (shopID == 1)
                {
                    elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, GetTextPictureElement("label:" + partNumber, pictureTemplate.PartNumber.readSettings), pictureTemplate.PartNumber.Width, pictureTemplate.PartNumber.Height, pictureTemplate.PartNumber.Anchor));
                }
                if (brandID == 4865)
                {
                    int countInSet = 0;
                    if (groupSetIDs.Contains(ebayItemID))
                    {
                        countInSet = GetGSCount(conn, ebayItemID);
                    }
                    else
                    {
                        if (partNumber.EndsWith("S") || partNumber.EndsWith("s"))
                        {
                            countInSet = 1;
                        }
                        else
                        {
                            countInSet = skuCyl_HUST_Pair[sku];
                        }
                    }
                    string queSize = "select ac.criterion_value from asvela.supplier_data sd " +
                            "join Tecdoc_2019_11_22.Article_Criteria ac on ac.man_article_number = sd.tecdoc_number and ac.supplier_id = sd.supplier_id " +
                            $"where sd.asvela_sku = '{sku}' and ac.criterion_id = 1292;";
                    string size = conn.Query<string>(queSize).FirstOrDefault();
                    if (String.IsNullOrEmpty(size))
                    {
                        partNumber += " /STD";
                    }
                    else
                    {
                        partNumber += $" /+" + size + "mm";
                    }
                    string cylindersCountPath = $"E:\\Generic Images\\Hastings_Gen_Img\\{countInSet}_cyl.png";

                    elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, cylindersCountPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));
                }
                else
                {
                    if (brandID == 4460)
                    {
                        int pairCount = Convert.ToInt32(partNumber.Replace(" ", "").Substring(2, 1));
                        string pairCountPath = $"E:\\Generic Images\\King\\{pairCount}_pib.png";
                        elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, pairCountPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));
                        if (!File.Exists(pathImage))
                        {
                            pathImage = "E:\\Done Programm\\KING\\Images_ger\\Pictures\\none.png";
                        }
                    }
                    else
                    {
                        if (File.Exists("E:\\TecDocs\\2104" + pathImage))
                        {
                            pathImage = "E:\\TecDocs\\2104" + pathImage;

                            string actualImgPath;
                            if (shopID == 4)
                            {
                                actualImgPath = "E:\\Generic Images\\Actual_Image_DE.png";
                            }
                            else
                            {
                                actualImgPath = "E:\\Generic Images\\Actual image_3.png";
                            }
                            elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, actualImgPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));
                        }
                        else
                        {
                            if (File.Exists("E:\\TecAlliance Data Package" + pathImage))
                            {
                                pathImage = "E:\\TecAlliance Data Package" + pathImage;

                                string actualImgPath;
                                if (shopID == 4)
                                {
                                    actualImgPath = "E:\\Generic Images\\Actual_Image_DE.png";
                                }
                                else
                                {
                                    actualImgPath = "E:\\Generic Images\\Actual image_3.png";
                                }
                                elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, actualImgPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));
                            }
                            else
                            {
                                if (!pathImage.Contains("http"))
                                {
                                    string genericPath = "";
                                    switch (shopID)
                                    {
                                        case 1:
                                            genericPath = "E:\\Generic Images\\Generic_image_couk.png";
                                            break;
                                        case 2:
                                            genericPath = "E:\\Generic Images\\Generic_image_couk2.png";
                                            break;
                                        case 4:
                                            genericPath = "E:\\Generic Images\\Generic_DE.png";
                                            break;
                                    }


                                    elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, genericPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));

                                    string partType;
                                    pathImage = GetPathGenericImage(conn, ebayItemID, out partType);
                                    if (pathImage == "")
                                    {
                                        return;
                                    }

                                    nameForFtp += partType + "_GenImg";
                                }
                                else
                                {
                                    string actualImgPath;
                                    if (shopID == 4)
                                    {
                                        actualImgPath = "E:\\Generic Images\\Actual_Image_DE.png";
                                    }
                                    else
                                    {
                                        actualImgPath = "E:\\Generic Images\\Actual image_3.png";
                                    }
                                    elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, actualImgPath, pictureTemplate.GenericImage.Width, pictureTemplate.GenericImage.Height, pictureTemplate.GenericImage.Anchor));
                                }

                            }

                        }
                    }
                }
                if (groupSetIDs.Contains(ebayItemID))
                {
                    int countInGS = GetGSCount(conn, ebayItemID);
                    string countPath = $"E:\\Generic Images\\{countInGS}pcs.png";
                    elements.Add(new PictureElement(GRID_SIZE, CELL_SIZE, countPath, pictureTemplate.Count.Width, pictureTemplate.Count.Height, pictureTemplate.Count.Anchor));
                }

                List<Point> boundingPoints = GetBoundingPoints(elements);

                Point center = new Point(IMAGE_SIZE / 2, (int)Math.Round(IMAGE_SIZE / 2 * pictureTemplate.BaseImage.VerticalPositionKoef / 100.0, 0));

                double minDistanceToBoundingPoint = GetMinDistanceToPoint(center, boundingPoints);


                PictureElement tmpBase = new PictureElement(pathImage, center, minDistanceToBoundingPoint, pictureTemplate.BaseImage.SizeKoef, watermarkPath, pictureTemplate.Watermark.TransparencyLevel, pictureTemplate.Watermark.PercentSizeBaseImage / 100.0);
                elements.Add(tmpBase);
                MagickImage result = CompositeResult(elements);

                byte[] byteArr = result.ToByteArray(MagickFormat.Jpg);
                lock (locker)
                {
                    if (nameForFtp.Contains("_ver"))
                    {
                        string pathForDelete = pictureInDB.Where(p => p.ItemID == itemID && p.ShopID == shopID).Select(p => p.URL).FirstOrDefault();
                        pathForDelete = "/var/www/html" + pathForDelete.Replace("https://shop.elartcom.eu", "");
                        try
                        {
                            ftpClient.Delete(pathForDelete);
                        }
                        catch
                        {
                            File.AppendAllText("error.txt", $"File for delete on FTP ({pathForDelete}) not found \n");
                        }
                    }
                    string pathFTP = ftpClient.Upload(byteArr, nameForFtp, brandID, shopID);
                    SavePictureToDB(conn, itemID, shopID, pathFTP);
                }

                //}
            }
            catch (Exception exc)
            {
                lock (locker)
                {
                    File.AppendAllText("error.txt", exc.Message + "\n");
                }
            }

        }

        private List<SaveErrData> GetFromFTP(FtpOperation ftpClient)
        {
            byte[] saveErrorArr = ftpClient.Download("/var/www/html/admin/stas/save_error.csv").ToArray();
            List<string> saveErr = Encoding.UTF8.GetString(saveErrorArr).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            saveErrorArr = ftpClient.Download("/var/www/html/admin/save_error.csv").ToArray();
            saveErr.AddRange(saveErr.Concat(Encoding.UTF8.GetString(saveErrorArr).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)).ToList());
            saveErrorArr = ftpClient.Download("/var/www/html/admin/andrey/save_error.csv").ToArray();
            saveErr.AddRange(saveErr.Concat(Encoding.UTF8.GetString(saveErrorArr).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)).ToList());
            saveErrorArr = ftpClient.Download("/var/www/html/admin/ros/save_error.csv").ToArray();
            saveErr.AddRange(saveErr.Concat(Encoding.UTF8.GetString(saveErrorArr).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)).ToList());

            saveErr = saveErr.Distinct().ToList();

            List<SaveErrData> badFiles = new List<SaveErrData>();
            foreach (string err in saveErr)
            {
                string[] tmp = err.Split('|');
                SaveErrData tmpData = new SaveErrData
                {
                    sku = tmp[0],
                    brandID = Convert.ToInt32(tmp[2]),
                    fileName = tmp[4]
                };
                badFiles.Add(tmpData);
            }
            //var test = badFiles.Where(bf => bf.sku == "BOSCHF026400375").FirstOrDefault();
            return badFiles;
        }

        private class SaveErrData
        {
            public string sku { get; set; }
            public int brandID { get; set; }
            public string fileName { get; set; }
        }

        private class PictureInDB
        {
            public int ItemID { get; set; }
            public int ShopID { get; set; }
            public string URL { get; set; }
            public int Version { get; set; }
        }

        private int GetGSCount(MySqlConnection conn, string ebayItemID)
        {
            string que = "select GS.Count from Elart.ShopItemGroupSets SIGS " +
                         "join Elart.ItemsGroupSet GS on SIGS.ItemsGroupSetID = GS.ID " +
                         $"where SIGS.EbayItemID = \"{ebayItemID}\"";
            return conn.Query<int>(que).FirstOrDefault();
        }

        private void SavePictureToDB(MySqlConnection conn, long itemID, int shopID, string pathFTP)
        {
            DateTime currentDate = DateTime.Now;

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

            int id = conn.Query<int>($"select ID from Elart.ShopItemMainPictures where ItemID = {itemID} and ShopID = {shopID}").FirstOrDefault();
            if (id == 0)
            {
                conn.Execute("insert into Elart.ShopItemMainPictures (ItemID, ShopID, URL, CreateDate) values (@itemID, @shopID, @pathFTP, @currentDate)", new { itemID, shopID, pathFTP, currentDate });
            }
            else
            {
                conn.Execute($"update Elart.ShopItemMainPictures set URL = @pathFTP, UpdateDate = @currentDate, Version = Version + 1, IsActual = 1 where ID = {id}", new { pathFTP, currentDate });
            }

        }

        private string GetPathGenericImage(MySqlConnection conn, string ebayItemID, out string partType)
        {
            string que = "select distinct LD.description from Elart.ShopItems as SI " +
                         "join Elart.Items as I on SI.ItemID = I.ID " +
                         "join asvela.supplier_data as SD on LEFT(I.SKU, locate(\"_\", I.SKU)-1) = SD.asvela_sku " +
                         "join Temp.Article_to_Generic_Article_Allocation as AGAA on SD.tecdoc_number = AGAA.man_article_number and SD.supplier_id = AGAA.supplier_id " +
                         "join Temp.Generic_Articles ga on ga.gen_article_id = AGAA.gen_article_id " +
                         "join Temp.Language_Descriptions as LD on ga.description_id = LD.description_id " +
                         $"where SI.EbayItemID = \"{ebayItemID}\" and LD.lang_id = 4 " +
                         "UNION " +
                         "select distinct LD.description from Elart.ShopItems as SI " +
                         "join Elart.Items as I on SI.ItemID = I.ID " +
                         "join asvela.supplier_data as SD on I.SKU = SD.asvela_sku " +
                         "join Temp.Article_to_Generic_Article_Allocation as AGAA on SD.tecdoc_number = AGAA.man_article_number and SD.supplier_id = AGAA.supplier_id " +
                         "join Temp.Generic_Articles ga on ga.gen_article_id = AGAA.gen_article_id " +
                         "join Temp.Language_Descriptions as LD on ga.description_id = LD.description_id " +
                         $"where SI.EbayItemID = \"{ebayItemID}\" and LD.lang_id = 4 ";
            partType = conn.Query<string>(que).FirstOrDefault();
            if (partType == null)
            {
                que = "select distinct LD.description from Elart.ShopItems as SI " +
                         "join Elart.Items as I on SI.ItemID = I.ID " +
                         "join asvela.supplier_data as SD on LEFT(I.SKU, locate(\"_\", I.SKU)-1) = SD.asvela_sku " +
                         "join ElartTecDoc.Article_to_Generic_Article_Allocation as AGAA on SD.tecdoc_number = AGAA.man_article_number and SD.supplier_id = AGAA.supplier_id " +
                         "join Temp.Generic_Articles ga on ga.gen_article_id = AGAA.gen_article_id " +
                         "join Temp.Language_Descriptions as LD on ga.description_id = LD.description_id " +
                         $"where SI.EbayItemID = \"{ebayItemID}\" and LD.lang_id = 4 " +
                         "UNION " +
                         "select distinct LD.description from Elart.ShopItems as SI " +
                         "join Elart.Items as I on SI.ItemID = I.ID " +
                         "join asvela.supplier_data as SD on I.SKU = SD.asvela_sku " +
                         "join ElartTecDoc.Article_to_Generic_Article_Allocation as AGAA on SD.tecdoc_number = AGAA.man_article_number and SD.supplier_id = AGAA.supplier_id " +
                         "join Temp.Generic_Articles ga on ga.gen_article_id = AGAA.gen_article_id " +
                         "join Temp.Language_Descriptions as LD on ga.description_id = LD.description_id " +
                         $"where SI.EbayItemID = \"{ebayItemID}\" and LD.lang_id = 4 ";
                partType = conn.Query<string>(que).FirstOrDefault();
                if (partType == null)
                {
                    lock (locker)
                    {
                        this.Invoke(new Action(() => SendText($"PartType not found for {ebayItemID}\n")));
                        //File.AppendAllText("error.txt", $"PartType not found for {ebayItemID}\n");
                    }
                    return "";
                }
            }
            partType = RemoveExtraCharacters(partType).ToUpper();
            DirectoryInfo dirInfo = new DirectoryInfo("E:\\Generic Images\\Images");
            string partTypeLocal = partType;
            FileInfo fileInfo = dirInfo.GetFiles().Where(n => n.FullName.Contains("\\" + partTypeLocal + ".")).FirstOrDefault();
            if (fileInfo == null)
            {
                lock (locker)
                {
                    File.AppendAllText("error.txt", "Not found " + partType + " generic image \n");
                    return "";
                }
            }
            else
            {
                return fileInfo.FullName;
            }
        }

        private string RemoveExtraCharacters(string v)
        {
            return v.Replace(" / ", " ").Replace("/-", " ").Replace("- /", " ").Replace("/ ", " ").Replace("-/", " ").Replace(",", "")
                .Replace("/", " ").Replace("-", " ").Replace("(", "").Replace(")", "").Replace("'", "").Trim().Replace("   ", " ").Replace("  ", " ");
        }

        private string GetPartNumber(MySqlConnection conn, string ebayItemID)
        {
            string que = "select distinct SD.tecdoc_number from Elart.ShopItems as SI " +
                         "join Elart.Items AS I on SI.ItemID = I.ID " +
                         $"join asvela.supplier_data AS SD on LEFT(I.SKU, locate(\"_\", I.SKU)-1) = SD.asvela_sku where SI.EbayItemID = {ebayItemID} " +
                         "UNION " +
                         "select distinct SD.tecdoc_number from Elart.ShopItems as SI " +
                         "join Elart.Items AS I on SI.ItemID = I.ID " +
                         $"join asvela.supplier_data AS SD on I.SKU = SD.asvela_sku where SI.EbayItemID = {ebayItemID} ";
            string partNumber = conn.Query<string>(que).FirstOrDefault();
            partNumber = partNumber == null ? partNumber = "" : partNumber;
            return partNumber;
        }

        private string GetPartNumber(MySqlConnection conn, int itemID)
        {
            string que = "select distinct SD.tecdoc_number from Elart.Items AS I " +
                         $"join asvela.supplier_data AS SD on LEFT(I.SKU, locate(\"_\", I.SKU)-1) = SD.asvela_sku where I.ID = {itemID} " +
                         "UNION " +
                         "select distinct SD.tecdoc_number from Elart.Items AS I " +
                         $"join asvela.supplier_data AS SD on I.SKU = SD.asvela_sku where I.ID = {itemID};";
            string partNumber = conn.Query<string>(que).FirstOrDefault();
            partNumber = partNumber == null ? partNumber = "" : partNumber;
            return partNumber;
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

        private MagickImage GetTextPictureElement(string v, MagickReadSettings settings)
        {
            MagickReadSettings readSettings = settings;
            readSettings.TextGravity = Gravity.Center;
            readSettings.FillColor = MagickColors.DarkSlateGray;
            readSettings.Width = 13 * (v.Length - 6);
            readSettings.Height = CELL_SIZE;
            MagickImage label = new MagickImage(v, readSettings);
            return label;
        }

        private string GetPathImage(MySqlConnection conn, string ebayItemID, string partNumber, out int brandID, out string nameForFTP, out long itemID, out string sku)
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
            var res1 = conn.Query<(string, long)>($"select I.SKU, I.ID from Elart.ShopItems SI join Elart.Items I on SI.ItemID = I.ID where SI.EbayItemID = '{ebayItemID}';").FirstOrDefault();
            sku = res1.Item1;
            itemID = res1.Item2;

            int localItemID = (int)itemID;

            nameForFTP = sku + "_";

            Regex regularExpressionForGroupSetsSKU = new Regex("(_){1}[0-9]{1,2}(X|x){1}$", RegexOptions.Compiled);
            var match = regularExpressionForGroupSetsSKU.Match(sku);
            if (match.Success)
            {
                sku = sku.Replace(match.Value, "");
            }



            string que = "select GD.graphic_file_number, SD.supplier_id, GD.doc_key from asvela.supplier_data SD " +
                    "join Temp.Allocation_of_Graphics_to_Article_Numbers AGAN " +
                    "on SD.tecdoc_number = AGAN.man_article_number and SD.supplier_id = AGAN.supplier_id and AGAN.sort_key = 1 " +
                    "join Temp.Graphics_Documents GD on AGAN.graphic_number = GD.graphic_number and (GD.doc_key = 1 or GD.doc_key = 3) " +
                    $"where SD.asvela_sku = '{sku}'; ";
            var res2 = conn.Query<(string, int, int)>(que).FirstOrDefault();

            if (res2.Item1 == null && res2.Item2 == 0 && res2.Item3 == 0)
            {
                que = "select GD.graphic_file_number, SD.supplier_id, GD.doc_key from asvela.supplier_data SD " +
                    "join ElartTecDoc.Allocation_of_Graphics_to_Article_Numbers AGAN on SD.tecdoc_number = AGAN.man_article_number and SD.supplier_id = AGAN.supplier_id and AGAN.sort_key = 1 " +
                    "join ElartTecDoc.Graphics_and_Documents GD on AGAN.graphic_number = GD.graphic_number and (GD.doc_key = 1 or GD.doc_key = 3 or GD.doc_key = 6) " +
                    $"where SD.asvela_sku = '{sku}'; ";
                res2 = conn.Query<(string, int, int)>(que).FirstOrDefault();
            }

            brandID = res2.Item2 != 0 ? res2.Item2 : conn.Query<int>($"select SD.supplier_id from Elart.ShopItems SI " +
                $"join Elart.Items I on SI.ItemID = I.ID join asvela.supplier_data SD on LEFT(I.SKU, locate(\"_\", I.SKU)-1) = SD.asvela_sku " +
                $"where SI.EbayItemID = '{ebayItemID}' " +
                "UNION " +
                $"select SD.supplier_id from Elart.ShopItems SI " +
                $"join Elart.Items I on SI.ItemID = I.ID join asvela.supplier_data SD on I.SKU = SD.asvela_sku " +
                $"where SI.EbayItemID = '{ebayItemID}'").FirstOrDefault();

            if (brandID == 30 && res2.Item1 != null && !res2.Item1.Contains(sku.Replace("BOSCH", "")))
            {
                que = "select GD.graphic_file_number, SD.supplier_id, GD.doc_key from asvela.supplier_data SD " +
                    "join Temp.Allocation_of_Graphics_to_Article_Numbers AGAN on SD.tecdoc_number = AGAN.man_article_number and SD.supplier_id = AGAN.supplier_id and AGAN.sort_key = 2 " +
                    "join Temp.Graphics_Documents GD on AGAN.graphic_number = GD.graphic_number and (GD.doc_key = 1 or GD.doc_key = 3) " +
                    $"where SD.asvela_sku = '{sku}'; ";
                res2 = conn.Query<(string, int, int)>(que).FirstOrDefault();

                if (res2.Item1 != null && !res2.Item1.Contains(sku.Replace("BOSCH", "")))
                {
                    que = "select GD.graphic_file_number, SD.supplier_id, GD.doc_key from asvela.supplier_data SD " +
                        "join Temp.Allocation_of_Graphics_to_Article_Numbers AGAN on SD.tecdoc_number = AGAN.man_article_number and SD.supplier_id = AGAN.supplier_id and AGAN.sort_key = 3 " +
                        "join Temp.Graphics_Documents GD on AGAN.graphic_number = GD.graphic_number and (GD.doc_key = 1 or GD.doc_key = 3) " +
                        $"where SD.asvela_sku = '{sku}'; ";
                    res2 = conn.Query<(string, int, int)>(que).FirstOrDefault();
                }

                if (res2.Item1 != null && !res2.Item1.Contains(sku.Replace("BOSCH", "")))
                {
                    res2.Item1 = "";
                }
            }

            string folder = brandID.ToString();
            while (folder.Length < 4)
            {
                folder = folder.Insert(0, "0");
            }

            string fileExtention = "";
            switch (res2.Item3)
            {
                case 1:
                    fileExtention = ".BMP";
                    break;
                case 3:
                    fileExtention = ".JPG";
                    break;
                case 5:
                    fileExtention = ".JPG";
                    break;
                case 6:
                    fileExtention = ".PNG";
                    break;
                case 7:
                    fileExtention = ".GIF";
                    break;
                default:
                    fileExtention = ".JPG";
                    break;
            }

            //if (!File.Exists("\\PIC_FILES\\" + folder + "\\PIC.7z.001\\" + res2.Item1 + fileExtention))
            //{
            //    que = "select GD.graphic_file_number, SD.supplier_id, GD.doc_key from asvela.supplier_data SD " +
            //        "join Test.Allocation_of_Graphics_to_Article_Numbers AGAN on SD.tecdoc_number = AGAN.man_article_number and AGAN.sort_key = 1 " +
            //        "join Test.Graphics_and_Documents GD on AGAN.graphic_number = GD.graphic_number " +
            //        $"where SD.asvela_sku = '{sku}'; ";
            //    var res3 = conn.Query<(string, int, int)>(que).FirstOrDefault();
            //    if (res3.Item1 != null)
            //    {
            //        res2 = res3;
            //    }
            //}

            //string path = "";

            nameForFTP = res2.Item1 == null ? nameForFTP : nameForFTP + res2.Item1.Trim();

            if (pictureInDB.Any(p => p.ItemID == localItemID && p.ShopID == shopID))
            {
                int version = pictureInDB.Where(p => p.ItemID == localItemID && p.ShopID == shopID).Select(p => p.Version).FirstOrDefault();
                nameForFTP += $"_ver{version + 1}";
            }

            //itemID = res.Item4;
            //sku = res.Item3;
            string fileName = res2.Item1 == null || res2.Item1 == "MS_120000316001" ? "" : res2.Item1.Trim();
            switch (res2.Item3)
            {
                case 1:
                    fileExtention = ".BMP";
                    break;
                case 3:
                    fileExtention = ".JPG";
                    break;
                case 5:
                    fileExtention = ".JPG";
                    break;
                case 6:
                    fileExtention = ".PNG";
                    break;
                case 7:
                    fileExtention = ".GIF";
                    break;
                default:
                    fileExtention = ".JPG";
                    break;
            }

            if (brandID == 4865)
            {
                int cylCount;
                if (partNumber.EndsWith("S") || partNumber.EndsWith("s"))
                {
                    cylCount = 1;
                }
                else
                {
                    cylCount = skuCyl_HUST_Pair[sku];
                }
                //int cylCount = skuCyl_HUST_Pair[sku];
                if (cylCount != 1 && cylCount != 2 && cylCount != 3 && cylCount != 4 && cylCount != 6 && cylCount != 8)
                    return $"E:\\Generic Images\\HASTINGS\\none.jpg";
                return $"E:\\Generic Images\\HASTINGS\\{cylCount}c_1.jpg";
            }

            if (brandID == 4460)
            {
                //string partNumber = GetPartNumber(conn, ebayItemID);
                partNumber = partNumber.Replace(" ", "");
                string kingFileName = partNumber.Substring(0, 3);
                if (partNumber.Contains("XP"))
                {
                    kingFileName += "XP";
                }
                return "E:\\Done Programm\\KING\\Images_ger\\Pictures\\" + kingFileName + ".png";
            }

            if (brandID == 0)
            {
                lock (locker)
                {
                    File.AppendAllText("error.txt", $"Brand not found for {ebayItemID} ({sku}) \n");
                }
                return "";
            }

            if (picturesException.ContainsKey((int)itemID))
            {
                string excURL = picturesException[(int)itemID];
                return excURL;
            }

            string localSku = sku;
            int localBtandID = brandID;
            if (brandID != 30)
            {
                if (badFiles.Any(f => f.brandID == localBtandID && f.sku == localSku))
                {
                    fileName = "";
                }
            }



            return "\\PIC_FILES\\" + folder + "\\PIC.7z.001\\" + fileName + fileExtention;
        }

        private async void Form2_Shown(object sender, EventArgs e)
        {
            GetErrorFileFromFTPAsync();
            await Task.Run(() => ReadFromDBAsync());

            comboBoxShop.Items.AddRange(shops.Select(s => s.Alias).ToArray());
            comboBoxShop.SelectedItem = comboBoxShop.Items[1];


            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            checkedListBoxBrands.Items.AddRange(brandIDs.Select(b => b.ToString()).ToArray());
        }

        private async void GetErrorFileFromFTPAsync()
        {
            FtpOperation ftpClient = new FtpOperation();
            badFiles = await Task.Run(() => GetFromFTP(ftpClient));
        }

        private async void ReadFromDBAsync()
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
                }
                shops = (await conn.QueryAsync<Shop>($"SELECT * FROM Elart.Shops;")).ToList();



                picturesException = (await conn.QueryAsync<(int, string)>("select ItemID, URL from Elart.ItemsValidPictureReplacement;")).ToDictionary(ex => ex.Item1, ex => ex.Item2);


                groupSetIDs = (await conn.QueryAsync<string>($"select distinct EbayItemID from Elart.ShopItemGroupSets where EbayListingStatus = 0;")).ToList();
                brandIDs = (await conn.QueryAsync<int>($"select distinct TecdocSupplierID from Elart.BrandPicrureTemplates order by TecdocSupplierID")).ToList();

                conn.Close();
            }
        }

        private void checkBoxAllBrands_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAllBrands.Checked == true)
            {
                checkedListBoxBrands.Enabled = false;
            }
            else
            {
                checkedListBoxBrands.Enabled = true;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.CurrentDirectory;
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;


                List<string> skuFromFiles = new List<string>();
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    skuFromFiles = File.ReadAllLines(openFileDialog.FileName).ToList();
                }

                skuFromFiles = skuFromFiles.Select(s => s.Split(';')[0]).ToList();

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
                    shopID = shops.Where(p => p.Alias == (string)comboBoxShop.SelectedItem).Select(p => p.ID).FirstOrDefault();

                    string que = $"select I.SKU, MP.URL from Elart.ShopItemMainPictures MP join Elart.Items I on MP.ItemID = I.ID where I.SKU in ({"\"" + string.Join("\",\"", skuFromFiles) + "\""}) and MP.ShopID = {shopID}";
                    List<string> resPair = conn.Query<(string, string)>(que).Select(l => l.Item1 + ";" + l.Item2).ToList();
                    File.WriteAllLines(openFileDialog.FileName.Replace(".csv", "_result.csv"), resPair);
                    MessageBox.Show($"Data was fixed in file {openFileDialog.FileName.Replace(".csv", "_result.csv")}");
                    conn.Close();
                }
                button3.Enabled = true;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //List<string> dataFromDB = (e.Argument as DoWorkArguments).DataFromDB;
            MySqlConnection conn = (e.Argument as DoWorkArguments).connection;
            //PictureTemplate pictureTemplate = (e.Argument as DoWorkArguments).pictureTemplate;

            List<int> brandForAnalize = (e.Argument as DoWorkArguments).BrandForAnalize;

            foreach (int brandID in brandForAnalize)
            {
                if (brandID == 4865)
                {
                    FillCylindPair();
                }
                //###################
                //if (brandID < 30)
                //    continue;
                //###################
                //textBox1.AppendText(new string('-', 10) + "\n");
                //textBox1.AppendText($"BrandID: {brandID}:\n");
                this.Invoke(new Action(() => SendText(new string('-', 10) + "\n")));
                this.Invoke(new Action(() => SendText($"BrandID: {brandID}:\n")));

                List<string> dataFromDB = conn.Query<string>("SELECT distinct SI.EbayItemID FROM Elart.ShopItems SI " +
                                                 "join Elart.Items I on SI.ItemID = I.ID " +
                                                 $"join asvela.supplier_data SD on LEFT(I.SKU, locate(\"_\", I.SKU)-1) = SD.asvela_sku and SD.supplier_id = {brandID} " +
                                                 "left join Elart.ShopItemMainPictures as SIMP on SI.ItemID = SIMP.ItemID and SI.ShopID = SIMP.ShopID " +
                                                 $"where (SIMP.IsActual is null or SIMP.IsActual = 0) and SI.EbayListingStatus = 0 and SI.ShopID = {shopID} " +
                                                 "UNION " +
                                                 "SELECT distinct SI.EbayItemID FROM Elart.ShopItems SI " +
                                                 "join Elart.Items I on SI.ItemID = I.ID " +
                                                 $"join asvela.supplier_data SD on I.SKU = SD.asvela_sku and SD.supplier_id = {brandID} " +
                                                 "left join Elart.ShopItemMainPictures as SIMP on SI.ItemID = SIMP.ItemID and SI.ShopID = SIMP.ShopID " +
                                                 $"where (SIMP.IsActual is null or SIMP.IsActual = 0) and SI.EbayListingStatus = 0 and SI.ShopID = {shopID};").ToList();

                //dataFromDB = dataFromDB.Take(100).ToList();

                //dataFromDB = dataFromDB.Where(d => d == "353640273288").ToList();

                this.Invoke(new Action(() => SendText($"Listings count: {dataFromDB.Count}\n")));

                this.Invoke(new Action(() => SetProgressBar(dataFromDB.Count)));

                byte[] res = conn.Query<byte[]>($"select Template from Elart.BrandPicrureTemplates where TecdocSupplierID = {brandID} and ShopID = {shopID};").FirstOrDefault();
                if (res == null)
                {
                    this.Invoke(new Action(() => SendText("Template not found\n")));
                    continue;
                }
                PictureTemplate pictureTemplate = PictureTemplate.GetTemplateFromByte(res);


                List<Task> tasks = new List<Task>();
                List<MySqlConnection> listConn = new List<MySqlConnection>();

                //if (dataFromDB.Count < threadsCount)
                {
                    MySqlConnection localConn = (MySqlConnection)conn.Clone();
                    CreatePictureInThread(localConn, dataFromDB, pictureTemplate);
                    listConn.Add(localConn);
                }
                //else
                //{
                //    List<string>[] listsForTheads = new List<string>[threadsCount];
                //    for (int i = 0; i < dataFromDB.Count; i++)
                //    {
                //        if (i < threadsCount)
                //        {
                //            listsForTheads[i] = new List<string>();
                //        }
                //        listsForTheads[i % threadsCount].Add(dataFromDB[i]);
                //    }

                //    for (int j = 0; j < threadsCount; j++)
                //    {
                //        List<string> lstr = listsForTheads[j];
                //        MySqlConnection localConn = (MySqlConnection)conn.Clone();
                //        tasks.Add(Task.Run(() => CreatePictureInThread(localConn, lstr, pictureTemplate)));
                //        listConn.Add(localConn);
                //    }
                //}
                Task.WaitAll(tasks.ToArray());
                foreach (var localConn in listConn)
                {
                    localConn.Close();
                }
                conn.Close();
            }
        }

        private void SetProgressBar(int maxVal)
        {
            progressBar1.Maximum = maxVal;
            progressBar1.Value = 0;
        }

        private void SendText(string text)
        {
            textBox1.AppendText(text);
        }

        private void AddValueProgresBar()
        {
            progressBar1.PerformStep();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            this.Invoke(new Action(() => AddValueProgresBar()));
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Creating of pictures was complete");
            button1.Enabled = true;
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            List<string> skuFromFiles = e.Argument as List<string>;
            //#######
            //skuFromFiles = new List<string> { skuFromFiles.FirstOrDefault() };
            //#######

            this.Invoke(new Action(() => SetProgressBar(skuFromFiles.Count)));
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

                pictureInDB = conn.Query<PictureInDB>($"select * from Elart.ShopItemMainPictures where ShopID = {shopID};").ToList();
                groupSetIDs = conn.Query<string>($"select distinct EbayItemID from Elart.ShopItemGroupSets where ShopID = {shopID} and EbayListingStatus = 0;").ToList();
                List<MySqlConnection> listConn = new List<MySqlConnection>();
                //if (skuFromFiles.Count < threadsCount)
                {
                    MySqlConnection localConn = (MySqlConnection)conn.Clone();
                    CreatePictureInThread(localConn, skuFromFiles);
                    listConn.Add(localConn);
                }
                //else
                //{
                //    List<string>[] listsForTheads = new List<string>[threadsCount];
                //    for (int i = 0; i < skuFromFiles.Count; i++)
                //    {
                //        if (i < threadsCount)
                //        {
                //            listsForTheads[i] = new List<string>();
                //        }
                //        listsForTheads[i % threadsCount].Add(skuFromFiles[i]);
                //    }
                //    List<Task> tasks = new List<Task>();

                //    for (int j = 0; j < threadsCount; j++)
                //    {
                //        List<string> lstr = listsForTheads[j];
                //        MySqlConnection localConn = (MySqlConnection)conn.Clone();
                //        tasks.Add(Task.Run(() => CreatePictureInThread(localConn, lstr)));
                //        listConn.Add(localConn);
                //    }

                //    //await Task.WhenAll(tasks);
                //    Task.WaitAll(tasks.ToArray());
                //    foreach (Task task in tasks)
                //    {
                //        this.Invoke(new Action(() => SendText(task.Exception.Message)));
                //    }

                //}
                foreach (var localConn in listConn)
                {
                    localConn.Close();
                }
                conn.Close();
            }
        }

        private void button_UM_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.CurrentDirectory;
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;


                List<string> skuFromFiles = new List<string>();
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    skuFromFiles = File.ReadAllLines(openFileDialog.FileName).ToList();
                }

                //############ for generic

                Dictionary<string, string> pairForGeneric = new Dictionary<string, string>();

                string pathToPics = "E:\\Done Programm\\UM\\forSite2";
                string[] files = Directory.GetFiles(pathToPics);
                Dictionary<string, string> result = new Dictionary<string, string>();

                foreach (string str in skuFromFiles)
                {
                    if (!pairForGeneric.ContainsKey(str.Split(';')[0]))
                    {
                        pairForGeneric.Add(str.Split(';')[0], str.Split(';')[1]);
                    }
                }

                foreach (var pair in pairForGeneric)
                {
                    string listPaths = files.Where(f => f.Contains(pair.Value)).OrderBy(f => f).FirstOrDefault();
                    result.Add(pair.Key, listPaths);
                }

                List<string> foundPictures = new List<string>();
                List<string> notFoundPictures = new List<string>();

                foreach (var res in result)
                {
                    if (res.Value != null)
                    {
                        //string joinedNames = string.Join(";", res.Value).Replace(pathToPics, "");
                        foundPictures.Add(res.Key + ";" + res.Value.Replace(pathToPics + "\\", ""));
                    }
                    else
                    {
                        notFoundPictures.Add(res.Key);
                    }
                }

                File.WriteAllLines("FoundedGenericPicturesUM.csv", foundPictures);
                File.WriteAllLines("NOT_FoundedPicturesUM.csv", notFoundPictures);

                //#####################################

                //string pathToPics = "E:\\Done Programm\\UM\\UM_picture\\";
                //string[] files = Directory.GetFiles(pathToPics);

                //Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
                //foreach (string sku in skuFromFiles)
                //{
                //    List<string> listPaths = files.Where(f => f.Contains(sku)).OrderBy(f => f).ToList();
                //    result.Add(sku, listPaths);
                //}

                //List<string> foundPictures = new List<string>();
                //List<string> notFoundPictures = new List<string>();

                //foreach(var res in result)
                //{
                //    if(res.Value.Count > 0)
                //    {
                //        string joinedNames = string.Join(";", res.Value).Replace(pathToPics, "");
                //        foundPictures.Add(res.Key + ";" + joinedNames);
                //    }
                //    else
                //    {
                //        notFoundPictures.Add(res.Key);
                //    }
                //}

                //File.WriteAllLines("FoundedPicturesUM.csv", foundPictures);
                //File.WriteAllLines("NOT_FoundedPicturesUM.csv", notFoundPictures);
            }
        }
    }

    class DoWorkArguments
    {
        public List<int> BrandForAnalize { get; set; }
        public MySqlConnection connection { get; set; }
        //public PictureTemplate pictureTemplate { get; set; }
    }
}
