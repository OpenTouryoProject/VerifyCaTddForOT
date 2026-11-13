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
    /// <summary>テストの間だけアプリ（dotnet run）を起動し、ブラウザを用意する（名前空間の全テストに 1 回）</summary>
    [SetUpFixture]
    public class AppHost
    {
        public static string BaseUrl { get; private set; }
        public static IBrowser Browser { get; private set; }

        /// <summary>テストの出力（bin/Debug/net10.0）から Web のプロジェクトのフォルダへ</summary>
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
            BaseUrl = "http://127.0.0.1:" + FreePort();

            ProcessStartInfo psi = new ProcessStartInfo("dotnet", "run --no-launch-profile --urls " + BaseUrl)
            {
                WorkingDirectory = WebDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            // OT_RESOURCE_ROOT など、appsettings.json が参照する環境変数は、テストを実行するシェルから引き継ぐ

            app = Process.Start(psi);
            app.OutputDataReceived += (s, e) => { if (e.Data != null) TestContext.Progress.WriteLine("[app] " + e.Data); };
            app.ErrorDataReceived += (s, e) => { if (e.Data != null) TestContext.Progress.WriteLine("[app:err] " + e.Data); };
            app.BeginOutputReadLine();
            app.BeginErrorReadLine();

            // ログインの要らない URL（/Ping）が 200 を返すまで待つ
            using (HttpClient http = new HttpClient())
            {
                DateTime deadline = DateTime.Now.AddMinutes(3);
                while (true)
                {
                    if (app.HasExited) throw new InvalidOperationException("アプリが起動しなかった（exit " + app.ExitCode + "）");
                    try
                    {
                        HttpResponseMessage res = await http.GetAsync(BaseUrl + "/Ping");
                        if (res.StatusCode == HttpStatusCode.OK) break;
                    }
                    catch (HttpRequestException) { }
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
                app.Kill(entireProcessTree: true);   // dotnet run の子（アプリ）まで止める
                app.WaitForExit(10000);
            }
        }

        private static int FreePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
