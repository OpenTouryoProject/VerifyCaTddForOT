using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace MVC_Sample.E2ETests
{
    /// <summary>
    /// 受注帳票プレビュー（SC-ORD-05）と、受注詳細からの遷移（SC-ORD-02 の B04・B05）の画面のテスト（MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）。
    /// 仕様：docs/spec/test3-testfirst-OrdTfReport-mvc-core-tests.md（NorthwindDbSampleSpec@b60c760）の 4 章。
    /// B層で確かめること（金額の丸め・支払期限の月末・読むだけで更新しないこと）は繰り返さない。
    /// </summary>
    public class OrdTfReportScreenTests
    {
        private const string MsgNotFound = "対象のデータは存在しません。";

        /// <summary>E2E の受注日は 2198 年（B層のテストは 2199 年）</summary>
        private static readonly DateTime Start2198 = new DateTime(2198, 1, 1);

        private static readonly Regex Danger = new Regex(@"\btext-danger\b");
        private static readonly Regex Info = new Regex(@"\btext-info\b");
        private static readonly Regex Empty = new Regex(@"^\s*$");

        /// <summary>宛先の連結：出荷先の 6 項目がこの順に並ぶ（区切りは期待にしない。DEC-ORD-65）</summary>
        private static readonly Regex ShipToPattern =
            new Regex(@"^\s*ShipTo Co([\s\S]*)4-5-6 Ginza([\s\S]*)Chuo([\s\S]*)Tokyo([\s\S]*)104-0061([\s\S]*)Japan\s*$");

        private IBrowserContext context;
        private IPage page;
        private TestData data;

        [SetUp]
        public async Task SetUpAsync()
        {
            data = new TestData();
            context = await AppHost.Browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = AppHost.BaseUrl });
            page = await context.NewPageAsync();
            page.SetDefaultTimeout(5000);

            // ログイン（/Home/Login。利用者名を入れれば通る）
            await page.GotoAsync("/Home/Login");
            await page.FillAsync("#UserName", "e2e");
            await page.ClickAsync("input[name=normal]");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            if (context != null) await context.CloseAsync();   // ブラウザを閉じてからデータを消す
            context = null; page = null;
            if (data != null) { TestData d = data; data = null; d.CleanUp(); }
        }

        // ================================================================ 操作の部品
        // 要素が無いときに、操作が待ち続けて TimeoutException になるのではなく、Expect で落ちるように、操作の前に Expect で待つ。

        private ILocator Loc(string selector) { return page.Locator(selector); }

        private ILocator Rows { get { return page.Locator("#orderList tbody tr"); } }

        private ILocator Cell(int row, int col) { return Rows.Nth(row).Locator("td").Nth(col); }

        private ILocator LineRows { get { return page.Locator("#reportLines tbody tr"); } }

        private ILocator LineCell(int row, int col) { return LineRows.Nth(row).Locator("td").Nth(col); }

        private async Task SelectAsync(string selector, string value)
        {
            await Expect(Loc(selector)).ToBeVisibleAsync();
            await page.SelectOptionAsync(selector, value);
        }

        private async Task ClickAsync(string selector)
        {
            await Expect(Loc(selector)).ToBeEnabledAsync();
            await page.ClickAsync(selector);
        }

        private async Task OpenListAsync() { await page.GotoAsync("/OrdTf"); }

        private async Task OpenDetailAsync(int orderId) { await page.GotoAsync("/OrdTf/Detail?id=" + orderId); }

        private async Task OpenReportAsync(int orderId, string type) { await page.GotoAsync("/OrdTf/Report?id=" + orderId + "&type=" + type); }

        /// <summary>顧客を選んで検索する</summary>
        private async Task OpenAndSearchAsync(string customerId)
        {
            await OpenListAsync();
            await SelectAsync("#CustomerID", customerId);
            await ClickAsync("#btnMain1");
        }

        /// <summary>一覧の行の受注 ID（1 列目のセルの &lt;a&gt;）を選ぶ</summary>
        private async Task ClickOrderIdAsync(int row)
        {
            ILocator link = Cell(row, 0).Locator("a");
            await Expect(link).ToBeVisibleAsync();
            await link.ClickAsync();
        }

        private async Task ExpectListAsync(IList<int> ids)
        {
            await Expect(Rows).ToHaveCountAsync(ids.Count);
            for (int i = 0; i < ids.Count; i++)
                await Expect(Cell(i, 0)).ToHaveTextAsync(ids[i].ToString());
        }

        private async Task ExpectPageAsync(string pageInfo, string totalCount)
        {
            await Expect(Loc("#pageInfo")).ToHaveTextAsync(pageInfo);
            await Expect(Loc("#totalCount")).ToHaveTextAsync(totalCount);
        }

        /// <summary>i 番目（1 から）の受注 = ids[i - 1]。新しい順（i の降順）で topI から bottomI まで</summary>
        private static List<int> Range(List<int> ids, int topI, int bottomI)
        {
            List<int> r = new List<int>();
            for (int i = topI; i >= bottomI; i--) r.Add(ids[i - 1]);
            return r;
        }

        private static string TodayText() { return DateTime.Today.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture); }

        /// <summary>翌月末（今日の月の翌月の末日）</summary>
        private static string DueText()
        {
            DateTime t = DateTime.Today;
            return new DateTime(t.Year, t.Month, 1).AddMonths(2).AddDays(-1).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }

        /// <summary>データ R（テスト仕様 4 章の冒頭）：受注 1 件と明細 3 行</summary>
        private sealed class DataR
        {
            public int OrderId;
            public string Last, First;
            public int ShipperId;
            public int ProductA, ProductM, ProductX;
        }

        private DataR NewDataR()
        {
            DataR r = new DataR();
            string k = data.NewCustomer('K', address: "CustStreet", city: "CustCity", country: "CustLand");
            r.Last = TestData.NewLastName('L');
            r.First = TestData.NewFirstName('F');
            int e = data.NewEmployee(r.Last, r.First);
            r.ShipperId = data.NewShipper();
            r.ProductA = data.NewProduct(mark: 'A');
            r.ProductM = data.NewProduct(mark: 'M');
            r.ProductX = data.NewProduct(mark: 'X');
            DateTime? shippedDate = new DateTime(2198, 3, 10);
            r.OrderId = data.NewOrder(customerId: k, employeeId: e, orderDate: new DateTime(2198, 3, 5, 15, 30, 0),
                requiredDate: new DateTime(2198, 4, 1), shippedDate: shippedDate, shipVia: r.ShipperId, freight: 34.50m,
                shipName: "ShipTo Co", shipAddress: "4-5-6 Ginza", shipCity: "Chuo", shipRegion: "Tokyo",
                shipPostalCode: "104-0061", shipCountry: "Japan");
            data.NewDetail(r.OrderId, r.ProductM, 50.00m, 2, 0.25f);   // M・X・A の順に登録する
            data.NewDetail(r.OrderId, r.ProductX, 20.00m, 3, 0.05f);
            data.NewDetail(r.OrderId, r.ProductA, 100.00m, 12, 0.15f);
            return r;
        }

        /// <summary>データ R の明細（A・M・X の順）と小計・送料・合計を確かめる</summary>
        private async Task ExpectLinesAsync(DataR r)
        {
            await Expect(Loc("#reportLines thead th")).ToHaveTextAsync(new[]
            {
                "商品名", "単価", "割引率", "価格", "数量", "明細金額",
            });
            await Expect(LineRows).ToHaveCountAsync(3);
            await Expect(LineRows.Nth(0).Locator("td")).ToHaveTextAsync(new[]
                { data.ProductNames[r.ProductA], "100.00", "15%", "85.00", "12", "1,020.00" });
            await Expect(LineRows.Nth(1).Locator("td")).ToHaveTextAsync(new[]
                { data.ProductNames[r.ProductM], "50.00", "25%", "37.50", "2", "75.00" });
            await Expect(LineRows.Nth(2).Locator("td")).ToHaveTextAsync(new[]
                { data.ProductNames[r.ProductX], "20.00", "5%", "19.00", "3", "57.00" });
            await Expect(Loc("#subtotal")).ToHaveTextAsync("1,152.00");
            await Expect(Loc("#freight")).ToHaveTextAsync("34.50");
            await Expect(Loc("#total")).ToHaveTextAsync("1,186.50");
        }

        // ================================================================ 受注帳票プレビューの表示（EV-ORD-501）

        [Test, Basis("EV-ORD-501"), Basis("SC-ORD-05-I01"), Basis("SC-ORD-05-I02"), Basis("SC-ORD-05-I03"), Basis("SC-ORD-05-I04"),
            Basis("SC-ORD-05-I05"), Basis("SC-ORD-05-I06"), Basis("SC-ORD-05-I07"), Basis("SC-ORD-05-I08"), Basis("SC-ORD-05-I09"),
            Basis("SC-ORD-05-I10"), Basis("SC-ORD-05-I11"), Basis("SC-ORD-05-I12"), Basis("SC-ORD-05-I13"), Basis("EMP-T1"),
            Basis("DEC-ORD-68"), Basis("共通仕様 3 節（金額の算出式）"), Basis("共通仕様 4 節（日付・空値の表示）")]
        public async Task 帳票_納品書の表示()
        {
            DataR r = NewDataR();

            await OpenReportAsync(r.OrderId, "delivery");

            await Expect(Loc("#reportTitle")).ToHaveTextAsync("納品書");
            await Expect(Loc("#reportType")).ToHaveValueAsync("delivery");
            await Expect(Loc("#reportType option:checked")).ToHaveTextAsync("納品書");
            await Expect(Loc("#reportType option")).ToHaveTextAsync(new[] { "納品書", "請求書" });
            await Expect(Loc("#reportType option").Nth(0)).ToHaveAttributeAsync("value", "delivery");
            await Expect(Loc("#reportType option").Nth(1)).ToHaveAttributeAsync("value", "invoice");
            await Expect(Loc("#issueDate")).ToHaveTextAsync(TodayText());
            await Expect(Loc("#orderId")).ToHaveTextAsync(r.OrderId.ToString());
            await Expect(Loc("#shipTo")).ToHaveTextAsync(ShipToPattern);
            await Expect(Loc("#shipTo")).Not.ToContainTextAsync("CustStreet");
            await Expect(Loc("#shipTo")).Not.ToContainTextAsync("CustCity");
            await Expect(Loc("#shipTo")).Not.ToContainTextAsync("CustLand");
            await Expect(Loc("#orderDate")).ToHaveTextAsync("2198/03/05");
            await Expect(Loc("#rowShippedDate")).ToBeVisibleAsync();
            await Expect(Loc("#shippedDate")).ToHaveTextAsync("2198/03/10");
            await Expect(Loc("#rowShipper")).ToBeVisibleAsync();
            await Expect(Loc("#shipperName")).ToHaveTextAsync(data.ShipperNames[r.ShipperId]);
            await Expect(Loc("#rowDueDate")).ToBeHiddenAsync();
            await Expect(Loc("#employeeName")).ToHaveTextAsync(r.Last + " " + r.First);
            await ExpectLinesAsync(r);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
        }

        [Test, Basis("EV-ORD-501"), Basis("SC-ORD-05-I01"), Basis("SC-ORD-05-I02"), Basis("SC-ORD-05-I03"), Basis("SC-ORD-05-I14"),
            Basis("DEC-ORD-68")]
        public async Task 帳票_請求書の表示()
        {
            DataR r = NewDataR();

            await OpenReportAsync(r.OrderId, "invoice");

            await Expect(Loc("#reportTitle")).ToHaveTextAsync("請求書");
            await Expect(Loc("#reportType")).ToHaveValueAsync("invoice");
            await Expect(Loc("#reportType option:checked")).ToHaveTextAsync("請求書");
            await Expect(Loc("#issueDate")).ToHaveTextAsync(TodayText());
            await Expect(Loc("#rowDueDate")).ToBeVisibleAsync();
            await Expect(Loc("#dueDate")).ToHaveTextAsync(DueText());
            await Expect(Loc("#rowShippedDate")).ToBeHiddenAsync();
            await Expect(Loc("#rowShipper")).ToBeHiddenAsync();
            // 納品書のときと同じ値
            await Expect(Loc("#orderId")).ToHaveTextAsync(r.OrderId.ToString());
            await Expect(Loc("#shipTo")).ToHaveTextAsync(ShipToPattern);
            await Expect(Loc("#orderDate")).ToHaveTextAsync("2198/03/05");
            await Expect(Loc("#employeeName")).ToHaveTextAsync(r.Last + " " + r.First);
            await ExpectLinesAsync(r);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
        }

        [Test, Basis("EV-ORD-501"), Basis("共通仕様 4 節（空値の表示）"), Basis("DEC-CMN-02"), Basis("DEC-CMN-03"), Basis("ORD-T1")]
        public async Task 帳票_NULLは空欄で金額は0()
        {
            int o = data.NewOrder(freight: null);   // CustomerID・EmployeeID・日付・ShipVia・Ship*・送料がすべて NULL、明細なし

            await OpenReportAsync(o, "delivery");

            await Expect(Loc("#reportTitle")).ToHaveTextAsync("納品書");
            await Expect(Loc("#orderDate")).ToHaveTextAsync(Empty);
            await Expect(Loc("#employeeName")).ToHaveTextAsync(Empty);
            await Expect(Loc("#freight")).ToHaveTextAsync(Empty);
            // 納品書は出荷日・配送業者の項目を表示し、値が空
            await Expect(Loc("#rowShippedDate")).ToBeVisibleAsync();
            await Expect(Loc("#rowShipper")).ToBeVisibleAsync();
            await Expect(Loc("#shippedDate")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipperName")).ToHaveTextAsync(Empty);
            await Expect(LineRows).ToHaveCountAsync(0);
            await Expect(Loc("#subtotal")).ToHaveTextAsync("0.00");
            await Expect(Loc("#total")).ToHaveTextAsync("0.00");
        }

        [Test, Basis("EV-ORD-501"), Basis("ERR-NOTFOUND"), Basis("画面遷移 F-4")]
        public async Task 帳票_存在しない受注は一覧へ戻りエラーを表示する()
        {
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 5));
            TestData.DeleteOrder(o);

            await OpenReportAsync(o, "delivery");

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await Expect(Loc("#reportTitle")).ToHaveCountAsync(0);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNotFound);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Info);
        }

        [Test, Basis("EV-ORD-501"), Basis("ERR-NOTFOUND"), Basis("画面遷移 F-2"), Basis("画面遷移 F-4"), Basis("DEC-ORD-70")]
        public async Task 帳票_存在しない受注で保持された一覧状態で一覧へ戻る()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 25, Start2198);

            await OpenAndSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await ExpectPageAsync("2 / 2 ページ", "全 25 件");
            await ExpectListAsync(Range(ids, 5, 1));
            await ClickOrderIdAsync(0);
            await Expect(Loc("#orderId")).ToHaveTextAsync(ids[4].ToString());
            TestData.DeleteOrder(ids[4]);
            await ClickAsync("#btnDelivery");

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await Expect(Loc("#reportTitle")).ToHaveCountAsync(0);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNotFound);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            // 保持された一覧状態で戻る（DEC-ORD-70）
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await ExpectPageAsync("2 / 2 ページ", "全 24 件");
            await ExpectListAsync(Range(ids, 4, 1));
        }

        [Test, Basis("SC-ORD-05-I10"), Basis("DEC-ORD-68"), Basis("DEC-ORD-69")]
        public async Task 帳票_割引率と価格の表示()
        {
            int pa = data.NewProduct(mark: 'A');
            int pm = data.NewProduct(mark: 'M');
            int px = data.NewProduct(mark: 'X');
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1));
            data.NewDetail(o, pa, 0.50m, 3, 0.15f);
            data.NewDetail(o, pm, 100.00m, 1, 0.125f);
            data.NewDetail(o, px, 20.00m, 1, 0f);

            await OpenReportAsync(o, "delivery");

            await Expect(LineRows).ToHaveCountAsync(3);
            // 価格 0.425 は小数第 3 位で四捨五入して 0.43（切り捨て・偶数丸めなら 0.42）
            await Expect(LineCell(0, 2)).ToHaveTextAsync("15%");
            await Expect(LineCell(0, 3)).ToHaveTextAsync("0.43");
            await Expect(LineCell(0, 5)).ToHaveTextAsync("1.28");
            await Expect(LineCell(1, 2)).ToHaveTextAsync("12.5%");
            await Expect(LineCell(1, 3)).ToHaveTextAsync("87.50");
            await Expect(LineCell(2, 2)).ToHaveTextAsync("0%");
            await Expect(LineCell(2, 3)).ToHaveTextAsync("20.00");
        }

        [Test, Basis("SC-ORD-05-I10"), Basis("DEC-ORD-67")]
        public async Task 帳票_廃番商品の商品名()
        {
            int p = data.NewProduct(mark: 'P');
            int q = data.NewProduct(mark: 'Q', discontinued: true);
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1));
            data.NewDetail(o, p, 10.00m, 1, 0f);
            data.NewDetail(o, q, 10.00m, 1, 0f);

            await OpenReportAsync(o, "delivery");

            await Expect(LineRows).ToHaveCountAsync(2);
            await Expect(LineCell(0, 0)).ToHaveTextAsync(data.ProductNames[p]);
            await Expect(LineCell(1, 0)).ToHaveTextAsync(data.ProductNames[q]);   // 完全一致＝「（廃番）」を付記しない
        }

        // ================================================================ 帳票種別の切替（EV-ORD-502）

        [Test, Basis("EV-ORD-502"), Basis("SC-ORD-05-I01"), Basis("SC-ORD-05-I02"), Basis("SC-ORD-05-I07"), Basis("SC-ORD-05-I08"),
            Basis("SC-ORD-05-I14")]
        public async Task 帳票_種別を切り替えるとDBを読み直さずに表示が変わる()
        {
            DataR r = NewDataR();
            await OpenReportAsync(r.OrderId, "delivery");
            await Expect(Loc("#reportTitle")).ToHaveTextAsync("納品書");
            await Expect(Loc("#freight")).ToHaveTextAsync("34.50");
            TestData.BumpOrder(r.OrderId, 99.00m);   // 他の利用者の更新。読み直せば送料が 99.00 になる
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            List<string> requests = new List<string>();
            page.Request += (_, req) => requests.Add(req.Method + " " + req.Url);

            // 請求書へ
            await SelectAsync("#reportType", "invoice");
            await Expect(Loc("#reportTitle")).ToHaveTextAsync("請求書");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Expect(Loc("#rowDueDate")).ToBeVisibleAsync();
            await Expect(Loc("#dueDate")).ToHaveTextAsync(DueText());
            await Expect(Loc("#rowShippedDate")).ToBeHiddenAsync();
            await Expect(Loc("#rowShipper")).ToBeHiddenAsync();
            await Expect(Loc("#freight")).ToHaveTextAsync("34.50");   // 99.00 でない＝DB を読み直していない

            // 納品書へ戻す
            await SelectAsync("#reportType", "delivery");
            await Expect(Loc("#reportTitle")).ToHaveTextAsync("納品書");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Expect(Loc("#rowShippedDate")).ToBeVisibleAsync();
            await Expect(Loc("#shippedDate")).ToHaveTextAsync("2198/03/10");
            await Expect(Loc("#rowShipper")).ToBeVisibleAsync();
            await Expect(Loc("#shipperName")).ToHaveTextAsync(data.ShipperNames[r.ShipperId]);
            await Expect(Loc("#rowDueDate")).ToBeHiddenAsync();
            await Expect(Loc("#freight")).ToHaveTextAsync("34.50");

            Assert.That(requests, Is.Empty, "切り替えの間に、サーバへの要求が 0 件");
        }

        // ================================================================ 詳細からの遷移（EV-ORD-204）

        [TestCase(null)]
        [TestCase("2198/03/10")]
        [Basis("EV-ORD-204"), Basis("SC-ORD-02-B04"), Basis("SC-ORD-02-B05")]
        public async Task 詳細_帳票ボタンは常時活性(string shipped)
        {
            DateTime? shippedDate = shipped == null ? (DateTime?)null : DateTime.ParseExact(shipped, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            int p = data.NewProduct(mark: 'P');
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 4, 1), shippedDate: shippedDate);
            data.NewDetail(o, p, 10.00m, 1, 0f);

            await OpenDetailAsync(o);

            await Expect(Loc("#contents #btnDelivery")).ToHaveAccessibleNameAsync("納品書");
            await Expect(Loc("#contents #btnInvoice")).ToHaveAccessibleNameAsync("請求書");
            await Expect(Loc("#btnDelivery")).ToBeVisibleAsync();
            await Expect(Loc("#btnDelivery")).ToBeEnabledAsync();
            await Expect(Loc("#btnInvoice")).ToBeVisibleAsync();
            await Expect(Loc("#btnInvoice")).ToBeEnabledAsync();
            await Expect(Loc("#footer-buttons #btnDelivery")).ToHaveCountAsync(0);
            await Expect(Loc("#footer-buttons #btnInvoice")).ToHaveCountAsync(0);
            await Expect(Loc("#contents input:not([type=hidden]):not([type=submit]):not([type=button]), #contents select, #contents textarea"))
                .ToHaveCountAsync(0);
        }

        [TestCase("delivery", "#btnDelivery", "納品書")]
        [TestCase("invoice", "#btnInvoice", "請求書")]
        [Basis("EV-ORD-204"), Basis("画面遷移（SC-ORD-02 → SC-ORD-05：受注 ID、帳票種別）")]
        public async Task 詳細_帳票ボタンで帳票へ遷移する(string type, string button, string title)
        {
            DataR r = NewDataR();

            await OpenDetailAsync(r.OrderId);
            await ClickAsync(button);

            await Expect(Loc("#reportTitle")).ToHaveTextAsync(title);
            await Expect(Loc("#reportType")).ToHaveValueAsync(type);
            await Expect(Loc("#orderId")).ToHaveTextAsync(r.OrderId.ToString());
            Uri u = new Uri(page.Url);
            Assert.That(u.AbsolutePath, Is.EqualTo("/OrdTf/Report"));
            Assert.That(u.Query, Does.Contain("id=" + r.OrderId));
            Assert.That(u.Query, Does.Contain("type=" + type));
        }

        // ================================================================ 印刷（EV-ORD-503）

        [Test, Basis("EV-ORD-503"), Basis("SC-ORD-05-B01")]
        public async Task 印刷_ボタンはブラウザの印刷機能を呼ぶ()
        {
            DataR r = NewDataR();
            // ページが読み込まれる前に window.print を、呼び出し回数を数える関数に差し替える
            await page.AddInitScriptAsync("window.__printCount = 0; window.print = function () { window.__printCount++; };");
            await OpenReportAsync(r.OrderId, "delivery");
            await Expect(Loc("#reportTitle")).ToHaveTextAsync("納品書");
            await Expect(Loc("#btnPrint")).ToHaveAccessibleNameAsync("印刷");
            await Expect(Loc("#btnPrint")).ToBeVisibleAsync();
            await Expect(Loc("#btnPrint")).ToBeEnabledAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            List<string> requests = new List<string>();
            page.Request += (_, req) => requests.Add(req.Method + " " + req.Url);
            await page.ClickAsync("#btnPrint");

            await page.WaitForFunctionAsync("() => window.__printCount >= 1");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.That(await page.EvaluateAsync<int>("() => window.__printCount"), Is.EqualTo(1), "window.print が 1 回呼ばれた");
            Assert.That(requests, Is.Empty, "サーバへの要求が 0 件");
            await Expect(Loc("#reportTitle")).ToHaveTextAsync("納品書");
        }

        [TestCase("delivery")]
        [TestCase("invoice")]
        [Basis("EV-ORD-503"), Basis("SC-ORD-05-I01"), Basis("SC-ORD-05-B01"), Basis("SC-ORD-05-B02")]
        public async Task 印刷_印刷メディアでは操作用の要素を除く(string type)
        {
            DataR r = NewDataR();
            await OpenReportAsync(r.OrderId, type);
            await Expect(Loc("#reportTitle")).ToBeVisibleAsync();
            string[] operations = { "#reportType", "#btnPrint", "#btnBack", ".page-header", ".nav-side-menu" };
            string[] body = { "#reportTitle", "#orderId", "#reportLines", "#total" };

            // （1）画面のメディア：すべて見える
            foreach (string s in operations) await Expect(Loc(s)).ToBeVisibleAsync();

            // （2）印刷メディア：操作用の要素が除かれ、帳票の本体は残る
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
            foreach (string s in operations) await Expect(Loc(s)).ToBeHiddenAsync();
            foreach (string s in body) await Expect(Loc(s)).ToBeVisibleAsync();

            // （3）画面のメディアに戻せば再び見える（除くのは印刷のときだけ）
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Screen });
            foreach (string s in operations) await Expect(Loc(s)).ToBeVisibleAsync();
        }

        [Test, Basis("EV-ORD-503"), Basis("DEC-ORD-64")]
        public async Task 印刷_フッタのボタンも除く()
        {
            DataR r = NewDataR();
            await OpenReportAsync(r.OrderId, "delivery");
            await Expect(Loc("#reportTitle")).ToBeVisibleAsync();
            await Expect(Loc("#footer-buttons")).ToBeVisibleAsync();

            await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });

            await Expect(Loc("#footer-buttons")).ToBeHiddenAsync();
        }

        [Test, Basis("cmn-framework.md の画面共通"), Basis("決定 1")]
        public async Task 帳票_メインボタン()
        {
            DataR r = NewDataR();

            await OpenReportAsync(r.OrderId, "delivery");

            await Expect(Loc("#reportTitle")).ToHaveTextAsync("納品書");
            // フッタの 5 つはすべて非活性（キャプションなし）
            for (int i = 1; i <= 5; i++)
            {
                await Expect(Loc("#contents #btnMain" + i)).ToBeVisibleAsync();
                await Expect(Loc("#contents #btnMain" + i)).ToBeDisabledAsync();
                await Expect(Loc("#btnMain" + i)).ToHaveTextAsync(Empty);
            }
            // 印刷は画面上部（フッタの外）
            await Expect(Loc("#contents #btnPrint")).ToBeVisibleAsync();
            await Expect(Loc("#footer-buttons #btnPrint")).ToHaveCountAsync(0);
        }

        // ================================================================ 受注詳細への復帰（EV-ORD-504）

        [TestCase("delivery")]
        [TestCase("invoice")]
        [Basis("EV-ORD-504"), Basis("SC-ORD-05-I04"), Basis("SC-ORD-05-B02"), Basis("画面遷移（SC-ORD-05 → SC-ORD-02：受注 ID）")]
        public async Task 戻る_受注詳細へ復帰する(string type)
        {
            DataR r = NewDataR();
            await OpenReportAsync(r.OrderId, type);
            await Expect(Loc("#reportTitle")).ToBeVisibleAsync();
            await Expect(Loc("#btnBack")).ToHaveAccessibleNameAsync("戻る");
            await Expect(Loc("#btnBack")).ToBeVisibleAsync();
            await Expect(Loc("#btnBack")).ToBeEnabledAsync();
            await Expect(Loc("#contents #btnBack")).ToBeVisibleAsync();
            await Expect(Loc("#footer-buttons #btnBack")).ToHaveCountAsync(0);

            await page.ClickAsync("#btnBack");

            await Expect(Loc("#detailList")).ToBeVisibleAsync();
            await Expect(Loc("#orderId")).ToHaveTextAsync(r.OrderId.ToString());
            await Expect(Loc("#reportTitle")).ToHaveCountAsync(0);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            Uri u = new Uri(page.Url);
            Assert.That(u.AbsolutePath, Is.EqualTo("/OrdTf/Detail"));
            Assert.That(u.Query, Does.Contain("id=" + r.OrderId));
        }

        [Test, Basis("EV-ORD-504"), Basis("EV-ORD-206"), Basis("画面遷移 F-2"), Basis("EventSpec「一覧状態の保持」")]
        public async Task 戻る_帳票から戻った詳細から一覧へ戻ると一覧状態が復元される()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 25, Start2198);

            await OpenAndSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await ExpectPageAsync("2 / 2 ページ", "全 25 件");
            await ExpectListAsync(Range(ids, 5, 1));
            await ClickOrderIdAsync(0);
            await Expect(Loc("#orderId")).ToHaveTextAsync(ids[4].ToString());
            await ClickAsync("#btnDelivery");
            await Expect(Loc("#reportTitle")).ToHaveTextAsync("納品書");
            await ClickAsync("#btnBack");   // 帳票の「戻る」
            // 詳細が表示されたことを待つ（詳細と帳票で #btnBack の id が同じ）
            await Expect(Loc("#detailList")).ToBeVisibleAsync();
            Assert.That(new Uri(page.Url).AbsolutePath, Is.EqualTo("/OrdTf/Detail"));
            await Expect(Loc("#btnBack")).ToHaveAccessibleNameAsync("一覧へ戻る");
            await ClickAsync("#btnBack");   // 詳細の「一覧へ戻る」

            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await ExpectPageAsync("2 / 2 ページ", "全 25 件");
            await ExpectListAsync(Range(ids, 5, 1));
        }
    }
}
