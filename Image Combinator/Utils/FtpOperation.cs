using Renci.SshNet;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Image_Combinator.Utils
{
    class FtpOperation
    {
        SftpClient client;
        string ftpServer = "shop.elartcom.eu";

        public FtpOperation()
        {
            Stream stream = File.OpenRead("ec2-efsuser.pem");
            var privateKey = new PrivateKeyFile(stream);
            client = new SftpClient(ftpServer, "ec2-efsuser", new[] { privateKey });
            client.Connect();
        }

        ~FtpOperation()
        {
            client.Disconnect();
        }

        public void Delete(string path)
        {
            while (!client.IsConnected)
            {
                try
                {
                    client.Connect();
                }
                catch
                {
                    Task.Delay(50);
                }
            }
            try
            {
                client.DeleteFile(path);
            }
            finally
            {
                //client.Disconnect();
            }
        }

        public string Upload(byte[] byteArr, string name, int brandID, int shopID)
        {
            while (!client.IsConnected)
            {
                try
                {                    
                    client.Connect();
                }
                catch
                {
                    Task.Delay(50);
                }
            }
            string folder = brandID.ToString();
            while (folder.Length < 4)
            {
                folder = folder.Insert(0, "0");
            }

            string ftpFullFileName;


            string shopName = "";
            switch (shopID)
            {
                case 1:
                    shopName = "couk";
                    break;
                case 2:
                    shopName = "couk2";
                    break;
                case 3:
                    shopName = "com";
                    break;
                case 4:
                    shopName = "de";
                    break;
            }
            name = name.Replace("/", "");
            try
            {
                string ftpDirectory = $"/var/www/html/static/ebay/{shopName}/new/" + folder;
                if (!client.Exists(ftpDirectory))
                {
                    client.CreateDirectory(ftpDirectory);
                }

                ftpFullFileName = ftpDirectory + "/" + name + ".jpg";
                using (MemoryStream fstream = new MemoryStream(byteArr))
                {
                    client.UploadFile(fstream, ftpFullFileName);
                }
            }
            finally
            {
                //client.Disconnect();
            }
            return "https://" + ftpServer + ftpFullFileName.Remove(0, 13);
        }

        public MemoryStream Download(string path)
        {

            while (!client.IsConnected)
            {
                try
                {
                    client.Connect();
                }
                catch
                {
                    Task.Delay(50);
                }
            }

            MemoryStream mstream = new MemoryStream();

            try
            {
                client.DownloadFile(path, mstream);
            }
            finally
            {
                //client.Disconnect();
            }

            return mstream;

        }

    }
}
