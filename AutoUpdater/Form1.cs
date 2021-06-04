using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ionic.Zip;

namespace AutoUpdater
{
    public partial class Form1 : Form
    {
        private string releaseAddr = "https://github.com/josh-hsu/CrossAuto/blob/master/CrossAuto/Release/";
        private string downloadAddr = "https://github.com/josh-hsu/CrossAuto/blob/master/CrossAuto/Release/CrossAuto_v21.6.3.02.zip?raw=true";
        private string filePrefix = "CrossAuto_";
        private Boolean currVersionFound = false;
        private String currVersion = "";
        private String newestVersion = "";
        private Boolean newestVersionFound = false;
        private String downloadedFileName;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private Boolean NeedUpdate()
        {
            List<string> availableVersions = GetAvaiableVersions();
            string newestAvailablVersion = availableVersions[availableVersions.Count - 1];
            newestVersion = newestAvailablVersion.Split('v')[1].Split('z')[0];

            GetCurrentVersion();

            if (!currVersionFound)
            {
                Console.WriteLine("找不到現有版本，重新下載");
                return true;
            }
            
            try
            {
                Console.WriteLine("Old " + currVersion + ", New " + newestVersion);
                string[] oldRaw = currVersion.Split('.');
                string[] newRaw = newestVersion.Split('.');
                int oldY = int.Parse(oldRaw[0]);
                int oldM = int.Parse(oldRaw[1]);
                int oldD = int.Parse(oldRaw[2]);
                int oldS = int.Parse(oldRaw[3]);
                int newY = int.Parse(newRaw[0]);
                int newM = int.Parse(newRaw[1]);
                int newD = int.Parse(newRaw[2]);
                int newS = int.Parse(newRaw[3]);

                Console.WriteLine("Old Y:" + oldY + ", M:" + oldM + ", D:" + oldD + ", S:" + oldS);
                Console.WriteLine("New Y:" + newY + ", M:" + newM + ", D:" + newD + ", S:" + newS);

                if (newY > oldY)
                    return true;
                else if (newM > oldM)
                    return true;
                else if (newD > oldD)
                    return true;
                else if (newS > oldS)
                    return true;
            }
            catch
            {
                MessageBox.Show("抱歉，版本比較失敗。");
            }

            return false;
        }

        private void GetCurrentVersion()
        {
            try
            {
                String[] rawVersion = File.ReadAllLines("version.txt");
                currVersionFound = true;
                currVersion = rawVersion[0]; //pure version number without any alphabet
            }
            catch
            {
                Console.WriteLine("Cannot open version.txt");
            }
        }

        private List<string> GetAvaiableVersions()
        {
            List<string> fileList = new List<string>();
            WebClient client = new WebClient();
            string downloadString = client.DownloadString(releaseAddr);
            string[] htmlLine = downloadString.Split('\n');
            foreach(string line in htmlLine) {
                if (line.Contains("data-pjax=\"#repo-content-pjax-container\"") && line.Contains(filePrefix))
                {
                    Console.WriteLine(line);
                    string[] entity = line.Split(' ');
                    foreach(string search in entity)
                    {
                        if (search.StartsWith("title="))
                        {
                            char[] charsToTrim = { '\"' };
                            fileList.Add( search.Split('=')[1].Trim(charsToTrim) );
                        }
                    }
                }
            }

            foreach(string fileName in fileList)
            {
                Console.WriteLine(" find: " + fileName);
            }
            return fileList;
        }

        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            Console.WriteLine("Progress: " + progress);
            progressBarUpdate.Value = progress;
            if (progress != 100)
            {
                labelInfo.Text = "檔案下載中，完成 " + progress + " %";
            }
            else
            {
                labelInfo.Text = "下載完成";
                ReplaceAndUpdate(downloadedFileName);
            }
        }

        private void DownloadFile(string fileName)
        {
            downloadAddr = releaseAddr + fileName + "?raw=true";
            downloadedFileName = fileName;
            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadFileAsync(
                    new System.Uri(downloadAddr),
                    fileName
                );
            }
        }

        private void WriteVersion(string version)
        {
            File.WriteAllText("version.txt", version);
        }

        private void ReplaceAndUpdate(string fileName)
        {
            string extractedDirectory = "CrossAuto";

            buttonCheckUpdate.Text = "解壓縮...";
            buttonCheckUpdate.Enabled = false;
            labelInfo.Text = "正在安裝，請勿關閉本程式";
            try
            {
                if (Directory.Exists(extractedDirectory))
                {
                    if (Directory.Exists(extractedDirectory + "\\resources"))
                        Directory.Delete(extractedDirectory + "\\resources", true);
                    if (File.Exists("CrossAuto.exe"))
                        File.Delete("CrossAuto.exe");
                }
                ReadOptions options = new ReadOptions();
                options.Encoding = Encoding.Default;

                using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(fileName, options))
                {
                    foreach (ZipEntry e in zip)
                    {
                        e.Extract(extractedDirectory, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("解壓縮失敗了: " + e.ToString());
            }

            WriteVersion(newestVersion);

            buttonCheckUpdate.Text = "檢查更新";
            newestVersionFound = false;
            buttonCheckUpdate.Enabled = true;
            labelInfo.Text = "安裝完畢";
        }

        private void UpdateNewVersion()
        {
            List<string> availableVersions = GetAvaiableVersions();
            string newestAvailablVersion = availableVersions[availableVersions.Count - 1];

            DownloadFile(newestAvailablVersion);
        }

        private void buttonCheckUpdate_Click(object sender, EventArgs e)
        {
            if (newestVersionFound)
            {
                UpdateNewVersion();
            }
            else
            {
                labelInfo.Text = "檢查檔案中";
                if (NeedUpdate())
                {
                    labelInfo.Text = "找到更新的版本: " + newestVersion + " 現有版本 " + currVersion;
                    newestVersionFound = true;
                    buttonCheckUpdate.Text = "下載新版本 " + newestVersion;
                }
                else
                {
                    labelInfo.Text = "版本 " + currVersion + " 為最新版本";
                }
            }
        }
    }
}
