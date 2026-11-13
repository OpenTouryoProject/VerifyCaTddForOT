using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace MVC_Sample.E2ETests
{
    /// <summary>
    /// 受注一覧（SC-ORD-01）の画面のテスト（MVC_Sample ＝ net48 ASP.NET MVC）。
    /// 仕様：docs/spec/test3-testfirst-OrdTf-mvc-tests.md（NorthwindDbSampleSpec@72e156e）の 4 章。
    /// B層で確かめること（抽出条件・並び・金額の計算・状況の判定）は繰り返さない。
    /// </summary>
    public class OrdTfScreenTests
    {
        private const string MsgFormat = "受注日は yyyy/MM/dd の形式で指定してください。";
        private const string MsgRelation = "受注日の開始は終了以前を指定してください。";
        private const string MsgNone = "該当するデータがありません。";

        /// <summary>E2E の受注日は 2198 年（B層のテストは 2199 年）</summary>
        private static readonly DateTime Start2198 = new DateTime(2198, 1, 1);

        private static readonly Regex Danger = new Regex(@"\btext-danger\b");
        private static readonly Regex Info = new Regex(@"\btext-info\b");
        private static readonly Regex RowDanger = new Regex(@"\btable-danger\b");
        private static readonly Regex Empty = new Regex(@"^\s*$");

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
        // 要素が無いときに、操作が 30 秒待って TimeoutException になるのではなく、Expect で落ちるように、操作の前に Expect で待つ。

        private ILocator Loc(string selector) { return page.Locator(selector); }

        private ILocator Rows { get { return page.Locator("#orderList tbody tr"); } }

        private ILocator Cell(int row, int col) { return Rows.Nth(row).Locator("td").Nth(col); }

        private async Task SelectAsync(string selector, string value)
        {
            await Expect(Loc(selector)).ToBeVisibleAsync();
            await page.SelectOptionAsync(selector, value);
        }

        private async Task FillAsync(string selector, string value)
        {
            await Expect(Loc(selector)).ToBeVisibleAsync();
            await page.FillAsync(selector, value);
        }

        private async Task ClickAsync(string selector)
        {
            await Expect(Loc(selector)).ToBeEnabledAsync();
            await page.ClickAsync(selector);
        }

        private async Task OpenAsync()
        {
            await page.GotoAsync("/OrdTf");
        }

        /// <summary>検索（#btnMain1）</summary>
        private async Task SearchAsync()
        {
            await ClickAsync("#btnMain1");
        }

        /// <summary>顧客を選んで検索する</summary>
        private async Task OpenAndSearchAsync(string customerId)
        {
            await OpenAsync();
            await SelectAsync("#CustomerID", customerId);
            await SearchAsync();
        }

        private async Task ExpectPagerAsync(bool first, bool prev, bool next, bool last)
        {
            await ExpectEnabledAsync("#btnPageFirst", first);
            await ExpectEnabledAsync("#btnPagePrev", prev);
            await ExpectEnabledAsync("#btnPageNext", next);
            await ExpectEnabledAsync("#btnPageLast", last);
        }

        private async Task ExpectEnabledAsync(string selector, bool enabled)
        {
            if (enabled) await Expect(Loc(selector)).ToBeEnabledAsync();
            else await Expect(Loc(selector)).ToBeDisabledAsync();
        }

        private async Task ExpectListAsync(IList<int> ids)
        {
            await Expect(Rows).ToHaveCountAsync(ids.Count);
            for (int i = 0; i < ids.Count; i++)
                await Expect(Cell(i, 0)).ToHaveTextAsync(ids[i].ToString());
        }

        /// <summary>i 番目（1 から）の受注 = ids[i - 1]。新しい順（i の降順）で topI から bottomI まで</summary>
        private static List<int> Range(List<int> ids, int topI, int bottomI)
        {
            List<int> r = new List<int>();
            for (int i = topI; i >= bottomI; i--) r.Add(ids[i - 1]);
            return r;
        }

        // ================================================================ 初期表示（EV-ORD-101）

        [Test, Basis("EV-ORD-101"), Basis("SC-ORD-01-I01"), Basis("SC-ORD-01-I02"), Basis("SC-ORD-01-I03"),
            Basis("SC-ORD-01-I04"), Basis("SC-ORD-01-I05")]
        public async Task 初期表示_選択肢と既定の条件()
        {
            string k = data.NewCustomer('K');
            string last = TestData.NewLastName('L');
            string first = TestData.NewFirstName('F');
            int e = data.NewEmployee(last, first);

            await OpenAsync();

            // 顧客・担当者：先頭が空白の選択肢で、テストが入れた行を含む
            await Expect(Loc("#CustomerID option").First).ToHaveAttributeAsync("value", "");
            await Expect(Loc("#CustomerID option").First).ToHaveTextAsync(Empty);
            await Expect(Loc("#CustomerID option[value='" + k + "']")).ToHaveTextAsync(data.CustomerNames[k]);
            await Expect(Loc("#EmployeeID option").First).ToHaveAttributeAsync("value", "");
            await Expect(Loc("#EmployeeID option").First).ToHaveTextAsync(Empty);
            await Expect(Loc("#EmployeeID option[value='" + e + "']")).ToHaveTextAsync(last + " " + first);
            // 受注日は空
            await Expect(Loc("#OrderDateFrom")).ToHaveValueAsync("");
            await Expect(Loc("#OrderDateTo")).ToHaveValueAsync("");
            // 出荷状況：固定の 4 つ（この順）、選択は「すべて」
            await Expect(Loc("#ShippedStatus option")).ToHaveTextAsync(new[] { "すべて", "未出荷", "出荷済", "納期遅延" });
            await Expect(Loc("#ShippedStatus option:checked")).ToHaveTextAsync("すべて");
        }

        [Test, Basis("EV-ORD-101"), Basis("SC-ORD-01-I06"), Basis("SC-ORD-01-I07"), Basis("SC-ORD-01-I08")]
        public async Task 初期表示_一覧とページ情報()
        {
            // テストが受注を 21 件入れる（2198 年。既存データの件数には頼らない）
            string k = data.NewCustomer('K');
            data.NewOrdersSequence(k, 21, Start2198);

            await OpenAsync();

            await Expect(Rows).ToHaveCountAsync(20);
            await Expect(Loc("#totalCount")).ToHaveTextAsync(new Regex(@"^全 \d+ 件$"));
            string totalText = await Loc("#totalCount").InnerTextAsync();
            int total = int.Parse(Regex.Match(totalText, @"\d+").Value);
            Assert.That(total, Is.GreaterThanOrEqualTo(21), "テストが入れた 21 件以上がある");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync(new Regex(@"^1 / \d+ ページ$"));
            await Expect(Loc("#orderList thead th")).ToHaveTextAsync(new[]
            {
                "受注 ID", "顧客名", "担当者名", "受注日", "要求納期", "出荷日", "配送業者", "合計金額", "状況",
            });
        }

        [Test, Basis("EV-ORD-101"), Basis("SC-ORD-01-B01"), Basis("SC-ORD-01-B05")]
        public async Task 初期表示_ボタン()
        {
            string k = data.NewCustomer('K');
            data.NewOrdersSequence(k, 21, Start2198);   // 21 件以上あるので次ページが存在する

            await OpenAsync();

            // メインボタンはコンテンツのフッタ部に 5 つ。検索だけ活性
            // キャプションは要素の種類（button・input）に頼らず、アクセシブル名で確かめる
            await Expect(Loc("#contents #btnMain1")).ToHaveAccessibleNameAsync("検索");
            await Expect(Loc("#contents #btnMain1")).ToBeEnabledAsync();
            for (int i = 2; i <= 5; i++)
            {
                await Expect(Loc("#contents #btnMain" + i)).ToBeVisibleAsync();
                await Expect(Loc("#contents #btnMain" + i)).ToBeDisabledAsync();
            }
            // 1 ページ目：先頭・前は非活性、次・末尾は活性
            await ExpectPagerAsync(false, false, true, true);
        }

        // ================================================================ 検索（EV-ORD-102）

        [Test, Basis("EV-ORD-102"), Basis("SC-ORD-01-I06"), Basis("SC-ORD-01-I07"), Basis("SC-ORD-01-I08"), Basis("OD-T4")]
        public async Task 検索_顧客で絞った一覧の表示()
        {
            string k = data.NewCustomer('K');
            string last = TestData.NewLastName('L');
            string first = TestData.NewFirstName('F');
            int e = data.NewEmployee(last, first);
            int s = data.NewShipper();
            int newer = data.NewOrder(customerId: k, employeeId: e, orderDate: new DateTime(2198, 3, 5, 15, 30, 0),
                requiredDate: new DateTime(2198, 4, 1), shippedDate: null, shipVia: s, freight: 34.50m);
            data.NewDetail(newer, 1, 100.00m, 12, 0f);
            int older = data.NewOrder(customerId: k, employeeId: null, orderDate: new DateTime(2198, 3, 1),
                requiredDate: null, shippedDate: null, shipVia: null, freight: 0m);

            await OpenAndSearchAsync(k);

            await Expect(Rows).ToHaveCountAsync(2);
            await Expect(Cell(0, 0)).ToHaveTextAsync(newer.ToString());
            await Expect(Cell(0, 1)).ToHaveTextAsync(data.CustomerNames[k]);
            await Expect(Cell(0, 2)).ToHaveTextAsync(last + " " + first);
            await Expect(Cell(0, 3)).ToHaveTextAsync("2198/03/05");
            await Expect(Cell(0, 4)).ToHaveTextAsync("2198/04/01");
            await Expect(Cell(0, 5)).ToHaveTextAsync(Empty);
            await Expect(Cell(0, 6)).ToHaveTextAsync(data.ShipperNames[s]);
            await Expect(Cell(0, 7)).ToHaveTextAsync("1,234.50");
            await Expect(Cell(0, 8)).ToHaveTextAsync("未出荷");
            await Expect(Cell(1, 0)).ToHaveTextAsync(older.ToString());
            await Expect(Cell(1, 2)).ToHaveTextAsync(Empty);
            await Expect(Cell(1, 4)).ToHaveTextAsync(Empty);
            await Expect(Cell(1, 5)).ToHaveTextAsync(Empty);
            await Expect(Cell(1, 6)).ToHaveTextAsync(Empty);
            await Expect(Cell(1, 7)).ToHaveTextAsync("0.00");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 2 件");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("1 / 1 ページ");
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            await ExpectPagerAsync(false, false, false, false);
        }

        [Test, Basis("EV-ORD-102"), Basis("SC-ORD-01-I01"), Basis("SC-ORD-01-I02"), Basis("SC-ORD-01-I03"),
            Basis("SC-ORD-01-I04"), Basis("SC-ORD-01-I05")]
        public async Task 検索_すべての条件の指定()
        {
            string k = data.NewCustomer('K');
            int e1 = data.NewEmployee(TestData.NewLastName('X'), TestData.NewFirstName('A'));
            int e2 = data.NewEmployee(TestData.NewLastName('Y'), TestData.NewFirstName('B'));
            data.NewOrder(customerId: k, employeeId: e1, orderDate: new DateTime(2198, 5, 10));
            int hit = data.NewOrder(customerId: k, employeeId: e1, orderDate: new DateTime(2198, 5, 20));
            data.NewOrder(customerId: k, employeeId: e1, orderDate: new DateTime(2198, 5, 20), shippedDate: new DateTime(2198, 5, 25));
            data.NewOrder(customerId: k, employeeId: e2, orderDate: new DateTime(2198, 5, 20));
            data.NewOrder(customerId: k, employeeId: e1, orderDate: new DateTime(2198, 6, 1));

            await OpenAsync();
            await SelectAsync("#CustomerID", k);
            await SelectAsync("#EmployeeID", e1.ToString());
            await FillAsync("#OrderDateFrom", "2198/05/15");
            await FillAsync("#OrderDateTo", "2198/05/20");
            await SelectAsync("#ShippedStatus", "未出荷");
            await SearchAsync();

            await ExpectListAsync(new[] { hit });
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 1 件");
            // 5 つの入力欄が操作後も入力のまま
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await Expect(Loc("#EmployeeID")).ToHaveValueAsync(e1.ToString());
            await Expect(Loc("#OrderDateFrom")).ToHaveValueAsync("2198/05/15");
            await Expect(Loc("#OrderDateTo")).ToHaveValueAsync("2198/05/20");
            await Expect(Loc("#ShippedStatus")).ToHaveValueAsync("未出荷");
        }

        [Test, Basis("EV-ORD-101"), Basis("SC-ORD-01-I06")]
        public async Task 検索_納期遅延の行は強調()
        {
            string k = data.NewCustomer('K');
            int late = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 2),
                requiredDate: new DateTime(2000, 1, 1), shippedDate: null);
            int done = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1),
                requiredDate: new DateTime(2199, 12, 31), shippedDate: new DateTime(2001, 1, 1));

            await OpenAndSearchAsync(k);

            await Expect(Rows).ToHaveCountAsync(2);
            await Expect(Cell(0, 0)).ToHaveTextAsync(late.ToString());
            await Expect(Cell(0, 8)).ToHaveTextAsync("納期遅延");
            await Expect(Rows.Nth(0)).ToHaveClassAsync(RowDanger);
            await Expect(Cell(1, 0)).ToHaveTextAsync(done.ToString());
            await Expect(Cell(1, 8)).ToHaveTextAsync("出荷済");
            await Expect(Rows.Nth(1)).Not.ToHaveClassAsync(RowDanger);
        }

        [Test, Basis("EV-ORD-102"), Basis("SC-ORD-01-I05")]
        public async Task 検索_出荷状況の選択が一覧に効く()
        {
            string k = data.NewCustomer('K');
            int late = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 2),
                requiredDate: new DateTime(2000, 1, 1), shippedDate: null);
            data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1),
                requiredDate: new DateTime(2199, 12, 31), shippedDate: new DateTime(2001, 1, 1));

            await OpenAsync();
            await SelectAsync("#CustomerID", k);
            await SelectAsync("#ShippedStatus", "納期遅延");
            await SearchAsync();

            await ExpectListAsync(new[] { late });
            await Expect(Cell(0, 8)).ToHaveTextAsync("納期遅延");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 1 件");
        }

        [Test, Basis("EV-ORD-101"), Basis("EV-ORD-102"), Basis("DEC-ORD-02")]
        public async Task 検索_0件の知らせ_エラーにしない()
        {
            string k = data.NewCustomer('K');   // 受注の無い顧客

            await OpenAndSearchAsync(k);

            await Expect(Rows).ToHaveCountAsync(0);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNone);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Info);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Danger);
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 0 件");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("1 / 1 ページ");
            await ExpectPagerAsync(false, false, false, false);
        }

        // ================================================================ 検索：検証（VAL-DATE・ERR-VALIDATION）

        [Test, Basis("VAL-DATE"), Basis("ERR-VALIDATION"), Basis("DEC-ORD-09"), Basis("DEC-CMN-05")]
        public async Task 検索_日付の形式違反は該当項目の直下だけ()
        {
            await OpenAsync();
            await FillAsync("#OrderDateFrom", "abc");
            await FillAsync("#OrderDateTo", "");
            await SearchAsync();

            await Expect(Loc("#OrderDateFromError")).ToHaveTextAsync(MsgFormat);
            await Expect(Loc("#OrderDateToError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            await Expect(Loc("#OrderDateFrom")).ToHaveValueAsync("abc");
        }

        [Test, Basis("DEC-CMN-05"), Basis("DEC-ORD-09")]
        public async Task 検索_終了の形式違反と両方の違反()
        {
            await OpenAsync();
            // (1) 終了だけ
            await FillAsync("#OrderDateTo", "2198-01-05");
            await SearchAsync();
            await Expect(Loc("#OrderDateToError")).ToHaveTextAsync(MsgFormat);
            await Expect(Loc("#OrderDateFromError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);

            // (2) 開始も違反
            await FillAsync("#OrderDateFrom", "2198/1/5");
            await SearchAsync();
            await Expect(Loc("#OrderDateFromError")).ToHaveTextAsync(MsgFormat);
            await Expect(Loc("#OrderDateToError")).ToHaveTextAsync(MsgFormat);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
        }

        [Test, Basis("VAL-DATE"), Basis("EV-ORD-102"), Basis("DEC-CMN-04")]
        public async Task 検索_開始が終了より後は共通メッセージ領域()
        {
            await OpenAsync();
            await FillAsync("#OrderDateFrom", "2198/03/11");
            await FillAsync("#OrderDateTo", "2198/03/10");
            await SearchAsync();

            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgRelation);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Info);
            await Expect(Loc("#OrderDateFromError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#OrderDateToError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#OrderDateFrom")).ToHaveValueAsync("2198/03/11");
            await Expect(Loc("#OrderDateTo")).ToHaveValueAsync("2198/03/10");
        }

        /// <summary>違反を入れて検索し、違反の表示が出るまで待つ（画面が更新される前に確かめないため）</summary>
        private async Task ViolateAndSearchAsync(string kind)
        {
            if (kind == "format")
            {
                await FillAsync("#OrderDateFrom", "abc");
                await SearchAsync();
                await Expect(Loc("#OrderDateFromError")).ToHaveTextAsync(MsgFormat);
            }
            else
            {
                await FillAsync("#OrderDateFrom", "2198/03/11");
                await FillAsync("#OrderDateTo", "2198/03/10");
                await SearchAsync();
                await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgRelation);
            }
        }

        [TestCase("format")]
        [TestCase("relation")]
        [Basis("DEC-ORD-03"), Basis("ERR-VALIDATION"), Basis("EV-ORD-102")]
        public async Task 検索_検証エラーでは直前の一覧を残す(string kind)
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 2, Start2198);
            List<int> expected = Range(ids, 2, 1);

            await OpenAndSearchAsync(k);
            await ExpectListAsync(expected);

            await ViolateAndSearchAsync(kind);

            await ExpectListAsync(expected);
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 2 件");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("1 / 1 ページ");
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            if (kind == "format")
            {
                await Expect(Loc("#OrderDateFrom")).ToHaveValueAsync("abc");
            }
            else
            {
                await Expect(Loc("#OrderDateFrom")).ToHaveValueAsync("2198/03/11");
                await Expect(Loc("#OrderDateTo")).ToHaveValueAsync("2198/03/10");
            }
        }

        // ================================================================ ページ切替（EV-ORD-104）

        [Test, Basis("EV-ORD-104"), Basis("SC-ORD-01-B05"), Basis("DEC-ORD-05")]
        public async Task ページ切替_先頭_前_次_末尾()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 45, Start2198);

            await OpenAndSearchAsync(k);
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("1 / 3 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 45 件");
            await ExpectListAsync(Range(ids, 45, 26));
            await ExpectPagerAsync(false, false, true, true);

            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 3 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 45 件");
            await ExpectListAsync(Range(ids, 25, 6));
            await ExpectPagerAsync(true, true, true, true);

            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("3 / 3 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 45 件");
            await ExpectListAsync(Range(ids, 5, 1));
            await ExpectPagerAsync(true, true, false, false);

            await ClickAsync("#btnPagePrev");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 3 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 45 件");

            await ClickAsync("#btnPageLast");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("3 / 3 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 45 件");

            await ClickAsync("#btnPageFirst");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("1 / 3 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 45 件");
        }

        [Test, Basis("EV-ORD-102")]
        public async Task ページ切替_検索するとページは1に戻る()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 45, Start2198);

            await OpenAndSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 3 ページ");

            await SearchAsync();

            await Expect(Loc("#pageInfo")).ToHaveTextAsync("1 / 3 ページ");
            await Expect(Cell(0, 0)).ToHaveTextAsync(ids[44].ToString());
        }

        [Test, Basis("DEC-ORD-04"), Basis("EV-ORD-104")]
        public async Task ページ切替_入力欄だけ変えても最後に検索した条件で移る()
        {
            string k = data.NewCustomer('K');
            string l = data.NewCustomer('L');
            data.NewOrdersSequence(k, 45, Start2198);
            data.NewOrdersSequence(l, 1, Start2198);

            await OpenAndSearchAsync(k);
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("1 / 3 ページ");
            await SelectAsync("#CustomerID", l);   // 検索はしない

            await ClickAsync("#btnPageNext");

            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 3 ページ");
            await Expect(Rows).ToHaveCountAsync(20);
            for (int i = 0; i < 20; i++)
                await Expect(Cell(i, 1)).ToHaveTextAsync(data.CustomerNames[k]);
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 45 件");
        }

        [TestCase("format")]
        [TestCase("relation")]
        [Basis("DEC-ORD-03"), Basis("EV-ORD-104"), Basis("EV-ORD-102")]
        public async Task ページ切替_検証エラーの後も直前の検索条件で移る(string kind)
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 45, Start2198);

            await OpenAndSearchAsync(k);
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("1 / 3 ページ");

            await ViolateAndSearchAsync(kind);

            // エラーの直後は直前の一覧のまま
            await ExpectListAsync(Range(ids, 45, 26));
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("1 / 3 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 45 件");

            await ClickAsync("#btnPageNext");

            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 3 ページ");
            await ExpectListAsync(Range(ids, 25, 6));
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 45 件");
        }

        [Test, Basis("DEC-ORD-06"), Basis("EV-ORD-104"), Basis("DEC-ORD-02")]
        public async Task ページ切替_移動先が無くなっていたら最終ページ()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 45, Start2198);

            await OpenAndSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 3 ページ");

            // テストが DB から K の受注を 30 件消す（15 件残す）。画面は古いままで、次へは活性
            data.DeleteOrders(ids.GetRange(0, 30));
            await ClickAsync("#btnPageNext");

            await Expect(Loc("#pageInfo")).ToHaveTextAsync("1 / 1 ページ");
            await Expect(Rows).ToHaveCountAsync(15);
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 15 件");
            await ExpectPagerAsync(false, false, false, false);
        }
    }
}
