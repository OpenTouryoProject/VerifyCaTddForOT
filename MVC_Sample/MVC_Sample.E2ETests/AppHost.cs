using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace MVC_Sample.E2ETests
{
    /// <summary>テストの間だけアプリ（IIS Express）を起動し、ブラウザを用意する（名前空間の全テストに 1 回）</summary>
    /// <remarks>Web は先に msbuild でビルドしておく（テストはビルドしない）。</remarks>
    [SetUpFixture]
    public class AppHost
    {
        /// <summary>IIS Express のポート（固定。使われていたらテストを失敗にする）</summary>
        private const int Port = 8090;

        public static string BaseUrl { get; private set; }
        public static IBrowser Browser { get; private set; }

        /// <summary>テストの出力（bin\Debug\net48）から Web のプロジェクトのフォルダ（Web.config のある所）へ</summary>
        public static string WebDir
        {
            get
            {
                return Path.GetFullPath(Path.Combine(
                    TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "MVC_Sample"));
            }
        }

        private static Process app;
        private static IPlaywright playwright;

        [OneTimeSetUp]
        public async Task StartAsync()
        {
            // 前の回の IIS Express が残っていると、古いビルドに当たったまま 200 で通ってしまう
            if (IsPortInUse(Port))
                throw new InvalidOperationException("ポート " + Port + " は使用中（前の回の iisexpress が残っていないか確認する）");
            BaseUrl = "http://localhost:" + Port;

            string iis = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IIS Express", "iisexpress.exe");
            ProcessStartInfo psi = new ProcessStartInfo(iis, "/path:\"" + WebDir + "\" /port:" + Port + " /clr:v4.0")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            // Web.config（app.config）が参照する変数を、テストを実行するシェルから明示で渡す
            string root = Environment.GetEnvironmentVariable("OT_RESOURCE_ROOT");
            if (root != null) psi.EnvironmentVariables["OT_RESOURCE_ROOT"] = root;
            app = Process.Start(psi);

            // ログインの要らない URL（/Home/Login）が 200 を返すまで待つ
            using (HttpClient http = new HttpClient())
            {
                DateTime deadline = DateTime.Now.AddMinutes(3);
                while (true)
                {
                    if (app.HasExited) throw new InvalidOperationException("IIS Express が起動しなかった（exit " + app.ExitCode + "）");
                    try
                    {
                        HttpResponseMessage res = await http.GetAsync(BaseUrl + "/Home/Login");
                        if (res.StatusCode == HttpStatusCode.OK) break;
                    }
                    catch (HttpRequestException) { }
                    catch (TaskCanceledException) { }
                    if (DateTime.Now > deadline) throw new TimeoutException("アプリが応答しない: " + BaseUrl);
                    await Task.Delay(500);   // 起動の待ち合わせだけ。テストの中では固定の時間を待たない
                }
            }

            playwright = await Playwright.CreateAsync();
            // Edge のチャネル（Windows に標準。Playwright の chromium の版に縛られない）
            Browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, Channel = "msedge" });
        }

        [OneTimeTearDown]
        public async Task StopAsync()
        {
            if (Browser != null) await Browser.DisposeAsync();
            if (playwright != null) playwright.Dispose();
            if (app != null && !app.HasExited)
            {
                app.Kill();   // net48 の Process に entireProcessTree は無い（IIS Express は子のプロセスを持たない）
                app.WaitForExit(10000);
            }
        }

        /// <summary>ポートが使われているか（接続できる、または待ち受けを開始できない）</summary>
        private static bool IsPortInUse(int port)
        {
            try
            {
                using (TcpClient c = new TcpClient())
                {
                    Task t = c.ConnectAsync(IPAddress.Loopback, port);
                    if (t.Wait(1000) && c.Connected) return true;
                }
            }
            catch (Exception) { }
            try
            {
                TcpListener l = new TcpListener(IPAddress.Loopback, port);
                l.Start();
                l.Stop();
                return false;
            }
            catch (SocketException) { return true; }
        }
    }
}
