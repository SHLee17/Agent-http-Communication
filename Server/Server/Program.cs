using System.Text;
using System.Timers;

namespace MyApp
{
    internal class Program
    {
        static object previousValue = null;
        static string logFile;

        static async Task Main(string[] args)
        {
            string log = Path.Combine(Directory.GetCurrentDirectory(), "Log");
            NewFolder(log, "LogFolder");
            logFile = Path.Combine(log, "Log");
            NewFile(logFile, "Log");

            string path = Path.Combine(Directory.GetCurrentDirectory(), "Path");
            NewFolder(path, "Path");
            string uploadfilePath = Path.Combine(path, "Upload Path" );
            string downloadfilePath = Path.Combine(path, "Download Path");
            NewFile(uploadfilePath, "Upload Path");
            NewFile(downloadfilePath, "Download Path");

            string folderPath = Path.Combine(ReadFileToString(uploadfilePath), "Upload");
            string downloadFolderPath = Path.Combine(ReadFileToString(uploadfilePath), "Download");
            NewFolder(folderPath, "Upload");
            NewFolder(downloadFolderPath, "Download");

            string[] filePaths = Directory.GetFiles(downloadFolderPath, "*.txt");
            DateTime earliestModifiedDate = DateTime.MaxValue;
            string earliestFilePath = "";

            foreach (string filePath in filePaths)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.LastWriteTime < earliestModifiedDate)
                {
                    earliestFilePath = filePath;
                    earliestModifiedDate = fileInfo.LastWriteTime;
                }
            }

            if (earliestModifiedDate < DateTime.MaxValue)
                previousValue = await File.ReadAllTextAsync(earliestFilePath);

            // 폴더의 파일 변경을 모니터링하기 위한 FileSystemWatcher 개체 생성
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = folderPath;
            watcher.Filter = "*.txt"; // 확장자 필터
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += OnChanged;
            watcher.EnableRaisingEvents = true;

            using var httpClient = new HttpClient();
            string previousFilePath = earliestFilePath;

            while (true)
            {
                var response = await httpClient.GetAsync("http://r741.realserver2.com/api/post.php");
                response.EnsureSuccessStatusCode();
                string currentValue = await response.Content.ReadAsStringAsync();
                if (previousFilePath == null || !File.Exists(previousFilePath) || currentValue != File.ReadAllText(previousFilePath))
                {
                    string fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
                    string filePath = Path.Combine(downloadFolderPath, fileName);
                    File.WriteAllText(filePath, currentValue);
                    previousFilePath = filePath;
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            //System.Timers.Timer timer = new System.Timers.Timer(10000);
            //timer.Elapsed += OnTimerElapsed;
            //timer.Start();

            //Console.WriteLine("Press enter to exit.");
            //Console.ReadLine();
        }
        static void NewFile(string path, string name)
        {
            if (File.Exists(path))
            {
                Console.WriteLine("중복된 파일 이름이 존재합니다.");
            }
            else
            {
                // 파일 생성
                File.WriteAllText(path, "");
                Console.WriteLine($"{name} 파일이 생성되었습니다.");
            }
        }
        static void NewFolder(string path, string name)
        {
            if (!Directory.Exists(path))
            {
                // 폴더가 없는 경우 폴더 만들기
                Directory.CreateDirectory(path);
                Console.WriteLine($"New {name} folder created.");
            }
            else
            {
                Console.WriteLine($"{name} Folder already exists.");
            }
            Console.WriteLine($"{name} Folder path: " + path);
        }
        static async void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"{e.FullPath} 생성되었습니다.");

            // 파일 내용을 읽고 문자열로 변환
            string fileContents = File.ReadAllText(e.FullPath);

            using var httpClient = new HttpClient();

            // HTTP 요청을 사용하여 홈 페이지로 문자열 전송
                var content = new StringContent(fileContents, Encoding.UTF8, "text/plain");
                var response = await httpClient.PostAsync("http://r741.realserver2.com/api/testJSON.php", content);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();

                File.AppendAllText(logFile, $"{DateTime.Now} - Success: {e.FullPath} - {responseBody}\n");
                Console.WriteLine(responseBody);
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                File.AppendAllText(logFile, $"{DateTime.Now} - Error: {e.FullPath} - {response.StatusCode}\n");
            }
        }
        static string ReadFileToString(string filePath)
        {
            // 파일이 존재하는지 확인
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return null;
            }

            try
            {
                // 파일을 읽어서 string으로 변환하여 반환
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read file: {ex.Message}");
                return null;
            }
        }
        //static async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        //{
        //    // HTTP 요청을 사용하여 홈페이지에서 보낸 문자열 값 확인
        //    //using (HttpClient client = new HttpClient())
        //    //{
        //    //    var response = await client.GetAsync("http://r741.realserver2.com/api/post.php");
        //    //    string currentValue = await response.Content.ReadAsStringAsync();
        //    //    if (previousValue != currentValue)
        //    //        previousValue = currentValue;
        //    //    else return;
        //    //    // 현재 날짜 및 시간을 기준으로 고유한 파일 이름 생성
        //    //    string fileName = "Download_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
        //    //    string filePath = Path.Combine(downloadFolderPath, fileName);

        //    //    File.WriteAllText(filePath, currentValue);
        //    //}
        //}

    }

}



