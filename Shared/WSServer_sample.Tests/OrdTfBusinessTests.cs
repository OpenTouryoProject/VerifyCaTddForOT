using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Touryo.Infrastructure.Business.Util;
using Touryo.Infrastructure.Public.Db;

using WSIFType_sample;
using WSServer_sample.Business;

namespace WSServer_sample.Tests
{
    /// <summary>
    /// 受注一覧 OrdTf の B層（OrdTfBusiness）のルート メソッドの単体テスト。
    /// 仕様：docs/spec/test3-testfirst-OrdTf-mvc-tests.md（NorthwindDbSampleSpec@72e156e）の 3 章。
    /// </summary>
    public class OrdTfBusinessTests
    {
        private const string MsgFormat = "受注日は yyyy/MM/dd の形式で指定してください。";
        private const string MsgRelation = "受注日の開始は終了以前を指定してください。";

        private TestData data;

        [SetUp]
        public void SetUp()
        {
            data = new TestData();
        }

        [TearDown]
        public void TearDown()
        {
            // 失敗したテストの後でも消える。親子は子から消す（TestData.CleanUp）
            data.CleanUp();
        }

        // ================================================================ 呼び出し

        /// <summary>DoBusinessLogic の境界で呼ぶ（UOC_ は直接呼ばない）</summary>
        private static OrdTfReturnValue Call(string method, Action<OrdTfParameterValue> fill)
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(
                "OrdTf", "-", method, "SQL", new MyUserInfo("test", "127.0.0.1"));
            fill(pv);
            return (OrdTfReturnValue)new OrdTfBusiness().DoBusinessLogic(pv, DbEnum.IsolationLevelEnum.ReadCommitted);
        }

        private static OrdTfReturnValue Init()
        {
            return Call("OrdTfInit", pv => { });
        }

        private static OrdTfReturnValue Search(Action<OrdTfParameterValue> fill)
        {
            return Call("OrdTfSearch", pv => { pv.PageNo = 1; fill(pv); });
        }

        private static OrdTfReturnValue SearchCustomer(string customerId)
        {
            return Search(pv => pv.CustomerID = customerId);
        }

        private static List<int> Ids(OrdTfReturnValue rv)
        {
            return rv.Rows.Select(r => r.OrderID).ToList();
        }

        private static DateTime D(int y, int m, int d, int h = 0, int mi = 0)
        {
            return new DateTime(y, m, d, h, mi, 0);
        }

        // ================================================================ 3.1 選択肢の構築

        [Test, Basis("EV-ORD-101"), Basis("SC-ORD-01-I01")]
        public void OrdTfInit_顧客の先頭は空白の選択肢()
        {
            data.NewCustomer('A');
            data.NewCustomer('B');

            OrdTfReturnValue rv = Init();

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.CustomerOptions.Count, Is.GreaterThan(0));
            Assert.That(rv.CustomerOptions[0].Value, Is.EqualTo(""));
            Assert.That(rv.CustomerOptions[0].Text, Is.EqualTo(""));
            Assert.That(rv.CustomerOptions.Count(o => o.Value == ""), Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-101"), Basis("SC-ORD-01-I01")]
        public void OrdTfInit_顧客の値と表示と並び()
        {
            // CustomerID の昇順と CompanyName の昇順が逆：K1 は ID が小さく会社名が大きい
            string idSmall, idLarge;
            while (true)
            {
                string a = TestData.NewCustomerId();
                string b = TestData.NewCustomerId();
                if (a == b) continue;
                idSmall = string.CompareOrdinal(a, b) < 0 ? a : b;
                idLarge = idSmall == a ? b : a;
                break;
            }
            string k1 = data.NewCustomer('Z', idSmall);
            string k2 = data.NewCustomer('A', idLarge);

            OrdTfReturnValue rv = Init();

            List<OrdTfOption> opts = rv.CustomerOptions;
            int i1 = opts.FindIndex(o => o.Value == k1);
            int i2 = opts.FindIndex(o => o.Value == k2);
            Assert.That(i1, Is.GreaterThanOrEqualTo(0), "K1 が選択肢に含まれる");
            Assert.That(i2, Is.GreaterThanOrEqualTo(0), "K2 が選択肢に含まれる");
            Assert.That(opts[i1].Text, Is.EqualTo(data.CustomerNames[k1]));
            Assert.That(opts[i2].Text, Is.EqualTo(data.CustomerNames[k2]));
            Assert.That(i2, Is.LessThan(i1), "会社名の昇順（K2 が K1 より前）");
        }

        [Test, Basis("EV-ORD-101"), Basis("SC-ORD-01-I02")]
        public void OrdTfInit_担当者の先頭は空白の選択肢()
        {
            data.NewEmployee(TestData.NewLastName('X'), TestData.NewFirstName('A'));
            data.NewEmployee(TestData.NewLastName('X'), TestData.NewFirstName('B'));
            data.NewEmployee(TestData.NewLastName('Y'), TestData.NewFirstName('C'));

            OrdTfReturnValue rv = Init();

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.EmployeeOptions.Count, Is.GreaterThan(0));
            Assert.That(rv.EmployeeOptions[0].Value, Is.EqualTo(""));
            Assert.That(rv.EmployeeOptions[0].Text, Is.EqualTo(""));
            Assert.That(rv.EmployeeOptions.Count(o => o.Value == ""), Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-101"), Basis("SC-ORD-01-I02")]
        public void OrdTfInit_担当者の値と表示と並び()
        {
            // E1・E2 は姓が同じ（GUID も共有）、E3 の姓はそれより大きい。名は E3 < E2 < E1。ID は E1 < E2 < E3
            string guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            string lastXX = "TfX" + guid;
            string lastYY = "TfY" + guid;
            string g4 = guid.Substring(0, 4);
            string f1 = "C" + g4, f2 = "B" + g4, f3 = "A" + g4;
            int e1 = data.NewEmployee(lastXX, f1);
            int e2 = data.NewEmployee(lastXX, f2);
            int e3 = data.NewEmployee(lastYY, f3);

            OrdTfReturnValue rv = Init();

            List<OrdTfOption> opts = rv.EmployeeOptions;
            int i1 = opts.FindIndex(o => o.Value == e1.ToString());
            int i2 = opts.FindIndex(o => o.Value == e2.ToString());
            int i3 = opts.FindIndex(o => o.Value == e3.ToString());
            Assert.That(i1, Is.GreaterThanOrEqualTo(0), "E1 が選択肢に含まれる");
            Assert.That(i2, Is.GreaterThanOrEqualTo(0), "E2 が選択肢に含まれる");
            Assert.That(i3, Is.GreaterThanOrEqualTo(0), "E3 が選択肢に含まれる");
            Assert.That(opts[i1].Text, Is.EqualTo(lastXX + " " + f1));
            Assert.That(opts[i2].Text, Is.EqualTo(lastXX + " " + f2));
            Assert.That(opts[i3].Text, Is.EqualTo(lastYY + " " + f3));
            Assert.That(i2, Is.LessThan(i1), "同じ姓は名の昇順（E2 が E1 より前）");
            Assert.That(i1, Is.LessThan(i3), "姓の昇順（E1 が E3 より前）");
        }

        // ================================================================ 3.2 抽出条件

        [Test, Basis("EV-ORD-102"), Basis("SC-ORD-01-I01")]
        public void OrdTfSearch_顧客で絞る()
        {
            string a = data.NewCustomer('A');
            string b = data.NewCustomer('B');
            int a1 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 1));
            int a2 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 2));
            data.NewOrder(customerId: b, orderDate: D(2199, 3, 3));

            OrdTfReturnValue rv = SearchCustomer(a);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EquivalentTo(new[] { a1, a2 }));
            Assert.That(rv.TotalCount, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-102"), Basis("SC-ORD-01-I02")]
        public void OrdTfSearch_担当者で絞る()
        {
            string a = data.NewCustomer('A');
            string b = data.NewCustomer('B');
            int e1 = data.NewEmployee(TestData.NewLastName('X'), TestData.NewFirstName('A'));
            int e2 = data.NewEmployee(TestData.NewLastName('Y'), TestData.NewFirstName('B'));
            int o1 = data.NewOrder(customerId: a, employeeId: e1, orderDate: D(2199, 3, 1));
            int o2 = data.NewOrder(customerId: b, employeeId: e1, orderDate: D(2199, 3, 2));
            data.NewOrder(customerId: a, employeeId: e2, orderDate: D(2199, 3, 3));

            OrdTfReturnValue rv = Search(pv => pv.EmployeeID = e1);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EquivalentTo(new[] { o1, o2 }));
            Assert.That(rv.TotalCount, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-102")]
        public void OrdTfSearch_顧客と担当者はAND()
        {
            string a = data.NewCustomer('A');
            string b = data.NewCustomer('B');
            int e1 = data.NewEmployee(TestData.NewLastName('X'), TestData.NewFirstName('A'));
            int e2 = data.NewEmployee(TestData.NewLastName('Y'), TestData.NewFirstName('B'));
            int ae1 = data.NewOrder(customerId: a, employeeId: e1, orderDate: D(2199, 3, 1));
            data.NewOrder(customerId: a, employeeId: e2, orderDate: D(2199, 3, 2));
            data.NewOrder(customerId: b, employeeId: e1, orderDate: D(2199, 3, 3));

            OrdTfReturnValue rv = Search(pv => { pv.CustomerID = a; pv.EmployeeID = e1; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EquivalentTo(new[] { ae1 }));
        }

        [Test, Basis("EV-ORD-102"), Basis("SC-ORD-01-I03")]
        public void OrdTfSearch_受注日の開始を含む()
        {
            string a = data.NewCustomer('A');
            data.NewOrder(customerId: a, orderDate: D(2199, 3, 9, 23, 59));
            int o0 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 10));
            int o1 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 11));

            OrdTfReturnValue rv = Search(pv => { pv.CustomerID = a; pv.OrderDateFrom = "2199/03/10"; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EquivalentTo(new[] { o0, o1 }));
        }

        [Test, Basis("EV-ORD-102"), Basis("SC-ORD-01-I04"), Basis("DEC-ORD-07")]
        public void OrdTfSearch_受注日の終了は当日の時刻付きも含む()
        {
            string a = data.NewCustomer('A');
            int om1 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 9));
            int o0 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 10));
            int o1530 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 10, 15, 30));
            data.NewOrder(customerId: a, orderDate: D(2199, 3, 11));

            OrdTfReturnValue rv = Search(pv => { pv.CustomerID = a; pv.OrderDateTo = "2199/03/10"; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EquivalentTo(new[] { om1, o0, o1530 }));
        }

        [Test, Basis("EV-ORD-102"), Basis("SC-ORD-01-I03"), Basis("SC-ORD-01-I04"), Basis("DEC-ORD-07")]
        public void OrdTfSearch_受注日の開始と終了が同じ日()
        {
            string a = data.NewCustomer('A');
            data.NewOrder(customerId: a, orderDate: D(2199, 3, 9));
            int o0 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 10));
            int o1530 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 10, 15, 30));
            data.NewOrder(customerId: a, orderDate: D(2199, 3, 11));

            OrdTfReturnValue rv = Search(pv =>
            {
                pv.CustomerID = a; pv.OrderDateFrom = "2199/03/10"; pv.OrderDateTo = "2199/03/10";
            });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EquivalentTo(new[] { o0, o1530 }));
        }

        [Test, Basis("EV-ORD-102"), Basis("DEC-ORD-07"), Basis("DEC-ORD-09")]
        public void OrdTfSearch_受注日の終了が上限の日付でも検索できる()
        {
            // 終了日の翌日を作って比べる実装は、9999/12/31 で DateTime の上限を超えて例外になる（その回帰）
            string a = data.NewCustomer('A');
            int o1530 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 10, 15, 30));
            int o11 = data.NewOrder(customerId: a, orderDate: D(2199, 3, 11));

            OrdTfReturnValue rv = null;
            // new Action(...) と書く（net48 の C# 7.3 では () => … が曖昧で CS0121）
            Assert.DoesNotThrow(new Action(() => { rv = Search(pv => { pv.CustomerID = a; pv.OrderDateTo = "9999/12/31"; }); }));

            Assert.That(rv, Is.Not.Null);
            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.ErrorFields, Is.Empty);
            Assert.That(Ids(rv), Is.EquivalentTo(new[] { o1530, o11 }));
        }

        /// <summary>「状況のデータ」の 8 件（顧客 A）。記号 → OrderID</summary>
        private Dictionary<char, int> StatusData(string customerId)
        {
            DateTime od = D(2199, 3, 1);
            DateTime today = DateTime.Today;
            Dictionary<char, int> m = new Dictionary<char, int>();
            m['a'] = data.NewOrder(customerId: customerId, orderDate: od, requiredDate: D(2000, 1, 1), shippedDate: null);
            m['b'] = data.NewOrder(customerId: customerId, orderDate: od, requiredDate: today, shippedDate: null);
            m['c'] = data.NewOrder(customerId: customerId, orderDate: od, requiredDate: D(2199, 12, 31), shippedDate: null);
            m['d'] = data.NewOrder(customerId: customerId, orderDate: od, requiredDate: null, shippedDate: null);
            m['e'] = data.NewOrder(customerId: customerId, orderDate: od, requiredDate: D(2001, 1, 1), shippedDate: D(2001, 1, 5));
            m['f'] = data.NewOrder(customerId: customerId, orderDate: od, requiredDate: D(2001, 1, 1), shippedDate: D(2001, 1, 1));
            m['g'] = data.NewOrder(customerId: customerId, orderDate: od, requiredDate: D(2001, 1, 5), shippedDate: D(2001, 1, 1));
            m['h'] = data.NewOrder(customerId: customerId, orderDate: od, requiredDate: null, shippedDate: D(2001, 1, 1));
            return m;
        }

        [TestCase("すべて", "abcdefgh")]
        [TestCase("未出荷", "abcd")]
        [TestCase("出荷済", "efgh")]
        [TestCase("納期遅延", "ae")]
        [Basis("EV-ORD-102"), Basis("SC-ORD-01-I05")]
        public void OrdTfSearch_出荷状況で絞る(string status, string expected)
        {
            string a = data.NewCustomer('A');
            Dictionary<char, int> m = StatusData(a);

            OrdTfReturnValue rv = Search(pv => { pv.CustomerID = a; pv.ShippedStatus = status; });

            Assert.That(rv.ErrorFlag, Is.False);
            int[] expectedIds = expected.Select(c => m[c]).ToArray();
            Assert.That(Ids(rv), Is.EquivalentTo(expectedIds));
            Assert.That(rv.TotalCount, Is.EqualTo(expectedIds.Length));
        }

        [TestCase(null)]
        [TestCase("")]
        [Basis("EV-ORD-102"), Basis("SC-ORD-01-I05")]
        public void OrdTfSearch_出荷状況が未指定ならすべて(string status)
        {
            string a = data.NewCustomer('A');
            Dictionary<char, int> m = StatusData(a);

            OrdTfReturnValue rv = Search(pv => { pv.CustomerID = a; pv.ShippedStatus = status; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EquivalentTo(m.Values.ToArray()));
        }

        [TestCase("   ", " ")]
        [TestCase(null, null)]
        [TestCase("", "")]
        [Basis("DEC-ORD-09"), Basis("EV-ORD-102")]
        public void OrdTfSearch_空白だけの日付は未指定(string from, string to)
        {
            string a = data.NewCustomer('A');
            data.NewOrder(customerId: a, orderDate: D(2199, 3, 9));
            data.NewOrder(customerId: a, orderDate: D(2199, 3, 10));
            data.NewOrder(customerId: a, orderDate: D(2199, 3, 11));

            OrdTfReturnValue rv = Search(pv => { pv.CustomerID = a; pv.OrderDateFrom = from; pv.OrderDateTo = to; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Count, Is.EqualTo(3));
        }

        [TestCase(true)]
        [TestCase(false)]
        [Basis("EV-ORD-102"), Basis("ORD-T1")]
        public void OrdTfSearch_受注日がNULLの受注は日付の範囲を指定すると含まない(bool fromOnly)
        {
            string a = data.NewCustomer('A');
            int dated = data.NewOrder(customerId: a, orderDate: D(2199, 3, 10));
            data.NewOrder(customerId: a, orderDate: null);

            OrdTfReturnValue rv = Search(pv =>
            {
                pv.CustomerID = a;
                if (fromOnly) pv.OrderDateFrom = "2199/03/01"; else pv.OrderDateTo = "2199/03/31";
            });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EquivalentTo(new[] { dated }));
            Assert.That(rv.TotalCount, Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-102"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_条件なしは全件の先頭ページ()
        {
            // 受注を 21 件入れる（既存データに頼らない）：T = 2199/12/30（全受注で最も新しい）、ほか 20 件 = 2199/12/01〜20
            string a = data.NewCustomer('A');
            for (int day = 1; day <= 20; day++)
                data.NewOrder(customerId: a, orderDate: D(2199, 12, day));
            int t = data.NewOrder(customerId: a, orderDate: D(2199, 12, 30));

            OrdTfReturnValue rv = Search(pv => { });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Count, Is.EqualTo(20));
            Assert.That(rv.Rows[0].OrderID, Is.EqualTo(t));
            Assert.That(rv.TotalPages, Is.GreaterThanOrEqualTo(2));
            Assert.That(rv.PageNo, Is.EqualTo(1));
        }

        // ================================================================ 3.3 並び・行の内容

        [Test, Basis("SC-ORD-01-I06"), Basis("DEC-ORD-07")]
        public void OrdTfSearch_受注日の降順で同日は受注IDの降順_時刻は見ない()
        {
            string a = data.NewCustomer('A');
            int x = data.NewOrder(customerId: a, orderDate: D(2199, 3, 10, 15, 30));
            int y = data.NewOrder(customerId: a, orderDate: D(2199, 3, 10, 9, 0));
            int z = data.NewOrder(customerId: a, orderDate: D(2199, 3, 11));
            int w = data.NewOrder(customerId: a, orderDate: D(2199, 3, 9));

            OrdTfReturnValue rv = SearchCustomer(a);

            Assert.That(Ids(rv), Is.EqualTo(new[] { z, y, x, w }));
        }

        [Test, Basis("DEC-ORD-08"), Basis("ORD-T1")]
        public void OrdTfSearch_受注日がNULLの受注は末尾()
        {
            string a = data.NewCustomer('A');
            int dated = data.NewOrder(customerId: a, orderDate: D(2199, 3, 10));
            int nul = data.NewOrder(customerId: a, orderDate: null);

            OrdTfReturnValue rv = SearchCustomer(a);

            Assert.That(Ids(rv), Is.EqualTo(new[] { dated, nul }));
        }

        [Test, Basis("SC-ORD-01-I06"), Basis("EV-ORD-102")]
        public void OrdTfSearch_行の内容()
        {
            string a = data.NewCustomer('A');
            string last = TestData.NewLastName('L');
            string first = TestData.NewFirstName('F');
            int e1 = data.NewEmployee(last, first);
            int s = data.NewShipper();
            int o = data.NewOrder(customerId: a, employeeId: e1, orderDate: D(2199, 3, 10, 15, 30),
                requiredDate: D(2199, 4, 1), shippedDate: D(2199, 3, 20), shipVia: s);

            OrdTfReturnValue rv = SearchCustomer(a);

            Assert.That(rv.Rows.Count, Is.EqualTo(1));
            OrdTfRow row = rv.Rows[0];
            Assert.That(row.OrderID, Is.EqualTo(o));
            Assert.That(row.CustomerName, Is.EqualTo(data.CustomerNames[a]));
            Assert.That(row.EmployeeName, Is.EqualTo(last + " " + first));
            Assert.That(row.ShipperName, Is.EqualTo(data.ShipperNames[s]));
            Assert.That(row.OrderDate, Is.EqualTo(D(2199, 3, 10, 15, 30)));
            Assert.That(row.RequiredDate, Is.EqualTo(D(2199, 4, 1)));
            Assert.That(row.ShippedDate, Is.EqualTo(D(2199, 3, 20)));
        }

        [Test, Basis("SC-ORD-01-I06"), Basis("EV-ORD-102")]
        public void OrdTfSearch_顧客_担当者_配送業者がNULLでも行は残り名称は空()
        {
            string a = data.NewCustomer('A');
            int e1 = data.NewEmployee(TestData.NewLastName('X'), TestData.NewFirstName('A'));
            int s = data.NewShipper();
            int o1 = data.NewOrder(customerId: null, employeeId: e1, orderDate: D(2199, 3, 1), shipVia: s);
            int o2 = data.NewOrder(customerId: a, employeeId: null, orderDate: D(2199, 3, 2), shipVia: s);
            int o3 = data.NewOrder(customerId: a, employeeId: e1, orderDate: D(2199, 3, 3), shipVia: null);

            OrdTfReturnValue byEmployee = Search(pv => pv.EmployeeID = e1);
            OrdTfReturnValue byCustomer = SearchCustomer(a);

            Assert.That(Ids(byEmployee), Is.EquivalentTo(new[] { o1, o3 }));
            OrdTfRow r1 = byEmployee.Rows.First(r => r.OrderID == o1);
            OrdTfRow r3 = byEmployee.Rows.First(r => r.OrderID == o3);
            Assert.That(r1.CustomerName, Is.Null.Or.Empty);
            Assert.That(r3.ShipperName, Is.Null.Or.Empty);

            Assert.That(Ids(byCustomer), Is.EquivalentTo(new[] { o2, o3 }));
            OrdTfRow r2 = byCustomer.Rows.First(r => r.OrderID == o2);
            Assert.That(r2.EmployeeName, Is.Null.Or.Empty);
        }

        [Test, Basis("SC-ORD-01-I06"), Basis("ORD-T1")]
        public void OrdTfSearch_日付がNULLの列はnull()
        {
            string a = data.NewCustomer('A');
            data.NewOrder(customerId: a, orderDate: null, requiredDate: null, shippedDate: null);

            OrdTfReturnValue rv = SearchCustomer(a);

            Assert.That(rv.Rows.Count, Is.EqualTo(1));
            Assert.That(rv.Rows[0].OrderDate, Is.Null);
            Assert.That(rv.Rows[0].RequiredDate, Is.Null);
            Assert.That(rv.Rows[0].ShippedDate, Is.Null);
        }

        [Test, Basis("EV-ORD-101"), Basis("ORD-T7"), Basis("ORD-T2")]
        public void OrdTfSearch_状況の判定()
        {
            string a = data.NewCustomer('A');
            Dictionary<char, int> m = StatusData(a);

            OrdTfReturnValue rv = SearchCustomer(a);

            Dictionary<char, string> expected = new Dictionary<char, string>
            {
                { 'a', "納期遅延" }, { 'b', "未出荷" }, { 'c', "未出荷" }, { 'd', "未出荷" },
                { 'e', "納期遅延" }, { 'f', "出荷済" }, { 'g', "出荷済" }, { 'h', "出荷済" },
            };
            Assert.That(rv.Rows.Count, Is.EqualTo(8));
            foreach (KeyValuePair<char, string> kv in expected)
            {
                OrdTfRow row = rv.Rows.FirstOrDefault(r => r.OrderID == m[kv.Key]);
                Assert.That(row, Is.Not.Null, "行 " + kv.Key + " が含まれる");
                Assert.That(row.Status, Is.EqualTo(kv.Value), "行 " + kv.Key + " の状況");
            }
        }

        // ================================================================ 3.4 合計金額

        private OrdTfRow SingleRow(string customerId)
        {
            OrdTfReturnValue rv = SearchCustomer(customerId);
            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Count, Is.EqualTo(1));
            return rv.Rows[0];
        }

        [Test, Basis("EV-ORD-102"), Basis("OD-T4")]
        public void OrdTfSearch_合計金額は小計と送料の和()
        {
            string a = data.NewCustomer('A');
            int o = data.NewOrder(customerId: a, orderDate: D(2199, 3, 1), freight: 5.50m);
            data.NewDetail(o, 1, 10.00m, 3, 0f);
            data.NewDetail(o, 2, 20.00m, 2, 0.25f);

            Assert.That(SingleRow(a).TotalAmount, Is.EqualTo(65.50m));
        }

        [Test, Basis("OD-T4"), Basis("EV-ORD-102")]
        public void OrdTfSearch_明細金額ごとに四捨五入して合算する()
        {
            string a = data.NewCustomer('A');
            int o = data.NewOrder(customerId: a, orderDate: D(2199, 3, 1), freight: 10.00m);
            data.NewDetail(o, 1, 0.50m, 1, 0.15f);
            data.NewDetail(o, 2, 0.50m, 1, 0.15f);

            Assert.That(SingleRow(a).TotalAmount, Is.EqualTo(10.86m));
        }

        [Test, Basis("OD-T4"), Basis("EV-ORD-102")]
        public void OrdTfSearch_明細金額の丸めは数量を掛けた後()
        {
            string a = data.NewCustomer('A');
            int o = data.NewOrder(customerId: a, orderDate: D(2199, 3, 1), freight: 0m);
            data.NewDetail(o, 1, 0.50m, 3, 0.15f);

            Assert.That(SingleRow(a).TotalAmount, Is.EqualTo(1.28m));
        }

        [Test, Basis("DEC-CMN-02")]
        public void OrdTfSearch_明細が無い受注の合計は送料だけ()
        {
            string a = data.NewCustomer('A');
            data.NewOrder(customerId: a, orderDate: D(2199, 3, 1), freight: 7.25m);

            Assert.That(SingleRow(a).TotalAmount, Is.EqualTo(7.25m));
        }

        [Test, Basis("DEC-CMN-03")]
        public void OrdTfSearch_送料がNULLなら0として足す()
        {
            string a = data.NewCustomer('A');
            int o = data.NewOrder(customerId: a, orderDate: D(2199, 3, 1), freight: null);
            data.NewDetail(o, 1, 12.50m, 1, 0f);

            Assert.That(SingleRow(a).TotalAmount, Is.EqualTo(12.50m));
        }

        [Test, Basis("DEC-CMN-02"), Basis("DEC-CMN-03")]
        public void OrdTfSearch_明細も送料も無い受注の合計は0()
        {
            string a = data.NewCustomer('A');
            data.NewOrder(customerId: a, orderDate: D(2199, 3, 1), freight: null);

            Assert.That(SingleRow(a).TotalAmount, Is.EqualTo(0m));
        }

        [Test, Basis("EV-ORD-102"), Basis("SC-ORD-01-I07")]
        public void OrdTfSearch_明細が複数でも受注は1行()
        {
            string a = data.NewCustomer('A');
            int o = data.NewOrder(customerId: a, orderDate: D(2199, 3, 1), freight: 0m);
            data.NewDetail(o, 1, 10.00m, 1, 0f);
            data.NewDetail(o, 2, 20.00m, 1, 0f);
            data.NewDetail(o, 3, 30.00m, 1, 0f);

            OrdTfReturnValue rv = SearchCustomer(a);

            Assert.That(rv.Rows.Count, Is.EqualTo(1));
            Assert.That(rv.TotalCount, Is.EqualTo(1));
            Assert.That(rv.Rows[0].TotalAmount, Is.EqualTo(60.00m));
        }

        // ================================================================ 3.5 ページング

        private static readonly DateTime SeqStart = new DateTime(2199, 1, 1);

        [TestCase(20, 1)]
        [TestCase(21, 2)]
        [TestCase(40, 2)]
        [TestCase(45, 3)]
        [Basis("EV-ORD-102"), Basis("SC-ORD-01-I07"), Basis("SC-ORD-01-I08")]
        public void OrdTfSearch_総件数と総ページ数(int n, int pages)
        {
            string a = data.NewCustomer('A');
            data.NewOrdersSequence(a, n, SeqStart);

            OrdTfReturnValue rv = SearchCustomer(a);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(n));
            Assert.That(rv.TotalPages, Is.EqualTo(pages));
        }

        [TestCase(1, 45, 26)]
        [TestCase(2, 25, 6)]
        [TestCase(3, 5, 1)]
        [Basis("EV-ORD-104"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_ページごとの20件(int pageNo, int topI, int bottomI)
        {
            string a = data.NewCustomer('A');
            string b = data.NewCustomer('B');
            List<int> aIds = data.NewOrdersSequence(a, 45, SeqStart);
            data.NewOrdersSequence(b, 3, SeqStart.AddDays(100));   // A のどれよりも新しい

            OrdTfReturnValue rv = Search(pv => { pv.CustomerID = a; pv.PageNo = pageNo; });

            Assert.That(rv.ErrorFlag, Is.False);
            // i 番目の受注 = aIds[i - 1]。新しい順（i の降順）で topI から bottomI まで
            List<int> expected = new List<int>();
            for (int i = topI; i >= bottomI; i--) expected.Add(aIds[i - 1]);
            Assert.That(Ids(rv), Is.EqualTo(expected));
            Assert.That(rv.TotalCount, Is.EqualTo(45));
            Assert.That(rv.TotalPages, Is.EqualTo(3));
            Assert.That(rv.PageNo, Is.EqualTo(pageNo));
        }

        [Test, Basis("EV-ORD-101"), Basis("DEC-ORD-02")]
        public void OrdTfSearch_0件のとき()
        {
            string a = data.NewCustomer('A');

            OrdTfReturnValue rv = SearchCustomer(a);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Count, Is.EqualTo(0));
            Assert.That(rv.TotalCount, Is.EqualTo(0));
            Assert.That(rv.TotalPages, Is.EqualTo(1));
            Assert.That(rv.PageNo, Is.EqualTo(1));
        }

        [Test, Basis("DEC-ORD-06"), Basis("EV-ORD-104")]
        public void OrdTfSearch_移動先が無くなっていたら最終ページ()
        {
            string a = data.NewCustomer('A');
            List<int> aIds = data.NewOrdersSequence(a, 25, SeqStart);

            OrdTfReturnValue rv = Search(pv => { pv.CustomerID = a; pv.PageNo = 3; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.PageNo, Is.EqualTo(2));
            Assert.That(rv.TotalPages, Is.EqualTo(2));
            Assert.That(Ids(rv), Is.EqualTo(new[] { aIds[4], aIds[3], aIds[2], aIds[1], aIds[0] }));
        }

        [Test, Basis("DEC-ORD-06"), Basis("DEC-ORD-02")]
        public void OrdTfSearch_0件で2ページ目を求めたら1ページ目()
        {
            string a = data.NewCustomer('A');

            OrdTfReturnValue rv = Search(pv => { pv.CustomerID = a; pv.PageNo = 2; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.PageNo, Is.EqualTo(1));
            Assert.That(rv.TotalPages, Is.EqualTo(1));
            Assert.That(rv.Rows.Count, Is.EqualTo(0));
        }

        // ================================================================ 3.6 検証

        [TestCase("abc")]
        [TestCase("2199/1/5")]
        [TestCase("2199-01-05")]
        [TestCase("2199/02/30")]
        [Basis("VAL-DATE"), Basis("ERR-VALIDATION"), Basis("DEC-ORD-09"), Basis("DEC-CMN-05")]
        public void OrdTfSearch_開始の形式違反(string from)
        {
            OrdTfReturnValue rv = Search(pv => pv.OrderDateFrom = from);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgFormat));
            Assert.That(rv.ErrorFields, Is.EqualTo(new[] { "OrderDateFrom" }));
        }

        [TestCase("abc")]
        [TestCase("2199/1/5")]
        [TestCase("2199-01-05")]
        [TestCase("2199/02/30")]
        [Basis("VAL-DATE"), Basis("ERR-VALIDATION"), Basis("DEC-ORD-09"), Basis("DEC-CMN-05")]
        public void OrdTfSearch_終了の形式違反(string to)
        {
            OrdTfReturnValue rv = Search(pv => pv.OrderDateTo = to);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgFormat));
            Assert.That(rv.ErrorFields, Is.EqualTo(new[] { "OrderDateTo" }));
        }

        [Test, Basis("DEC-CMN-05"), Basis("DEC-ORD-09")]
        public void OrdTfSearch_開始も終了も形式違反()
        {
            OrdTfReturnValue rv = Search(pv => { pv.OrderDateFrom = "abc"; pv.OrderDateTo = "2199-01-05"; });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgFormat));
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "OrderDateFrom", "OrderDateTo" }));
        }

        [Test, Basis("VAL-DATE"), Basis("EV-ORD-102"), Basis("DEC-CMN-04")]
        public void OrdTfSearch_開始が終了より後()
        {
            OrdTfReturnValue rv = Search(pv => { pv.OrderDateFrom = "2199/03/11"; pv.OrderDateTo = "2199/03/10"; });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgRelation));
            Assert.That(rv.ErrorFields, Is.Empty);
        }

        [Test, Basis("VAL-DATE"), Basis("DEC-ORD-09")]
        public void OrdTfSearch_検証に通る日付は受け付ける()
        {
            string a = data.NewCustomer('A');
            data.NewOrder(customerId: a, orderDate: D(2199, 3, 10));

            OrdTfReturnValue rv = Search(pv =>
            {
                pv.CustomerID = a; pv.OrderDateFrom = "2199/01/05"; pv.OrderDateTo = "2199/12/31";
            });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.ErrorFields, Is.Empty);
            Assert.That(rv.Rows.Count, Is.EqualTo(1));
        }

        // ================================================================ 3.7 並べ替え（EV-ORD-105。テスト仕様 8.2）

        private static readonly DateTime DMid = new DateTime(2199, 3, 10);
        private static readonly DateTime DHigh = new DateTime(2199, 3, 20);
        private static readonly DateTime DLow = new DateTime(2199, 3, 5);
        private static readonly DateTime DBase = new DateTime(2199, 3, 1);

        private static OrdTfReturnValue SortSearch(string customerId, int? employeeId, string column, bool desc, int pageNo = 1)
        {
            return Search(pv =>
            {
                pv.CustomerID = customerId;
                pv.EmployeeID = employeeId;
                pv.SortColumn = column;
                pv.SortDescending = desc;
                pv.PageNo = pageNo;
            });
        }

        /// <summary>o1 = 中、o2 = 高、o3 = 低：昇順 = (o3, o1, o2)、降順 = (o2, o1, o3)</summary>
        private static void AssertMidHighLow(OrdTfReturnValue rv, bool desc, int o1, int o2, int o3)
        {
            Assert.That(rv.ErrorFlag, Is.False);
            int[] expected = desc ? new[] { o2, o1, o3 } : new[] { o3, o1, o2 };
            Assert.That(Ids(rv), Is.EqualTo(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Basis("EV-ORD-105"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_並べ替え_受注ID(bool desc)
        {
            string a = data.NewCustomer('A');
            int o1 = data.NewOrder(customerId: a, orderDate: DBase);
            int o2 = data.NewOrder(customerId: a, orderDate: DBase);
            int o3 = data.NewOrder(customerId: a, orderDate: DBase);

            OrdTfReturnValue rv = SortSearch(a, null, "OrderID", desc);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EqualTo(desc ? new[] { o3, o2, o1 } : new[] { o1, o2, o3 }));
            Assert.That(rv.TotalCount, Is.EqualTo(3));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Basis("EV-ORD-105"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_並べ替え_顧客名(bool desc)
        {
            int e = data.NewEmployee(TestData.NewLastName('E'), TestData.NewFirstName('E'));
            string kM = data.NewCustomer('M');
            string kX = data.NewCustomer('X');
            string kC = data.NewCustomer('C');
            int o1 = data.NewOrder(customerId: kM, employeeId: e, orderDate: DBase);
            int o2 = data.NewOrder(customerId: kX, employeeId: e, orderDate: DBase);
            int o3 = data.NewOrder(customerId: kC, employeeId: e, orderDate: DBase);

            OrdTfReturnValue rv = SortSearch(null, e, "CustomerName", desc);

            AssertMidHighLow(rv, desc, o1, o2, o3);
        }

        [TestCase(false)]
        [TestCase(true)]
        [Basis("EV-ORD-105"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_並べ替え_担当者名(bool desc)
        {
            string a = data.NewCustomer('A');
            int eM = data.NewEmployee(TestData.NewLastName('M'), TestData.NewFirstName('M'));
            int eX = data.NewEmployee(TestData.NewLastName('X'), TestData.NewFirstName('X'));
            int eC = data.NewEmployee(TestData.NewLastName('C'), TestData.NewFirstName('C'));
            int o1 = data.NewOrder(customerId: a, employeeId: eM, orderDate: DBase);
            int o2 = data.NewOrder(customerId: a, employeeId: eX, orderDate: DBase);
            int o3 = data.NewOrder(customerId: a, employeeId: eC, orderDate: DBase);

            OrdTfReturnValue rv = SortSearch(a, null, "EmployeeName", desc);

            AssertMidHighLow(rv, desc, o1, o2, o3);
        }

        [TestCase(false)]
        [TestCase(true)]
        [Basis("DEC-ORD-15"), Basis("EV-ORD-105"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_並べ替え_担当者名は姓の順で同じ姓は名の順(bool desc)
        {
            // E1・E2・E3 は姓が同じ（目印 M。GUID も共有）で名は B・C・A。E4 の姓は小さい（目印 C）で名は Z。ID は E1 < E2 < E3 < E4
            string guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            string g4 = guid.Substring(0, 4);
            string lastM = "TfM" + guid;
            string lastC = "TfC" + guid;
            string a = data.NewCustomer('A');
            int e1 = data.NewEmployee(lastM, "B" + g4);
            int e2 = data.NewEmployee(lastM, "C" + g4);
            int e3 = data.NewEmployee(lastM, "A" + g4);
            int e4 = data.NewEmployee(lastC, "Z" + g4);
            int o1 = data.NewOrder(customerId: a, employeeId: e1, orderDate: DBase);
            int o2 = data.NewOrder(customerId: a, employeeId: e2, orderDate: DBase);
            int o3 = data.NewOrder(customerId: a, employeeId: e3, orderDate: DBase);
            int o4 = data.NewOrder(customerId: a, employeeId: e4, orderDate: DBase);

            OrdTfReturnValue rv = SortSearch(a, null, "EmployeeName", desc);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EqualTo(desc ? new[] { o2, o1, o3, o4 } : new[] { o4, o3, o1, o2 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Basis("EV-ORD-105"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_並べ替え_受注日(bool desc)
        {
            string a = data.NewCustomer('A');
            int o1 = data.NewOrder(customerId: a, orderDate: DMid);
            int o2 = data.NewOrder(customerId: a, orderDate: DHigh);
            int o3 = data.NewOrder(customerId: a, orderDate: DLow);

            OrdTfReturnValue rv = SortSearch(a, null, "OrderDate", desc);

            AssertMidHighLow(rv, desc, o1, o2, o3);
        }

        [TestCase(false)]
        [TestCase(true)]
        [Basis("EV-ORD-105"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_並べ替え_要求納期(bool desc)
        {
            string a = data.NewCustomer('A');
            int o1 = data.NewOrder(customerId: a, orderDate: DBase, requiredDate: DMid);
            int o2 = data.NewOrder(customerId: a, orderDate: DBase, requiredDate: DHigh);
            int o3 = data.NewOrder(customerId: a, orderDate: DBase, requiredDate: DLow);

            OrdTfReturnValue rv = SortSearch(a, null, "RequiredDate", desc);

            AssertMidHighLow(rv, desc, o1, o2, o3);
        }

        [TestCase(false)]
        [TestCase(true)]
        [Basis("EV-ORD-105"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_並べ替え_出荷日(bool desc)
        {
            string a = data.NewCustomer('A');
            int o1 = data.NewOrder(customerId: a, orderDate: DBase, shippedDate: DMid);
            int o2 = data.NewOrder(customerId: a, orderDate: DBase, shippedDate: DHigh);
            int o3 = data.NewOrder(customerId: a, orderDate: DBase, shippedDate: DLow);

            OrdTfReturnValue rv = SortSearch(a, null, "ShippedDate", desc);

            AssertMidHighLow(rv, desc, o1, o2, o3);
        }

        [TestCase(false)]
        [TestCase(true)]
        [Basis("EV-ORD-105"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_並べ替え_配送業者(bool desc)
        {
            string a = data.NewCustomer('A');
            int sM = data.NewShipper(mark: 'M');
            int sX = data.NewShipper(mark: 'X');
            int sC = data.NewShipper(mark: 'C');
            int o1 = data.NewOrder(customerId: a, orderDate: DBase, shipVia: sM);
            int o2 = data.NewOrder(customerId: a, orderDate: DBase, shipVia: sX);
            int o3 = data.NewOrder(customerId: a, orderDate: DBase, shipVia: sC);

            OrdTfReturnValue rv = SortSearch(a, null, "ShipperName", desc);

            AssertMidHighLow(rv, desc, o1, o2, o3);
        }

        [TestCase(null, false)]
        [TestCase("", true)]
        [TestCase(null, true)]
        [Basis("SC-ORD-01-I06"), Basis("EV-ORD-101")]
        public void OrdTfSearch_並べ替えの列が空なら既定の並び(string column, bool desc)
        {
            string a = data.NewCustomer('A');
            int o1 = data.NewOrder(customerId: a, orderDate: DMid);
            int o2 = data.NewOrder(customerId: a, orderDate: DHigh);
            int o3 = data.NewOrder(customerId: a, orderDate: DLow);

            OrdTfReturnValue rv = SortSearch(a, null, column, desc);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EqualTo(new[] { o2, o1, o3 }));
        }

        [Test, Basis("EV-ORD-105"), Basis("EV-ORD-102")]
        public void OrdTfSearch_並べ替えは抽出条件と併用する()
        {
            string a = data.NewCustomer('A');
            string b = data.NewCustomer('B');
            int o1 = data.NewOrder(customerId: a, orderDate: DMid);
            int o2 = data.NewOrder(customerId: a, orderDate: DHigh);
            int o3 = data.NewOrder(customerId: a, orderDate: DLow);
            data.NewOrder(customerId: b, orderDate: D(2199, 3, 15));

            OrdTfReturnValue rv = SortSearch(a, null, "OrderDate", false);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(Ids(rv), Is.EqualTo(new[] { o3, o1, o2 }));
            Assert.That(rv.TotalCount, Is.EqualTo(3));
        }

        [TestCase(1, 1, 20)]
        [TestCase(2, 21, 40)]
        [TestCase(3, 41, 45)]
        [Basis("EV-ORD-104"), Basis("EV-ORD-105")]
        public void OrdTfSearch_並べ替え後のページごとの20件(int pageNo, int topI, int bottomI)
        {
            string a = data.NewCustomer('A');
            // i 番目 = 受注日 SeqStart + i 日。登録は i の降順（ID は i が大きいほど小さい）
            int[] ids = new int[46];
            for (int i = 45; i >= 1; i--)
                ids[i] = data.NewOrder(customerId: a, orderDate: SeqStart.AddDays(i));

            OrdTfReturnValue rv = SortSearch(a, null, "OrderDate", false, pageNo);

            Assert.That(rv.ErrorFlag, Is.False);
            List<int> expected = new List<int>();
            for (int i = topI; i <= bottomI; i++) expected.Add(ids[i]);
            Assert.That(Ids(rv), Is.EqualTo(expected));
            Assert.That(rv.TotalCount, Is.EqualTo(45));
            Assert.That(rv.TotalPages, Is.EqualTo(3));
            Assert.That(rv.PageNo, Is.EqualTo(pageNo));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Basis("EV-ORD-104"), Basis("EV-ORD-105")]
        public void OrdTfSearch_並べ替えで同じ値が続いても全ページで重ならず抜けない(bool desc)
        {
            // 顧客がすべて同じ A＝顧客名がすべて同じ値。同じ値の中の並び順は期待にしない（DEC-ORD-12）
            string a = data.NewCustomer('A');
            List<int> registered = new List<int>();
            for (int i = 1; i <= 45; i++)
                registered.Add(data.NewOrder(customerId: a, orderDate: SeqStart.AddDays(i)));

            List<int> all = new List<int>();
            int[] expectedCounts = { 20, 20, 5 };
            for (int p = 1; p <= 3; p++)
            {
                OrdTfReturnValue rv = SortSearch(a, null, "CustomerName", desc, p);
                Assert.That(rv.ErrorFlag, Is.False);
                Assert.That(rv.Rows.Count, Is.EqualTo(expectedCounts[p - 1]), "ページ " + p + " の件数");
                all.AddRange(Ids(rv));
            }

            Assert.That(all.Distinct().Count(), Is.EqualTo(45), "重複なし");
            Assert.That(all, Is.EquivalentTo(registered), "欠落なし");
        }

        // ================================================================ 受注詳細（OrdTfDetail。EV-ORD-201）・削除（OrdTfDelete。EV-ORD-203）
        // 仕様：docs/spec/test3-testfirst-OrdTfDetail-mvc-core-tests.md（NorthwindDbSampleSpec@ec66d6e）の 3 章。

        private const string MsgNotFound = "対象のデータは存在しません。";
        private const string MsgConflict = "他の利用者によって更新されています。最新の内容を読み込み直してください。";
        private const string Discontinued = "（廃番）";

        private static OrdTfReturnValue Detail(int orderId)
        {
            return Call("OrdTfDetail", pv => pv.OrderID = orderId);
        }

        private static OrdTfReturnValue Delete(int orderId, int rowVersion)
        {
            return Call("OrdTfDelete", pv => { pv.OrderID = orderId; pv.RowVersion = rowVersion; });
        }

        /// <summary>日付の指定：null、"T"（今日）、"T-1"（昨日）、"yyyy/MM/dd"</summary>
        private static DateTime? Day(string s)
        {
            if (s == null) return null;
            if (s == "T") return DateTime.Today;
            if (s == "T-1") return DateTime.Today.AddDays(-1);
            return DateTime.ParseExact(s, "yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        // ---------------------------------------------------------------- 3.1 受注詳細

        [Test]
        [Basis("EV-ORD-201"), Basis("SC-ORD-02-I01"), Basis("SC-ORD-02-I02"), Basis("SC-ORD-02-I03"), Basis("SC-ORD-02-I04"), Basis("SC-ORD-02-I06"),
            Basis("SC-ORD-02-I07"), Basis("SC-ORD-02-I08"), Basis("SC-ORD-02-I09"), Basis("SC-ORD-02-I10"), Basis("EMP-T1")]
        public void OrdTfDetail_受注情報()
        {
            string k = data.NewCustomer('K', phone: "03-1234-5678");
            string last = TestData.NewLastName('L');
            string first = TestData.NewFirstName('F');
            int e = data.NewEmployee(last, first);
            int s = data.NewShipper();
            int o = data.NewOrder(customerId: k, employeeId: e, orderDate: D(2199, 3, 5), requiredDate: D(2199, 4, 1),
                shippedDate: D(2199, 3, 10), shipVia: s, freight: 0m);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.OrderID, Is.EqualTo(o));
            Assert.That(rv.CustomerName, Is.EqualTo(data.CustomerNames[k]));
            Assert.That(rv.CustomerPhone, Is.EqualTo("03-1234-5678"));
            Assert.That(rv.EmployeeName, Is.EqualTo(last + " " + first));
            Assert.That(rv.ShipperName, Is.EqualTo(data.ShipperNames[s]));
            Assert.That(rv.OrderDate, Is.EqualTo(D(2199, 3, 5)));
            Assert.That(rv.RequiredDate, Is.EqualTo(D(2199, 4, 1)));
            Assert.That(rv.ShippedDate, Is.EqualTo(D(2199, 3, 10)));
            Assert.That(rv.Status, Is.EqualTo("出荷済"));
        }

        [Test, Basis("EV-ORD-201"), Basis("SC-ORD-02-I11"), Basis("SC-ORD-02-I12"), Basis("SC-ORD-02-I13"),
            Basis("SC-ORD-02-I14"), Basis("SC-ORD-02-I15"), Basis("SC-ORD-02-I16"), Basis("ORD-T6")]
        public void OrdTfDetail_出荷先情報()
        {
            string k = data.NewCustomer('K', address: "Cust Addr", city: "Cust City", region: "Cust Reg", postalCode: "999-9999", country: "Cust Ctry");
            int o = data.NewOrder(customerId: k, orderDate: D(2199, 3, 5), shipName: "ShipTo Co", shipAddress: "4-5-6 Ginza",
                shipCity: "Chuo", shipRegion: "Tokyo", shipPostalCode: "104-0061", shipCountry: "Japan");

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.ShipName, Is.EqualTo("ShipTo Co"));
            Assert.That(rv.ShipAddress, Is.EqualTo("4-5-6 Ginza"));
            Assert.That(rv.ShipCity, Is.EqualTo("Chuo"));
            Assert.That(rv.ShipRegion, Is.EqualTo("Tokyo"));
            Assert.That(rv.ShipPostalCode, Is.EqualTo("104-0061"));
            Assert.That(rv.ShipCountry, Is.EqualTo("Japan"));
        }

        [Test, Basis("SC-ORD-02-I05"), Basis("DEC-ORD-18")]
        public void OrdTfDetail_顧客住所は空の要素を詰めて連結する()
        {
            // 区切り文字は期待にしない（DEC-ORD-18）。要素が順に並び、区切りが同じ文字列で、空の要素の分の区切りが残らない
            string k0 = data.NewCustomer('K', address: "A1", city: "C1", region: null, postalCode: null, country: "Jp");
            string k1 = data.NewCustomer('K', address: "A1", city: "C1", region: null, postalCode: "", country: "Jp");
            string k2 = data.NewCustomer('K', address: "A1", city: "C1", region: "R1", postalCode: "P1", country: "Jp");
            int o0 = data.NewOrder(customerId: k0, orderDate: D(2199, 3, 5));
            int o1 = data.NewOrder(customerId: k1, orderDate: D(2199, 3, 5));
            int o2 = data.NewOrder(customerId: k2, orderDate: D(2199, 3, 5));

            string a0 = Detail(o0).CustomerAddress;
            string a1 = Detail(o1).CustomerAddress;
            string a2 = Detail(o2).CustomerAddress;

            Assert.That(a0, Is.Not.Null, "K0 の住所が返る");
            Assert.That(a1, Is.Not.Null, "K1 の住所が返る");
            Assert.That(a2, Is.Not.Null, "K2 の住所が返る");
            Assert.That(a0, Does.Match(@"^A1([\s\S]*)C1\1Jp$"), "K0");
            Assert.That(a1, Is.EqualTo(a0), "K1（PostalCode が空文字）は K0（NULL）と同じ");
            Assert.That(a2, Does.Match(@"^A1([\s\S]*)C1\1R1\1P1\1Jp$"), "K2");
        }

        [Test, Basis("EV-ORD-201"), Basis("共通仕様 4 節（空値の表示）"), Basis("ORD-T1")]
        public void OrdTfDetail_顧客_担当者_配送業者が無いと空()
        {
            int o = data.NewOrder(freight: 0m);   // CustomerID・EmployeeID・日付・ShipVia・Ship* がすべて NULL

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.CustomerName, Is.EqualTo(""));
            Assert.That(rv.CustomerPhone, Is.EqualTo(""));
            Assert.That(rv.CustomerAddress, Is.EqualTo(""));
            Assert.That(rv.EmployeeName, Is.EqualTo(""));
            Assert.That(rv.ShipperName, Is.EqualTo(""));
            Assert.That(rv.ShipName, Is.EqualTo(""));
            Assert.That(rv.ShipAddress, Is.EqualTo(""));
            Assert.That(rv.ShipCity, Is.EqualTo(""));
            Assert.That(rv.ShipRegion, Is.EqualTo(""));
            Assert.That(rv.ShipPostalCode, Is.EqualTo(""));
            Assert.That(rv.ShipCountry, Is.EqualTo(""));
            Assert.That(rv.OrderDate, Is.Null);
            Assert.That(rv.RequiredDate, Is.Null);
            Assert.That(rv.ShippedDate, Is.Null);
        }

        [TestCase("2000/01/01", null, "納期遅延")]
        [TestCase("2199/12/31", null, "未出荷")]
        [TestCase("T", null, "未出荷")]
        [TestCase("T-1", null, "納期遅延")]
        [TestCase(null, null, "未出荷")]
        [TestCase("2199/12/31", "2001/01/01", "出荷済")]
        [TestCase("2001/01/01", "2001/01/01", "出荷済")]
        [TestCase("2001/01/01", "2001/01/02", "納期遅延")]
        [TestCase(null, "2001/01/01", "出荷済")]
        [Basis("EV-ORD-201"), Basis("ORD-T7"), Basis("ORD-T2"), Basis("DEC-ORD-27")]
        public void OrdTfDetail_状況の判定(string required, string shipped, string expected)
        {
            int o = data.NewOrder(orderDate: D(2199, 3, 1), requiredDate: Day(required), shippedDate: Day(shipped));

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Status, Is.EqualTo(expected));
        }

        [TestCase(null, true)]
        [TestCase("2001/01/01", false)]
        [Basis("EV-ORD-201"), Basis("ORD-T4"), Basis("SC-ORD-02-B01")]
        public void OrdTfDetail_出荷済は編集不可(string shipped, bool expected)
        {
            int o = data.NewOrder(orderDate: D(2199, 3, 1), shippedDate: Day(shipped));

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.CanEdit, Is.EqualTo(expected));
        }

        [Test]
        [Basis("EV-ORD-201"), Basis("SC-ORD-02-I17"), Basis("SC-ORD-02-I18"), Basis("SC-ORD-02-I19"), Basis("SC-ORD-02-I20"),
            Basis("OD-T4"), Basis("共通仕様 3 節（金額の算出式）")]
        public void OrdTfDetail_明細の項目と並びと金額()
        {
            int pa = data.NewProduct(mark: 'A', unitsInStock: 5);
            int pm = data.NewProduct(mark: 'M', unitsInStock: 17);
            int px = data.NewProduct(mark: 'X', unitsInStock: 0);
            int o = data.NewOrder(orderDate: D(2199, 3, 1), freight: 34.50m);
            data.NewDetail(o, pm, 50.00m, 2, 0.25f);   // M・X・A の順に登録する
            data.NewDetail(o, px, 20.00m, 3, 0.05f);
            data.NewDetail(o, pa, 100.00m, 12, 0.15f);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Lines.Count, Is.EqualTo(3));
            Assert.That(rv.Lines.Select(l => l.ProductName).ToList(),
                Is.EqualTo(new List<string> { data.ProductNames[pa], data.ProductNames[pm], data.ProductNames[px] }), "商品名の昇順（A・M・X）");

            OrdTfDetailLine a = rv.Lines[0], m = rv.Lines[1], x = rv.Lines[2];
            Assert.That(a.UnitPrice, Is.EqualTo(100.00m)); Assert.That(a.Discount, Is.EqualTo(0.15m)); Assert.That(a.Price, Is.EqualTo(85.00m));
            Assert.That(a.Quantity, Is.EqualTo(12)); Assert.That(a.UnitsInStock, Is.EqualTo(5)); Assert.That(a.Amount, Is.EqualTo(1020.00m));
            Assert.That(m.UnitPrice, Is.EqualTo(50.00m)); Assert.That(m.Discount, Is.EqualTo(0.25m)); Assert.That(m.Price, Is.EqualTo(37.50m));
            Assert.That(m.Quantity, Is.EqualTo(2)); Assert.That(m.UnitsInStock, Is.EqualTo(17)); Assert.That(m.Amount, Is.EqualTo(75.00m));
            Assert.That(x.UnitPrice, Is.EqualTo(20.00m)); Assert.That(x.Discount, Is.EqualTo(0.05m)); Assert.That(x.Price, Is.EqualTo(19.00m));
            Assert.That(x.Quantity, Is.EqualTo(3)); Assert.That(x.UnitsInStock, Is.EqualTo(0)); Assert.That(x.Amount, Is.EqualTo(57.00m));
            Assert.That(rv.Subtotal, Is.EqualTo(1152.00m));
            Assert.That(rv.Freight, Is.EqualTo(34.50m));
            Assert.That(rv.Total, Is.EqualTo(1186.50m));
        }

        [Test, Basis("EV-ORD-201"), Basis("SC-ORD-02-I17")]
        public void OrdTfDetail_廃番商品は商品名に付記する()
        {
            int p = data.NewProduct(mark: 'P');
            int q = data.NewProduct(mark: 'Q', discontinued: true);
            int o = data.NewOrder(orderDate: D(2199, 3, 1));
            data.NewDetail(o, p, 10.00m, 1, 0f);
            data.NewDetail(o, q, 10.00m, 1, 0f);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.False);
            OrdTfDetailLine lp = rv.Lines.FirstOrDefault(l => l.ProductName.StartsWith(data.ProductNames[p]));
            OrdTfDetailLine lq = rv.Lines.FirstOrDefault(l => l.ProductName.StartsWith(data.ProductNames[q]));
            Assert.That(lp, Is.Not.Null, "P の行");
            Assert.That(lq, Is.Not.Null, "Q の行");
            Assert.That(lp.ProductName, Is.EqualTo(data.ProductNames[p]));
            Assert.That(lp.ProductName, Does.Not.Contain(Discontinued));
            Assert.That(lq.ProductName, Does.EndWith(Discontinued));
        }

        [Test, Basis("共通仕様 3 節（明細金額は小数第 3 位を四捨五入し、その値を合算）"), Basis("EV-ORD-201")]
        public void OrdTfDetail_明細金額は四捨五入して合算する()
        {
            int p1 = data.NewProduct(mark: 'P');
            int p2 = data.NewProduct(mark: 'Q');
            int o = data.NewOrder(orderDate: D(2199, 3, 1), freight: 10.00m);
            data.NewDetail(o, p1, 0.50m, 1, 0.15f);
            data.NewDetail(o, p2, 0.50m, 1, 0.15f);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.Lines.Count, Is.EqualTo(2));
            foreach (OrdTfDetailLine l in rv.Lines)
            {
                Assert.That(l.Price, Is.EqualTo(0.425m));
                Assert.That(l.Amount, Is.EqualTo(0.43m));
            }
            Assert.That(rv.Subtotal, Is.EqualTo(0.86m));
            Assert.That(rv.Total, Is.EqualTo(10.86m));
        }

        [Test, Basis("共通仕様 3 節（金額の算出式）"), Basis("EV-ORD-201")]
        public void OrdTfDetail_明細金額の丸めは数量を掛けた後()
        {
            int p = data.NewProduct(mark: 'P');
            int o = data.NewOrder(orderDate: D(2199, 3, 1), freight: 0m);
            data.NewDetail(o, p, 0.50m, 3, 0.15f);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.Lines.Count, Is.EqualTo(1));
            Assert.That(rv.Lines[0].Price, Is.EqualTo(0.425m));
            Assert.That(rv.Lines[0].Amount, Is.EqualTo(1.28m));
        }

        [Test, Basis("DEC-CMN-02"), Basis("EV-ORD-201")]
        public void OrdTfDetail_明細が無い受注()
        {
            int o = data.NewOrder(orderDate: D(2199, 3, 1), freight: 7.25m);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Lines.Count, Is.EqualTo(0));
            Assert.That(rv.Subtotal, Is.EqualTo(0m));
            Assert.That(rv.Freight, Is.EqualTo(7.25m));
            Assert.That(rv.Total, Is.EqualTo(7.25m));
        }

        [Test, Basis("DEC-CMN-03"), Basis("共通仕様 4 節（空値の表示）")]
        public void OrdTfDetail_送料がNULLなら0として足す()
        {
            int p = data.NewProduct(mark: 'P');
            int o = data.NewOrder(orderDate: D(2199, 3, 1), freight: null);
            data.NewDetail(o, p, 12.50m, 1, 0f);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Freight, Is.Null);
            Assert.That(rv.Subtotal, Is.EqualTo(12.50m));
            Assert.That(rv.Total, Is.EqualTo(12.50m));
        }

        [Test, Basis("EV-ORD-201"), Basis("共通仕様 9.1 節（既定 1・更新で +1）")]
        public void OrdTfDetail_行バージョン()
        {
            int o = data.NewOrder(orderDate: D(2199, 3, 1));

            Assert.That(Detail(o).RowVersion, Is.EqualTo(1));

            TestData.BumpOrder(o, 1.00m);
            Assert.That(Detail(o).RowVersion, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-201")]
        public void OrdTfDetail_ほかの受注の明細は含めない()
        {
            int p1 = data.NewProduct(mark: 'A');
            int p2 = data.NewProduct(mark: 'M');
            int p3 = data.NewProduct(mark: 'X');
            int o1 = data.NewOrder(orderDate: D(2199, 3, 1), freight: 0m);
            int o2 = data.NewOrder(orderDate: D(2199, 3, 1), freight: 0m);
            data.NewDetail(o1, p1, 10.00m, 1, 0f);
            data.NewDetail(o1, p2, 20.00m, 1, 0f);
            data.NewDetail(o2, p3, 300.00m, 1, 0f);

            OrdTfReturnValue rv = Detail(o1);

            Assert.That(rv.Lines.Count, Is.EqualTo(2));
            Assert.That(rv.Lines.Any(l => l.ProductName.StartsWith(data.ProductNames[p3])), Is.False, "o2 の商品を含まない");
            Assert.That(rv.Subtotal, Is.EqualTo(30.00m));
        }

        [Test, Basis("共通仕様 4 節（空値の表示）"), Basis("SC-ORD-02-I17")]
        public void OrdTfDetail_在庫がNULLの商品()
        {
            int p = data.NewProduct(mark: 'P', unitsInStock: null);
            int o = data.NewOrder(orderDate: D(2199, 3, 1));
            data.NewDetail(o, p, 10.00m, 1, 0f);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.Lines.Count, Is.EqualTo(1));
            Assert.That(rv.Lines[0].UnitsInStock, Is.Null);
        }

        [Test, Basis("EV-ORD-201"), Basis("DEC-ORD-24"), Basis("共通仕様 6 節（DB は妥当性を保証しない）")]
        public void OrdTfDetail_商品マスタに無い明細は含めない()
        {
            int exists = data.NewProduct(mark: 'A');
            int gone = data.NewProduct(mark: 'M');
            int o = data.NewOrder(orderDate: D(2199, 3, 1), freight: 5.00m);
            data.NewDetail(o, exists, 10.00m, 2, 0f);
            data.NewDetail(o, gone, 100.00m, 1, 0f);
            data.DeleteProduct(gone);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Lines.Count, Is.EqualTo(1));
            Assert.That(rv.Lines[0].ProductName, Is.EqualTo(data.ProductNames[exists]));
            Assert.That(rv.Subtotal, Is.EqualTo(20.00m));
            Assert.That(rv.Total, Is.EqualTo(25.00m));
        }

        [Test, Basis("DEC-ORD-25"), Basis("SC-ORD-02-I17"), Basis("共通仕様 3 節（金額の算出式）")]
        public void OrdTfDetail_割引率は端数も0も10進数で返る()
        {
            int pa = data.NewProduct(mark: 'A');
            int pm = data.NewProduct(mark: 'M');
            int o = data.NewOrder(orderDate: D(2199, 3, 1), freight: 0m);
            data.NewDetail(o, pa, 100.00m, 1, 0.125f);
            data.NewDetail(o, pm, 20.00m, 1, 0f);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.Lines.Count, Is.EqualTo(2));
            OrdTfDetailLine a = rv.Lines[0], m = rv.Lines[1];
            Assert.That(a.Discount, Is.EqualTo(0.125m)); Assert.That(a.Price, Is.EqualTo(87.50m)); Assert.That(a.Amount, Is.EqualTo(87.50m));
            Assert.That(m.Discount, Is.EqualTo(0m)); Assert.That(m.Price, Is.EqualTo(20.00m)); Assert.That(m.Amount, Is.EqualTo(20.00m));
        }

        [Test, Basis("EV-ORD-201"), Basis("ERR-NOTFOUND")]
        public void OrdTfDetail_存在しない受注はERR_NOTFOUND()
        {
            int o = data.NewOrder(orderDate: D(2199, 3, 1));
            TestData.DeleteOrder(o);

            OrdTfReturnValue rv = Detail(o);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgNotFound));
        }

        // ---------------------------------------------------------------- 3.2 受注の削除

        [Test, Basis("EV-ORD-203"), Basis("ORD-T8")]
        public void OrdTfDelete_受注と明細が消える()
        {
            int p1 = data.NewProduct(mark: 'A');
            int p2 = data.NewProduct(mark: 'M');
            int o = data.NewOrder(orderDate: D(2199, 3, 1));
            data.NewDetail(o, p1, 10.00m, 1, 0f);
            data.NewDetail(o, p2, 20.00m, 1, 0f);

            OrdTfReturnValue rv = Delete(o, TestData.GetRowVersion(o));

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(TestData.CountOrders(o), Is.EqualTo(0));
            Assert.That(TestData.CountDetails(o), Is.EqualTo(0));
        }

        [Test, Basis("EV-ORD-203")]
        public void OrdTfDelete_ほかの受注と明細は消えない()
        {
            int p1 = data.NewProduct(mark: 'A');
            int p2 = data.NewProduct(mark: 'M');
            int p3 = data.NewProduct(mark: 'X');
            int o1 = data.NewOrder(orderDate: D(2199, 3, 1));
            int o2 = data.NewOrder(orderDate: D(2199, 3, 1), freight: 12.34m);
            data.NewDetail(o1, p1, 10.00m, 1, 0f);
            data.NewDetail(o1, p2, 20.00m, 1, 0f);
            data.NewDetail(o2, p3, 30.00m, 2, 0f);
            OrdTfReturnValue before = Detail(o2);

            OrdTfReturnValue rv = Delete(o1, TestData.GetRowVersion(o1));

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(TestData.CountOrders(o1), Is.EqualTo(0));
            Assert.That(TestData.CountDetails(o1), Is.EqualTo(0));
            Assert.That(TestData.CountOrders(o2), Is.EqualTo(1));
            Assert.That(TestData.CountDetails(o2), Is.EqualTo(1));
            OrdTfReturnValue after = Detail(o2);
            Assert.That(after.Freight, Is.EqualTo(before.Freight));
            Assert.That(after.Subtotal, Is.EqualTo(before.Subtotal));
            Assert.That(after.Lines.Select(l => l.ProductName).ToList(), Is.EqualTo(before.Lines.Select(l => l.ProductName).ToList()));
        }

        [Test, Basis("EV-ORD-203"), Basis("DEC-CMN-02")]
        public void OrdTfDelete_明細が無い受注も消える()
        {
            int o = data.NewOrder(orderDate: D(2199, 3, 1));

            OrdTfReturnValue rv = Delete(o, TestData.GetRowVersion(o));

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(TestData.CountOrders(o), Is.EqualTo(0));
        }

        [Test, Basis("EV-ORD-203"), Basis("ERR-CONFLICT"), Basis("共通仕様 9.1 節")]
        public void OrdTfDelete_行バージョンが違えばERR_CONFLICTでロールバック()
        {
            int p1 = data.NewProduct(mark: 'A');
            int p2 = data.NewProduct(mark: 'M');
            int o = data.NewOrder(orderDate: D(2199, 3, 1), freight: 5.00m);
            data.NewDetail(o, p1, 10.00m, 1, 0f);
            data.NewDetail(o, p2, 20.00m, 1, 0f);
            TestData.BumpOrder(o, 99.00m);   // 行バージョン = 2

            OrdTfReturnValue rv = Delete(o, 1);   // 古い値

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgConflict));
            Assert.That(TestData.CountOrders(o), Is.EqualTo(1));
            Assert.That(TestData.CountDetails(o), Is.EqualTo(2), "明細も消えない（ロールバック）");
            Assert.That(TestData.GetRowVersion(o), Is.EqualTo(2));
            Assert.That(Detail(o).Freight, Is.EqualTo(99.00m));
        }

        [Test, Basis("EV-ORD-203"), Basis("ERR-NOTFOUND"), Basis("共通仕様 9.1 節")]
        public void OrdTfDelete_存在しない受注はERR_NOTFOUND()
        {
            int o = data.NewOrder(orderDate: D(2199, 3, 1));
            TestData.DeleteOrder(o);

            OrdTfReturnValue rv = Delete(o, 1);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgNotFound));
        }

        [Test, Basis("EV-ORD-203"), Basis("ERR-NOTFOUND")]
        public void OrdTfDelete_消した受注をもう一度消すとERR_NOTFOUND()
        {
            int o = data.NewOrder(orderDate: D(2199, 3, 1));

            OrdTfReturnValue first = Delete(o, 1);
            OrdTfReturnValue second = Delete(o, 1);

            Assert.That(first.ErrorFlag, Is.False, "1 回目");
            Assert.That(second.ErrorFlag, Is.True, "2 回目");
            Assert.That(second.ErrorMessage, Is.EqualTo(MsgNotFound));
        }

        // ================================================================ 受注詳細（登録・編集。OrdTfEditInit・OrdTfCustomer・OrdTfProduct・OrdTfSave。EV-ORD-301〜308）
        // 仕様：docs/spec/test3-testfirst-OrdTfEdit-mvc-core-tests.md（NorthwindDbSampleSpec@693e663）の 3 章。

        private const string MsgShipped = "出荷済の受注は編集できません。";
        private const string MsgDup = "同一商品の明細が重複しています。";
        private const string MsgNoLine = "明細を 1 行以上入力してください。";
        private const string MsgDiscontinued = "廃番商品は指定できません。";

        private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

        private static OrdTfReturnValue EditInit(string mode, int orderId = 0)
        {
            return Call("OrdTfEditInit", pv => { pv.Mode = mode; pv.OrderID = orderId; });
        }

        private static OrdTfReturnValue CustomerInfo(string customerId)
        {
            return Call("OrdTfCustomer", pv => pv.CustomerID = customerId);
        }

        private static OrdTfReturnValue ProductInfo(int productId)
        {
            return Call("OrdTfProduct", pv => pv.ProductID = productId);
        }

        /// <summary>保存。成功して採番された受注 ID は後始末のために記録する</summary>
        private OrdTfReturnValue Save(Action<OrdTfParameterValue> fill)
        {
            OrdTfReturnValue rv = Call("OrdTfSave", fill);
            if (rv.OrderID > 0) data.Track(rv.OrderID);
            return rv;
        }

        /// <summary>保存の共通の前提（顧客 K・担当者 E・配送業者 S・商品 P1・P2。ShipName の目印）</summary>
        private sealed class Ctx
        {
            public string K;
            public int E;
            public int S;
            public int P1;
            public int P2;
            public string Mark;
        }

        private Ctx NewCtx()
        {
            Ctx c = new Ctx();
            c.K = data.NewCustomer('K', phone: "03-1234-5678", address: "A1", city: "C1", region: "R1", postalCode: "P1", country: "Jp");
            c.E = data.NewEmployee(TestData.NewLastName('E'), TestData.NewFirstName('F'));
            c.S = data.NewShipper();
            c.P1 = data.NewProduct(mark: 'A', unitPrice: 10.00m, unitsInStock: 100);
            c.P2 = data.NewProduct(mark: 'B', unitPrice: 20.00m, unitsInStock: 100);
            c.Mark = data.NewMark();
            return c;
        }

        private static OrdTfSaveLine NewLine(int productId, string price, string discount, string quantity)
        {
            OrdTfSaveLine l = new OrdTfSaveLine();
            l.ProductIDText = productId.ToString(Inv);
            l.UnitPriceText = price;
            l.DiscountText = discount;
            l.QuantityText = quantity;
            return l;
        }

        /// <summary>有効な入力 V（登録）</summary>
        private static void Valid(Ctx c, OrdTfParameterValue pv)
        {
            pv.Mode = "new";
            pv.CustomerID = c.K;
            pv.EmployeeIDText = c.E.ToString(Inv);
            pv.OrderDateText = "2199/03/05";
            pv.RequiredDateText = "2199/04/01";
            pv.ShipViaText = c.S.ToString(Inv);
            pv.FreightText = "12.50";
            pv.ShipName = c.Mark;
            pv.ShipAddress = "4-5-6 Ginza";
            pv.ShipCity = "Chuo";
            pv.ShipRegion = "Tokyo";
            pv.ShipPostalCode = "104-0061";
            pv.ShipCountry = "Japan";
            pv.SaveLines = new List<OrdTfSaveLine> { NewLine(c.P1, "10.00", "0.15", "2") };
        }

        /// <summary>有効な入力 V から、指定した項目だけを変えて保存する（登録）</summary>
        private OrdTfReturnValue SaveV(Ctx c, Action<OrdTfParameterValue> change)
        {
            return Save(pv => { Valid(c, pv); change(pv); });
        }

        /// <summary>違反があれば DB を更新しない（登録）：OrderID = 0、顧客 K の受注 0 件、目印の出荷先の受注 0 件</summary>
        private static void AssertNothingSaved(Ctx c, OrdTfReturnValue rv, string shipNameMark = null)
        {
            Assert.That(rv.OrderID, Is.EqualTo(0), "検証の違反のとき OrderID は 0");
            Assert.That(TestData.CountOrdersOfCustomer(c.K), Is.EqualTo(0), "顧客 K の受注が入っていない");
            Assert.That(TestData.CountOrdersOfShipName(shipNameMark ?? c.Mark), Is.EqualTo(0), "目印の出荷先の受注が入っていない");
        }

        /// <summary>保存に成功し、受注が入ったこと（戻り値の OrderID &gt; 0 と、GetOrder で読めること）</summary>
        private static TestData.OrderRow AssertSaved(OrdTfReturnValue rv)
        {
            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.OrderID, Is.GreaterThan(0), "戻り値の OrderID > 0");
            TestData.OrderRow row = TestData.GetOrder(rv.OrderID);
            Assert.That(row, Is.Not.Null, "受注が入っている");
            return row;
        }

        // ---------------------------------------------------------------- 3.1 OrdTfEditInit

        [Test, Basis("EV-ORD-301"), Basis("SC-ORD-03-I02")]
        public void OrdTfEditInit_顧客の選択肢()
        {
            string idSmall, idLarge;
            while (true)
            {
                string a = TestData.NewCustomerId();
                string b = TestData.NewCustomerId();
                if (a == b) continue;
                idSmall = string.CompareOrdinal(a, b) < 0 ? a : b;
                idLarge = idSmall == a ? b : a;
                break;
            }
            string k1 = data.NewCustomer('Z', idSmall);
            string k2 = data.NewCustomer('A', idLarge);

            OrdTfReturnValue rv = EditInit("new");

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            List<OrdTfOption> opts = rv.CustomerOptions;
            Assert.That(opts.Count, Is.GreaterThan(0));
            Assert.That(opts[0].Value, Is.EqualTo(""));
            Assert.That(opts[0].Text, Is.EqualTo(""));
            Assert.That(opts.Count(o => o.Value == ""), Is.EqualTo(1));
            int i1 = opts.FindIndex(o => o.Value == k1);
            int i2 = opts.FindIndex(o => o.Value == k2);
            Assert.That(i1, Is.GreaterThanOrEqualTo(0), "K1 が選択肢に含まれる");
            Assert.That(i2, Is.GreaterThanOrEqualTo(0), "K2 が選択肢に含まれる");
            Assert.That(opts[i1].Text, Is.EqualTo(data.CustomerNames[k1]));
            Assert.That(opts[i2].Text, Is.EqualTo(data.CustomerNames[k2]));
            Assert.That(i2, Is.LessThan(i1), "会社名の昇順（K2 が K1 より前）");
        }

        [Test, Basis("EV-ORD-301"), Basis("SC-ORD-03-I05")]
        public void OrdTfEditInit_担当者の選択肢()
        {
            string guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            string lastXX = "TfX" + guid;
            string lastYY = "TfY" + guid;
            string g4 = guid.Substring(0, 4);
            string f1 = "C" + g4, f2 = "B" + g4, f3 = "A" + g4;
            int e1 = data.NewEmployee(lastXX, f1);
            int e2 = data.NewEmployee(lastXX, f2);
            int e3 = data.NewEmployee(lastYY, f3);

            OrdTfReturnValue rv = EditInit("new");

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            List<OrdTfOption> opts = rv.EmployeeOptions;
            Assert.That(opts.Count, Is.GreaterThan(0));
            Assert.That(opts[0].Value, Is.EqualTo(""));
            Assert.That(opts[0].Text, Is.EqualTo(""));
            Assert.That(opts.Count(o => o.Value == ""), Is.EqualTo(1));
            int i1 = opts.FindIndex(o => o.Value == e1.ToString());
            int i2 = opts.FindIndex(o => o.Value == e2.ToString());
            int i3 = opts.FindIndex(o => o.Value == e3.ToString());
            Assert.That(i1, Is.GreaterThanOrEqualTo(0), "E1 が選択肢に含まれる");
            Assert.That(i2, Is.GreaterThanOrEqualTo(0), "E2 が選択肢に含まれる");
            Assert.That(i3, Is.GreaterThanOrEqualTo(0), "E3 が選択肢に含まれる");
            Assert.That(opts[i1].Text, Is.EqualTo(lastXX + " " + f1));
            Assert.That(opts[i2].Text, Is.EqualTo(lastXX + " " + f2));
            Assert.That(opts[i3].Text, Is.EqualTo(lastYY + " " + f3));
            Assert.That(i2, Is.LessThan(i1), "同じ姓は名の昇順（E2 が E1 より前）");
            Assert.That(i1, Is.LessThan(i3), "姓の昇順（E1 が E3 より前）");
        }

        [Test, Basis("EV-ORD-301"), Basis("SC-ORD-03-I08")]
        public void OrdTfEditInit_配送業者の選択肢()
        {
            // S1 は ID が小さく会社名が大きい（会社名の昇順と ID の昇順が逆）
            int s1 = data.NewShipper(mark: 'Z');
            int s2 = data.NewShipper(mark: 'A');

            OrdTfReturnValue rv = EditInit("new");

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            List<OrdTfOption> opts = rv.ShipperOptions;
            Assert.That(opts.Count, Is.GreaterThan(0));
            Assert.That(opts[0].Value, Is.EqualTo(""));
            Assert.That(opts[0].Text, Is.EqualTo(""));
            Assert.That(opts.Count(o => o.Value == ""), Is.EqualTo(1));
            int i1 = opts.FindIndex(o => o.Value == s1.ToString());
            int i2 = opts.FindIndex(o => o.Value == s2.ToString());
            Assert.That(i1, Is.GreaterThanOrEqualTo(0), "S1 が選択肢に含まれる");
            Assert.That(i2, Is.GreaterThanOrEqualTo(0), "S2 が選択肢に含まれる");
            Assert.That(opts[i1].Text, Is.EqualTo(data.ShipperNames[s1]));
            Assert.That(opts[i2].Text, Is.EqualTo(data.ShipperNames[s2]));
            Assert.That(i2, Is.LessThan(i1), "会社名の昇順（S2 が S1 より前）");
        }

        [Test, Basis("EV-ORD-301"), Basis("OD-T3")]
        public void OrdTfEditInit_商品の選択肢は廃番を除く()
        {
            int p1 = data.NewProduct(mark: 'Z');   // ID が小さく商品名が大きい
            int p2 = data.NewProduct(mark: 'A');
            int q = data.NewProduct(mark: 'M', discontinued: true);

            OrdTfReturnValue rv = EditInit("new");

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            List<OrdTfOption> opts = rv.ProductOptions;
            Assert.That(opts.Count, Is.GreaterThan(0));
            Assert.That(opts[0].Value, Is.EqualTo(""));
            Assert.That(opts[0].Text, Is.EqualTo(""));
            Assert.That(opts.Count(o => o.Value == ""), Is.EqualTo(1));
            int i1 = opts.FindIndex(o => o.Value == p1.ToString());
            int i2 = opts.FindIndex(o => o.Value == p2.ToString());
            Assert.That(i1, Is.GreaterThanOrEqualTo(0), "P1 が選択肢に含まれる");
            Assert.That(i2, Is.GreaterThanOrEqualTo(0), "P2 が選択肢に含まれる");
            Assert.That(opts[i1].Text, Is.EqualTo(data.ProductNames[p1]));
            Assert.That(opts[i2].Text, Is.EqualTo(data.ProductNames[p2]));
            Assert.That(i2, Is.LessThan(i1), "商品名の昇順（P2 が P1 より前）");
            Assert.That(opts.Any(o => o.Value == q.ToString()), Is.False, "廃番の商品 Q を含まない");
        }

        [Test, Basis("EV-ORD-301"), Basis("SC-ORD-03-I01"), Basis("SC-ORD-03-I15"), Basis("ORD-T6"), Basis("DEC-ORD-45")]
        public void OrdTfEditInit_編集の受注情報()
        {
            string k = data.NewCustomer('K', phone: "03-1234-5678", address: "A1", city: "C1", region: "R1", postalCode: "P1", country: "Jp");
            int e = data.NewEmployee(TestData.NewLastName('E'), TestData.NewFirstName('F'));
            int s = data.NewShipper();
            int o = data.NewOrder(customerId: k, employeeId: e, orderDate: D(2199, 3, 5), requiredDate: D(2199, 4, 1), shipVia: s, freight: 34.50m,
                shipName: "ShipTo Co", shipAddress: "4-5-6 Ginza", shipCity: "Chuo", shipRegion: "Tokyo", shipPostalCode: "104-0061", shipCountry: "Japan");

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.OrderID, Is.EqualTo(o));
            Assert.That(rv.EditCustomerID, Is.EqualTo(k));
            Assert.That(rv.EditEmployeeID, Is.EqualTo(e));
            Assert.That(rv.EditShipVia, Is.EqualTo(s));
            Assert.That(rv.OrderDate, Is.EqualTo(D(2199, 3, 5)));
            Assert.That(rv.RequiredDate, Is.EqualTo(D(2199, 4, 1)));
            Assert.That(rv.Freight, Is.EqualTo(34.50m));
            Assert.That(rv.ShipName, Is.EqualTo("ShipTo Co"));
            Assert.That(rv.ShipAddress, Is.EqualTo("4-5-6 Ginza"));
            Assert.That(rv.ShipCity, Is.EqualTo("Chuo"));
            Assert.That(rv.ShipRegion, Is.EqualTo("Tokyo"));
            Assert.That(rv.ShipPostalCode, Is.EqualTo("104-0061"));
            Assert.That(rv.ShipCountry, Is.EqualTo("Japan"));
            Assert.That(rv.CustomerPhone, Is.EqualTo("03-1234-5678"));
            Assert.That(rv.RowVersion, Is.EqualTo(1));
            Assert.That(rv.CustomerAddress, Is.Not.Null);
            Assert.That(rv.CustomerAddress, Does.Match(@"^A1([\s\S]*)C1\1R1\1P1\1Jp$"), "区切りは期待にしない");
        }

        [Test, Basis("EV-ORD-301"), Basis("共通仕様 9.1 節")]
        public void OrdTfEditInit_編集の行バージョン()
        {
            int o = data.NewOrder(orderDate: D(2199, 3, 5));
            TestData.BumpOrder(o, 7.00m);

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.RowVersion, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-301"), Basis("ORD-T1")]
        public void OrdTfEditInit_編集で顧客_担当者_配送業者が無い()
        {
            int p = data.NewProduct(mark: 'A');
            int o = data.NewOrder(freight: 0m);
            data.NewDetail(o, p, 10.00m, 1, 0f);

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.OrderID, Is.EqualTo(o), "受注が読めている");
            Assert.That(rv.EditCustomerID, Is.Null);
            Assert.That(rv.EditEmployeeID, Is.Null);
            Assert.That(rv.EditShipVia, Is.Null);
            Assert.That(rv.CustomerPhone, Is.Null.Or.Empty);
            Assert.That(rv.CustomerAddress, Is.Null.Or.Empty);
            Assert.That(rv.OrderDate, Is.Null);
            Assert.That(rv.RequiredDate, Is.Null);
        }

        [Test, Basis("EV-ORD-301"), Basis("SC-ORD-03-I16"), Basis("SC-ORD-03-I17"), Basis("SC-ORD-03-I18"), Basis("OD-T4"), Basis("共通仕様 3 節")]
        public void OrdTfEditInit_編集の明細の項目と並びと金額()
        {
            int pa = data.NewProduct(mark: 'A', unitsInStock: 5);
            int pm = data.NewProduct(mark: 'M', unitsInStock: 17);
            int px = data.NewProduct(mark: 'X', unitsInStock: null);
            int o = data.NewOrder(orderDate: D(2199, 3, 5), freight: 34.50m);
            data.NewDetail(o, pm, 50.00m, 2, 0.25f);
            data.NewDetail(o, px, 20.00m, 3, 0.05f);
            data.NewDetail(o, pa, 100.00m, 12, 0.15f);
            int o2 = data.NewOrder(orderDate: D(2199, 3, 5));
            int p2 = data.NewProduct(mark: 'Q');
            data.NewDetail(o2, p2, 1.00m, 1, 0f);

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.EditLines.Count, Is.EqualTo(3));
            Assert.That(rv.EditLines.Select(l => l.ProductID).ToList(), Is.EqualTo(new List<int> { pa, pm, px }), "商品名の昇順 A・M・X");
            Assert.That(rv.EditLines[0].ProductName, Is.EqualTo(data.ProductNames[pa]), "付記なし");
            Assert.That(rv.EditLines[0].UnitPrice, Is.EqualTo(100.00m));
            Assert.That(rv.EditLines[0].Discount, Is.EqualTo(0.15m));
            Assert.That(rv.EditLines[0].Quantity, Is.EqualTo(12));
            Assert.That(rv.EditLines[0].UnitsInStock, Is.EqualTo(5));
            Assert.That(rv.EditLines[1].UnitPrice, Is.EqualTo(50.00m));
            Assert.That(rv.EditLines[1].Discount, Is.EqualTo(0.25m));
            Assert.That(rv.EditLines[1].Quantity, Is.EqualTo(2));
            Assert.That(rv.EditLines[1].UnitsInStock, Is.EqualTo(17));
            Assert.That(rv.EditLines[2].UnitPrice, Is.EqualTo(20.00m));
            Assert.That(rv.EditLines[2].Discount, Is.EqualTo(0.05m));
            Assert.That(rv.EditLines[2].Quantity, Is.EqualTo(3));
            Assert.That(rv.EditLines[2].UnitsInStock, Is.Null);
            Assert.That(rv.EditLines.All(l => l.RowVersion == 1), Is.True);
            Assert.That(rv.Subtotal, Is.EqualTo(1152.00m));
            Assert.That(rv.Total, Is.EqualTo(1186.50m));
        }

        [Test, Basis("共通仕様 3 節"), Basis("OD-T4"), Basis("EV-ORD-301")]
        public void OrdTfEditInit_編集の明細金額は四捨五入して合算する()
        {
            int p1 = data.NewProduct(mark: 'A');
            int p2 = data.NewProduct(mark: 'B');
            int o = data.NewOrder(orderDate: D(2199, 3, 5), freight: 10.00m);
            data.NewDetail(o, p1, 0.50m, 1, 0.15f);
            data.NewDetail(o, p2, 0.50m, 1, 0.15f);

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.EditLines.Count, Is.EqualTo(2));
            Assert.That(rv.EditLines.All(l => l.UnitPrice == 0.50m && l.Discount == 0.15m && l.Quantity == 1), Is.True);
            Assert.That(rv.Subtotal, Is.EqualTo(0.86m), "0.425 を四捨五入して 0.43 ずつ");
            Assert.That(rv.Total, Is.EqualTo(10.86m));
        }

        [Test, Basis("共通仕様 3 節"), Basis("OD-T4"), Basis("EV-ORD-301")]
        public void OrdTfEditInit_編集の明細金額の丸めは数量を掛けた後()
        {
            int p = data.NewProduct(mark: 'A');
            int o = data.NewOrder(orderDate: D(2199, 3, 5), freight: 0m);
            data.NewDetail(o, p, 0.50m, 3, 0.15f);

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.EditLines.Count, Is.EqualTo(1));
            Assert.That(rv.EditLines[0].Discount, Is.EqualTo(0.15m));
            Assert.That(rv.Subtotal, Is.EqualTo(1.28m), "0.425 × 3 = 1.275 を丸める（価格を先に丸めると 1.29）");
            Assert.That(rv.Total, Is.EqualTo(1.28m));
        }

        [Test, Basis("EV-ORD-301"), Basis("DEC-CMN-03"), Basis("共通仕様 4 節")]
        public void OrdTfEditInit_編集で送料がNULLなら0として足す()
        {
            int p = data.NewProduct(mark: 'A');
            int o = data.NewOrder(orderDate: D(2199, 3, 5), freight: null);
            data.NewDetail(o, p, 12.50m, 1, 0f);

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.OrderID, Is.EqualTo(o));
            Assert.That(rv.Freight, Is.Null);
            Assert.That(rv.Subtotal, Is.EqualTo(12.50m));
            Assert.That(rv.Total, Is.EqualTo(12.50m));
        }
        [Test, Basis("EV-ORD-301")]
        public void OrdTfEditInit_編集の明細の行バージョン()
        {
            int pa = data.NewProduct(mark: 'A');
            int pm = data.NewProduct(mark: 'M');
            int o = data.NewOrder(orderDate: D(2199, 3, 5));
            data.NewDetail(o, pa, 10.00m, 1, 0f);
            data.NewDetail(o, pm, 20.00m, 1, 0f);
            TestData.BumpDetail(o, pm);

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.EditLines.Count, Is.EqualTo(2));
            Assert.That(rv.EditLines.Single(l => l.ProductID == pm).RowVersion, Is.EqualTo(2));
            Assert.That(rv.EditLines.Single(l => l.ProductID == pa).RowVersion, Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-301"), Basis("OD-T3")]
        public void OrdTfEditInit_廃番の既存明細は残し選択肢には無い()
        {
            int p = data.NewProduct(mark: 'A');
            int q = data.NewProduct(mark: 'B', discontinued: true);
            int o = data.NewOrder(orderDate: D(2199, 3, 5));
            data.NewDetail(o, p, 10.00m, 1, 0f);
            data.NewDetail(o, q, 20.00m, 1, 0f);

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.EditLines.Count, Is.EqualTo(2));
            Assert.That(rv.EditLines.Single(l => l.ProductID == q).Discontinued, Is.True);
            Assert.That(rv.EditLines.Single(l => l.ProductID == p).Discontinued, Is.False);
            Assert.That(rv.ProductOptions.Any(x => x.Value == q.ToString()), Is.False, "選択肢に Q を含まない");
            Assert.That(rv.ProductOptions.Any(x => x.Value == p.ToString()), Is.True, "選択肢に P を含む");
        }

        [Test, Basis("EV-ORD-301"), Basis("ERR-NOTFOUND")]
        public void OrdTfEditInit_編集で存在しない受注()
        {
            int o = data.NewOrder(orderDate: D(2199, 3, 5));
            TestData.DeleteOrder(o);

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgNotFound));
        }

        [Test, Basis("EV-ORD-301"), Basis("ORD-T4"), Basis("ERR-BIZ")]
        [TestCase("2001/01/01")]
        [TestCase("T")]
        public void OrdTfEditInit_編集で出荷済は編集させない(string shipped)
        {
            int o = data.NewOrder(orderDate: D(2001, 1, 1), shippedDate: Day(shipped));

            OrdTfReturnValue rv = EditInit("edit", o);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgShipped));
        }

        // ---------------------------------------------------------------- 3.2 OrdTfCustomer

        [Test, Basis("EV-ORD-302"), Basis("SC-ORD-03-I03"), Basis("SC-ORD-03-I04"), Basis("DEC-ORD-45")]
        public void OrdTfCustomer_電話と住所()
        {
            string k = data.NewCustomer('K', phone: "03-1234-5678", address: "A1", city: "C1", region: "R1", postalCode: "P1", country: "Jp");

            OrdTfReturnValue rv = CustomerInfo(k);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.CustomerPhone, Is.EqualTo("03-1234-5678"));
            Assert.That(rv.CustomerAddress, Is.Not.Null);
            Assert.That(rv.CustomerAddress, Does.Match(@"^A1([\s\S]*)C1\1R1\1P1\1Jp$"), "区切りは期待にしない");
        }

        [Test, Basis("EV-ORD-302"), Basis("ORD-T6")]
        public void OrdTfCustomer_出荷先の既定値()
        {
            string k = data.NewCustomer('K', phone: "03-1234-5678", address: "Addr 1", city: "City 1", region: "Reg 1", postalCode: "Post 1", country: "Ctry 1");

            OrdTfReturnValue rv = CustomerInfo(k);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.ShipName, Is.EqualTo(data.CustomerNames[k]));
            Assert.That(rv.ShipAddress, Is.EqualTo("Addr 1"));
            Assert.That(rv.ShipCity, Is.EqualTo("City 1"));
            Assert.That(rv.ShipRegion, Is.EqualTo("Reg 1"));
            Assert.That(rv.ShipPostalCode, Is.EqualTo("Post 1"));
            Assert.That(rv.ShipCountry, Is.EqualTo("Ctry 1"));
        }

        [Test, Basis("EV-ORD-302"), Basis("共通仕様 4 節"), Basis("DEC-ORD-45")]
        public void OrdTfCustomer_空の要素は空()
        {
            string k0 = data.NewCustomer('K', address: "A1", city: "C1", region: null, postalCode: "", country: "Jp");

            OrdTfReturnValue rv = CustomerInfo(k0);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.ShipName, Is.EqualTo(data.CustomerNames[k0]), "顧客が読めている");
            Assert.That(rv.CustomerPhone, Is.Null.Or.Empty);
            Assert.That(rv.ShipRegion, Is.Null.Or.Empty);
            Assert.That(rv.ShipPostalCode, Is.Null.Or.Empty);
            Assert.That(rv.CustomerAddress, Is.Not.Null);
            Assert.That(rv.CustomerAddress, Does.Match(@"^A1([\s\S]*)C1\1Jp$"), "空の要素を詰める");
        }

        [Test, Basis("EV-ORD-302"), Basis("ERR-NOTFOUND")]
        public void OrdTfCustomer_存在しない顧客()
        {
            string k = TestData.NewCustomerId();

            OrdTfReturnValue rv = CustomerInfo(k);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgNotFound));
        }

        // ---------------------------------------------------------------- 3.3 OrdTfProduct

        [Test, Basis("EV-ORD-306"), Basis("OD-T2")]
        public void OrdTfProduct_単価と在庫()
        {
            int p = data.NewProduct(mark: 'A', unitPrice: 12.50m, unitsInStock: 7);

            OrdTfReturnValue rv = ProductInfo(p);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.ProductName, Is.EqualTo(data.ProductNames[p]));
            Assert.That(rv.UnitPrice, Is.EqualTo(12.50m));
            Assert.That(rv.UnitsInStock, Is.EqualTo(7));
        }

        [Test, Basis("EV-ORD-306"), Basis("共通仕様 4 節")]
        public void OrdTfProduct_在庫がNULL()
        {
            int p = data.NewProduct(mark: 'A', unitPrice: 12.50m, unitsInStock: null);

            OrdTfReturnValue rv = ProductInfo(p);

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.ProductName, Is.EqualTo(data.ProductNames[p]), "商品が読めている");
            Assert.That(rv.UnitsInStock, Is.Null);
        }

        [Test, Basis("EV-ORD-306"), Basis("ERR-NOTFOUND"), Basis("VAL-EXISTS")]
        public void OrdTfProduct_存在しない商品()
        {
            int p = data.NewProduct(mark: 'A');
            data.DeleteProduct(p);

            OrdTfReturnValue rv = ProductInfo(p);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgNotFound));
        }

        // ---------------------------------------------------------------- 3.4 OrdTfSave の登録

        [Test, Basis("EV-ORD-308")]
        public void OrdTfSave_登録で受注と明細が入る()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.SaveLines = new List<OrdTfSaveLine>
            {
                NewLine(c.P1, "10.00", "0.15", "2"),
                NewLine(c.P2, "20.00", "0", "1")
            });

            TestData.OrderRow row = AssertSaved(rv);
            Assert.That(row.CustomerID, Is.EqualTo(c.K));
            Assert.That(row.EmployeeID, Is.EqualTo(c.E));
            Assert.That(row.OrderDate, Is.EqualTo(D(2199, 3, 5)));
            Assert.That(row.RequiredDate, Is.EqualTo(D(2199, 4, 1)));
            Assert.That(row.ShipVia, Is.EqualTo(c.S));
            Assert.That(row.Freight, Is.EqualTo(12.50m));
            Assert.That(row.ShipName, Is.EqualTo(c.Mark));
            Assert.That(row.ShipAddress, Is.EqualTo("4-5-6 Ginza"));
            Assert.That(row.ShipCity, Is.EqualTo("Chuo"));
            Assert.That(row.ShipRegion, Is.EqualTo("Tokyo"));
            Assert.That(row.ShipPostalCode, Is.EqualTo("104-0061"));
            Assert.That(row.ShipCountry, Is.EqualTo("Japan"));
            Assert.That(row.RowVersion, Is.EqualTo(1));
            Assert.That(rv.RowVersion, Is.EqualTo(1));
            List<TestData.DetailRow> ds = TestData.GetDetails(rv.OrderID);
            Assert.That(ds.Count, Is.EqualTo(2));
            TestData.DetailRow d1 = ds.Single(d => d.ProductID == c.P1);
            TestData.DetailRow d2 = ds.Single(d => d.ProductID == c.P2);
            Assert.That(d1.UnitPrice, Is.EqualTo(10.00m));
            Assert.That(d1.Quantity, Is.EqualTo(2));
            Assert.That(d1.Discount, Is.EqualTo(0.15m));
            Assert.That(d2.UnitPrice, Is.EqualTo(20.00m));
            Assert.That(d2.Quantity, Is.EqualTo(1));
            Assert.That(d2.Discount, Is.EqualTo(0m));
            Assert.That(d1.RowVersion, Is.EqualTo(1));
            Assert.That(d2.RowVersion, Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-308")]
        public void OrdTfSave_登録の出荷日はNULL()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => { });

            TestData.OrderRow row = AssertSaved(rv);
            Assert.That(row.ShippedDate, Is.Null);
        }

        [Test, Basis("DEC-ORD-01")]
        public void OrdTfSave_登録で送料が空なら0()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.FreightText = "");

            TestData.OrderRow row = AssertSaved(rv);
            Assert.That(row.Freight, Is.EqualTo(0m), "NULL でなく 0");
        }

        [Test, Basis("EV-ORD-308"), Basis("SC-ORD-03-I07"), Basis("SC-ORD-03-I08"), Basis("DEC-ORD-36")]
        public void OrdTfSave_登録で要求納期_配送業者を省略できる()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv =>
            {
                pv.RequiredDateText = "";
                pv.ShipViaText = "";
                pv.ShipName = "";
                pv.ShipAddress = "";
                pv.ShipCity = "";
                pv.ShipRegion = "";
                pv.ShipPostalCode = "";
                pv.ShipCountry = "";
            });

            TestData.OrderRow row = AssertSaved(rv);
            Assert.That(row.RequiredDate, Is.Null);
            Assert.That(row.ShipVia, Is.Null);
            Assert.That(row.ShipName, Is.Null, "空の出荷先は NULL（空文字でない）");
            Assert.That(row.ShipAddress, Is.Null);
            Assert.That(row.ShipCity, Is.Null);
            Assert.That(row.ShipRegion, Is.Null);
            Assert.That(row.ShipPostalCode, Is.Null);
            Assert.That(row.ShipCountry, Is.Null);
        }

        [Test, Basis("OD-T4"), Basis("共通仕様 3 節")]
        public void OrdTfSave_登録の明細の割引は端数も入る()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.SaveLines = new List<OrdTfSaveLine> { NewLine(c.P1, "100.00", "0.125", "1") });

            AssertSaved(rv);
            List<TestData.DetailRow> ds = TestData.GetDetails(rv.OrderID);
            Assert.That(ds.Count, Is.EqualTo(1));
            Assert.That(ds[0].Discount, Is.EqualTo(0.125m));
            Assert.That(ds[0].UnitPrice, Is.EqualTo(100.00m));
        }

        [Test, Basis("SC-ORD-03-I16"), Basis("EV-ORD-306"), Basis("EV-ORD-307")]
        public void OrdTfSave_在庫を超える数量でも保存できる()
        {
            Ctx c = NewCtx();
            int p = data.NewProduct(mark: 'S', unitPrice: 10.00m, unitsInStock: 5);

            OrdTfReturnValue rv = SaveV(c, pv => pv.SaveLines = new List<OrdTfSaveLine> { NewLine(p, "10.00", "0", "6") });

            AssertSaved(rv);
            List<TestData.DetailRow> ds = TestData.GetDetails(rv.OrderID);
            Assert.That(ds.Count, Is.EqualTo(1));
            Assert.That(ds[0].Quantity, Is.EqualTo(6));
        }

        [Test, Basis("EV-ORD-308")]
        public void OrdTfSave_登録でほかの受注に触れない()
        {
            Ctx c = NewCtx();
            int o2 = data.NewOrder(customerId: c.K, orderDate: D(2199, 3, 1), freight: 3.00m);
            data.NewDetail(o2, c.P2, 20.00m, 1, 0f);
            TestData.BumpOrder(o2, 3.00m);

            OrdTfReturnValue rv = SaveV(c, pv => { });

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.OrderID, Is.GreaterThan(0));
            Assert.That(rv.OrderID, Is.Not.EqualTo(o2));
            Assert.That(TestData.GetRowVersion(o2), Is.EqualTo(2));
            Assert.That(TestData.GetOrder(o2).Freight, Is.EqualTo(3.00m));
            Assert.That(TestData.GetDetails(o2).Count, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- 3.5 OrdTfSave の検証

        [Test, Basis("VAL-REQUIRED"), Basis("ORD-T1"), Basis("DEC-CMN-05")]
        [TestCase("CustomerID")]
        [TestCase("EmployeeID")]
        [TestCase("OrderDate")]
        public void OrdTfSave_必須の欠け(string field)
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv =>
            {
                if (field == "CustomerID") pv.CustomerID = "";
                if (field == "EmployeeID") pv.EmployeeIDText = "";
                if (field == "OrderDate") pv.OrderDateText = "";
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { field }));
            Assert.That(rv.ErrorMessages, Is.Empty);
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("VAL-REQUIRED"), Basis("EV-ORD-308"), Basis("DEC-ORD-43")]
        [TestCase("ProductID")]
        [TestCase("UnitPrice")]
        [TestCase("Discount")]
        [TestCase("Quantity")]
        public void OrdTfSave_明細の必須の欠け(string field)
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv =>
            {
                OrdTfSaveLine l = NewLine(c.P1, "10.00", "0.15", "2");
                if (field == "ProductID") l.ProductIDText = "";
                if (field == "UnitPrice") l.UnitPriceText = "";
                if (field == "Discount") l.DiscountText = "";
                if (field == "Quantity") l.QuantityText = "";
                pv.SaveLines = new List<OrdTfSaveLine> { l };
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "Lines[0]." + field }));
            Assert.That(rv.ErrorMessages, Is.Empty, "ORD-T5 に当たらない（商品が空の行も 1 行に数える）");
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("VAL-REQUIRED"), Basis("DEC-ORD-09"), Basis("DEC-ORD-29")]
        public void OrdTfSave_受注日の空白だけは必須の欠け()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.OrderDateText = "   ");

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "OrderDate" }));
            Assert.That(rv.ErrorMessages, Is.Empty);
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("VAL-DATE"), Basis("DEC-ORD-09"), Basis("DEC-ORD-29"), Basis("DEC-CMN-05")]
        [TestCase("abc")]
        [TestCase("2199/1/5")]
        [TestCase("2199-01-05")]
        [TestCase("2199/02/30")]
        public void OrdTfSave_受注日の形式(string text)
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.OrderDateText = text);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "OrderDate" }));
            Assert.That(rv.ErrorMessages, Is.Empty);
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("VAL-DATE"), Basis("DEC-ORD-29")]
        [TestCase("abc")]
        [TestCase("2199/1/5")]
        [TestCase("2199-01-05")]
        [TestCase("2199/02/30")]
        public void OrdTfSave_要求納期の形式(string text)
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.RequiredDateText = text);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "RequiredDate" }));
            Assert.That(rv.ErrorMessages, Is.Empty);
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("VAL-DATE"), Basis("DEC-ORD-09"), Basis("DEC-ORD-29")]
        public void OrdTfSave_要求納期の空白だけは未指定()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.RequiredDateText = "   ");

            TestData.OrderRow row = AssertSaved(rv);
            Assert.That(row.RequiredDate, Is.Null);
        }

        [Test, Basis("VAL-DATE"), Basis("TableSchema Orders（datetime）")]
        [TestCase("1753/01/01", "")]
        [TestCase("2199/03/05", "9999/12/31")]
        public void OrdTfSave_日付の範囲の端は通る(string orderDate, string requiredDate)
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => { pv.OrderDateText = orderDate; pv.RequiredDateText = requiredDate; });

            TestData.OrderRow row = AssertSaved(rv);
            Assert.That(row.OrderDate, Is.EqualTo(DateTime.ParseExact(orderDate, "yyyy/MM/dd", Inv)));
            if (requiredDate == "") Assert.That(row.RequiredDate, Is.Null);
            else Assert.That(row.RequiredDate, Is.EqualTo(DateTime.ParseExact(requiredDate, "yyyy/MM/dd", Inv)));
        }

        [Test, Basis("VAL-DATE"), Basis("ORD-T3"), Basis("DEC-CMN-04"), Basis("DEC-ORD-31")]
        public void OrdTfSave_受注日が要求納期より後は関連チェック()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => { pv.OrderDateText = "2199/04/02"; pv.RequiredDateText = "2199/04/01"; });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.Empty, "関連チェックは項目に紐付けない");
            Assert.That(rv.ErrorMessages.Count, Is.EqualTo(1));
            Assert.That(rv.ErrorMessages[0], Is.Not.Null.And.Not.Empty);
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("ORD-T3")]
        public void OrdTfSave_受注日と要求納期が同じ日は通る()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => { pv.OrderDateText = "2199/04/01"; pv.RequiredDateText = "2199/04/01"; });

            TestData.OrderRow row = AssertSaved(rv);
            Assert.That(row.OrderDate, Is.EqualTo(D(2199, 4, 1)));
            Assert.That(row.RequiredDate, Is.EqualTo(D(2199, 4, 1)));
        }

        [Test, Basis("VAL-NUMERIC"), Basis("EV-ORD-303")]
        [TestCase("-0.01")]
        [TestCase("abc")]
        public void OrdTfSave_送料の違反(string text)
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.FreightText = text);

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "Freight" }));
            Assert.That(rv.ErrorMessages, Is.Empty);
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("VAL-NUMERIC")]
        public void OrdTfSave_送料0は通る()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.FreightText = "0");

            TestData.OrderRow row = AssertSaved(rv);
            Assert.That(row.Freight, Is.EqualTo(0m));
        }

        [Test, Basis("VAL-NUMERIC"), Basis("EV-ORD-308")]
        [TestCase("-0.01", false)]
        [TestCase("abc", false)]
        [TestCase("0", true)]
        public void OrdTfSave_単価の違反と0(string text, bool ok)
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.SaveLines = new List<OrdTfSaveLine> { NewLine(c.P1, text, "0.15", "2") });

            if (ok)
            {
                AssertSaved(rv);
                Assert.That(TestData.GetDetails(rv.OrderID).Single().UnitPrice, Is.EqualTo(0m));
            }
            else
            {
                Assert.That(rv.ErrorFlag, Is.True);
                Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "Lines[0].UnitPrice" }));
                Assert.That(rv.ErrorMessages, Is.Empty);
                AssertNothingSaved(c, rv);
            }
        }

        [Test, Basis("VAL-NUMERIC")]
        [TestCase("-0.01", false)]
        [TestCase("1.01", false)]
        [TestCase("abc", false)]
        [TestCase("0", true)]
        [TestCase("1", true)]
        public void OrdTfSave_割引率の違反と境界0_1(string text, bool ok)
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.SaveLines = new List<OrdTfSaveLine> { NewLine(c.P1, "10.00", text, "2") });

            if (ok)
            {
                AssertSaved(rv);
                Assert.That(TestData.GetDetails(rv.OrderID).Single().Discount, Is.EqualTo(decimal.Parse(text, Inv)));
            }
            else
            {
                Assert.That(rv.ErrorFlag, Is.True);
                Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "Lines[0].Discount" }));
                Assert.That(rv.ErrorMessages, Is.Empty);
                AssertNothingSaved(c, rv);
            }
        }

        [Test, Basis("VAL-NUMERIC")]
        [TestCase("0", false)]
        [TestCase("-1", false)]
        [TestCase("1.5", false)]
        [TestCase("abc", false)]
        [TestCase("1", true)]
        public void OrdTfSave_数量の違反と境界1(string text, bool ok)
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.SaveLines = new List<OrdTfSaveLine> { NewLine(c.P1, "10.00", "0.15", text) });

            if (ok)
            {
                AssertSaved(rv);
                Assert.That(TestData.GetDetails(rv.OrderID).Single().Quantity, Is.EqualTo(1));
            }
            else
            {
                Assert.That(rv.ErrorFlag, Is.True);
                Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "Lines[0].Quantity" }));
                Assert.That(rv.ErrorMessages, Is.Empty);
                AssertNothingSaved(c, rv);
            }
        }

        [Test, Basis("VAL-LENGTH"), Basis("TableSchema Orders")]
        [TestCase("ShipName", 40)]
        [TestCase("ShipAddress", 60)]
        [TestCase("ShipCity", 15)]
        [TestCase("ShipRegion", 15)]
        [TestCase("ShipPostalCode", 10)]
        [TestCase("ShipCountry", 15)]
        public void OrdTfSave_出荷先の桁数(string field, int max)
        {
            // 日本語の文字を含む。ShipName は "Tf" + GUID 8 桁 + 「あ」で 40 桁・41 桁に作る（他のケース・前の実行の残りと混ざらない）
            Func<int, string> make = n =>
            {
                string head = field == "ShipName" ? "Tf" + Guid.NewGuid().ToString("N").Substring(0, 8) : "";
                return head + new string('あ', n - head.Length);
            };
            string exact = make(max);
            string over = field == "ShipName" ? exact + "あ" : make(max + 1);
            Assert.That(exact.Length, Is.EqualTo(max));
            Assert.That(over.Length, Is.EqualTo(max + 1));
            Ctx c = NewCtx();
            data.TrackShipName(field == "ShipName" ? exact : c.Mark);
            Action<OrdTfParameterValue, string> set = (pv, v) =>
            {
                if (field == "ShipName") pv.ShipName = v;
                if (field == "ShipAddress") pv.ShipAddress = v;
                if (field == "ShipCity") pv.ShipCity = v;
                if (field == "ShipRegion") pv.ShipRegion = v;
                if (field == "ShipPostalCode") pv.ShipPostalCode = v;
                if (field == "ShipCountry") pv.ShipCountry = v;
            };

            // 41 桁（桁数 + 1）は違反：何も入らない
            OrdTfReturnValue bad = SaveV(c, pv => set(pv, over));
            Assert.That(bad.ErrorFlag, Is.True);
            Assert.That(bad.ErrorFields, Is.EquivalentTo(new[] { field }));
            Assert.That(bad.ErrorMessages, Is.Empty);
            AssertNothingSaved(c, bad, field == "ShipName" ? over : null);

            // 桁数ちょうどは通る：保存された値が同じ
            OrdTfReturnValue ok = SaveV(c, pv => set(pv, exact));
            TestData.OrderRow row = AssertSaved(ok);
            string saved = field == "ShipName" ? row.ShipName : field == "ShipAddress" ? row.ShipAddress : field == "ShipCity" ? row.ShipCity
                : field == "ShipRegion" ? row.ShipRegion : field == "ShipPostalCode" ? row.ShipPostalCode : row.ShipCountry;
            Assert.That(saved, Is.EqualTo(exact));
        }

        [Test, Basis("VAL-EXISTS"), Basis("DEC-CMN-05")]
        [TestCase("CustomerID")]
        [TestCase("EmployeeID", "2147483647")]
        [TestCase("EmployeeID", "abc")]
        [TestCase("ShipVia", "2147483647")]
        [TestCase("ShipVia", "abc")]
        [TestCase("ProductID")]
        [TestCase("ProductID", "abc")]
        public void OrdTfSave_存在しない顧客_担当者_配送業者_商品(string field, string value = null)
        {
            Ctx c = NewCtx();
            int gone = data.NewProduct(mark: 'G');
            data.DeleteProduct(gone);

            OrdTfReturnValue rv = SaveV(c, pv =>
            {
                if (field == "CustomerID") pv.CustomerID = TestData.NewCustomerId();
                if (field == "EmployeeID") pv.EmployeeIDText = value;
                if (field == "ShipVia") pv.ShipViaText = value;
                if (field == "ProductID") pv.SaveLines = new List<OrdTfSaveLine> { NewLine(gone, "10.00", "0", "1") };
                if (field == "ProductID" && value != null) pv.SaveLines[0].ProductIDText = value;
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { field == "ProductID" ? "Lines[0].ProductID" : field }));
            Assert.That(rv.ErrorMessages, Is.Empty);
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("VAL-DUP"), Basis("OD-T1"), Basis("DEC-CMN-04")]
        public void OrdTfSave_商品の重複()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.SaveLines = new List<OrdTfSaveLine>
            {
                NewLine(c.P1, "10.00", "0", "1"),
                NewLine(c.P1, "10.00", "0", "2")
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessages, Does.Contain(MsgDup));
            Assert.That(rv.ErrorFields, Is.Empty);
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("ORD-T5"), Basis("ERR-BIZ")]
        public void OrdTfSave_明細が0行()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv => pv.SaveLines = new List<OrdTfSaveLine>());

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessages, Does.Contain(MsgNoLine));
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("OD-T3"), Basis("ERR-BIZ")]
        public void OrdTfSave_新規の廃番商品は指定できない()
        {
            Ctx c = NewCtx();
            int q = data.NewProduct(mark: 'Q', discontinued: true, unitPrice: 5.00m);

            OrdTfReturnValue rv = SaveV(c, pv => pv.SaveLines = new List<OrdTfSaveLine> { NewLine(q, "5.00", "0", "1") });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessages, Does.Contain(MsgDiscontinued));
            AssertNothingSaved(c, rv);
        }

        [Test, Basis("OD-T3"), Basis("EV-ORD-308"), Basis("ERR-BIZ"), Basis("DEC-ORD-49")]
        public void OrdTfSave_登録でOriginalProductIDを付けても新規の廃番商品は指定できない()
        {
            Ctx c = NewCtx();
            int q = data.NewProduct(mark: 'Q', discontinued: true, unitPrice: 5.00m);

            OrdTfReturnValue rv = SaveV(c, pv =>
            {
                OrdTfSaveLine l = NewLine(q, "5.00", "0", "1");
                l.OriginalProductID = q;      // 細工した入力：読み込み時の行だと名乗る（登録モードに読み込み時の行は無い）
                l.OriginalUnitPrice = 5.00m;
                l.OriginalDiscount = 0m;
                l.OriginalQuantity = 1;
                pv.SaveLines = new List<OrdTfSaveLine> { l };
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessages, Does.Contain(MsgDiscontinued));
            AssertNothingSaved(c, rv);
        }
        [Test, Basis("EV-ORD-308"), Basis("DEC-CMN-04"), Basis("DEC-CMN-05")]
        public void OrdTfSave_違反は全部まとめて返す()
        {
            Ctx c = NewCtx();

            OrdTfReturnValue rv = SaveV(c, pv =>
            {
                pv.CustomerID = "";
                pv.OrderDateText = "2199/04/02";
                pv.RequiredDateText = "2199/04/01";
                pv.FreightText = "-1";
                pv.SaveLines = new List<OrdTfSaveLine>
                {
                    NewLine(c.P1, "10.00", "0", "0"),
                    NewLine(c.P1, "10.00", "0", "0")
                };
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Does.Contain("CustomerID"));
            Assert.That(rv.ErrorFields, Does.Contain("Freight"));
            Assert.That(rv.ErrorFields, Does.Contain("Lines[0].Quantity"));
            Assert.That(rv.ErrorFields, Does.Contain("Lines[1].Quantity"));
            Assert.That(rv.ErrorMessages.Count, Is.EqualTo(2), "重複と関連チェックの 2 件");
            Assert.That(rv.ErrorMessages, Does.Contain(MsgDup));
            AssertNothingSaved(c, rv);
        }

        // ---------------------------------------------------------------- 3.6 OrdTfSave の編集

        private sealed class Ln
        {
            public int P;
            public decimal Price;
            public float Disc;
            public short Qty;
        }

        private static Ln L(int p, decimal price, float disc, short qty)
        {
            return new Ln { P = p, Price = price, Disc = disc, Qty = qty };
        }

        /// <summary>編集用の受注 o（K・E・S、受注日 2199/03/05、送料 5.00、出荷日 NULL）と明細を作る</summary>
        private int NewEditOrder(Ctx c, params Ln[] lines)
        {
            int o = data.NewOrder(customerId: c.K, employeeId: c.E, orderDate: D(2199, 3, 5), requiredDate: D(2199, 4, 1), shipVia: c.S, freight: 5.00m,
                shipName: "ShipTo Co", shipAddress: "4-5-6 Ginza", shipCity: "Chuo", shipRegion: "Tokyo", shipPostalCode: "104-0061", shipCountry: "Japan");
            foreach (Ln l in lines) data.NewDetail(o, l.P, l.Price, l.Qty, l.Disc);
            return o;
        }

        /// <summary>読み込み時のままの行（Original* を設定）</summary>
        private static OrdTfSaveLine Same(Ln l)
        {
            OrdTfSaveLine s = NewLine(l.P, l.Price.ToString(Inv), ((decimal)l.Disc).ToString(Inv), l.Qty.ToString(Inv));
            s.OriginalProductID = l.P;
            s.OriginalUnitPrice = l.Price;
            s.OriginalDiscount = (decimal)l.Disc;
            s.OriginalQuantity = l.Qty;
            return s;
        }

        /// <summary>編集の入力（読み込んだ状態のまま）。change で一部を変える</summary>
        private OrdTfReturnValue SaveEdit(Ctx c, int orderId, int rowVersion, Ln[] loaded, Action<OrdTfParameterValue> change)
        {
            return Save(pv =>
            {
                pv.Mode = "edit";
                pv.OrderID = orderId;
                pv.RowVersion = rowVersion;
                pv.CustomerID = c.K;
                pv.EmployeeIDText = c.E.ToString(Inv);
                pv.OrderDateText = "2199/03/05";
                pv.RequiredDateText = "2199/04/01";
                pv.ShipViaText = c.S.ToString(Inv);
                pv.FreightText = "5.00";
                pv.ShipName = "ShipTo Co";
                pv.ShipAddress = "4-5-6 Ginza";
                pv.ShipCity = "Chuo";
                pv.ShipRegion = "Tokyo";
                pv.ShipPostalCode = "104-0061";
                pv.ShipCountry = "Japan";
                pv.SaveLines = loaded.Select(Same).ToList();
                pv.DeletedProductIDs = new List<int>();
                change(pv);
            });
        }

        [Test, Basis("EV-ORD-308"), Basis("共通仕様 9.1 節")]
        public void OrdTfSave_編集で受注の項目が更新される()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12), m = L(c.P2, 50.00m, 0.25f, 2);
            int o = NewEditOrder(c, a, m);
            int e2 = data.NewEmployee(TestData.NewLastName('G'), TestData.NewFirstName('H'));
            int s2 = data.NewShipper();

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a, m }, pv =>
            {
                pv.OrderDateText = "2199/03/06";
                pv.RequiredDateText = "2199/05/01";
                pv.EmployeeIDText = e2.ToString();
                pv.ShipViaText = s2.ToString();
                pv.FreightText = "9.25";
                pv.ShipName = "Other Co";
                pv.ShipAddress = "1-2-3 Umeda";
                pv.ShipCity = "Osaka";
                pv.ShipRegion = "Kansai";
                pv.ShipPostalCode = "530-0001";
                pv.ShipCountry = "Japan2";
            });

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.OrderID, Is.EqualTo(o));
            Assert.That(rv.RowVersion, Is.EqualTo(2));
            TestData.OrderRow row = TestData.GetOrder(o);
            Assert.That(row.RowVersion, Is.EqualTo(2));
            Assert.That(row.CustomerID, Is.EqualTo(c.K));
            Assert.That(row.EmployeeID, Is.EqualTo(e2));
            Assert.That(row.ShipVia, Is.EqualTo(s2));
            Assert.That(row.OrderDate, Is.EqualTo(D(2199, 3, 6)));
            Assert.That(row.RequiredDate, Is.EqualTo(D(2199, 5, 1)));
            Assert.That(row.Freight, Is.EqualTo(9.25m));
            Assert.That(row.ShipName, Is.EqualTo("Other Co"));
            Assert.That(row.ShipAddress, Is.EqualTo("1-2-3 Umeda"));
            Assert.That(row.ShipCity, Is.EqualTo("Osaka"));
            Assert.That(row.ShipRegion, Is.EqualTo("Kansai"));
            Assert.That(row.ShipPostalCode, Is.EqualTo("530-0001"));
            Assert.That(row.ShipCountry, Is.EqualTo("Japan2"));
            Assert.That(row.ShippedDate, Is.Null, "付随の期待：ShippedDate を NULL で SET する実装は検出しない");
        }

        [Test, Basis("DEC-ORD-36"), Basis("EV-ORD-308")]
        public void OrdTfSave_編集で空にした出荷先_要求納期_配送業者はNULLになる()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv =>
            {
                pv.RequiredDateText = "";
                pv.ShipViaText = "";
                pv.ShipName = "";
                pv.ShipAddress = "";
                pv.ShipCity = "";
                pv.ShipRegion = "";
                pv.ShipPostalCode = "";
                pv.ShipCountry = "";
            });

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.RowVersion, Is.EqualTo(2), "保存された");
            TestData.OrderRow row = TestData.GetOrder(o);
            Assert.That(row.RequiredDate, Is.Null);
            Assert.That(row.ShipVia, Is.Null);
            Assert.That(row.ShipName, Is.Null);
            Assert.That(row.ShipAddress, Is.Null);
            Assert.That(row.ShipCity, Is.Null);
            Assert.That(row.ShipRegion, Is.Null);
            Assert.That(row.ShipPostalCode, Is.Null);
            Assert.That(row.ShipCountry, Is.Null);
        }

        [Test, Basis("EV-ORD-308")]
        public void OrdTfSave_編集でほかの受注に触れない()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);
            int o2 = NewEditOrder(c, L(c.P2, 20.00m, 0f, 1));
            decimal before = TestData.GetOrder(o2).Freight.Value;
            int e2 = data.NewEmployee(TestData.NewLastName('G'), TestData.NewFirstName('H'));
            int s2 = data.NewShipper();

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv =>
            {
                pv.OrderDateText = "2199/03/06";
                pv.RequiredDateText = "2199/05/01";
                pv.EmployeeIDText = e2.ToString();
                pv.ShipViaText = s2.ToString();
                pv.FreightText = "9.25";
                pv.ShipName = "Other Co";
                pv.ShipAddress = "1-2-3 Umeda";
                pv.ShipCity = "Osaka";
                pv.ShipRegion = "Kansai";
                pv.ShipPostalCode = "530-0001";
                pv.ShipCountry = "Japan2";
            });

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(rv.RowVersion, Is.EqualTo(2), "保存された");
            Assert.That(TestData.GetRowVersion(o2), Is.EqualTo(1));
            Assert.That(TestData.GetOrder(o2).Freight, Is.EqualTo(before));
            Assert.That(TestData.GetDetails(o2).Count, Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-308"), Basis("OD-T6")]
        public void OrdTfSave_編集で明細を行ごとに適用する()
        {
            Ctx c = NewCtx();
            int px = data.NewProduct(mark: 'X', unitPrice: 20.00m);
            int pn = data.NewProduct(mark: 'N', unitPrice: 30.00m);
            Ln a = L(c.P1, 100.00m, 0.15f, 12), m = L(c.P2, 50.00m, 0.25f, 2), x = L(px, 20.00m, 0.05f, 3);
            int o = NewEditOrder(c, a, m, x);
            TestData.BumpDetail(o, a.P);   // A の行バージョン 2（受注は変えない）。画面を開いた後の状態

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a, m, x }, pv =>
            {
                pv.SaveLines = new List<OrdTfSaveLine>
                {
                    Same(a),
                    NewLine(m.P, "50.00", "0.5", "5"),   // M：数量 2 → 5、割引 0.25 → 0.5
                    NewLine(pn, "30.00", "0", "4")       // N：足した行
                };
                pv.SaveLines[1].OriginalProductID = m.P;
                pv.SaveLines[1].OriginalUnitPrice = 50.00m;
                pv.SaveLines[1].OriginalDiscount = 0.25m;
                pv.SaveLines[1].OriginalQuantity = 2;
                pv.DeletedProductIDs = new List<int> { px };   // X：削除
            });

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            List<TestData.DetailRow> ds = TestData.GetDetails(o);
            Assert.That(ds.Select(d => d.ProductID).OrderBy(i => i).ToList(), Is.EqualTo(new List<int> { a.P, m.P, pn }.OrderBy(i => i).ToList()), "A・M・N");
            Assert.That(ds.Single(d => d.ProductID == a.P).RowVersion, Is.EqualTo(2), "変更の無い行に触れない（全削除・再登録でない）");
            TestData.DetailRow dm = ds.Single(d => d.ProductID == m.P);
            Assert.That(dm.Quantity, Is.EqualTo(5));
            Assert.That(dm.Discount, Is.EqualTo(0.5m));
            Assert.That(dm.UnitPrice, Is.EqualTo(50.00m));
            Assert.That(dm.RowVersion, Is.EqualTo(2));
            Assert.That(ds.Single(d => d.ProductID == pn).RowVersion, Is.EqualTo(1));
            Assert.That(ds.Any(d => d.ProductID == px), Is.False, "X が無い");
        }

        [Test, Basis("EV-ORD-308"), Basis("OD-T6")]
        public void OrdTfSave_編集で単価だけ変えた行も更新される()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv => pv.SaveLines[0].UnitPriceText = "99.99");

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            TestData.DetailRow d = TestData.GetDetails(o).Single(x => x.ProductID == a.P);
            Assert.That(d.UnitPrice, Is.EqualTo(99.99m));
            Assert.That(d.RowVersion, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-308"), Basis("OD-T6")]
        public void OrdTfSave_編集で商品を変えた行は削除して挿入する()
        {
            Ctx c = NewCtx();
            int pn = data.NewProduct(mark: 'N', unitPrice: 30.00m);
            Ln a = L(c.P1, 100.00m, 0.15f, 12), m = L(c.P2, 50.00m, 0.25f, 2);
            int o = NewEditOrder(c, a, m);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a, m }, pv =>
            {
                pv.SaveLines[0].ProductIDText = pn.ToString();   // A の行の商品を N に（OriginalProductID は A のまま）
                pv.SaveLines[0].UnitPriceText = "30.00";
                pv.SaveLines[0].QuantityText = "7";
                pv.SaveLines[0].DiscountText = "0";
            });

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            List<TestData.DetailRow> ds = TestData.GetDetails(o);
            Assert.That(ds.Count, Is.EqualTo(2));
            Assert.That(ds.Any(d => d.ProductID == a.P), Is.False, "A が無い");
            TestData.DetailRow dn = ds.Single(d => d.ProductID == pn);
            Assert.That(dn.UnitPrice, Is.EqualTo(30.00m));
            Assert.That(dn.Quantity, Is.EqualTo(7));
            Assert.That(dn.RowVersion, Is.EqualTo(1));
            Assert.That(ds.Single(d => d.ProductID == m.P).Quantity, Is.EqualTo(2), "M は変わらない");
        }

        [Test, Basis("EV-ORD-308"), Basis("OD-T6")]
        public void OrdTfSave_編集で2行の商品を入れ替える()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12), m = L(c.P2, 50.00m, 0.25f, 2);
            int o = NewEditOrder(c, a, m);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a, m }, pv =>
            {
                pv.SaveLines[0].ProductIDText = m.P.ToString();   // 1 行目（元 A）を M に
                pv.SaveLines[1].ProductIDText = a.P.ToString();   // 2 行目（元 M）を A に
            });

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            List<TestData.DetailRow> ds = TestData.GetDetails(o);
            Assert.That(ds.Count, Is.EqualTo(2));
            TestData.DetailRow da = ds.Single(d => d.ProductID == a.P);
            TestData.DetailRow dm = ds.Single(d => d.ProductID == m.P);
            Assert.That(da.UnitPrice, Is.EqualTo(50.00m), "A に 2 行目の入力値");
            Assert.That(da.Quantity, Is.EqualTo(2));
            Assert.That(dm.UnitPrice, Is.EqualTo(100.00m), "M に 1 行目の入力値");
            Assert.That(dm.Quantity, Is.EqualTo(12));
        }

        [Test, Basis("EV-ORD-308"), Basis("OD-T6")]
        public void OrdTfSave_編集で削除した行の商品を別の行に付け替える()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12), m = L(c.P2, 50.00m, 0.25f, 2);
            int o = NewEditOrder(c, a, m);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a, m }, pv =>
            {
                pv.SaveLines = new List<OrdTfSaveLine> { Same(m) };
                pv.SaveLines[0].ProductIDText = a.P.ToString();   // M の行の商品を A に
                pv.DeletedProductIDs = new List<int> { a.P };     // A の行を削除
            });

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            List<TestData.DetailRow> ds = TestData.GetDetails(o);
            Assert.That(ds.Count, Is.EqualTo(1));
            Assert.That(ds[0].ProductID, Is.EqualTo(a.P));
            Assert.That(ds[0].UnitPrice, Is.EqualTo(50.00m), "M の行の入力値");
            Assert.That(ds[0].Quantity, Is.EqualTo(2));
        }

        [Test, Basis("OD-T3")]
        public void OrdTfSave_編集で既存の廃番商品の行は残せる()
        {
            Ctx c = NewCtx();
            int q = data.NewProduct(mark: 'Q', discontinued: true, unitPrice: 5.00m);
            Ln lq = L(q, 5.00m, 0f, 2), lp = L(c.P1, 10.00m, 0f, 1);
            int o = NewEditOrder(c, lq, lp);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { lq, lp }, pv => pv.SaveLines[1].QuantityText = "9");

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            List<TestData.DetailRow> ds = TestData.GetDetails(o);
            Assert.That(ds.Count, Is.EqualTo(2));
            Assert.That(ds.Single(d => d.ProductID == q).RowVersion, Is.EqualTo(1), "Q の行は変えていない");
            Assert.That(ds.Single(d => d.ProductID == c.P1).Quantity, Is.EqualTo(9));
        }

        [Test, Basis("OD-T3")]
        public void OrdTfSave_編集で既存の廃番商品の行は値を変えても保存できる()
        {
            Ctx c = NewCtx();
            int q = data.NewProduct(mark: 'Q', discontinued: true, unitPrice: 5.00m);
            Ln lq = L(q, 5.00m, 0f, 2);
            int o = NewEditOrder(c, lq);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { lq }, pv => pv.SaveLines[0].QuantityText = "3");

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            TestData.DetailRow d = TestData.GetDetails(o).Single(x => x.ProductID == q);
            Assert.That(d.Quantity, Is.EqualTo(3));
            Assert.That(d.RowVersion, Is.EqualTo(2));
        }

        [Test, Basis("OD-T3"), Basis("ERR-BIZ")]
        public void OrdTfSave_編集で新規に廃番商品は足せない()
        {
            Ctx c = NewCtx();
            int q = data.NewProduct(mark: 'Q', discontinued: true, unitPrice: 5.00m);
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv => pv.SaveLines.Add(NewLine(q, "5.00", "0", "1")));

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessages, Does.Contain(MsgDiscontinued));
            Assert.That(TestData.GetRowVersion(o), Is.EqualTo(1));
            Assert.That(TestData.GetDetails(o).Select(d => d.ProductID).ToList(), Is.EqualTo(new List<int> { a.P }));
        }

        [Test, Basis("OD-T3"), Basis("EV-ORD-308"), Basis("ERR-BIZ"), Basis("DEC-ORD-49")]
        public void OrdTfSave_編集で読み込み時に無かった行にOriginalProductIDを付けても廃番商品は足せない()
        {
            Ctx c = NewCtx();
            int q = data.NewProduct(mark: 'Q', discontinued: true, unitPrice: 5.00m);
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);   // Q の明細は無い

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv =>
            {
                OrdTfSaveLine l = NewLine(q, "5.00", "0", "1");
                l.OriginalProductID = q;      // 細工した入力：読み込み時の行だと名乗る（DB に (o, Q) の明細は無い）
                l.OriginalUnitPrice = 5.00m;
                l.OriginalDiscount = 0m;
                l.OriginalQuantity = 1;
                pv.SaveLines.Add(l);
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessages, Does.Contain(MsgDiscontinued));
            Assert.That(TestData.GetRowVersion(o), Is.EqualTo(1));
            Assert.That(TestData.GetDetails(o).Select(d => d.ProductID).ToList(), Is.EqualTo(new List<int> { a.P }), "Q の行は入らない");
        }

        [Test, Basis("VAL-REQUIRED"), Basis("ORD-T1"), Basis("EV-ORD-308"), Basis("DEC-CMN-05"), Basis("DEC-ORD-48")]
        [TestCase("CustomerID")]
        [TestCase("EmployeeID")]
        [TestCase("OrderDate")]
        public void OrdTfSave_編集でも必須の欠けは違反(string field)
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv =>
            {
                if (field == "CustomerID") pv.CustomerID = "";
                if (field == "EmployeeID") pv.EmployeeIDText = "";
                if (field == "OrderDate") pv.OrderDateText = "";
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { field }));
            Assert.That(rv.ErrorMessages, Is.Empty);
            Assert.That(rv.OrderID, Is.EqualTo(0), "検証の違反のとき OrderID は 0");
            TestData.OrderRow row = TestData.GetOrder(o);
            Assert.That(row.RowVersion, Is.EqualTo(1));
            Assert.That(row.CustomerID, Is.EqualTo(c.K));
            Assert.That(row.EmployeeID, Is.EqualTo(c.E));
            Assert.That(row.OrderDate, Is.EqualTo(D(2199, 3, 5)));
            Assert.That(TestData.GetDetails(o).Count, Is.EqualTo(1));
        }

        [Test, Basis("VAL-REQUIRED"), Basis("VAL-EXISTS"), Basis("EV-ORD-308"), Basis("DEC-ORD-49")]
        [TestCase(null)]
        [TestCase("abc")]
        public void OrdTfSave_編集で読み込み時の行の商品を空や不正にしても検証は外れない(string text)
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv => pv.SaveLines[0].ProductIDText = text);   // OriginalProductID = A のまま

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "Lines[0].ProductID" }));
            Assert.That(rv.ErrorMessages, Is.Empty);
            Assert.That(TestData.GetRowVersion(o), Is.EqualTo(1));
            List<TestData.DetailRow> ds = TestData.GetDetails(o);
            Assert.That(ds.Count, Is.EqualTo(1));
            Assert.That(ds[0].ProductID, Is.EqualTo(a.P));
            Assert.That(ds[0].Quantity, Is.EqualTo(12));
        }
        [Test, Basis("ORD-T5"), Basis("EV-ORD-308"), Basis("DEC-ORD-49")]
        [TestCase(false)]
        [TestCase(true)]
        public void OrdTfSave_編集で削除の印と同じ商品の行を残しても明細は1行以上残る(bool changeQuantity)
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv =>
            {
                if (changeQuantity) pv.SaveLines[0].QuantityText = "13";
                pv.DeletedProductIDs = new List<int> { a.P };   // 同じ商品を、残す行と削除の印の両方に指定する細工した入力
            });

            // ErrorFlag の真偽は期待にしない（エラーにして DB を変えない、または削除の印を無視して行を残す）。明細は 0 行にならない
            Assert.That(TestData.GetDetails(o).Count, Is.GreaterThanOrEqualTo(1), "明細が 0 行にならない（ORD-T5）");
            if (rv.ErrorFlag)
            {
                Assert.That(TestData.GetRowVersion(o), Is.EqualTo(1));
                TestData.DetailRow d = TestData.GetDetails(o).Single();
                Assert.That(d.ProductID, Is.EqualTo(a.P));
                Assert.That(d.Quantity, Is.EqualTo(12), "保存の前と同じ");
            }
        }
        [Test, Basis("ORD-T5"), Basis("ERR-BIZ")]
        public void OrdTfSave_編集で全明細を削除はできない()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv =>
            {
                pv.SaveLines = new List<OrdTfSaveLine>();
                pv.DeletedProductIDs = new List<int> { a.P };
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessages, Does.Contain(MsgNoLine));
            Assert.That(TestData.GetDetails(o).Count, Is.EqualTo(1), "明細 A が残る");
            Assert.That(TestData.GetRowVersion(o), Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-308"), Basis("ERR-CONFLICT")]
        public void OrdTfSave_編集で行バージョンが違えばERR_CONFLICTでロールバック()
        {
            Ctx c = NewCtx();
            int px = data.NewProduct(mark: 'X', unitPrice: 20.00m);
            Ln a = L(c.P1, 100.00m, 0.15f, 12), m = L(c.P2, 50.00m, 0.25f, 2);
            int o = NewEditOrder(c, a, m);
            TestData.BumpOrder(o, 99.00m);   // 行バージョン = 2

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a, m }, pv =>
            {
                pv.SaveLines = new List<OrdTfSaveLine> { Same(m), NewLine(px, "20.00", "0", "1") };
                pv.SaveLines[0].QuantityText = "9";   // M の数量を変える
                pv.DeletedProductIDs = new List<int> { a.P };   // A を削除
                pv.FreightText = "1.00";
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgConflict));
            List<TestData.DetailRow> ds = TestData.GetDetails(o);
            Assert.That(ds.Select(d => d.ProductID).OrderBy(i => i).ToList(), Is.EqualTo(new List<int> { a.P, m.P }.OrderBy(i => i).ToList()), "A・M のまま（X は無い）");
            Assert.That(ds.Single(d => d.ProductID == m.P).Quantity, Is.EqualTo(2), "M の数量も元のまま（ロールバック）");
            TestData.OrderRow row = TestData.GetOrder(o);
            Assert.That(row.Freight, Is.EqualTo(99.00m));
            Assert.That(row.RowVersion, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-308"), Basis("ERR-NOTFOUND"), Basis("共通仕様 9.1 節")]
        public void OrdTfSave_編集で存在しない受注はERR_NOTFOUND()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);
            TestData.DeleteOrder(o);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv => { });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorMessage, Is.EqualTo(MsgNotFound), "CONFLICT の文言ではない");
            Assert.That(TestData.CountOrders(o), Is.EqualTo(0));
            Assert.That(TestData.CountDetails(o), Is.EqualTo(0));
        }

        [Test, Basis("EV-ORD-308"), Basis("ERR-VALIDATION"), Basis("ERR-NOTFOUND")]
        public void OrdTfSave_編集で消された受注でも検証の違反が先に返る()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);
            TestData.DeleteOrder(o);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv => pv.FreightText = "-1");

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "Freight" }), "検証の違反が先（手順 1）");
            Assert.That(rv.ErrorMessage, Is.Not.EqualTo(MsgNotFound), "ERR-NOTFOUND の文言ではない");
        }
        [Test, Basis("EV-ORD-308"), Basis("ERR-VALIDATION")]
        public void OrdTfSave_編集の検証エラーでは何も更新しない()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12), m = L(c.P2, 50.00m, 0.25f, 2);
            int o = NewEditOrder(c, a, m);
            // 入力の並びは M の行が 2 行目になるよう A・M の順
            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a, m }, pv =>
            {
                pv.FreightText = "-1";
                pv.SaveLines[1].QuantityText = "0";
            });

            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "Freight", "Lines[1].Quantity" }));
            Assert.That(rv.ErrorMessages, Is.Empty);
            TestData.OrderRow row = TestData.GetOrder(o);
            Assert.That(row.RowVersion, Is.EqualTo(1));
            Assert.That(row.Freight, Is.EqualTo(5.00m));
            List<TestData.DetailRow> ds = TestData.GetDetails(o);
            Assert.That(ds.Count, Is.EqualTo(2));
            Assert.That(ds.Single(d => d.ProductID == m.P).Quantity, Is.EqualTo(2));
        }

        [Test, Basis("共通仕様 9.1 節"), Basis("ERR-CONFLICT")]
        public void OrdTfSave_編集で保存の後は古い行バージョンで保存できない()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);

            OrdTfReturnValue r1 = SaveEdit(c, o, 1, new[] { a }, pv => pv.FreightText = "6.00");
            OrdTfReturnValue r2 = SaveEdit(c, o, 1, new[] { a }, pv => pv.FreightText = "7.00");
            OrdTfReturnValue r3 = SaveEdit(c, o, r1.RowVersion, new[] { a }, pv => pv.FreightText = "8.00");

            Assert.That(r1.ErrorFlag, Is.False, "(1) " + r1.ErrorMessage);
            Assert.That(r1.RowVersion, Is.EqualTo(2), "(1)");
            Assert.That(r2.ErrorFlag, Is.True, "(2)");
            Assert.That(r2.ErrorMessage, Is.EqualTo(MsgConflict), "(2)");
            Assert.That(r3.ErrorFlag, Is.False, "(3) " + r3.ErrorMessage);
            Assert.That(r3.RowVersion, Is.EqualTo(3), "(3)");
        }

        [Test, Basis("EV-ORD-308"), Basis("OD-T6")]
        public void OrdTfSave_編集で他の受注の同じ商品の明細に触れない()
        {
            Ctx c = NewCtx();
            Ln a = L(c.P1, 100.00m, 0.15f, 12);
            int o = NewEditOrder(c, a);
            int o2 = NewEditOrder(c, a);
            int pm = data.NewProduct(mark: 'M', unitPrice: 20.00m);

            OrdTfReturnValue rv = SaveEdit(c, o, 1, new[] { a }, pv =>
            {
                pv.SaveLines[0].QuantityText = "13";
                pv.SaveLines.Add(NewLine(pm, "20.00", "0", "1"));
            });

            Assert.That(rv.ErrorFlag, Is.False, rv.ErrorMessage);
            Assert.That(TestData.GetDetails(o).Single(d => d.ProductID == a.P).Quantity, Is.EqualTo(13), "o の A は更新された");
            TestData.DetailRow d2 = TestData.GetDetails(o2).Single(d => d.ProductID == a.P);
            Assert.That(d2.Quantity, Is.EqualTo(12));
            Assert.That(d2.RowVersion, Is.EqualTo(1));
        }
    }
}
