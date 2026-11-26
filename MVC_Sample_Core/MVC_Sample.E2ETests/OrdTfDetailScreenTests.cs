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
    /// 受注詳細（SC-ORD-02）と、受注一覧からの遷移・一覧状態の復元の画面のテスト（MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）。
    /// 仕様：docs/spec/test3-testfirst-OrdTfDetail-mvc-core-tests.md（NorthwindDbSampleSpec@ec66d6e）の 4 章。
    /// B層で確かめること（状況の判定・金額の丸め・削除の範囲とロールバック）は繰り返さない。
    /// </summary>
    public class OrdTfDetailScreenTests
    {
        private const string MsgNotFound = "対象のデータは存在しません。";
        private const string MsgConflict = "他の利用者によって更新されています。最新の内容を読み込み直してください。";

        /// <summary>E2E の受注日は 2198 年（B層のテストは 2199 年）</summary>
        private static readonly DateTime Start2198 = new DateTime(2198, 1, 1);

        private static readonly Regex Danger = new Regex(@"\btext-danger\b");
        private static readonly Regex Info = new Regex(@"\btext-info\b");
        private static readonly Regex Empty = new Regex(@"^\s*$");

        private IBrowserContext context;
        private IPage page;
        private TestData data;

        /// <summary>確認ダイアログのメッセージの記録</summary>
        private readonly List<string> dialogs = new List<string>();

        /// <summary>確認ダイアログを承認するか（false は取り消し）</summary>
        private bool acceptDialog = true;

        [SetUp]
        public async Task SetUpAsync()
        {
            data = new TestData();
            dialogs.Clear();
            acceptDialog = true;
            context = await AppHost.Browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = AppHost.BaseUrl });
            page = await context.NewPageAsync();
            page.SetDefaultTimeout(5000);
            page.Dialog += async (_, d) =>
            {
                dialogs.Add(d.Message);
                if (acceptDialog) await d.AcceptAsync(); else await d.DismissAsync();
            };

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

        private ILocator DetailRows { get { return page.Locator("#detailList tbody tr"); } }

        private ILocator DetailCell(int row, int col) { return DetailRows.Nth(row).Locator("td").Nth(col); }

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

        private async Task OpenListAsync() { await page.GotoAsync("/OrdTf"); }

        private async Task OpenDetailAsync(int orderId) { await page.GotoAsync("/OrdTf/Detail?id=" + orderId); }

        private async Task SearchAsync() { await ClickAsync("#btnMain1"); }

        /// <summary>顧客を選んで検索する</summary>
        private async Task OpenAndSearchAsync(string customerId)
        {
            await OpenListAsync();
            await SelectAsync("#CustomerID", customerId);
            await SearchAsync();
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

        /// <summary>i の昇順で fromI から toI まで</summary>
        private static List<int> RangeAsc(List<int> ids, int fromI, int toI)
        {
            List<int> r = new List<int>();
            for (int i = fromI; i <= toI; i++) r.Add(ids[i - 1]);
            return r;
        }

        // ================================================================ 受注詳細の表示（EV-ORD-201）

        [Test, Basis("EV-ORD-201"), Basis("SC-ORD-02-I01"), Basis("SC-ORD-02-I02"), Basis("SC-ORD-02-I03"), Basis("SC-ORD-02-I04"),
            Basis("SC-ORD-02-I06"), Basis("SC-ORD-02-I07"), Basis("SC-ORD-02-I08"), Basis("SC-ORD-02-I09"), Basis("SC-ORD-02-I10"),
            Basis("SC-ORD-02-I11"), Basis("SC-ORD-02-I12"), Basis("SC-ORD-02-I13"), Basis("SC-ORD-02-I14"), Basis("SC-ORD-02-I15"),
            Basis("SC-ORD-02-I16"), Basis("SC-ORD-02-I17"), Basis("SC-ORD-02-I18"), Basis("SC-ORD-02-I19"),
            Basis("SC-ORD-02-I20"), Basis("EMP-T1")]
        public async Task 詳細_受注情報と明細と金額の表示()
        {
            string k = data.NewCustomer('K', phone: "03-1234-5678");
            string last = TestData.NewLastName('L');
            string first = TestData.NewFirstName('F');
            int e = data.NewEmployee(last, first);
            int s = data.NewShipper();
            int pa = data.NewProduct(mark: 'A', unitsInStock: 5);
            int pm = data.NewProduct(mark: 'M', unitsInStock: 17);
            int px = data.NewProduct(mark: 'X', unitsInStock: 0);
            int o = data.NewOrder(customerId: k, employeeId: e, orderDate: new DateTime(2198, 3, 5, 15, 30, 0),
                requiredDate: new DateTime(2198, 4, 1), shippedDate: null, shipVia: s, freight: 34.50m,
                shipName: "ShipTo Co", shipAddress: "4-5-6 Ginza", shipCity: "Chuo", shipRegion: "Tokyo",
                shipPostalCode: "104-0061", shipCountry: "Japan");
            data.NewDetail(o, pm, 50.00m, 2, 0.25f);   // M・X・A の順に登録する
            data.NewDetail(o, px, 20.00m, 3, 0.05f);
            data.NewDetail(o, pa, 100.00m, 12, 0.15f);

            await OpenDetailAsync(o);

            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(Loc("#status")).ToHaveTextAsync("未出荷");
            await Expect(Loc("#customerName")).ToHaveTextAsync(data.CustomerNames[k]);
            await Expect(Loc("#customerPhone")).ToHaveTextAsync("03-1234-5678");
            await Expect(Loc("#employeeName")).ToHaveTextAsync(last + " " + first);
            await Expect(Loc("#orderDate")).ToHaveTextAsync("2198/03/05");
            await Expect(Loc("#requiredDate")).ToHaveTextAsync("2198/04/01");
            await Expect(Loc("#shippedDate")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipperName")).ToHaveTextAsync(data.ShipperNames[s]);
            await Expect(Loc("#shipName")).ToHaveTextAsync("ShipTo Co");
            await Expect(Loc("#shipAddress")).ToHaveTextAsync("4-5-6 Ginza");
            await Expect(Loc("#shipCity")).ToHaveTextAsync("Chuo");
            await Expect(Loc("#shipRegion")).ToHaveTextAsync("Tokyo");
            await Expect(Loc("#shipPostalCode")).ToHaveTextAsync("104-0061");
            await Expect(Loc("#shipCountry")).ToHaveTextAsync("Japan");

            await Expect(Loc("#detailList thead th")).ToHaveTextAsync(new[]
            {
                "商品名", "単価", "割引率", "価格", "数量", "在庫", "明細金額",
            });
            await Expect(DetailRows).ToHaveCountAsync(3);
            await Expect(DetailRows.Nth(0).Locator("td")).ToHaveTextAsync(new[]
                { data.ProductNames[pa], "100.00", "15%", "85.00", "12", "5", "1,020.00" });
            await Expect(DetailRows.Nth(1).Locator("td")).ToHaveTextAsync(new[]
                { data.ProductNames[pm], "50.00", "25%", "37.50", "2", "17", "75.00" });
            await Expect(DetailRows.Nth(2).Locator("td")).ToHaveTextAsync(new[]
                { data.ProductNames[px], "20.00", "5%", "19.00", "3", "0", "57.00" });
            await Expect(Loc("#subtotal")).ToHaveTextAsync("1,152.00");
            await Expect(Loc("#freight")).ToHaveTextAsync("34.50");
            await Expect(Loc("#total")).ToHaveTextAsync("1,186.50");
            await Expect(Loc("#total")).ToHaveClassAsync(new Regex(@"\bfw-bold\b"));

            // 照会モード：入力欄を置かない
            await Expect(Loc("#contents input:not([type=hidden]):not([type=submit]):not([type=button]), #contents select, #contents textarea"))
                .ToHaveCountAsync(0);
            // 商品マスタに無い明細が無ければ警告を出さない（DEC-ORD-73）
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
        }

        [Test, Basis("SC-ORD-02-I05"), Basis("DEC-ORD-18")]
        public async Task 詳細_顧客住所は空の要素を詰めて連結する()
        {
            string k = data.NewCustomer('K', address: "A1", city: "C1", region: null, postalCode: "", country: "Jp");
            int o = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 5));

            await OpenDetailAsync(o);

            // 区切り文字は期待にしない（DEC-ORD-18）。要素が順に並び、空の要素の分の区切りが残らない
            await Expect(Loc("#customerAddress")).ToHaveTextAsync(new Regex(@"^A1([\s\S]*)C1\1Jp$"));
        }

        [TestCase(null, true)]
        [TestCase("2198/03/10", false)]
        [Basis("EV-ORD-201"), Basis("ORD-T4"), Basis("SC-ORD-02-B01"), Basis("SC-ORD-02-B02"), Basis("SC-ORD-02-B06"),
            Basis("cmn-framework.md の画面共通"), Basis("決定 4"), Basis("DEC-ORD-78")]
        public async Task 詳細_メインボタンと編集の活性(string shipped, bool editEnabled)
        {
            DateTime? shippedDate = shipped == null ? (DateTime?)null : DateTime.ParseExact(shipped, "yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture);
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 4, 1), shippedDate: shippedDate);

            await OpenDetailAsync(o);

            // メインボタンはコンテンツのフッタ部に 5 つ。削除だけ活性（出荷済は非活性。DEC-ORD-78。編集と同じ条件）
            await Expect(Loc("#contents #btnMain1")).ToHaveAccessibleNameAsync("削除");
            if (editEnabled) await Expect(Loc("#contents #btnMain1")).ToBeEnabledAsync();
            else await Expect(Loc("#contents #btnMain1")).ToBeDisabledAsync();
            for (int i = 2; i <= 5; i++)
            {
                await Expect(Loc("#contents #btnMain" + i)).ToBeVisibleAsync();
                await Expect(Loc("#contents #btnMain" + i)).ToBeDisabledAsync();
            }
            // 編集（画面上部）・一覧へ戻る（画面下部）はフッタの外
            await Expect(Loc("#contents #btnEdit")).ToHaveAccessibleNameAsync("編集");
            if (editEnabled) await Expect(Loc("#btnEdit")).ToBeEnabledAsync();
            else await Expect(Loc("#btnEdit")).ToBeDisabledAsync();
            await Expect(Loc("#contents #btnBack")).ToHaveAccessibleNameAsync("一覧へ戻る");
            await Expect(Loc("#btnBack")).ToBeEnabledAsync();
            await Expect(Loc("#footer-buttons #btnEdit")).ToHaveCountAsync(0);
            await Expect(Loc("#footer-buttons #btnBack")).ToHaveCountAsync(0);
        }

        [Test, Basis("EV-ORD-201"), Basis("SC-ORD-02-I17"), Basis("共通仕様 4 節（空値の表示）")]
        public async Task 詳細_廃番商品は商品名に付記し_在庫がNULLなら空()
        {
            int p = data.NewProduct(mark: 'P', unitsInStock: 5);
            int q = data.NewProduct(mark: 'Q', discontinued: true, unitsInStock: null);
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1));
            data.NewDetail(o, p, 10.00m, 1, 0f);
            data.NewDetail(o, q, 10.00m, 1, 0f);

            await OpenDetailAsync(o);

            await Expect(DetailRows).ToHaveCountAsync(2);
            await Expect(DetailCell(0, 0)).ToHaveTextAsync(data.ProductNames[p]);
            await Expect(DetailCell(0, 0)).Not.ToContainTextAsync("（廃番）");
            await Expect(DetailCell(0, 5)).ToHaveTextAsync("5");
            await Expect(DetailCell(1, 0)).ToContainTextAsync(data.ProductNames[q]);
            await Expect(DetailCell(1, 0)).ToContainTextAsync("（廃番）");
            await Expect(DetailCell(1, 5)).ToHaveTextAsync(Empty);
        }

        [Test, Basis("DEC-ORD-25"), Basis("DEC-ORD-26"), Basis("SC-ORD-02-I17"), Basis("共通仕様 3 節（画面表示は小数 2 桁）")]
        public async Task 詳細_割引率と価格の表示()
        {
            int pa = data.NewProduct(mark: 'A');
            int pm = data.NewProduct(mark: 'M');
            int px = data.NewProduct(mark: 'X');
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1));
            data.NewDetail(o, pa, 0.50m, 3, 0.15f);
            data.NewDetail(o, pm, 100.00m, 1, 0.125f);
            data.NewDetail(o, px, 20.00m, 1, 0f);

            await OpenDetailAsync(o);

            await Expect(DetailRows).ToHaveCountAsync(3);
            // 価格 0.425 は小数第 3 位で四捨五入して 0.43（切り捨て・偶数丸めなら 0.42）
            await Expect(DetailCell(0, 2)).ToHaveTextAsync("15%");
            await Expect(DetailCell(0, 3)).ToHaveTextAsync("0.43");
            await Expect(DetailCell(0, 6)).ToHaveTextAsync("1.28");
            await Expect(DetailCell(1, 2)).ToHaveTextAsync("12.5%");
            await Expect(DetailCell(1, 3)).ToHaveTextAsync("87.50");
            await Expect(DetailCell(2, 2)).ToHaveTextAsync("0%");
            await Expect(DetailCell(2, 3)).ToHaveTextAsync("20.00");
        }

        [Test, Basis("EV-ORD-201"), Basis("共通仕様 4 節（空値の表示）"), Basis("DEC-CMN-02"), Basis("DEC-CMN-03")]
        public async Task 詳細_NULLは空欄で金額は0()
        {
            int o = data.NewOrder(freight: null);   // CustomerID・EmployeeID・日付・ShipVia・Ship*・送料がすべて NULL

            await OpenDetailAsync(o);

            await Expect(Loc("#status")).ToHaveTextAsync("未出荷");
            foreach (string id in new[] { "#customerName", "#customerPhone", "#customerAddress", "#employeeName", "#orderDate",
                "#requiredDate", "#shippedDate", "#shipperName", "#shipName", "#shipAddress", "#shipCity", "#shipRegion",
                "#shipPostalCode", "#shipCountry", "#freight" })
            {
                await Expect(Loc(id)).ToHaveTextAsync(Empty);
            }
            await Expect(DetailRows).ToHaveCountAsync(0);
            await Expect(Loc("#subtotal")).ToHaveTextAsync("0.00");
            await Expect(Loc("#total")).ToHaveTextAsync("0.00");
        }

        [Test, Basis("EV-ORD-201"), Basis("ERR-NOTFOUND"), Basis("画面遷移 F-2"), Basis("画面遷移 F-4"), Basis("DEC-ORD-28")]
        public async Task 詳細_存在しない受注は一覧へ戻りエラーを表示する()
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
            await page.ReloadAsync();

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await Expect(Loc("#orderId")).ToHaveCountAsync(0);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNotFound);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Info);
            // 保持された一覧状態で戻る（DEC-ORD-28）
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await ExpectPageAsync("2 / 2 ページ", "全 24 件");
            await ExpectListAsync(Range(ids, 4, 1));
        }

        // ================================================================ 一覧からの遷移・一覧状態の復元（EV-ORD-106・206）

        [Test, Basis("EV-ORD-106"), Basis("SC-ORD-01-I06")]
        public async Task 一覧_受注IDを選ぶと詳細へ遷移する()
        {
            string k = data.NewCustomer('K');
            int o = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1));

            await OpenAndSearchAsync(k);
            await ClickOrderIdAsync(0);

            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            Uri u = new Uri(page.Url);
            Assert.That(u.AbsolutePath, Is.EqualTo("/OrdTf/Detail"));
            Assert.That(u.Query, Does.Contain("id=" + o));
        }

        [Test, Basis("EV-ORD-106"), Basis("EV-ORD-206"), Basis("EV-ORD-101"), Basis("画面遷移 F-2"), Basis("共通仕様 4 節（検索条件の保持）")]
        public async Task 一覧_詳細から戻ると検索条件_ページ_並べ替えが復元される()
        {
            string k = data.NewCustomer('K');
            int e = data.NewEmployee(TestData.NewLastName('L'), TestData.NewFirstName('F'));
            int e2 = data.NewEmployee(TestData.NewLastName('M'), TestData.NewFirstName('G'));
            List<int> ids = data.NewOrdersSequence(k, 45, Start2198, employeeId: e);
            // 範囲（i = 5〜44）の中の、担当者・出荷状況の条件で除かれる 2 件
            data.NewOrder(customerId: k, employeeId: e2, orderDate: new DateTime(2198, 1, 20));
            data.NewOrder(customerId: k, employeeId: e, orderDate: new DateTime(2198, 2, 10), shippedDate: new DateTime(2198, 2, 11));

            await OpenListAsync();
            await SelectAsync("#CustomerID", k);
            await SelectAsync("#EmployeeID", e.ToString());
            await FillAsync("#OrderDateFrom", "2198-01-06");
            await FillAsync("#OrderDateTo", "2198-02-14");
            await SelectAsync("#ShippedStatus", "未出荷");
            await SearchAsync();
            await ExpectPageAsync("1 / 2 ページ", "全 40 件");
            await ClickAsync("#sortOrderDate");
            await ExpectListAsync(RangeAsc(ids, 5, 24));
            await ClickAsync("#btnPageNext");
            await ExpectPageAsync("2 / 2 ページ", "全 40 件");
            await ExpectListAsync(RangeAsc(ids, 25, 44));
            await ClickOrderIdAsync(0);
            await Expect(Loc("#orderId")).ToHaveTextAsync(ids[24].ToString());
            await ClickAsync("#btnBack");

            // 戻った直後：条件・ページ・並べ替えが復元され、復元した条件で再検索される（x・y を含まない）
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await Expect(Loc("#EmployeeID")).ToHaveValueAsync(e.ToString());
            await Expect(Loc("#OrderDateFrom")).ToHaveValueAsync("2198-01-06");
            await Expect(Loc("#OrderDateTo")).ToHaveValueAsync("2198-02-14");
            await Expect(Loc("#ShippedStatus option:checked")).ToHaveTextAsync("未出荷");
            await ExpectPageAsync("2 / 2 ページ", "全 40 件");
            await ExpectListAsync(RangeAsc(ids, 25, 44));
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);

            await ClickAsync("#btnPageFirst");
            await ExpectPageAsync("1 / 2 ページ", "全 40 件");
            await ExpectListAsync(RangeAsc(ids, 5, 24));   // 並べ替え（受注日の昇順）も保持
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
        }

        [Test, Basis("EV-ORD-206"), Basis("EV-ORD-101")]
        public async Task 一覧_詳細から戻ると再検索される()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 25, Start2198);

            await OpenAndSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await ExpectPageAsync("2 / 2 ページ", "全 25 件");
            await ExpectListAsync(Range(ids, 5, 1));
            await ClickOrderIdAsync(0);
            await Expect(Loc("#orderId")).ToHaveTextAsync(ids[4].ToString());
            TestData.DeleteOrder(ids[0]);   // i = 1 を、他の利用者が消す
            await ClickAsync("#btnBack");

            await ExpectPageAsync("2 / 2 ページ", "全 24 件");
            await ExpectListAsync(Range(ids, 5, 2));
        }

        [Test, Basis("EV-ORD-106"), Basis("EV-ORD-102"), Basis("DEC-ORD-04"), Basis("DEC-ORD-19")]
        public async Task 一覧_入力欄だけ変えて詳細へ行っても最後に検索した条件で復元される()
        {
            string k = data.NewCustomer('K');
            string l = data.NewCustomer('L');
            List<int> ids = data.NewOrdersSequence(k, 3, Start2198);
            data.NewOrder(customerId: l, orderDate: new DateTime(2198, 3, 1));

            await OpenAndSearchAsync(k);
            await ExpectListAsync(Range(ids, 3, 1));
            await SelectAsync("#CustomerID", l);   // 検索しない
            await ClickOrderIdAsync(0);
            await Expect(Loc("#orderId")).ToHaveTextAsync(ids[2].ToString());
            await ClickAsync("#btnBack");

            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await ExpectListAsync(Range(ids, 3, 1));
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 3 件");
        }

        [Test, Basis("EV-ORD-101"), Basis("EV-ORD-206")]
        public async Task 一覧_詳細を直接開いて戻ると既定の一覧()
        {
            string k = data.NewCustomer('K');
            int o = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1));

            await OpenDetailAsync(o);   // このブラウザでは一覧を開いていない
            await ClickAsync("#btnBack");

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await Expect(Loc("#CustomerID")).ToHaveValueAsync("");
            await Expect(Loc("#EmployeeID")).ToHaveValueAsync("");
            await Expect(Loc("#OrderDateFrom")).ToHaveValueAsync("");
            await Expect(Loc("#OrderDateTo")).ToHaveValueAsync("");
            await Expect(Loc("#ShippedStatus option:checked")).ToHaveTextAsync("すべて");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync(new Regex(@"^1 / \d+ ページ$"));
        }

        [Test, Basis("EV-ORD-101"), Basis("DEC-ORD-20")]
        public async Task 一覧_復帰以外で直接開くと保持した状態は使わない()
        {
            string k = data.NewCustomer('K');
            data.NewOrdersSequence(k, 3, Start2198);

            await OpenAndSearchAsync(k);
            await ClickOrderIdAsync(0);
            await Expect(Loc("#orderId")).ToBeVisibleAsync();
            await OpenListAsync();   // メニューからと同じ。#btnBack を使わない

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await Expect(Loc("#CustomerID")).ToHaveValueAsync("");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync(new Regex(@"^1 / \d+ ページ$"));
        }

        [Test, Basis("EV-ORD-107"), Basis("SC-ORD-01-B03"), Basis("決定 4")]
        public async Task 新規登録_一覧状態を保持して登録のURLへ遷移する()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 45, Start2198);

            await OpenAndSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await ExpectPageAsync("2 / 3 ページ", "全 45 件");
            await Expect(Loc("#contents #btnNew")).ToHaveAccessibleNameAsync("新規登録");
            await Expect(Loc("#btnNew")).ToBeEnabledAsync();
            await Expect(Loc("#footer-buttons #btnNew")).ToHaveCountAsync(0);
            await page.ClickAsync("#btnNew");

            await Expect(page).ToHaveURLAsync(new Regex(@"/OrdTf/Edit\?"));
            Uri u = new Uri(page.Url);
            Assert.That(u.AbsolutePath, Is.EqualTo("/OrdTf/Edit"));
            Assert.That(u.Query, Does.Contain("mode=new"));
            Assert.That(u.Query, Does.Not.Contain("id="));

            await page.GotoAsync("/OrdTf/Back");
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await ExpectPageAsync("2 / 3 ページ", "全 45 件");
            await ExpectListAsync(Range(ids, 25, 6));
        }

        // ================================================================ 受注の削除（EV-ORD-203）

        [Test, Basis("EV-ORD-203"), Basis("共通仕様 4 節（削除の確認）")]
        public async Task 削除_確認を取り消すと何も起きない()
        {
            int p = data.NewProduct();
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1));
            data.NewDetail(o, p, 10.00m, 1, 0f);
            acceptDialog = false;

            await OpenDetailAsync(o);
            await ClickAsync("#btnMain1");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            Assert.That(dialogs, Is.EqualTo(new List<string> { "受注 ID " + o + " を削除します。よろしいですか？" }));
            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            Assert.That(TestData.CountOrders(o), Is.EqualTo(1));
            Assert.That(TestData.CountDetails(o), Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-203"), Basis("EV-ORD-206"), Basis("共通仕様 4 節（削除の確認・メッセージの区別）"), Basis("DEC-ORD-22")]
        public async Task 削除_承認すると削除して保持された一覧状態で一覧へ戻る()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 45, Start2198);
            int p = data.NewProduct();
            data.NewDetail(ids[24], p, 10.00m, 1, 0f);   // 25 番目の受注に明細 1 行
            int target = ids[24];

            await OpenAndSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await ExpectPageAsync("2 / 3 ページ", "全 45 件");
            await ExpectListAsync(Range(ids, 25, 6));
            await ClickOrderIdAsync(0);
            await Expect(Loc("#orderId")).ToHaveTextAsync(target.ToString());
            await ClickAsync("#btnMain1");

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            Assert.That(dialogs, Is.EqualTo(new List<string> { "受注 ID " + target + " を削除します。よろしいですか？" }));
            await Expect(Loc("#messageArea")).ToHaveTextAsync("受注 ID " + target + " を削除しました。");
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Info);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Danger);
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await ExpectPageAsync("2 / 3 ページ", "全 44 件");
            await ExpectListAsync(Range(ids, 24, 5));
            Assert.That(TestData.CountOrders(target), Is.EqualTo(0));
            Assert.That(TestData.CountDetails(target), Is.EqualTo(0));
        }

        [Test, Basis("EV-ORD-203"), Basis("DEC-ORD-06"), Basis("DEC-ORD-02"), Basis("DEC-ORD-23")]
        public async Task 削除_最後のページの最後の1件を消すと最終ページを表示する()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 41, Start2198);

            await OpenAndSearchAsync(k);
            await ClickAsync("#btnPageLast");
            await ExpectPageAsync("3 / 3 ページ", "全 41 件");
            await ExpectListAsync(new List<int> { ids[0] });
            await ClickOrderIdAsync(0);
            await Expect(Loc("#orderId")).ToHaveTextAsync(ids[0].ToString());
            await ClickAsync("#btnMain1");

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await ExpectPageAsync("2 / 2 ページ", "全 40 件");
            await ExpectListAsync(Range(ids, 21, 2));
            await Expect(Loc("#messageArea")).ToHaveTextAsync("受注 ID " + ids[0] + " を削除しました。");
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Info);
        }

        [Test, Basis("EV-ORD-203"), Basis("ERR-CONFLICT"), Basis("共通仕様 9.1 節"), Basis("EV-ORD-201")]
        public async Task 削除_他の利用者が更新していたらERR_CONFLICTで最新を再表示する()
        {
            int p = data.NewProduct();
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1), freight: 5.00m);
            data.NewDetail(o, p, 10.00m, 1, 0f);

            await OpenDetailAsync(o);
            await Expect(Loc("#freight")).ToHaveTextAsync("5.00");
            TestData.BumpOrder(o, 99.00m);
            await ClickAsync("#btnMain1");

            // 1 回目：競合。詳細のまま、最新（送料 99.00）を再表示する
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgConflict);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Info);
            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(Loc("#freight")).ToHaveTextAsync("99.00");
            Assert.That(TestData.CountOrders(o), Is.EqualTo(1));
            Assert.That(TestData.CountDetails(o), Is.EqualTo(1));

            // 2 回目：保持する RowVersion も更新されているので削除できる
            await ClickAsync("#btnMain1");
            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await Expect(Loc("#messageArea")).ToHaveTextAsync("受注 ID " + o + " を削除しました。");
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Info);
            Assert.That(TestData.CountOrders(o), Is.EqualTo(0));
        }

        [Test, Basis("EV-ORD-203"), Basis("ERR-NOTFOUND"), Basis("画面遷移 F-2"), Basis("画面遷移 F-4"), Basis("DEC-ORD-28")]
        public async Task 削除_既に消されていたらERR_NOTFOUNDで一覧へ戻る()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 25, Start2198);

            await OpenAndSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await ExpectPageAsync("2 / 2 ページ", "全 25 件");
            await ClickOrderIdAsync(0);
            await Expect(Loc("#orderId")).ToHaveTextAsync(ids[4].ToString());
            TestData.DeleteOrder(ids[4]);
            await ClickAsync("#btnMain1");

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await Expect(Loc("#orderId")).ToHaveCountAsync(0);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNotFound);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Info);
            // 保持された一覧状態で戻る（DEC-ORD-28）
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await ExpectPageAsync("2 / 2 ページ", "全 24 件");
            await ExpectListAsync(Range(ids, 4, 1));
        }

        // ================================================================ 受注管理の直し（OrdTfRevise）
        // 仕様：docs/spec/test3-testfirst-OrdTfRevise-mvc-core-tests.md（NorthwindDbSampleSpec@9be21c0）の 3.2。

        private static readonly Regex Warning = new Regex(@"\btext-warning\b");

        [Test, Basis("EV-ORD-203"), Basis("ERR-BIZ"), Basis("DEC-ORD-78"), Basis("DEC-ORD-81")]
        public async Task 削除_表示の後に出荷されていたらERR_BIZで詳細に留まる()
        {
            int p = data.NewProduct();
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 4, 1), freight: 5.00m);
            data.NewDetail(o, p, 10.00m, 1, 0f);

            await OpenDetailAsync(o);
            await Expect(Loc("#btnMain1")).ToBeEnabledAsync();
            TestData.SetShippedDate(o, new DateTime(2198, 3, 10));   // RowVersion は変えない
            await ClickAsync("#btnMain1");   // 確認ダイアログは承認する

            // 詳細のまま。出荷済の受注は削除できない
            await Expect(Loc("#messageArea")).ToHaveTextAsync("出荷済の受注は削除できません。");
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Info);
            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(Loc("#detailList")).ToBeVisibleAsync();
            Assert.That(TestData.CountOrders(o), Is.EqualTo(1));
            Assert.That(TestData.CountDetails(o), Is.EqualTo(1));
            // 詳細が最新に読み直される（DEC-ORD-81）：出荷日あり・削除と編集が非活性
            await Expect(Loc("#shippedDate")).ToHaveTextAsync("2198/03/10");
            await Expect(Loc("#status")).ToHaveTextAsync("出荷済");
            await Expect(Loc("#btnMain1")).ToBeDisabledAsync();
            await Expect(Loc("#btnEdit")).ToBeDisabledAsync();
        }

        [Test, Basis("EV-ORD-201"), Basis("DEC-ORD-24"), Basis("DEC-ORD-73"), Basis("DEC-ORD-75")]
        public async Task 詳細_商品マスタに無い明細は付記して小計に含め警告を出す()
        {
            int za = data.NewProduct(mark: 'Z', unitsInStock: 5);
            int zb = data.NewProduct(mark: 'Z', unitsInStock: 5);
            int pa = data.NewProduct(mark: 'A', unitsInStock: 7);
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1), freight: 5.00m);
            data.NewDetail(o, pa, 10.00m, 2, 0f);   // A・Zb・Za の順に登録する
            data.NewDetail(o, zb, 20.00m, 3, 0f);
            data.NewDetail(o, za, 100.00m, 1, 0f);
            data.DeleteProduct(za);
            data.DeleteProduct(zb);

            await OpenDetailAsync(o);

            await Expect(DetailRows).ToHaveCountAsync(3);
            await Expect(DetailCell(0, 0)).ToHaveTextAsync(data.ProductNames[pa]);
            await Expect(DetailCell(0, 5)).ToHaveTextAsync("7");
            await Expect(DetailCell(1, 0)).ToHaveTextAsync("（商品マスタに無い：ID " + za + "）");
            await Expect(DetailCell(1, 0)).Not.ToContainTextAsync("（廃番）");
            await Expect(DetailCell(1, 5)).ToHaveTextAsync(Empty);
            await Expect(DetailCell(2, 0)).ToHaveTextAsync("（商品マスタに無い：ID " + zb + "）");
            await Expect(DetailCell(2, 5)).ToHaveTextAsync(Empty);
            await Expect(Loc("#subtotal")).ToHaveTextAsync("180.00");
            await Expect(Loc("#total")).ToHaveTextAsync("185.00");
            await Expect(Loc("#messageArea")).ToHaveTextAsync("商品マスタに無い商品の明細があります。");
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Warning);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Danger);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Info);
        }
    }
}
