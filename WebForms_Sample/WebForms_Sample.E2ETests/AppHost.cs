using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace WebForms_Sample.E2ETests
{
    /// <summary>テストの間だけアプリ（IIS Express）を起動し、ブラウザを用意する（名前空間の全テストに 1 回）</summary>
    /// <remarks>Web は先に msbuild でビルドしておく（テストはビルドしない）。</remarks>
    [SetUpFixture]
    public class AppHost
    {
        /// <summary>IIS Express のポート（空きポートを起動のたびに取る。ケース1 の E2E の 8090 と衝突させない）</summary>
        private static int Port;

        public static string BaseUrl { get; private set; }
        public static IBrowser Browser { get; private set; }

        /// <summary>テストの出力（bin\Debug\net48）から Web のプロジェクトのフォルダ（Web.config のある所）へ</summary>
        public static string WebDir
        {
            get
            {
                return Path.GetFullPath(Path.Combine(
                    TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "WebForms_Sample"));
            }
        }

        private static Process app;
        private static IPlaywright playwright;

        [OneTimeSetUp]
        public async Task StartAsync()
        {
            // 空きポートを取る（前の回の IIS Express が残っていても、別のポートで新しく起動するので古いビルドに当たらない）
            TcpListener probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            Port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
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

            // ログインの要らない URL（/Aspx/start/login.aspx）が 200 を返すまで待つ
            using (HttpClient http = new HttpClient())
            {
                DateTime deadline = DateTime.Now.AddMinutes(3);
                while (true)
                {
                    if (app.HasExited) throw new InvalidOperationException("IIS Express が起動しなかった（exit " + app.ExitCode + "）");
                    try
                    {
                        HttpResponseMessage res = await http.GetAsync(BaseUrl + "/Aspx/start/login.aspx");
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
    }
}
