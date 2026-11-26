using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace MVC_Sample.E2ETests
{
    /// <summary>
    /// 出荷ステータス更新ダイアログ（SC-ORD-04。一覧の B04・詳細の B03 から開く）の画面のテスト（MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）。
    /// 仕様：docs/spec/test3-testfirst-OrdTfShip-mvc-core-tests.md（NorthwindDbSampleSpec@2962688）の 4 章。
    /// B層で確かめること（検証の全項目・在庫の加減算の値の細部・複数の不足の列挙・版数）は繰り返さない。
    /// </summary>
    public class OrdTfShipScreenTests
    {
        private const string MsgNotFound = "対象のデータは存在しません。";
        private const string MsgConflict = "他の利用者によって更新されています。最新の内容を読み込み直してください。";
        private const string MsgShort = "在庫が不足しています：";
        private const string MsgCancelConfirm = "出荷を取り消し、在庫を戻します。よろしいですか？";
        private const string MsgBeforeOrder = "出荷日は受注日以降を指定してください。";

        /// <summary>E2E の受注日は 2198 年（B層のテストは 2199 年）</summary>
        private static readonly DateTime Start2198 = new DateTime(2198, 1, 1);

        private static readonly Regex Danger = new Regex(@"\btext-danger\b");
        private static readonly Regex Info = new Regex(@"\btext-info\b");
        private static readonly Regex RowDanger = new Regex(@"\btable-danger\b");
        private static readonly Regex Empty = new Regex(@"^\s*$");
        private static readonly Regex NonEmpty = new Regex(@"\S");

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

        private ILocator ListRows { get { return page.Locator("#orderList tbody tr"); } }

        private ILocator ListCell(int row, int col) { return ListRows.Nth(row).Locator("td").Nth(col); }

        /// <summary>受注 ID のリンクがある一覧の行</summary>
        private ILocator RowOf(int orderId)
        {
            return ListRows.Filter(new LocatorFilterOptions { Has = page.Locator("td:first-child a:text-is('" + orderId + "')") });
        }

        private ILocator StockRows { get { return page.Locator("#shipStockList tbody tr"); } }

        private ILocator StockRowOf(string productName)
        {
            return StockRows.Filter(new LocatorFilterOptions { HasText = productName });
        }

        private static string Today { get { return DateTime.Today.ToString("yyyy/MM/dd"); } }
        /// <summary>日付の入力欄（type="date"）の値の形（yyyy-MM-dd）</summary>
        private static string TodayIso { get { return DateTime.Today.ToString("yyyy-MM-dd"); } }

        private async Task SelectAsync(string selector, string value)
        {
            await Expect(Loc(selector)).ToBeVisibleAsync();
            await page.SelectOptionAsync(selector, value);
        }

        /// <summary>入力して確定する（change を発火させる）</summary>
        private async Task SetAsync(string selector, string value)
        {
            await Expect(Loc(selector)).ToBeVisibleAsync();
            await page.FillAsync(selector, value);
            await page.Locator(selector).BlurAsync();
        }

        private async Task ClickAsync(string selector)
        {
            await Expect(Loc(selector)).ToBeEnabledAsync();
            await page.ClickAsync(selector);
        }

        /// <summary>数値の入力欄の値（0 と 0.00 のどちらでもよい）に当たる正規表現</summary>
        private static Regex NumRe(decimal v)
        {
            string s = v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            return s.Contains(".") ? new Regex("^" + Regex.Escape(s) + "0*$") : new Regex("^" + Regex.Escape(s) + @"(\.0+)?$");
        }

        /// <summary>顧客を選んで一覧を検索する</summary>
        private async Task ListSearchAsync(string customerId)
        {
            await page.GotoAsync("/OrdTf");
            await SelectAsync("#CustomerID", customerId);
            await ClickAsync("#btnMain1");
        }

        private async Task OpenDetailAsync(int orderId) { await page.GotoAsync("/OrdTf/Detail?id=" + orderId); }

        /// <summary>一覧（顧客 K を検索）の o の行の出荷更新を押し、ダイアログが見えるのを待つ</summary>
        private async Task OpenFromListAsync(string customerId, int orderId)
        {
            await ListSearchAsync(customerId);
            await OpenRowDialogAsync(orderId);
        }

        private async Task OpenRowDialogAsync(int orderId)
        {
            ILocator btn = RowOf(orderId).Locator("button.btn-ship");
            await Expect(btn).ToBeEnabledAsync();
            await btn.ClickAsync();
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
        }

        /// <summary>詳細の出荷更新を押し、ダイアログが見えるのを待つ</summary>
        private async Task OpenFromDetailAsync(int orderId)
        {
            await OpenDetailAsync(orderId);
            await ClickAsync("#btnShip");
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
        }

        /// <summary>出荷日・配送業者（null は変えない）・送料（null は変えない）を入れる</summary>
        private async Task FillShipAsync(string date, int? shipVia, string freight)
        {
            if (date != null) await SetAsync("#shipShippedDate", date);
            if (shipVia.HasValue) await SelectAsync("#shipShipVia", shipVia.Value.ToString());
            if (freight != null) await SetAsync("#shipFreight", freight);
        }

        /// <summary>E2E の共通の受注 Es（受注日 2198/03/01・要求納期 2198/12/31・配送業者 S0・送料 12.50・明細 P1 数量 3・P2 数量 2）</summary>
        private sealed class Es
        {
            public string K;
            public int S0;
            public int S1;
            public int P1;
            public int P2;
            public int O;
        }

        private Es NewEs(int p1Stock = 10, int p2Stock = 5, DateTime? shippedDate = null, decimal freight = 12.50m)
        {
            Es e = new Es();
            e.K = data.NewCustomer('K');
            e.S0 = data.NewShipper(mark: 'A');
            e.S1 = data.NewShipper(mark: 'B');
            e.P1 = data.NewProduct(mark: 'A', unitsInStock: p1Stock);
            e.P2 = data.NewProduct(mark: 'B', unitsInStock: p2Stock);
            e.O = NewEsOrder(e, shippedDate, freight);
            return e;
        }

        private int NewEsOrder(Es e, DateTime? shippedDate, decimal freight)
        {
            int o = data.NewOrder(customerId: e.K, orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 12, 31),
                shippedDate: shippedDate, shipVia: e.S0, freight: freight,
                shipName: "N", shipAddress: "A", shipCity: "C", shipRegion: "R", shipPostalCode: "P", shipCountry: "Y");
            data.NewDetail(o, e.P1, 10.00m, 3, 0f);
            data.NewDetail(o, e.P2, 10.00m, 2, 0f);
            return o;
        }

        private static readonly DateTime Shipped0405 = new DateTime(2198, 4, 5);

        private async Task ExpectDialogClosedAsync()
        {
            await Expect(Loc("#shipDialog")).ToBeHiddenAsync();
        }

        private async Task ExpectListMessageAsync(int orderId, string tail, Regex cls)
        {
            await Expect(Loc("#messageArea")).ToHaveTextAsync("受注 ID " + orderId + " " + tail);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(cls);
        }

        // ================================================================ 一覧の B04・詳細の B03（EV-ORD-108・205）

        [Test, Basis("SC-ORD-01-I06"), Basis("SC-ORD-01-B04"), Basis("EV-ORD-108")]
        public async Task 一覧_操作列と出荷更新ボタン()
        {
            string k = data.NewCustomer('K');
            int a = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 12, 31));
            int b = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 2), requiredDate: new DateTime(2001, 1, 1));

            await ListSearchAsync(k);

            await Expect(Loc("#orderList thead th")).ToHaveCountAsync(10);
            await Expect(Loc("#orderList thead th").Nth(9)).ToHaveTextAsync("操作");
            await Expect(Loc("#orderList thead th").Nth(9).Locator("button, a, input")).ToHaveCountAsync(0);
            foreach (int o in new[] { a, b })
            {
                ILocator btn = RowOf(o).Locator("td").Nth(9).Locator("button.btn-ship");
                await Expect(btn).ToHaveCountAsync(1);
                await Expect(btn).ToHaveTextAsync("出荷更新");
                await Expect(btn).ToBeEnabledAsync();
            }
        }

        [Test, Basis("SC-ORD-01-B04"), Basis("ORD-T2"), Basis("DEC-ORD-50")]
        public async Task 一覧_出荷済の行は出荷変更()
        {
            string k = data.NewCustomer('K');
            int c = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 12, 31), shippedDate: new DateTime(2198, 3, 5));
            int d = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 2), requiredDate: new DateTime(2198, 3, 1), shippedDate: new DateTime(2198, 3, 5));

            await ListSearchAsync(k);

            foreach (int o in new[] { c, d })
            {
                ILocator btn = RowOf(o).Locator("button.btn-ship");
                await Expect(btn).ToHaveCountAsync(1);
                await Expect(btn).ToHaveTextAsync("出荷変更");
                await Expect(btn).ToBeEnabledAsync();
            }
        }

        [Test, Basis("EV-ORD-108")]
        public async Task 一覧_出荷更新は行ごとに対象が違う()
        {
            string k = data.NewCustomer('K');
            int a = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 12, 31));
            int b = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 2), requiredDate: new DateTime(2198, 12, 31));

            await ListSearchAsync(k);
            await OpenRowDialogAsync(b);

            await Expect(Loc("#shipOrderId")).ToHaveTextAsync(b.ToString());
            await Expect(Loc("#shipOrderId")).Not.ToHaveTextAsync(a.ToString());
        }

        [Test, Basis("EV-ORD-108"), Basis("F-5"), Basis("決定 1")]
        public async Task 一覧_出荷更新で元画面の上に重ねて表示される()
        {
            Es e = NewEs();

            await ListSearchAsync(e.K);
            string pathBefore = new Uri(page.Url).AbsolutePath;   // 検索（POST）の後のパス。開いても変わらない
            await OpenRowDialogAsync(e.O);

            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            Assert.That(new Uri(page.Url).AbsolutePath, Is.EqualTo(pathBefore), "開く前と同じ URL");
            await Expect(Loc("#orderList")).ToHaveCountAsync(1);
            await Expect(RowOf(e.O)).ToBeVisibleAsync();
            await Expect(Loc("#btnMain1")).ToHaveAccessibleNameAsync("検索");
            await Expect(Loc("#btnMain1")).ToBeEnabledAsync();
            for (int i = 2; i <= 5; i++) await Expect(Loc("#btnMain" + i)).ToBeDisabledAsync();
            foreach (string id in new[] { "#shipMain1", "#shipMain2", "#shipMain3", "#shipMain4", "#shipMain5", "#btnShipClose" })
            {
                await Expect(Loc("#footer-buttons " + id)).ToHaveCountAsync(0);
                await Expect(Loc("#shipDialog " + id)).ToHaveCountAsync(1);
            }
            for (int i = 3; i <= 5; i++)
            {
                await Expect(Loc("#shipMain" + i)).ToBeVisibleAsync();
                await Expect(Loc("#shipMain" + i)).ToBeDisabledAsync();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Basis("SC-ORD-02-B03"), Basis("EV-ORD-205")]
        public async Task 詳細_出荷更新ボタン(bool shipped)
        {
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 12, 31),
                shippedDate: shipped ? new DateTime(2198, 4, 5) : (DateTime?)null);

            await OpenDetailAsync(o);

            await Expect(Loc("#contents #btnShip")).ToHaveAccessibleNameAsync("出荷更新");
            await Expect(Loc("#btnShip")).ToBeEnabledAsync();
            await Expect(Loc("#footer-buttons #btnShip")).ToHaveCountAsync(0);
            if (shipped) await Expect(Loc("#btnEdit")).ToBeDisabledAsync();
            else await Expect(Loc("#btnEdit")).ToBeEnabledAsync();
        }

        [Test, Basis("EV-ORD-205"), Basis("F-5")]
        public async Task 詳細_出荷更新で元画面の上に重ねて表示される()
        {
            Es e = NewEs();

            await OpenFromDetailAsync(e.O);

            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            Assert.That(new Uri(page.Url).AbsolutePath, Is.EqualTo("/OrdTf/Detail"));
            await Expect(Loc("#orderId")).ToHaveTextAsync(e.O.ToString());
            await Expect(Loc("#detailList")).ToHaveCountAsync(1);
            await Expect(Loc("#btnMain1")).ToHaveAccessibleNameAsync("削除");
        }

        // ================================================================ 表示（EV-ORD-401）

        [Test, Basis("EV-ORD-401"), Basis("SC-ORD-04-I01"), Basis("SC-ORD-04-I08"), Basis("SC-ORD-04-B01"), Basis("決定 1")]
        public async Task 表示_未出荷()
        {
            Es e = NewEs(10, 1);

            await OpenFromListAsync(e.K, e.O);

            await Expect(Loc("#shipOrderId")).ToHaveTextAsync(e.O.ToString());
            await Expect(Loc("#shipCustomerName")).ToHaveTextAsync(data.CustomerNames[e.K]);
            await Expect(Loc("#shipStatus")).ToHaveTextAsync("未出荷");
            await Expect(Loc("#shipRequiredDate")).ToHaveTextAsync("2198/12/31");
            await Expect(Loc("#shipShippedDate")).ToHaveValueAsync(TodayIso);
            await Expect(Loc("#shipShipVia")).ToHaveValueAsync(e.S0.ToString());
            await Expect(Loc("#shipFreight")).ToHaveValueAsync(NumRe(12.5m));
            await Expect(StockRows).ToHaveCountAsync(2);
            ILocator r1 = StockRowOf(data.ProductNames[e.P1]);
            ILocator r2 = StockRowOf(data.ProductNames[e.P2]);
            await Expect(r1.Locator(".ship-qty")).ToHaveTextAsync("3");
            await Expect(r1.Locator(".ship-stock")).ToHaveTextAsync("10");
            await Expect(r1).Not.ToHaveClassAsync(RowDanger);
            await Expect(r2.Locator(".ship-qty")).ToHaveTextAsync("2");
            await Expect(r2.Locator(".ship-stock")).ToHaveTextAsync("1");
            await Expect(r2).ToHaveClassAsync(RowDanger);
            await Expect(Loc("#shipMain1")).ToHaveAccessibleNameAsync("出荷確定");
            await Expect(Loc("#shipMain1")).ToBeEnabledAsync();
            await Expect(Loc("#shipMain2")).ToBeDisabledAsync();
            for (int i = 3; i <= 5; i++) await Expect(Loc("#shipMain" + i)).ToBeDisabledAsync();
            for (int i = 3; i <= 5; i++) await Expect(Loc("#shipMain" + i)).ToBeVisibleAsync();
            await Expect(Loc("#btnShipClose")).ToBeEnabledAsync();
            await Expect(Loc("#shipMessage")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipShippedDateError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipShipViaError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipFreightError")).ToHaveTextAsync(Empty);
        }

        [Test, Basis("EV-ORD-401")]
        public async Task 表示_未出荷で配送業者が未設定なら未選択()
        {
            string k = data.NewCustomer('K');
            int o = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 12, 31));

            await OpenFromListAsync(k, o);

            await Expect(Loc("#shipShipVia")).ToHaveValueAsync("");
        }

        [Test, Basis("EV-ORD-401"), Basis("選択肢の構築"), Basis("SC-ORD-04-I06")]
        public async Task 表示_配送業者の選択肢()
        {
            // ShipperID の昇順と会社名の昇順が逆：s1 は ID が小さく会社名が大きい
            int s1 = data.NewShipper(mark: 'Z');
            int s2 = data.NewShipper(mark: 'A');
            string k = data.NewCustomer('K');
            int o = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 12, 31));

            await OpenFromListAsync(k, o);

            ILocator opts = Loc("#shipShipVia option");
            await Expect(opts.First).ToHaveAttributeAsync("value", "");
            await Expect(opts.First).ToHaveTextAsync(Empty);
            IReadOnlyList<string> texts = await opts.AllInnerTextsAsync();
            int i1 = texts.ToList().IndexOf(data.ShipperNames[s1]);
            int i2 = texts.ToList().IndexOf(data.ShipperNames[s2]);
            Assert.That(i1, Is.GreaterThan(0));
            Assert.That(i2, Is.GreaterThan(0));
            Assert.That(i2, Is.LessThan(i1), "会社名の昇順（s2 が s1 より前）");
        }

        [Test, Basis("EV-ORD-401"), Basis("SC-ORD-04-I05"), Basis("SC-ORD-04-I08"), Basis("SC-ORD-04-B01"), Basis("SC-ORD-04-B02")]
        public async Task 表示_出荷済()
        {
            string k = data.NewCustomer('K');
            int s0 = data.NewShipper(mark: 'A');
            int p = data.NewProduct(unitsInStock: 5);
            int o = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 4, 1),
                shippedDate: Shipped0405, shipVia: s0, freight: 7.25m);
            data.NewDetail(o, p, 10.00m, 1, 0f);

            await OpenFromListAsync(k, o);

            await Expect(Loc("#shipStatus")).ToHaveTextAsync("納期遅延");
            await Expect(Loc("#shipShippedDate")).ToHaveValueAsync("2198-04-05");
            await Expect(Loc("#shipShipVia")).ToHaveValueAsync(s0.ToString());
            await Expect(Loc("#shipFreight")).ToHaveValueAsync(NumRe(7.25m));
            await Expect(Loc("#shipStockList")).ToBeHiddenAsync();
            await Expect(Loc("#shipMain1")).ToHaveAccessibleNameAsync("出荷情報を更新");
            await Expect(Loc("#shipMain1")).ToBeEnabledAsync();
            await Expect(Loc("#shipMain2")).ToHaveAccessibleNameAsync("出荷取消");
            await Expect(Loc("#shipMain2")).ToBeEnabledAsync();
            for (int i = 3; i <= 5; i++) await Expect(Loc("#shipMain" + i)).ToBeDisabledAsync();
        }

        [Test, Basis("EV-ORD-401"), Basis("状況の判定")]
        public async Task 表示_出荷済で納期内は出荷済()
        {
            string k = data.NewCustomer('K');
            int o = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 12, 31), shippedDate: new DateTime(2198, 3, 5));

            await OpenFromListAsync(k, o);

            await Expect(Loc("#shipStatus")).ToHaveTextAsync("出荷済");
        }

        [Test, Basis("EV-ORD-205"), Basis("EV-ORD-401")]
        public async Task 表示_詳細から開いても同じ内容()
        {
            Es e = NewEs(10, 1);

            await OpenFromDetailAsync(e.O);

            await Expect(Loc("#shipOrderId")).ToHaveTextAsync(e.O.ToString());
            await Expect(Loc("#shipStatus")).ToHaveTextAsync("未出荷");
            await Expect(Loc("#shipShippedDate")).ToHaveValueAsync(TodayIso);
            await Expect(StockRows).ToHaveCountAsync(2);
            await Expect(StockRowOf(data.ProductNames[e.P1])).Not.ToHaveClassAsync(RowDanger);
            await Expect(StockRowOf(data.ProductNames[e.P2])).ToHaveClassAsync(RowDanger);
            await Expect(Loc("#shipMain1")).ToHaveAccessibleNameAsync("出荷確定");
            await Expect(Loc("#shipMain2")).ToBeDisabledAsync();
        }

        [Test, Basis("共通仕様 4 節（必須項目は表示名の末尾に「＊」）"), Basis("SC-ORD-04-I05"), Basis("SC-ORD-04-I06"), Basis("SC-ORD-04-I07")]
        public async Task 表示_必須の印()
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);

            await Expect(Loc("label[for=shipShippedDate]")).ToHaveTextAsync("出荷日＊");
            await Expect(Loc("label[for=shipShipVia]")).ToHaveTextAsync("配送業者＊");
            await Expect(Loc("label[for=shipFreight]")).ToHaveTextAsync("送料");
        }

        [Test, Basis("EV-ORD-401"), Basis("ERR-NOTFOUND"), Basis("F-4"), Basis("DEC-ORD-51"), Basis("DEC-ORD-52")]
        public async Task 表示_消された受注は一覧へERR_NOTFOUND_一覧から()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 25, Start2198);

            await ListSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            int target = ids[4];   // 2 ページ目の先頭（i = 5）
            await Expect(RowOf(target)).ToBeVisibleAsync();
            TestData.DeleteOrder(target);
            await Expect(RowOf(target).Locator("button.btn-ship")).ToBeEnabledAsync();
            await RowOf(target).Locator("button.btn-ship").ClickAsync();

            await Expect(Loc("#shipDialog")).ToBeHiddenAsync();
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNotFound);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 24 件");
            await Expect(ListRows).ToHaveCountAsync(4);
        }

        [Test, Basis("EV-ORD-401"), Basis("ERR-NOTFOUND"), Basis("F-4"), Basis("DEC-ORD-28"), Basis("DEC-ORD-52")]
        public async Task 表示_消された受注は保持した一覧状態の一覧へERR_NOTFOUND_詳細から()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 25, Start2198);

            await ListSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await ListCell(0, 0).Locator("a").ClickAsync();
            await Expect(Loc("#orderId")).ToHaveTextAsync(ids[4].ToString());
            await Expect(Loc("#btnShip")).ToBeEnabledAsync();
            TestData.DeleteOrder(ids[4]);
            await Loc("#btnShip").ClickAsync();

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await Expect(Loc("#orderId")).ToHaveCountAsync(0);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNotFound);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 24 件");
            await Expect(ListRows).ToHaveCountAsync(4);
        }

        // ================================================================ 確定（EV-ORD-402）

        [Test, Basis("EV-ORD-402"), Basis("EV-ORD-108"), Basis("OD-T5"), Basis("DEC-ORD-55")]
        public async Task 確定_未出荷から確定して一覧へ戻る()
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);
            await FillShipAsync("2198-05-01", e.S1, "20");
            await ClickAsync("#shipMain1");

            await ExpectDialogClosedAsync();
            Assert.That(dialogs, Is.Empty, "確定では確認ダイアログを出さない");
            await ExpectListMessageAsync(e.O, "の出荷情報を更新しました。", Info);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Danger);
            ILocator row = RowOf(e.O);
            await Expect(row.Locator("td").Nth(5)).ToHaveTextAsync("2198/05/01");
            await Expect(row.Locator("td").Nth(6)).ToHaveTextAsync(data.ShipperNames[e.S1]);
            await Expect(row.Locator("td").Nth(7)).ToHaveTextAsync("70.00");
            await Expect(row.Locator("td").Nth(8)).ToHaveTextAsync("出荷済");
            await Expect(row.Locator("button.btn-ship")).ToHaveTextAsync("出荷変更");
            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.EqualTo(new DateTime(2198, 5, 1)));
            Assert.That(db.ShipVia, Is.EqualTo(e.S1));
            Assert.That(db.Freight, Is.EqualTo(20.00m));
            Assert.That(db.RowVersion, Is.EqualTo(2));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(7));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(3));
        }

        [Test, Basis("EV-ORD-402"), Basis("EV-ORD-402 手順 9")]
        public async Task 確定_成功の後に再読み込みしても二重に確定しない()
        {
            Es e = NewEs();
            await OpenFromListAsync(e.K, e.O);
            await FillShipAsync("2198-05-01", e.S1, "20");
            await ClickAsync("#shipMain1");
            await ExpectDialogClosedAsync();
            await ExpectListMessageAsync(e.O, "の出荷情報を更新しました。", Info);

            await page.ReloadAsync();

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(7));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(3));
            Assert.That(TestData.GetOrder(e.O).RowVersion, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-402"), Basis("EV-ORD-205"), Basis("EV-ORD-201")]
        public async Task 確定_詳細から確定すると詳細が最新になる()
        {
            Es e = NewEs();

            await OpenFromDetailAsync(e.O);
            await FillShipAsync("2198-05-01", e.S1, "20");
            await ClickAsync("#shipMain1");

            await ExpectDialogClosedAsync();
            await Expect(Loc("#orderId")).ToHaveTextAsync(e.O.ToString());
            await ExpectListMessageAsync(e.O, "の出荷情報を更新しました。", Info);
            await Expect(Loc("#shippedDate")).ToHaveTextAsync("2198/05/01");
            await Expect(Loc("#shipperName")).ToHaveTextAsync(data.ShipperNames[e.S1]);
            await Expect(Loc("#freight")).ToHaveTextAsync("20.00");
            await Expect(Loc("#status")).ToHaveTextAsync("出荷済");
            await Expect(Loc("#btnEdit")).ToBeDisabledAsync();
        }

        [Test, Basis("EV-ORD-205"), Basis("EV-ORD-203"), Basis("DEC-ORD-78")]
        public async Task 確定_詳細から確定すると削除は非活性になる()
        {
            Es e = NewEs();
            await OpenFromDetailAsync(e.O);
            await FillShipAsync("2198-05-01", e.S1, "20");
            await ClickAsync("#shipMain1");
            await ExpectDialogClosedAsync();
            await Expect(Loc("#shippedDate")).ToHaveTextAsync("2198/05/01");

            // 出荷済の受注は削除できない（DEC-ORD-78）：削除と編集が非活性
            await Expect(Loc("#status")).ToHaveTextAsync("出荷済");
            await Expect(Loc("#btnMain1")).ToBeDisabledAsync();
            await Expect(Loc("#btnEdit")).ToBeDisabledAsync();
            Assert.That(TestData.CountOrders(e.O), Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-402"), Basis("ERR-VALIDATION"), Basis("VAL-REQUIRED"), Basis("VAL-NUMERIC"), Basis("DEC-CMN-05")]
        public async Task 確定_検証エラーはダイアログに留まり項目の直下に出る()
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);
            await FillShipAsync("", null, "-1");
            await SelectAsync("#shipShipVia", "");
            await ClickAsync("#shipMain1");

            await Expect(Loc("#shipShippedDateError")).ToHaveTextAsync(NonEmpty);
            await Expect(Loc("#shipShippedDateError")).ToHaveClassAsync(Danger);
            await Expect(Loc("#shipShipViaError")).ToHaveTextAsync(NonEmpty);
            await Expect(Loc("#shipShipViaError")).ToHaveClassAsync(Danger);
            await Expect(Loc("#shipFreightError")).ToHaveTextAsync(NonEmpty);
            await Expect(Loc("#shipFreightError")).ToHaveClassAsync(Danger);
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            await Expect(Loc("#shipMessage")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipFreight")).ToHaveValueAsync("-1");
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.Null);
            Assert.That(db.RowVersion, Is.EqualTo(1));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(5));
        }

        [TestCase("-1")]
        [TestCase("abc")]
        [Basis("VAL-NUMERIC"), Basis("DEC-CMN-05"), Basis("DEC-ORD-56")]
        public async Task 確定_送料の不正値(string freight)
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);
            await FillShipAsync("2198-05-01", null, freight);
            await ClickAsync("#shipMain1");

            await Expect(Loc("#shipFreightError")).ToHaveTextAsync(NonEmpty);
            await Expect(Loc("#shipFreightError")).ToHaveClassAsync(Danger);
            await Expect(Loc("#shipShippedDateError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipShipViaError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            Assert.That(TestData.GetOrder(e.O).ShippedDate, Is.Null);
        }

        [Test, Basis("VAL-DATE"), Basis("ORD-T3"), Basis("DEC-ORD-57")]
        public async Task 確定_出荷日が受注日より前は出荷日の直下()
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);
            await FillShipAsync("2198-02-28", null, null);
            await ClickAsync("#shipMain1");

            await Expect(Loc("#shipShippedDateError")).ToHaveTextAsync(MsgBeforeOrder);
            await Expect(Loc("#shipShippedDateError")).ToHaveClassAsync(Danger);
            await Expect(Loc("#shipMessage")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            Assert.That(TestData.GetOrder(e.O).ShippedDate, Is.Null);
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));
        }

        [Test, Basis("EV-ORD-402")]
        public async Task 確定_検証エラーの後に直せば確定できる()
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);
            await FillShipAsync("", null, null);
            await ClickAsync("#shipMain1");
            await Expect(Loc("#shipShippedDateError")).ToHaveTextAsync(NonEmpty);
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            await FillShipAsync("2198-05-01", null, null);
            await ClickAsync("#shipMain1");

            await ExpectDialogClosedAsync();
            await ExpectListMessageAsync(e.O, "の出荷情報を更新しました。", Info);
            Assert.That(TestData.GetOrder(e.O).ShippedDate, Is.EqualTo(new DateTime(2198, 5, 1)));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(7));
        }

        [Test, Basis("EV-ORD-402"), Basis("PRD-T5"), Basis("ERR-BIZ"), Basis("DEC-ORD-54")]
        public async Task 確定_在庫不足はダイアログに留まりエラーを出す()
        {
            Es e = NewEs(10, 1);

            await OpenFromListAsync(e.K, e.O);
            await FillShipAsync("2198-05-01", null, null);
            await ClickAsync("#shipMain1");

            await Expect(Loc("#shipMessage")).ToHaveTextAsync(MsgShort + data.ProductNames[e.P2]);
            await Expect(Loc("#shipMessage")).ToHaveClassAsync(Danger);
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            await Expect(Loc("#shipStockList")).ToBeVisibleAsync();
            await Expect(Loc("#shipShippedDate")).ToHaveValueAsync("2198-05-01");
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.Null);
            Assert.That(db.RowVersion, Is.EqualTo(1));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(1));

            TestData.SetUnitsInStock(e.P2, 5);
            await ClickAsync("#shipMain1");

            await ExpectDialogClosedAsync();
            await ExpectListMessageAsync(e.O, "の出荷情報を更新しました。", Info);
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(7));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(3));
        }

        [Test, Basis("EV-ORD-402"), Basis("ERR-CONFLICT"), Basis("共通仕様 7 節（最新データを再表示）"), Basis("DEC-ORD-53")]
        public async Task 確定_競合は最新を再表示する()
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);
            TestData.BumpOrder(e.O, 99m);
            await FillShipAsync("2198-05-01", null, "20");
            await ClickAsync("#shipMain1");

            await Expect(Loc("#shipMessage")).ToHaveTextAsync(MsgConflict);
            await Expect(Loc("#shipMessage")).ToHaveClassAsync(Danger);
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            await Expect(Loc("#shipFreight")).ToHaveValueAsync(NumRe(99m));
            await Expect(Loc("#shipShippedDate")).ToHaveValueAsync(TodayIso);
            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.Null);
            Assert.That(db.Freight, Is.EqualTo(99.00m));
            Assert.That(db.RowVersion, Is.EqualTo(2));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));

            await FillShipAsync("2198-05-01", null, null);
            await ClickAsync("#shipMain1");

            await ExpectDialogClosedAsync();
            await ExpectListMessageAsync(e.O, "の出荷情報を更新しました。", Info);
            db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.EqualTo(new DateTime(2198, 5, 1)));
            Assert.That(db.RowVersion, Is.EqualTo(3));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(7));
        }

        [Test, Basis("EV-ORD-402"), Basis("ERR-CONFLICT"), Basis("OD-T5"), Basis("DEC-ORD-53")]
        public async Task 確定_他の利用者が先に出荷確定したら出荷済として再表示する()
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);
            TestData.ShipOrder(e.O);
            await FillShipAsync("2198-05-01", null, null);
            await ClickAsync("#shipMain1");

            await Expect(Loc("#shipMessage")).ToHaveTextAsync(MsgConflict);
            await Expect(Loc("#shipMessage")).ToHaveClassAsync(Danger);
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            await Expect(Loc("#shipStatus")).ToHaveTextAsync("出荷済");
            await Expect(Loc("#shipMain1")).ToHaveAccessibleNameAsync("出荷情報を更新");
            await Expect(Loc("#shipMain2")).ToBeEnabledAsync();
            await Expect(Loc("#shipStockList")).ToBeHiddenAsync();
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(5));
            Assert.That(TestData.GetOrder(e.O).RowVersion, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-402"), Basis("ERR-NOTFOUND"), Basis("F-4"), Basis("DEC-ORD-51"), Basis("DEC-ORD-52")]
        public async Task 確定_消されていたら一覧へERR_NOTFOUND()
        {
            Es e = NewEs();
            int o2 = data.NewOrder(customerId: e.K, orderDate: new DateTime(2198, 3, 2), requiredDate: new DateTime(2198, 12, 31));

            await OpenFromListAsync(e.K, e.O);
            TestData.DeleteOrder(e.O);
            await FillShipAsync("2198-05-01", null, null);
            await ClickAsync("#shipMain1");

            await ExpectDialogClosedAsync();
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNotFound);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(e.K);
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 1 件");
            await Expect(RowOf(e.O)).ToHaveCountAsync(0);
            await Expect(RowOf(o2)).ToHaveCountAsync(1);
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(5));
        }

        [Test, Basis("EV-ORD-402"), Basis("SC-ORD-04-B01")]
        public async Task 確定_出荷済の情報を更新する()
        {
            Es e = NewEs(7, 3, Shipped0405, 7.25m);

            await OpenFromListAsync(e.K, e.O);
            await Expect(Loc("#shipMain1")).ToHaveAccessibleNameAsync("出荷情報を更新");
            await FillShipAsync("2198-04-10", e.S1, "30");
            await ClickAsync("#shipMain1");

            await ExpectDialogClosedAsync();
            await ExpectListMessageAsync(e.O, "の出荷情報を更新しました。", Info);
            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.EqualTo(new DateTime(2198, 4, 10)));
            Assert.That(db.ShipVia, Is.EqualTo(e.S1));
            Assert.That(db.Freight, Is.EqualTo(30.00m));
            Assert.That(db.RowVersion, Is.EqualTo(2));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(7));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(3));
        }

        [Test, Basis("EV-ORD-108"), Basis("F-5"), Basis("DEC-ORD-51"), Basis("DEC-ORD-04")]
        public async Task 確定_一覧状態を保って一覧へ戻る()
        {
            string k = data.NewCustomer('K');
            int s0 = data.NewShipper(mark: 'A');
            int p = data.NewProduct(mark: 'P', unitsInStock: 10);
            List<int> ids = data.NewOrdersSequence(k, 25, Start2198);
            int target = ids[4];   // 2 ページ目の先頭（i = 5）
            data.NewDetail(target, p, 10.00m, 1, 0f);

            await ListSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await OpenRowDialogAsync(target);
            await FillShipAsync("2198-05-01", s0, null);
            await ClickAsync("#shipMain1");

            await ExpectDialogClosedAsync();
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 25 件");
            await Expect(ListRows).ToHaveCountAsync(5);
            await Expect(RowOf(target).Locator("td").Nth(5)).ToHaveTextAsync("2198/05/01");
            Assert.That(TestData.GetUnitsInStock(p), Is.EqualTo(9));
        }

        // ================================================================ 取消（EV-ORD-403）

        [Test, Basis("EV-ORD-403"), Basis("OD-T5"), Basis("DEC-ORD-55")]
        public async Task 取消_確認して承認すると未出荷に戻る()
        {
            Es e = NewEs(7, 3, Shipped0405, 7.25m);

            await OpenFromListAsync(e.K, e.O);
            await ClickAsync("#shipMain2");

            await ExpectDialogClosedAsync();
            Assert.That(dialogs, Is.EqualTo(new[] { MsgCancelConfirm }));
            await ExpectListMessageAsync(e.O, "の出荷を取り消しました。", Info);
            ILocator row = RowOf(e.O);
            await Expect(row.Locator("td").Nth(5)).ToHaveTextAsync(Empty);
            await Expect(row.Locator("td").Nth(6)).ToHaveTextAsync(data.ShipperNames[e.S0]);
            await Expect(row.Locator("td").Nth(8)).ToHaveTextAsync("未出荷");
            await Expect(row.Locator("button.btn-ship")).ToHaveTextAsync("出荷更新");
            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.Null);
            Assert.That(db.ShipVia, Is.EqualTo(e.S0));
            Assert.That(db.Freight, Is.EqualTo(7.25m));
            Assert.That(db.RowVersion, Is.EqualTo(2));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(5));
        }

        [Test, Basis("EV-ORD-403")]
        public async Task 取消_確認を取り消すと何もしない()
        {
            Es e = NewEs(7, 3, Shipped0405, 7.25m);
            acceptDialog = false;

            await OpenFromListAsync(e.K, e.O);
            await ClickAsync("#shipMain2");

            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            Assert.That(dialogs, Is.EqualTo(new[] { MsgCancelConfirm }));
            await Expect(Loc("#shipMessage")).ToHaveTextAsync(Empty);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.EqualTo(Shipped0405));
            Assert.That(db.RowVersion, Is.EqualTo(1));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(7));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(3));
        }

        [Test, Basis("EV-ORD-403"), Basis("引き継ぎ（POST の再送）")]
        public async Task 取消_成功の後に再読み込みしても二重に戻さない()
        {
            Es e = NewEs(7, 3, Shipped0405, 7.25m);
            await OpenFromListAsync(e.K, e.O);
            await ClickAsync("#shipMain2");
            await ExpectDialogClosedAsync();
            await ExpectListMessageAsync(e.O, "の出荷を取り消しました。", Info);

            await page.ReloadAsync();

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(5));
            Assert.That(TestData.GetOrder(e.O).RowVersion, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-403"), Basis("EV-ORD-205"), Basis("EV-ORD-201")]
        public async Task 取消_詳細から取り消すと詳細が最新になる()
        {
            Es e = NewEs(7, 3, Shipped0405, 7.25m);

            await OpenFromDetailAsync(e.O);
            await ClickAsync("#shipMain2");

            await ExpectDialogClosedAsync();
            await ExpectListMessageAsync(e.O, "の出荷を取り消しました。", Info);
            await Expect(Loc("#shippedDate")).ToHaveTextAsync(Empty);
            await Expect(Loc("#status")).ToHaveTextAsync("未出荷");
            await Expect(Loc("#btnEdit")).ToBeEnabledAsync();
        }

        [Test, Basis("EV-ORD-403"), Basis("ERR-CONFLICT"), Basis("DEC-ORD-53")]
        public async Task 取消_競合は最新を再表示する()
        {
            Es e = NewEs(7, 3, Shipped0405, 7.25m);

            await OpenFromListAsync(e.K, e.O);
            TestData.BumpOrder(e.O, 99m);
            await ClickAsync("#shipMain2");

            await Expect(Loc("#shipMessage")).ToHaveTextAsync(MsgConflict);
            await Expect(Loc("#shipMessage")).ToHaveClassAsync(Danger);
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            await Expect(Loc("#shipFreight")).ToHaveValueAsync(NumRe(99m));
            await Expect(Loc("#shipMain2")).ToBeEnabledAsync();
            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.EqualTo(Shipped0405));
            Assert.That(db.RowVersion, Is.EqualTo(2));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(7));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(3));
        }

        [Test, Basis("EV-ORD-403"), Basis("ERR-NOTFOUND"), Basis("DEC-ORD-51"), Basis("DEC-ORD-52")]
        public async Task 取消_消されていたら一覧へERR_NOTFOUND()
        {
            Es e = NewEs(7, 3, Shipped0405, 7.25m);
            int o2 = NewEsOrder(e, Shipped0405, 7.25m);

            await OpenFromListAsync(e.K, e.O);
            TestData.DeleteOrder(e.O);
            await ClickAsync("#shipMain2");

            await ExpectDialogClosedAsync();
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNotFound);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(e.K);
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 1 件");
            await Expect(RowOf(o2)).ToHaveCountAsync(1);
        }

        // ================================================================ 閉じる（EV-ORD-404）

        [Test, Basis("EV-ORD-404"), Basis("F-5"), Basis("EV-ORD-108")]
        public async Task 閉じる_一覧は変更がなくても再読み込みする()
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);
            await Expect(RowOf(e.O).Locator("td").Nth(7)).ToHaveTextAsync("62.50");
            TestData.BumpOrder(e.O, 99m);
            await ClickAsync("#btnShipClose");

            await ExpectDialogClosedAsync();
            await Expect(RowOf(e.O).Locator("td").Nth(7)).ToHaveTextAsync("149.00");
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.Null);
            Assert.That(db.RowVersion, Is.EqualTo(2));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));
        }

        [Test, Basis("EV-ORD-404"), Basis("EV-ORD-205"), Basis("EV-ORD-201"), Basis("EV-ORD-203"), Basis("DEC-ORD-78")]
        public async Task 閉じる_詳細は再読み込みする()
        {
            Es e = NewEs();

            await OpenFromDetailAsync(e.O);
            TestData.ShipOrder(e.O);
            await ClickAsync("#btnShipClose");

            await ExpectDialogClosedAsync();
            await Expect(Loc("#shippedDate")).ToHaveTextAsync(Today);
            await Expect(Loc("#status")).Not.ToHaveTextAsync("未出荷");
            await Expect(Loc("#btnEdit")).ToBeDisabledAsync();
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            await Expect(Loc("#btnMain1")).ToBeDisabledAsync();   // 出荷済の受注は削除できない（DEC-ORD-78）
            Assert.That(TestData.CountOrders(e.O), Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-404"), Basis("F-5"), Basis("DEC-ORD-51")]
        public async Task 閉じる_一覧の条件_ページ_並べ替えを保って再読み込みする()
        {
            string k = data.NewCustomer('K');
            data.NewOrdersSequence(k, 25, Start2198);

            await ListSearchAsync(k);
            await ClickAsync("#sortOrderID");
            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await Expect(ListRows).ToHaveCountAsync(5);
            List<string> before = new List<string>();
            for (int i = 0; i < 5; i++) before.Add(await ListCell(i, 0).InnerTextAsync());
            string sortBefore = await Loc("#orderList thead th").First.GetAttributeAsync("aria-sort");
            await Expect(ListRows.Nth(0).Locator("button.btn-ship")).ToBeEnabledAsync();
            await ListRows.Nth(0).Locator("button.btn-ship").ClickAsync();
            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            await ClickAsync("#btnShipClose");

            await ExpectDialogClosedAsync();
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 25 件");
            await Expect(ListRows).ToHaveCountAsync(5);
            for (int i = 0; i < 5; i++) await Expect(ListCell(i, 0)).ToHaveTextAsync(before[i]);
            Assert.That(await Loc("#orderList thead th").First.GetAttributeAsync("aria-sort"), Is.EqualTo(sortBefore));
        }

        [Test, Basis("EV-ORD-108"), Basis("EV-ORD-404"), Basis("DEC-ORD-51")]
        public async Task 閉じる_出荷状況で絞った一覧は同じ条件で再検索する()
        {
            string k = data.NewCustomer('K');
            int a = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 12, 31));
            int b = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 2), requiredDate: new DateTime(2198, 12, 31));
            int c = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 3), requiredDate: new DateTime(2198, 12, 31));

            await page.GotoAsync("/OrdTf");
            await SelectAsync("#CustomerID", k);
            await SelectAsync("#ShippedStatus", "未出荷");
            await ClickAsync("#btnMain1");
            await Expect(ListRows).ToHaveCountAsync(3);
            await OpenRowDialogAsync(a);
            TestData.ShipOrder(a);
            await ClickAsync("#btnShipClose");

            await ExpectDialogClosedAsync();
            await Expect(ListRows).ToHaveCountAsync(2);
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 2 件");
            await Expect(RowOf(a)).ToHaveCountAsync(0);
            await Expect(RowOf(b)).ToHaveCountAsync(1);
            await Expect(RowOf(c)).ToHaveCountAsync(1);
        }

        [Test, Basis("EV-ORD-404"), Basis("SC-ORD-04-I05"), Basis("SC-ORD-04-I07")]
        public async Task 閉じる_入力は破棄され開き直すと初期値()
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);
            await FillShipAsync("2198-05-01", null, "99");
            await ClickAsync("#btnShipClose");
            await ExpectDialogClosedAsync();

            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.Null);
            Assert.That(db.Freight, Is.EqualTo(12.50m));
            Assert.That(db.RowVersion, Is.EqualTo(1));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));

            await OpenRowDialogAsync(e.O);
            await Expect(Loc("#shipShippedDate")).ToHaveValueAsync(TodayIso);
            await Expect(Loc("#shipFreight")).ToHaveValueAsync(NumRe(12.5m));
        }

        // ================================================================ 受注管理の直し（OrdTfRevise）
        // 仕様：docs/spec/test3-testfirst-OrdTfRevise-mvc-core-tests.md（NorthwindDbSampleSpec@9be21c0）の 3.2。

        [Test, Basis("規約：日付の入力（cmn-framework.md「画面共通」）")]
        public async Task 表示_出荷日の入力欄はdate型()
        {
            Es e = NewEs();

            await OpenFromListAsync(e.K, e.O);

            await Expect(Loc("#shipShippedDate")).ToHaveAttributeAsync("type", "date");
        }

        [Test, Basis("規約：日付の入力（未入力として扱わない）"), Basis("VAL-DATE"), Basis("DEC-CMN-05"), Basis("DEC-ORD-56")]
        public async Task 確定_出荷日の入力途中は形式の違反として扱い確定しない()
        {
            Es e = NewEs();
            await OpenFromListAsync(e.K, e.O);
            await DateInput.TypePartialAsync(page, "#shipShippedDate");   // 入力途中の値
            List<string> posts = DateInput.RecordPosts(page);

            await ClickAsync("#shipMain1");

            await Expect(Loc("#shipDialog")).ToBeVisibleAsync();
            await Expect(Loc("#shipShippedDateError")).ToHaveTextAsync(NonEmpty);
            await Expect(Loc("#shipShippedDateError")).ToHaveClassAsync(Danger);
            await Expect(Loc("#shipShipViaError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipFreightError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#shipMessage")).ToHaveTextAsync(Empty);
            Assert.That(await DateInput.IsBadInputAsync(page, "#shipShippedDate"), Is.True, "入力途中のまま");
            await DateInput.ExpectNoPostAsync(page, posts);   // 送信を止めている（必須エラーでの拒否と区別する）
            TestData.OrderRow db = TestData.GetOrder(e.O);
            Assert.That(db.ShippedDate, Is.Null);
            Assert.That(db.RowVersion, Is.EqualTo(1));
            Assert.That(TestData.GetUnitsInStock(e.P1), Is.EqualTo(10));
            Assert.That(TestData.GetUnitsInStock(e.P2), Is.EqualTo(5));
        }

        [Test, Basis("EV-ORD-401"), Basis("DEC-ORD-77"), Basis("DEC-ORD-73"), Basis("DEC-ORD-75"), Basis("DEC-ORD-79")]
        public async Task 表示_商品マスタに無い明細は内訳に出て強調しない()
        {
            string k = data.NewCustomer('K');
            int s0 = data.NewShipper(mark: 'A');
            int za = data.NewProduct(mark: 'Z', unitsInStock: 5);
            int zb = data.NewProduct(mark: 'Z', unitsInStock: 5);
            int p1 = data.NewProduct(mark: 'A', unitsInStock: 10);
            int o = data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 1), requiredDate: new DateTime(2198, 12, 31),
                shipVia: s0, freight: 12.50m);
            data.NewDetail(o, p1, 10.00m, 3, 0f);   // P1・Zb・Za の順に登録する
            data.NewDetail(o, zb, 10.00m, 2, 0f);
            data.NewDetail(o, za, 10.00m, 5, 0f);
            data.DeleteProduct(za);
            data.DeleteProduct(zb);

            await OpenFromListAsync(k, o);

            await Expect(StockRows).ToHaveCountAsync(3);
            await Expect(StockRows.Nth(0).Locator(".ship-product")).ToHaveTextAsync(data.ProductNames[p1]);
            await Expect(StockRows.Nth(1).Locator(".ship-product")).ToHaveTextAsync("（商品マスタに無い：ID " + za + "）");
            await Expect(StockRows.Nth(1).Locator(".ship-qty")).ToHaveTextAsync("5");
            await Expect(StockRows.Nth(1).Locator(".ship-stock")).ToHaveTextAsync(Empty);
            await Expect(StockRows.Nth(1)).Not.ToHaveClassAsync(RowDanger);
            await Expect(StockRows.Nth(2).Locator(".ship-product")).ToHaveTextAsync("（商品マスタに無い：ID " + zb + "）");
            await Expect(StockRows.Nth(2).Locator(".ship-qty")).ToHaveTextAsync("2");
            await Expect(StockRows.Nth(2).Locator(".ship-stock")).ToHaveTextAsync(Empty);
            await Expect(StockRows.Nth(2)).Not.ToHaveClassAsync(RowDanger);
            await Expect(Loc("#shipMain1")).ToBeEnabledAsync();
            // #shipMessage は期待にしない（出荷ダイアログに警告を出すかは DEC-ORD-79：定めない）
        }

        [Test, Basis("EV-ORD-403"), Basis("EV-ORD-205"), Basis("EV-ORD-203"), Basis("DEC-ORD-78")]
        public async Task 取消_詳細から取り消した後の削除は競合しない()
        {
            Es e = NewEs(7, 3, Shipped0405, 7.25m);

            await OpenFromDetailAsync(e.O);
            await Expect(Loc("#btnMain1")).ToBeDisabledAsync();   // 出荷済は削除できない
            await ClickAsync("#shipMain2");   // 取消（確認は承認）
            await ExpectDialogClosedAsync();
            await Expect(Loc("#status")).ToHaveTextAsync("未出荷");
            await Expect(Loc("#btnMain1")).ToBeEnabledAsync();

            await ClickAsync("#btnMain1");   // 削除（確認は承認）

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await ExpectListMessageAsync(e.O, "を削除しました。", Info);   // ERR-CONFLICT にならない
            Assert.That(TestData.CountOrders(e.O), Is.EqualTo(0));
        }

        [Test, Basis("EV-ORD-404"), Basis("EV-ORD-205"), Basis("EV-ORD-201"), Basis("EV-ORD-203")]
        public async Task 閉じる_詳細の再読み込みの後の削除は競合しない()
        {
            Es e = NewEs();

            await OpenFromDetailAsync(e.O);
            TestData.BumpOrder(e.O, 99.00m);
            await ClickAsync("#btnShipClose");

            await ExpectDialogClosedAsync();
            await Expect(Loc("#freight")).ToHaveTextAsync("99.00");   // 再読み込みされた
            await Expect(Loc("#btnMain1")).ToBeEnabledAsync();

            await ClickAsync("#btnMain1");   // 削除（確認は承認）

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await ExpectListMessageAsync(e.O, "を削除しました。", Info);
            Assert.That(TestData.CountOrders(e.O), Is.EqualTo(0));
        }
    }
}
