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
    /// 仕様：docs/spec/test3-testfirst-OrdTf-mvc-tests.md（NorthwindDbSampleSpec@e391dca）の 3 章。
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
    }
}
