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
    /// 受注詳細（登録・編集。SC-ORD-03）の画面のテスト（MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）。
    /// 仕様：docs/spec/test3-testfirst-OrdTfEdit-mvc-core-tests.md（NorthwindDbSampleSpec@693e663）の 4 章。
    /// B層で確かめること（検証の全項目・金額の丸め・明細の行ごとの適用とロールバック）は繰り返さない。
    /// </summary>
    public class OrdTfEditScreenTests
    {
        private const string MsgNotFound = "対象のデータは存在しません。";
        private const string MsgConflict = "他の利用者によって更新されています。最新の内容を読み込み直してください。";
        private const string MsgShipped = "出荷済の受注は編集できません。";
        private const string MsgShippedFromDetail = "出荷済の受注は編集できません。出荷を取り消してから編集してください。";
        private const string MsgDup = "この商品は既に明細に存在します。数量を変更してください。";
        private const string MsgStock = "在庫数を超えています。";
        private const string MsgCancel = "入力内容は保存されません。よろしいですか？";

        /// <summary>E2E の受注日は 2198 年（B層のテストは 2199 年）</summary>
        private static readonly DateTime Start2198 = new DateTime(2198, 1, 1);

        private static readonly Regex Danger = new Regex(@"\btext-danger\b");
        private static readonly Regex Info = new Regex(@"\btext-info\b");
        private static readonly Regex Warning = new Regex(@"\btext-warning\b");
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

        private ILocator Lines { get { return page.Locator("#lineList tbody tr"); } }

        /// <summary>明細入力表の i 行目（0 始まり）</summary>
        private ILocator Row(int i) { return Lines.Nth(i); }

        private ILocator DetailRows { get { return page.Locator("#detailList tbody tr"); } }

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

        private async Task LineSelectAsync(int row, int productId)
        {
            ILocator sel = Row(row).Locator(".line-product");
            await Expect(sel).ToBeVisibleAsync();
            await sel.SelectOptionAsync(productId.ToString());
        }

        private async Task LineSetAsync(int row, string cls, string value)
        {
            ILocator input = Row(row).Locator("." + cls);
            await Expect(input).ToBeVisibleAsync();
            await input.FillAsync(value);
            await input.BlurAsync();
        }

        private async Task OpenNewAsync() { await page.GotoAsync("/OrdTf/Edit?mode=new"); }

        private async Task OpenEditAsync(int orderId) { await page.GotoAsync("/OrdTf/Edit?mode=edit&id=" + orderId); }

        private async Task OpenDetailAsync(int orderId) { await page.GotoAsync("/OrdTf/Detail?id=" + orderId); }

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

        private async Task ExpectListAsync(IList<int> ids)
        {
            await Expect(ListRows).ToHaveCountAsync(ids.Count);
            for (int i = 0; i < ids.Count; i++)
                await Expect(ListCell(i, 0)).ToHaveTextAsync(ids[i].ToString());
        }

        /// <summary>i 番目（1 から）の受注 = ids[i - 1]。新しい順（i の降順）で topI から bottomI まで</summary>
        private static List<int> Range(List<int> ids, int topI, int bottomI)
        {
            List<int> r = new List<int>();
            for (int i = topI; i >= bottomI; i--) r.Add(ids[i - 1]);
            return r;
        }

        private async Task ExpectNotFoundListAsync()
        {
            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await Expect(Loc("#orderId")).ToHaveCountAsync(0);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNotFound);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
        }

        /// <summary>商品 P1（単価 10.00）・P2（単価 20.00）、顧客 K（電話などあり）・担当者 E・配送業者 S を作る</summary>
        private sealed class Ctx
        {
            public string K, K2;
            public int E, S, P1, P2;
            public string EName;
        }

        private Ctx NewCtx()
        {
            Ctx c = new Ctx();
            c.K = data.NewCustomer('K', phone: "03-1111-1111", address: "A1", city: "C1", region: "R1", postalCode: "P1", country: "Jp");
            c.K2 = data.NewCustomer('L', phone: "03-2222-2222", address: "A2", city: "C2", region: "R2", postalCode: "P2", country: "Us");
            string last = TestData.NewLastName('E'), first = TestData.NewFirstName('F');
            c.E = data.NewEmployee(last, first);
            c.EName = last + " " + first;
            c.S = data.NewShipper();
            c.P1 = data.NewProduct(mark: 'A', unitPrice: 10.00m, unitsInStock: 100);
            c.P2 = data.NewProduct(mark: 'B', unitPrice: 20.00m, unitsInStock: 100);
            return c;
        }

        /// <summary>編集用の受注（K・E・S、受注日 2198/03/05、要求納期 2198/04/01、送料 5.00、Ship* の値）</summary>
        private int NewEditOrder(Ctx c)
        {
            return data.NewOrder(customerId: c.K, employeeId: c.E, orderDate: new DateTime(2198, 3, 5), requiredDate: new DateTime(2198, 4, 1),
                shipVia: c.S, freight: 5.00m, shipName: "ShipTo Co", shipAddress: "4-5-6 Ginza", shipCity: "Chuo", shipRegion: "Tokyo",
                shipPostalCode: "104-0061", shipCountry: "Japan");
        }

        // ================================================================ 登録の表示（EV-ORD-301）

        [Test, Basis("EV-ORD-301"), Basis("SC-ORD-03-I01"), Basis("SC-ORD-03-I06"), Basis("SC-ORD-03-I09"), Basis("SC-ORD-03-I16"),
            Basis("SC-ORD-03-I17"), Basis("SC-ORD-03-I18"), Basis("SC-ORD-03-B01"), Basis("SC-ORD-03-B03"), Basis("SC-ORD-03-B04"), Basis("DEC-ORD-39")]
        public async Task 登録_初期表示()
        {
            NewCtx();

            await OpenNewAsync();

            await Expect(Loc("#orderId")).ToHaveTextAsync("（登録時に採番）");
            await Expect(Loc("#OrderDate")).ToHaveValueAsync(DateTime.Today.ToString("yyyy/MM/dd"));
            await Expect(Loc("#Freight")).ToHaveValueAsync(NumRe(0m));
            await Expect(Loc("#CustomerID")).ToHaveValueAsync("");
            await Expect(Loc("#EmployeeID")).ToHaveValueAsync("");
            await Expect(Loc("#ShipVia")).ToHaveValueAsync("");
            await Expect(Loc("#RequiredDate")).ToHaveValueAsync("");
            foreach (string id in new[] { "ShipName", "ShipAddress", "ShipCity", "ShipRegion", "ShipPostalCode", "ShipCountry" })
                await Expect(Loc("#" + id)).ToHaveValueAsync("");
            await Expect(Loc("#customerPhone")).ToHaveTextAsync(Empty);
            await Expect(Loc("#customerAddress")).ToHaveTextAsync(Empty);
            await Expect(Lines).ToHaveCountAsync(1);
            await Expect(Row(0).Locator(".line-product")).ToHaveValueAsync("");
            await Expect(Row(0).Locator(".line-discount")).ToHaveValueAsync(NumRe(0m));
            await Expect(Row(0).Locator(".line-quantity")).ToHaveValueAsync(NumRe(1m));
            await Expect(Row(0).Locator(".line-remove")).ToBeDisabledAsync();
            await Expect(Loc("#subtotal")).ToHaveTextAsync("0.00");
            await Expect(Loc("#total")).ToHaveTextAsync("0.00");
            await Expect(Loc("#btnAddLine")).ToBeEnabledAsync();
            await Expect(Loc("#contents #btnMain1")).ToBeEnabledAsync();
            await Expect(Loc("#btnMain1")).ToHaveTextAsync("保存");
            for (int i = 2; i <= 5; i++) await Expect(Loc("#btnMain" + i)).ToBeDisabledAsync();
            await Expect(Loc("#btnCancel")).ToBeEnabledAsync();
            await Expect(Loc("#footer-buttons #btnCancel")).ToHaveCountAsync(0);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            // 入力欄は、受注情報 6 個・出荷先 6 個・明細の各行 4 個だけ
            await Expect(Loc("#contents input:not([type=hidden]):not([type=button]):not([type=submit]), #contents select, #contents textarea"))
                .ToHaveCountAsync(16);
        }

        [Test, Basis("EV-ORD-301"), Basis("OD-T3"), Basis("SC-ORD-03-I02"), Basis("SC-ORD-03-I05"), Basis("SC-ORD-03-I08"), Basis("SC-ORD-03-I16")]
        public async Task 登録_選択肢()
        {
            Ctx c = NewCtx();
            int q = data.NewProduct(mark: 'Q', discontinued: true);

            await OpenNewAsync();

            await Expect(Loc("#CustomerID")).ToBeVisibleAsync();
            IReadOnlyList<string> cust = await Loc("#CustomerID option").AllTextContentsAsync();
            Assert.That(cust[0].Trim(), Is.EqualTo(""), "顧客の先頭が空白");
            Assert.That(cust, Does.Contain(data.CustomerNames[c.K]));
            Assert.That(cust, Does.Contain(data.CustomerNames[c.K2]));
            IReadOnlyList<string> emp = await Loc("#EmployeeID option").AllTextContentsAsync();
            Assert.That(emp[0].Trim(), Is.EqualTo(""), "担当者の先頭が空白");
            Assert.That(emp, Does.Contain(c.EName), "担当者 E の選択肢");
            IReadOnlyList<string> shp = await Loc("#ShipVia option").AllTextContentsAsync();
            Assert.That(shp[0].Trim(), Is.EqualTo(""), "配送業者の先頭が空白");
            Assert.That(shp, Does.Contain(data.ShipperNames[c.S]), "配送業者 S の選択肢");
            IReadOnlyList<string> prod = await Row(0).Locator(".line-product option").AllTextContentsAsync();
            Assert.That(prod, Does.Contain(data.ProductNames[c.P1]));
            Assert.That(prod, Does.Contain(data.ProductNames[c.P2]));
            Assert.That(prod, Does.Not.Contain(data.ProductNames[q]), "廃番の商品を含まない");
        }

        // ================================================================ 顧客選択（EV-ORD-302）

        [Test, Basis("EV-ORD-302"), Basis("ORD-T6"), Basis("DEC-ORD-45")]
        public async Task 顧客選択_顧客情報と出荷先の既定値()
        {
            Ctx c = NewCtx();

            await OpenNewAsync();
            await SelectAsync("#CustomerID", c.K);

            await Expect(Loc("#customerPhone")).ToHaveTextAsync("03-1111-1111");
            await Expect(Loc("#customerAddress")).ToHaveTextAsync(new Regex(@"^A1([\s\S]*)C1\1R1\1P1\1Jp$"));   // 区切りは期待にしない
            await Expect(Loc("#ShipName")).ToHaveValueAsync(data.CustomerNames[c.K]);
            await Expect(Loc("#ShipAddress")).ToHaveValueAsync("A1");
            await Expect(Loc("#ShipCity")).ToHaveValueAsync("C1");
            await Expect(Loc("#ShipRegion")).ToHaveValueAsync("R1");
            await Expect(Loc("#ShipPostalCode")).ToHaveValueAsync("P1");
            await Expect(Loc("#ShipCountry")).ToHaveValueAsync("Jp");
            Assert.That(dialogs, Is.Empty, "確認ダイアログは出ない");
        }

        [Test, Basis("EV-ORD-302"), Basis("DEC-ORD-32")]
        public async Task 顧客選択_自動設定のままなら確認なしで上書き()
        {
            Ctx c = NewCtx();

            await OpenNewAsync();
            await SelectAsync("#CustomerID", c.K);
            await Expect(Loc("#ShipName")).ToHaveValueAsync(data.CustomerNames[c.K]);
            await SelectAsync("#CustomerID", c.K2);

            await Expect(Loc("#ShipName")).ToHaveValueAsync(data.CustomerNames[c.K2]);
            await Expect(Loc("#ShipAddress")).ToHaveValueAsync("A2");
            await Expect(Loc("#ShipCity")).ToHaveValueAsync("C2");
            await Expect(Loc("#ShipRegion")).ToHaveValueAsync("R2");
            await Expect(Loc("#ShipPostalCode")).ToHaveValueAsync("P2");
            await Expect(Loc("#ShipCountry")).ToHaveValueAsync("Us");
            Assert.That(dialogs, Is.Empty, "2 回目も確認ダイアログが出ない");
        }

        [Test, Basis("EV-ORD-302"), Basis("DEC-ORD-32"), Basis("DEC-ORD-33")]
        public async Task 顧客選択_手入力があれば確認して承認なら上書き()
        {
            Ctx c = NewCtx();
            acceptDialog = true;

            await OpenNewAsync();
            await SetAsync("#ShipName", "手入力");
            await SelectAsync("#CustomerID", c.K);

            await Expect(Loc("#ShipName")).ToHaveValueAsync(data.CustomerNames[c.K]);
            await Expect(Loc("#customerPhone")).ToHaveTextAsync("03-1111-1111");
            Assert.That(dialogs.Count, Is.EqualTo(1), "確認ダイアログが 1 回出る（文言は期待にしない）");
        }

        [Test, Basis("EV-ORD-302"), Basis("DEC-ORD-32"), Basis("DEC-ORD-33")]
        public async Task 顧客選択_手入力があれば確認して取り消しなら上書きしない()
        {
            Ctx c = NewCtx();
            acceptDialog = false;

            await OpenNewAsync();
            await SetAsync("#ShipName", "手入力");
            await SelectAsync("#CustomerID", c.K);

            await Expect(Loc("#customerPhone")).ToHaveTextAsync("03-1111-1111");   // 顧客情報は変わる（手順 3 は上書きと別）
            await Expect(Loc("#ShipName")).ToHaveValueAsync("手入力");
            await Expect(Loc("#ShipAddress")).ToHaveValueAsync("");
            Assert.That(dialogs.Count, Is.EqualTo(1), "確認ダイアログが 1 回出た");
        }

        [Test, Basis("EV-ORD-302")]
        public async Task 顧客選択_空に戻すと顧客情報だけ消える()
        {
            Ctx c = NewCtx();

            await OpenNewAsync();
            await SelectAsync("#CustomerID", c.K);
            await Expect(Loc("#ShipName")).ToHaveValueAsync(data.CustomerNames[c.K]);
            await SelectAsync("#CustomerID", "");

            await Expect(Loc("#customerPhone")).ToHaveTextAsync(Empty);
            await Expect(Loc("#customerAddress")).ToHaveTextAsync(Empty);
            await Expect(Loc("#ShipName")).ToHaveValueAsync(data.CustomerNames[c.K]);
            Assert.That(dialogs, Is.Empty);
        }

        [Test, Basis("EV-ORD-302")]
        public async Task 顧客選択_編集では出荷先を変えない()
        {
            Ctx c = NewCtx();
            int o = NewEditOrder(c);

            await OpenEditAsync(o);
            await SelectAsync("#CustomerID", c.K2);

            await Expect(Loc("#customerPhone")).ToHaveTextAsync("03-2222-2222");
            await Expect(Loc("#ShipName")).ToHaveValueAsync("ShipTo Co");
            await Expect(Loc("#ShipAddress")).ToHaveValueAsync("4-5-6 Ginza");
            await Expect(Loc("#ShipCity")).ToHaveValueAsync("Chuo");
            await Expect(Loc("#ShipRegion")).ToHaveValueAsync("Tokyo");
            await Expect(Loc("#ShipPostalCode")).ToHaveValueAsync("104-0061");
            await Expect(Loc("#ShipCountry")).ToHaveValueAsync("Japan");
            Assert.That(dialogs, Is.Empty);
        }

        [Test, Basis("EV-ORD-302"), Basis("ERR-NOTFOUND"), Basis("DEC-ORD-40")]
        public async Task 顧客選択_他の利用者が消した顧客はERR_NOTFOUNDで一覧へ()
        {
            string k2 = data.NewCustomer('L');
            string k1 = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k2, 25, Start2198);
            await ListSearchAsync(k2);
            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await ClickAsync("#btnNew");
            await Expect(Loc("#CustomerID")).ToBeVisibleAsync();
            await Expect(Loc("#CustomerID option[value='" + k1 + "']")).ToHaveCountAsync(1);

            TestData.DeleteCustomer(k1);
            await SelectAsync("#CustomerID", k1);

            await ExpectNotFoundListAsync();
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k2);
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 25 件");
            await ExpectListAsync(Range(ids, 5, 1));
        }

        // ================================================================ 送料（EV-ORD-303）

        /// <summary>登録を開き、行 1 に P（単価 10.00）を数量 2 で入れた状態（小計 20.00）</summary>
        private async Task OpenNewWithLineAsync(Ctx c)
        {
            await OpenNewAsync();
            await LineSelectAsync(0, c.P1);
            await LineSetAsync(0, "line-quantity", "2");
            await Expect(Loc("#subtotal")).ToHaveTextAsync("20.00");
        }

        [Test, Basis("EV-ORD-303"), Basis("共通仕様 3 節")]
        public async Task 送料_変更で合計が変わる()
        {
            Ctx c = NewCtx();
            await OpenNewWithLineAsync(c);

            await SetAsync("#Freight", "5.5");

            await Expect(Loc("#total")).ToHaveTextAsync("25.50");
            await Expect(Loc("#subtotal")).ToHaveTextAsync("20.00");
            await Expect(Loc("#FreightError")).ToHaveTextAsync(Empty);
        }

        [Test, Basis("EV-ORD-303"), Basis("VAL-NUMERIC"), Basis("DEC-CMN-05")]
        [TestCase("-1")]
        [TestCase("abc")]
        public async Task 送料_不正値はエラーで合計を変えない(string text)
        {
            Ctx c = NewCtx();
            await OpenNewWithLineAsync(c);
            await SetAsync("#Freight", "5.5");
            await Expect(Loc("#total")).ToHaveTextAsync("25.50");

            await SetAsync("#Freight", text);

            await Expect(Loc("#FreightError")).ToHaveTextAsync(NonEmpty);
            await Expect(Loc("#FreightError")).ToHaveClassAsync(Danger);
            await Expect(Loc("#total")).ToHaveTextAsync("25.50");
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
        }

        // ================================================================ 明細の追加・削除（EV-ORD-304・305）

        [Test, Basis("EV-ORD-304"), Basis("SC-ORD-03-B01"), Basis("DEC-ORD-39")]
        public async Task 明細行追加_空行が足され既存の入力は残る()
        {
            Ctx c = NewCtx();
            await OpenNewAsync();
            await LineSelectAsync(0, c.P1);
            await LineSetAsync(0, "line-quantity", "3");

            await ClickAsync("#btnAddLine");

            await Expect(Lines).ToHaveCountAsync(2);
            await Expect(Row(1).Locator(".line-product")).ToHaveValueAsync("");
            await Expect(Row(1).Locator(".line-discount")).ToHaveValueAsync(NumRe(0m));
            await Expect(Row(1).Locator(".line-quantity")).ToHaveValueAsync(NumRe(1m));
            await Expect(Row(1).Locator(".line-unitprice")).ToHaveValueAsync("");
            await Expect(Row(0).Locator(".line-product")).ToHaveValueAsync(c.P1.ToString());
            await Expect(Row(0).Locator(".line-quantity")).ToHaveValueAsync("3");
            await Expect(Row(0).Locator(".line-remove")).ToBeEnabledAsync();
            await Expect(Row(1).Locator(".line-remove")).ToBeEnabledAsync();
        }

        [Test, Basis("EV-ORD-305"), Basis("SC-ORD-03-B02")]
        public async Task 明細行削除_行が減り金額が再計算される()
        {
            Ctx c = NewCtx();
            await OpenNewAsync();
            await LineSelectAsync(0, c.P1);
            await LineSetAsync(0, "line-quantity", "2");
            await ClickAsync("#btnAddLine");
            await Expect(Lines).ToHaveCountAsync(2);
            await LineSelectAsync(1, c.P2);
            await Expect(Loc("#subtotal")).ToHaveTextAsync("40.00");

            await ClickAsync("#lineList tbody tr:nth-child(1) .line-remove");

            await Expect(Lines).ToHaveCountAsync(1);
            await Expect(Row(0).Locator(".line-product")).ToHaveValueAsync(c.P2.ToString());
            await Expect(Loc("#subtotal")).ToHaveTextAsync("20.00");
            await Expect(Row(0).Locator(".line-remove")).ToBeDisabledAsync();
        }

        [Test, Basis("EV-ORD-305"), Basis("SC-ORD-03-B02")]
        public async Task 明細行削除_1行のときは削除できない()
        {
            NewCtx();

            await OpenNewAsync();

            await Expect(Lines).ToHaveCountAsync(1);
            await Expect(Row(0).Locator(".line-remove")).ToBeDisabledAsync();
        }

        // ================================================================ 商品選択（EV-ORD-306）

        [Test, Basis("EV-ORD-306"), Basis("OD-T2"), Basis("共通仕様 3 節")]
        public async Task 商品選択_単価と在庫と金額が反映される()
        {
            int p = data.NewProduct(mark: 'A', unitPrice: 12.50m, unitsInStock: 20);
            await OpenNewAsync();

            await LineSelectAsync(0, p);

            await Expect(Row(0).Locator(".line-unitprice")).ToHaveValueAsync(NumRe(12.50m));
            await Expect(Row(0).Locator(".line-stock")).ToHaveTextAsync("20");
            await Expect(Row(0).Locator(".line-price")).ToHaveTextAsync("12.50");
            await Expect(Row(0).Locator(".line-amount")).ToHaveTextAsync("12.50");
            await Expect(Loc("#subtotal")).ToHaveTextAsync("12.50");
            await Expect(Loc("#total")).ToHaveTextAsync("12.50");
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Warning);
        }

        [Test, Basis("EV-ORD-306"), Basis("共通仕様 4 節")]
        public async Task 商品選択_在庫がNULLの商品は在庫を空にする()
        {
            int p = data.NewProduct(mark: 'A', unitPrice: 12.50m, unitsInStock: null);
            await OpenNewAsync();

            await LineSelectAsync(0, p);

            await Expect(Row(0).Locator(".line-unitprice")).ToHaveValueAsync(NumRe(12.50m));   // 選べている
            await Expect(Row(0).Locator(".line-stock")).ToHaveTextAsync(Empty);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Warning);
        }

        [Test, Basis("EV-ORD-306"), Basis("OD-T1"), Basis("ERR-BIZ")]
        public async Task 商品選択_重複は元に戻してエラーを出す()
        {
            Ctx c = NewCtx();
            await OpenNewAsync();
            await LineSelectAsync(0, c.P1);
            await ClickAsync("#btnAddLine");
            await Expect(Lines).ToHaveCountAsync(2);

            await LineSelectAsync(1, c.P1);

            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgDup);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Row(1).Locator(".line-product")).ToHaveValueAsync("");
            await Expect(Loc("#subtotal")).ToHaveTextAsync("10.00");
        }

        [Test, Basis("EV-ORD-306")]
        public async Task 商品選択_空に戻すと行の値が消える()
        {
            int p = data.NewProduct(mark: 'A', unitPrice: 12.50m, unitsInStock: 20);
            await OpenNewAsync();
            await LineSelectAsync(0, p);
            await LineSetAsync(0, "line-quantity", "2");
            await Expect(Row(0).Locator(".line-amount")).ToHaveTextAsync("25.00");
            await SetAsync("#Freight", "3");
            await Expect(Loc("#total")).ToHaveTextAsync("28.00");

            await Row(0).Locator(".line-product").SelectOptionAsync("");

            await Expect(Row(0).Locator(".line-unitprice")).ToHaveValueAsync("");
            await Expect(Row(0).Locator(".line-stock")).ToHaveTextAsync(Empty);
            await Expect(Row(0).Locator(".line-price")).ToHaveTextAsync(Empty);
            await Expect(Row(0).Locator(".line-amount")).ToHaveTextAsync(Empty);
            await Expect(Loc("#subtotal")).ToHaveTextAsync("0.00");
            await Expect(Loc("#total")).ToHaveTextAsync("3.00");
        }

        [Test, Basis("EV-ORD-306"), Basis("ERR-NOTFOUND"), Basis("DEC-ORD-40")]
        public async Task 商品選択_他の利用者が消した商品はERR_NOTFOUNDで一覧へ()
        {
            int p = data.NewProduct(mark: 'A', unitPrice: 12.50m);
            await OpenNewAsync();
            await Expect(Row(0).Locator(".line-product option[value='" + p + "']")).ToHaveCountAsync(1);

            data.DeleteProduct(p);
            await LineSelectAsync(0, p);

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgNotFound);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
        }

        // ================================================================ 在庫超過・再計算（EV-ORD-306・307）

        [Test, Basis("EV-ORD-306"), Basis("EV-ORD-307"), Basis("共通仕様 4 節"), Basis("DEC-ORD-44")]
        public async Task 在庫超過_警告が出て入力は妨げない()
        {
            int p = data.NewProduct(mark: 'A', unitPrice: 10.00m, unitsInStock: 5);
            await OpenNewAsync();
            await LineSelectAsync(0, p);

            await LineSetAsync(0, "line-quantity", "6");

            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgStock);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Warning);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Danger);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Info);
            await Expect(Row(0).Locator(".line-quantity")).ToHaveValueAsync("6");
        }

        [Test, Basis("EV-ORD-307"), Basis("共通仕様 3 節"), Basis("DEC-ORD-46")]
        public async Task 明細の再計算_単価_割引率_数量の変更()
        {
            int p = data.NewProduct(mark: 'A', unitPrice: 0.50m, unitsInStock: 100);
            await OpenNewAsync();
            await LineSelectAsync(0, p);
            await Expect(Row(0).Locator(".line-unitprice")).ToHaveValueAsync(NumRe(0.50m));

            await LineSetAsync(0, "line-unitprice", "0.50");
            await LineSetAsync(0, "line-discount", "0.15");
            await LineSetAsync(0, "line-quantity", "3");

            await Expect(Row(0).Locator(".line-price")).ToHaveTextAsync("0.43");
            await Expect(Row(0).Locator(".line-amount")).ToHaveTextAsync("1.28");
            await Expect(Loc("#subtotal")).ToHaveTextAsync("1.28");

            await LineSetAsync(0, "line-quantity", "1");
            await LineSetAsync(0, "line-discount", "0");
            await LineSetAsync(0, "line-unitprice", "100");
            await Expect(Row(0).Locator(".line-amount")).ToHaveTextAsync("100.00");
        }

        [Test, Basis("EV-ORD-307"), Basis("VAL-NUMERIC"), Basis("DEC-CMN-05")]
        [TestCase("line-unitprice", "-1")]
        [TestCase("line-discount", "1.5")]
        [TestCase("line-quantity", "0")]
        public async Task 明細の再計算_不正値は項目にエラーを出して再計算しない(string cls, string text)
        {
            Ctx c = NewCtx();
            await OpenNewAsync();
            await LineSelectAsync(0, c.P1);
            await LineSetAsync(0, "line-quantity", "2");
            await Expect(Row(0).Locator(".line-amount")).ToHaveTextAsync("20.00");

            await LineSetAsync(0, cls, text);

            await Expect(Row(0).Locator("." + cls + "-error")).ToHaveTextAsync(NonEmpty);
            await Expect(Row(0).Locator("." + cls + "-error")).ToHaveClassAsync(Danger);
            await Expect(Row(0).Locator(".line-amount")).ToHaveTextAsync("20.00");
            await Expect(Loc("#subtotal")).ToHaveTextAsync("20.00");
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
        }

        // ================================================================ 編集の表示（EV-ORD-301）

        [Test, Basis("EV-ORD-301"), Basis("SC-ORD-03-I01"), Basis("SC-ORD-03-I17"), Basis("SC-ORD-03-I18")]
        public async Task 編集_読み込み()
        {
            Ctx c = NewCtx();
            int pa = data.NewProduct(mark: 'A', unitsInStock: 5);
            int pm = data.NewProduct(mark: 'M', unitsInStock: 17);
            int px = data.NewProduct(mark: 'X', unitsInStock: 0);
            int o = data.NewOrder(customerId: c.K, employeeId: c.E, orderDate: new DateTime(2198, 3, 5), requiredDate: new DateTime(2198, 4, 1),
                shipVia: c.S, freight: 34.50m, shipName: "ShipTo Co", shipAddress: "4-5-6 Ginza", shipCity: "Chuo", shipRegion: "Tokyo",
                shipPostalCode: "104-0061", shipCountry: "Japan");
            data.NewDetail(o, pm, 50.00m, 2, 0.25f);   // M・X・A の順に登録する
            data.NewDetail(o, px, 20.00m, 3, 0.05f);
            data.NewDetail(o, pa, 100.00m, 12, 0.15f);

            await OpenEditAsync(o);

            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(c.K);
            await Expect(Loc("#EmployeeID")).ToHaveValueAsync(c.E.ToString());
            await Expect(Loc("#ShipVia")).ToHaveValueAsync(c.S.ToString());
            await Expect(Loc("#OrderDate")).ToHaveValueAsync("2198/03/05");
            await Expect(Loc("#RequiredDate")).ToHaveValueAsync("2198/04/01");
            await Expect(Loc("#Freight")).ToHaveValueAsync(NumRe(34.50m));
            await Expect(Loc("#ShipName")).ToHaveValueAsync("ShipTo Co");
            await Expect(Loc("#ShipAddress")).ToHaveValueAsync("4-5-6 Ginza");
            await Expect(Loc("#ShipCity")).ToHaveValueAsync("Chuo");
            await Expect(Loc("#ShipRegion")).ToHaveValueAsync("Tokyo");
            await Expect(Loc("#ShipPostalCode")).ToHaveValueAsync("104-0061");
            await Expect(Loc("#ShipCountry")).ToHaveValueAsync("Japan");
            await Expect(Loc("#customerPhone")).ToHaveTextAsync("03-1111-1111");
            await Expect(Lines).ToHaveCountAsync(3);
            int[] prods = { pa, pm, px };
            string[] unit = { "100", "50", "20" };
            string[] disc = { "0.15", "0.25", "0.05" };
            string[] qty = { "12", "2", "3" };
            string[] price = { "85.00", "37.50", "19.00" };
            string[] amount = { "1,020.00", "75.00", "57.00" };
            for (int i = 0; i < 3; i++)
            {
                await Expect(Row(i).Locator(".line-product")).ToHaveValueAsync(prods[i].ToString());
                await Expect(Row(i).Locator(".line-unitprice")).ToHaveValueAsync(NumRe(decimal.Parse(unit[i], System.Globalization.CultureInfo.InvariantCulture)));
                await Expect(Row(i).Locator(".line-discount")).ToHaveValueAsync(NumRe(decimal.Parse(disc[i], System.Globalization.CultureInfo.InvariantCulture)));
                await Expect(Row(i).Locator(".line-quantity")).ToHaveValueAsync(qty[i]);
                await Expect(Row(i).Locator(".line-price")).ToHaveTextAsync(price[i]);
                await Expect(Row(i).Locator(".line-amount")).ToHaveTextAsync(amount[i]);
                await Expect(Row(i).Locator(".line-remove")).ToBeEnabledAsync();
            }
            await Expect(Loc("#subtotal")).ToHaveTextAsync("1,152.00");
            await Expect(Loc("#total")).ToHaveTextAsync("1,186.50");
            await Expect(Loc("#btnMain1")).ToBeEnabledAsync();
        }

        [Test, Basis("EV-ORD-301"), Basis("OD-T3")]
        public async Task 編集_廃番の既存明細は選択状態で残り他の行の選択肢には無い()
        {
            Ctx c = NewCtx();
            int p = data.NewProduct(mark: 'A', unitPrice: 10.00m);
            int q = data.NewProduct(mark: 'B', discontinued: true, unitPrice: 5.00m);
            int o = NewEditOrder(c);
            data.NewDetail(o, p, 10.00m, 1, 0f);
            data.NewDetail(o, q, 5.00m, 1, 0f);

            await OpenEditAsync(o);

            await Expect(Lines).ToHaveCountAsync(2);
            await Expect(Row(0).Locator(".line-product")).ToHaveValueAsync(p.ToString());   // 商品名の昇順（P < Q）
            await Expect(Row(1).Locator(".line-product")).ToHaveValueAsync(q.ToString());
            await Expect(Row(1).Locator(".line-product option[value='" + q + "']")).ToHaveCountAsync(1);
            await Expect(Row(0).Locator(".line-product option[value='" + q + "']")).ToHaveCountAsync(0);
        }

        [Test, Basis("EV-ORD-301"), Basis("ERR-NOTFOUND")]
        public async Task 編集_存在しない受注はERR_NOTFOUNDで一覧へ()
        {
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 5));
            TestData.DeleteOrder(o);

            await OpenEditAsync(o);

            await ExpectNotFoundListAsync();
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Info);
        }

        [Test, Basis("EV-ORD-301"), Basis("ORD-T4"), Basis("ERR-BIZ")]
        public async Task 編集_出荷済は編集させず詳細へ戻す()
        {
            int o = data.NewOrder(orderDate: new DateTime(2198, 3, 5), shippedDate: new DateTime(2198, 3, 10));

            await OpenEditAsync(o);

            await Expect(Loc("#detailList")).ToBeVisibleAsync();
            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgShipped);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
        }

        // ================================================================ 詳細の「編集」（EV-ORD-202）

        [Test, Basis("EV-ORD-202"), Basis("SC-ORD-02-B01")]
        public async Task 詳細の編集_編集画面へ遷移する()
        {
            Ctx c = NewCtx();
            int o = NewEditOrder(c);
            await OpenDetailAsync(o);

            await ClickAsync("#btnEdit");

            await Expect(page).ToHaveURLAsync(new Regex(@"/OrdTf/Edit\?(?=.*\bmode=edit\b)(?=.*\bid=" + o + @"\b)"));
            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(c.K);
        }

        [Test, Basis("EV-ORD-202"), Basis("ORD-T4"), Basis("ERR-BIZ")]
        public async Task 詳細の編集_表示後に出荷確定されたら遷移せずエラー()
        {
            Ctx c = NewCtx();
            int o = NewEditOrder(c);
            await OpenDetailAsync(o);
            await Expect(Loc("#btnEdit")).ToBeEnabledAsync();

            TestData.ShipOrder(o);
            await page.ClickAsync("#btnEdit");

            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgShippedFromDetail);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(Loc("#CustomerID")).ToHaveCountAsync(0);
        }

        // ================================================================ 保存（EV-ORD-308）

        /// <summary>登録を開いて、K・E・S と日付・送料・明細 2 行を入れて保存する。採番された受注 ID（詳細の #orderId）を返す</summary>
        private async Task<int> RegisterAsync(Ctx c)
        {
            await OpenNewAsync();
            await SelectAsync("#CustomerID", c.K);
            await SelectAsync("#EmployeeID", c.E.ToString());
            await SelectAsync("#ShipVia", c.S.ToString());
            await SetAsync("#OrderDate", "2198/03/05");
            await SetAsync("#RequiredDate", "2198/04/01");
            await SetAsync("#Freight", "10");
            await LineSelectAsync(0, c.P1);
            await LineSetAsync(0, "line-unitprice", "10.00");
            await LineSetAsync(0, "line-discount", "0.15");
            await LineSetAsync(0, "line-quantity", "2");
            await ClickAsync("#btnAddLine");
            await Expect(Lines).ToHaveCountAsync(2);
            await LineSelectAsync(1, c.P2);
            await ClickAsync("#btnMain1");
            await Expect(Loc("#detailList")).ToBeVisibleAsync();
            string id = await Loc("#orderId").InnerTextAsync();
            return int.Parse(id.Trim());
        }

        [Test, Basis("EV-ORD-308"), Basis("SC-ORD-02"), Basis("DEC-ORD-35")]
        public async Task 保存_登録に成功すると詳細へ遷移する()
        {
            Ctx c = NewCtx();

            int o = await RegisterAsync(c);

            await Expect(page).ToHaveURLAsync(new Regex(@"/OrdTf/Detail\?.*\bid=" + o + @"\b"));
            await Expect(Loc("#messageArea")).ToHaveTextAsync("受注 ID " + o + " を保存しました。");
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Info);
            await Expect(Loc("#messageArea")).Not.ToHaveClassAsync(Danger);
            await Expect(Loc("#customerName")).ToHaveTextAsync(data.CustomerNames[c.K]);
            await Expect(Loc("#orderDate")).ToHaveTextAsync("2198/03/05");
            await Expect(Loc("#shipName")).ToHaveTextAsync(data.CustomerNames[c.K]);
            await Expect(DetailRows).ToHaveCountAsync(2);
            await Expect(Loc("#freight")).ToHaveTextAsync("10.00");
            Assert.That(TestData.CountOrdersOfCustomer(c.K), Is.EqualTo(1));
            Assert.That(TestData.CountDetails(o), Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-308"), Basis("EV-ORD-201")]
        public async Task 保存_登録の後に詳細を再読み込みしても二重登録しない()
        {
            Ctx c = NewCtx();
            int o = await RegisterAsync(c);
            await Expect(Loc("#messageArea")).ToHaveTextAsync("受注 ID " + o + " を保存しました。");

            await page.ReloadAsync();

            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(DetailRows).ToHaveCountAsync(2);
            Assert.That(TestData.CountOrdersOfCustomer(c.K), Is.EqualTo(1), "二重登録しない");
        }

        [Test, Basis("EV-ORD-308"), Basis("ERR-VALIDATION"), Basis("DEC-CMN-04"), Basis("DEC-CMN-05")]
        public async Task 保存_検証エラーは項目の直下と共通メッセージ領域に出て画面に留まる()
        {
            Ctx c = NewCtx();
            await OpenNewAsync();
            await SelectAsync("#CustomerID", c.K);
            await SelectAsync("#EmployeeID", c.E.ToString());
            await SetAsync("#OrderDate", "2198/04/02");
            await SetAsync("#RequiredDate", "2198/04/01");
            await SetAsync("#Freight", "-1");
            await SetAsync("#ShipName", "手入力");

            await ClickAsync("#btnMain1");

            await Expect(Loc("#FreightError")).ToHaveTextAsync(NonEmpty);
            await Expect(Loc("#FreightError")).ToHaveClassAsync(Danger);
            await Expect(Row(0).Locator(".line-product-error")).ToHaveTextAsync(NonEmpty);
            await Expect(Row(0).Locator(".line-product-error")).ToHaveClassAsync(Danger);
            await Expect(Loc("#OrderDateError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#RequiredDateError")).ToHaveTextAsync(Empty);
            await Expect(Loc("#messageArea")).ToHaveTextAsync(NonEmpty);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#orderId")).ToHaveTextAsync("（登録時に採番）");
            await Expect(Loc("#ShipName")).ToHaveValueAsync("手入力");
            await Expect(Loc("#OrderDate")).ToHaveValueAsync("2198/04/02");
            Assert.That(TestData.CountOrdersOfCustomer(c.K), Is.EqualTo(0));
        }

        [Test, Basis("EV-ORD-308"), Basis("ORD-T4")]
        public async Task 保存_編集に成功すると詳細へ遷移し最新が出る()
        {
            Ctx c = NewCtx();
            int px = data.NewProduct(mark: 'X', unitPrice: 20.00m);
            int o = NewEditOrder(c);
            data.NewDetail(o, c.P1, 100.00m, 12, 0.15f);   // A
            data.NewDetail(o, c.P2, 50.00m, 2, 0.25f);     // M（名前の昇順で A の次）
            await OpenEditAsync(o);
            await Expect(Lines).ToHaveCountAsync(2);

            await SetAsync("#Freight", "9");
            await SetAsync("#ShipCity", "Osaka");
            await LineSetAsync(1, "line-quantity", "5");
            await ClickAsync("#lineList tbody tr:nth-child(1) .line-remove");   // A の行を削除
            await Expect(Lines).ToHaveCountAsync(1);
            await ClickAsync("#btnAddLine");
            await Expect(Lines).ToHaveCountAsync(2);
            await LineSelectAsync(1, px);
            await LineSetAsync(1, "line-unitprice", "10.00");
            await LineSetAsync(1, "line-discount", "0");
            await LineSetAsync(1, "line-quantity", "1");
            await ClickAsync("#btnMain1");

            await Expect(Loc("#detailList")).ToBeVisibleAsync();
            await Expect(Loc("#messageArea")).ToHaveTextAsync("受注 ID " + o + " を保存しました。");
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Info);
            await Expect(Loc("#freight")).ToHaveTextAsync("9.00");
            await Expect(Loc("#shipCity")).ToHaveTextAsync("Osaka");
            await Expect(DetailRows).ToHaveCountAsync(2);
            await Expect(DetailRows.Nth(0).Locator("td").Nth(4)).ToHaveTextAsync("5");   // M の数量
            await Expect(Loc("#shippedDate")).ToHaveTextAsync(Empty);
            Assert.That(TestData.GetOrder(o).RowVersion, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-308"), Basis("ERR-CONFLICT"), Basis("共通仕様 7 節"), Basis("DEC-ORD-34")]
        public async Task 保存_他の利用者が更新していたらERR_CONFLICTで最新を再表示する()
        {
            Ctx c = NewCtx();
            int o = NewEditOrder(c);
            data.NewDetail(o, c.P1, 10.00m, 1, 0f);
            await OpenEditAsync(o);
            await SetAsync("#ShipCity", "Osaka");
            TestData.BumpOrder(o, 99.00m);

            await ClickAsync("#btnMain1");

            await Expect(Loc("#messageArea")).ToHaveTextAsync(MsgConflict);
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Danger);
            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(Loc("#Freight")).ToHaveValueAsync(NumRe(99m));
            await Expect(Loc("#ShipCity")).ToHaveValueAsync("Chuo");   // 入力（Osaka）を捨てて最新
            Assert.That(TestData.GetOrder(o).ShipCity, Is.EqualTo("Chuo"), "DB は元のまま");

            await SetAsync("#ShipCity", "Kyoto");
            await ClickAsync("#btnMain1");

            await Expect(Loc("#detailList")).ToBeVisibleAsync();
            await Expect(Loc("#messageArea")).ToHaveTextAsync("受注 ID " + o + " を保存しました。");
            await Expect(Loc("#messageArea")).ToHaveClassAsync(Info);
            await Expect(Loc("#shipCity")).ToHaveTextAsync("Kyoto");
            await Expect(Loc("#freight")).ToHaveTextAsync("99.00");
        }

        [Test, Basis("EV-ORD-308"), Basis("ERR-NOTFOUND")]
        public async Task 保存_編集で消されていたらERR_NOTFOUNDで一覧へ()
        {
            Ctx c = NewCtx();
            List<int> ids = data.NewOrdersSequence(c.K, 25, Start2198);
            data.NewDetail(ids[4], c.P1, 10.00m, 1, 0f);
            await ListSearchAsync(c.K);
            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await ListCell(0, 0).Locator("a").ClickAsync();
            await Expect(Loc("#orderId")).ToHaveTextAsync(ids[4].ToString());
            await ClickAsync("#btnEdit");
            await SetAsync("#Freight", "7");

            TestData.DeleteOrder(ids[4]);
            await ClickAsync("#btnMain1");

            await ExpectNotFoundListAsync();
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(c.K);
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 2 ページ");
            await Expect(Loc("#totalCount")).ToHaveTextAsync("全 24 件");
            await ExpectListAsync(Range(ids, 4, 1));
        }

        // ================================================================ キャンセル（EV-ORD-309）

        [Test, Basis("EV-ORD-309"), Basis("画面遷移 F-3")]
        public async Task キャンセル_編集で変更が無ければ確認なしで詳細へ()
        {
            Ctx c = NewCtx();
            int o = NewEditOrder(c);
            await OpenEditAsync(o);
            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());

            await ClickAsync("#btnCancel");

            await Expect(Loc("#detailList")).ToBeVisibleAsync();
            await Expect(Loc("#orderId")).ToHaveTextAsync(o.ToString());
            await Expect(Loc("#messageArea")).ToHaveTextAsync(Empty);
            Assert.That(dialogs, Is.Empty, "確認ダイアログが出ない");
        }

        [Test, Basis("EV-ORD-309"), Basis("画面遷移 F-3"), Basis("DEC-ORD-37")]
        public async Task キャンセル_編集で変更があれば確認し取り消すと何もしない()
        {
            Ctx c = NewCtx();
            int o = NewEditOrder(c);
            await OpenEditAsync(o);
            await SetAsync("#ShipCity", "Osaka");
            acceptDialog = false;

            await ClickAsync("#btnCancel");

            await Expect(Loc("#ShipCity")).ToHaveValueAsync("Osaka");
            Assert.That(dialogs, Is.EqualTo(new List<string> { MsgCancel }));
            Assert.That(TestData.GetOrder(o).ShipCity, Is.EqualTo("Chuo"));
            Assert.That(TestData.GetOrder(o).RowVersion, Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-309")]
        public async Task キャンセル_編集で変更があり承認すると詳細へ戻る()
        {
            Ctx c = NewCtx();
            int o = NewEditOrder(c);
            await OpenEditAsync(o);
            await SetAsync("#ShipCity", "Osaka");
            acceptDialog = false;
            await ClickAsync("#btnCancel");
            await Expect(Loc("#ShipCity")).ToHaveValueAsync("Osaka");
            acceptDialog = true;

            await ClickAsync("#btnCancel");

            await Expect(Loc("#detailList")).ToBeVisibleAsync();
            await Expect(Loc("#shipCity")).ToHaveTextAsync("Chuo");
            Assert.That(TestData.GetOrder(o).ShipCity, Is.EqualTo("Chuo"));
            Assert.That(TestData.GetOrder(o).RowVersion, Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-309"), Basis("画面遷移 F-2"), Basis("画面遷移 F-3")]
        public async Task キャンセル_登録で変更があり承認すると保持した一覧状態の一覧へ戻る()
        {
            string k = data.NewCustomer('K');
            List<int> ids = data.NewOrdersSequence(k, 45, Start2198);
            await ListSearchAsync(k);
            await ClickAsync("#btnPageNext");
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 3 ページ");
            await ClickAsync("#btnNew");
            await SetAsync("#Freight", "5");

            await ClickAsync("#btnCancel");

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            Assert.That(dialogs, Is.EqualTo(new List<string> { MsgCancel }));
            await Expect(Loc("#CustomerID")).ToHaveValueAsync(k);
            await Expect(Loc("#pageInfo")).ToHaveTextAsync("2 / 3 ページ");
            await Expect(ListCell(0, 0)).ToHaveTextAsync(ids[24].ToString());
            Assert.That(TestData.CountOrdersOfCustomer(k), Is.EqualTo(45));
        }

        [Test, Basis("EV-ORD-309")]
        public async Task キャンセル_登録で変更が無ければ確認なしで一覧へ()
        {
            string k = data.NewCustomer('K');
            data.NewOrder(customerId: k, orderDate: new DateTime(2198, 3, 5));
            await page.GotoAsync("/OrdTf");
            await ClickAsync("#btnNew");

            await ClickAsync("#btnCancel");

            await Expect(Loc("#orderList")).ToBeVisibleAsync();
            Assert.That(dialogs, Is.Empty, "確認ダイアログが出ない");
        }
    }
}
