using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

using MVC_Sample.Logic.Business;
using MVC_Sample.Logic.Common;

using Touryo.Infrastructure.Business.Util;
using Touryo.Infrastructure.Public.Db;
using Touryo.Infrastructure.Public.Util;

namespace MVC_Sample.Tests
{
    /// <summary>
    /// OrdTfBusiness.OrdTfSearch の単体テスト。
    /// 仕様：docs/spec/test6-testfirst-mvc-core-tests.md（NorthwindDbSampleSpec@6545f92）
    /// </summary>
    public class OrdTfBusinessTests
    {
        private const string Method = "OrdTfSearch";

        private static readonly Random Rnd = new Random();

        /// <summary>顧客 A・B（英大文字 5 桁。CUS-T1 / VAL-CODE）</summary>
        private string custA;
        private string custB;

        private readonly DateTime today = DateTime.Today;

        #region 前処理・後処理

        [SetUp]
        public void SetUp()
        {
            // 既存の担当者（1・2）を参照する。無ければ前提が崩れているので失敗させる。
            Assert.That(Scalar("SELECT COUNT(*) FROM Employees WHERE EmployeeID IN (1, 2)"), Is.EqualTo(2),
                "前提：Employees に EmployeeID = 1・2 が存在すること");
            // 既存の商品（1・2）、配送業者（1）も参照する。
            Assert.That(Scalar("SELECT COUNT(*) FROM Products WHERE ProductID IN (1, 2)"), Is.EqualTo(2),
                "前提：Products に ProductID = 1・2 が存在すること");
            Assert.That(Scalar("SELECT COUNT(*) FROM Shippers WHERE ShipperID = 1"), Is.EqualTo(1),
                "前提：Shippers に ShipperID = 1 が存在すること");

            custA = NewCustomerId();
            do { custB = NewCustomerId(); } while (custB == custA);
            Exec("INSERT INTO Customers (CustomerID, CompanyName) VALUES (@id, @name)",
                P("@id", custA), P("@name", "TestCo-" + custA));
            Exec("INSERT INTO Customers (CustomerID, CompanyName) VALUES (@id, @name)",
                P("@id", custB), P("@name", "TestCo-" + custB));
        }

        [TearDown]
        public void TearDown()
        {
            // 失敗したテストの後でも消える。テストが作った顧客の受注だけを消す。
            foreach (string c in new[] { custA, custB })
            {
                if (c == null) continue;
                Exec("DELETE FROM [Order Details] WHERE OrderID IN (SELECT OrderID FROM Orders WHERE CustomerID = @c)", P("@c", c));
                Exec("DELETE FROM Orders WHERE CustomerID = @c", P("@c", c));
                Exec("DELETE FROM Customers WHERE CustomerID = @c", P("@c", c));
            }
        }

        #endregion

        #region 2.1 抽出条件（EV-ORD-102 手順 2）

        [Test, Basis("EV-ORD-102")]
        public void OrdTfSearch_顧客で絞る()
        {
            AddOrder(custA, today); AddOrder(custA, today); AddOrder(custB, today);

            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(2));
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(2));
            Assert.That(CustomerNames(rv), Is.All.EqualTo("TestCo-" + custA));
        }

        [Test, Basis("EV-ORD-102")]
        public void OrdTfSearch_担当者で絞る()
        {
            AddOrder(custA, today, employeeId: 1); AddOrder(custA, today, employeeId: 1); AddOrder(custA, today, employeeId: 2);

            var rv = Search(pv => { pv.CustomerID = custA; pv.EmployeeID = 1; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(2));
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(2));
            Assert.That(EmployeeNames(rv).Distinct().Count(), Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-102")]
        public void OrdTfSearch_受注日開始を含む()
        {
            AddOrder(custA, today.AddDays(-1)); AddOrder(custA, today); AddOrder(custA, today.AddDays(1));

            var rv = Search(pv => { pv.CustomerID = custA; pv.OrderDateFrom = today; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(2)); // D と D+1（当該日を含む）
        }

        [Test, Basis("EV-ORD-102")]
        public void OrdTfSearch_受注日終了を含む()
        {
            AddOrder(custA, today.AddDays(-1)); AddOrder(custA, today); AddOrder(custA, today.AddDays(1));

            var rv = Search(pv => { pv.CustomerID = custA; pv.OrderDateTo = today; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(2)); // D-1 と D（当該日を含む）
        }

        [Test, Basis("EV-ORD-102")]
        public void OrdTfSearch_受注日の範囲()
        {
            AddOrder(custA, today.AddDays(-1)); AddOrder(custA, today); AddOrder(custA, today.AddDays(1));

            var rv = Search(pv => { pv.CustomerID = custA; pv.OrderDateFrom = today; pv.OrderDateTo = today; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(1)); // 開始 = 終了は有効
        }

        [Test, Basis("EV-ORD-102")]
        public void OrdTfSearch_条件はANDで組み合わさる()
        {
            AddOrder(custA, today, employeeId: 1);
            AddOrder(custA, today.AddDays(5), employeeId: 1);
            AddOrder(custA, today, employeeId: 2);

            var rv = Search(pv =>
            {
                pv.CustomerID = custA; pv.EmployeeID = 1; pv.OrderDateFrom = today; pv.OrderDateTo = today;
            });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-102")]
        public void OrdTfSearch_未指定の条件は加えない()
        {
            // 既存のどの受注より新しい受注日にし、既定の並び（受注日の降順）で 1 ページ目に入れる。
            DateTime far = new DateTime(2199, 1, 1);
            int id1 = AddOrder(custA, far); int id2 = AddOrder(custA, far);

            var rv = Search(pv => { });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.GreaterThanOrEqualTo(2));
            var ids = OrderIds(rv);
            Assert.That(ids, Does.Contain(id1));
            Assert.That(ids, Does.Contain(id2));
        }

        [Test, Basis("EV-ORD-102"), Basis("ORD-T2")]
        public void OrdTfSearch_未出荷()
        {
            AddOrder(custA, today, shipped: null); AddOrder(custA, today, shipped: null); AddOrder(custA, today, shipped: today);

            var rv = Search(pv => { pv.CustomerID = custA; pv.ShipStatus = "未出荷"; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(2));
        }

        [Test, Basis("EV-ORD-102"), Basis("ORD-T2")]
        public void OrdTfSearch_出荷済()
        {
            AddOrder(custA, today, shipped: null); AddOrder(custA, today, shipped: null); AddOrder(custA, today, shipped: today);

            var rv = Search(pv => { pv.CustomerID = custA; pv.ShipStatus = "出荷済"; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-102"), Basis("ORD-T7")]
        public void OrdTfSearch_納期遅延_未出荷で期限切れ()
        {
            AddOrder(custA, today.AddDays(-10), required: today.AddDays(-1), shipped: null);

            var rv = Search(pv => { pv.CustomerID = custA; pv.ShipStatus = "納期遅延"; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-102"), Basis("ORD-T7")]
        public void OrdTfSearch_納期遅延_出荷が期限後()
        {
            AddOrder(custA, today.AddDays(-20), required: today.AddDays(-10), shipped: today.AddDays(-5));

            var rv = Search(pv => { pv.CustomerID = custA; pv.ShipStatus = "納期遅延"; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(1));
        }

        [Test, Basis("EV-ORD-102"), Basis("ORD-T7")]
        public void OrdTfSearch_納期遅延でない行は除く()
        {
            AddOrder(custA, today.AddDays(-10), required: today, shipped: null);                                  // (a) 今日（< 当日でない）
            AddOrder(custA, today.AddDays(-10), required: today.AddDays(1), shipped: null);                       // (b) 明日
            AddOrder(custA, today.AddDays(-10), required: today.AddDays(-5), shipped: today.AddDays(-5));         // (c) 出荷日 = 期限
            AddOrder(custA, today.AddDays(-10), required: today.AddDays(-5), shipped: today.AddDays(-6));         // (d) 出荷日 < 期限

            var rv = Search(pv => { pv.CustomerID = custA; pv.ShipStatus = "納期遅延"; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(0));
        }

        [Test, Basis("EV-ORD-102"), Basis("ORD-T7")]
        public void OrdTfSearch_納期遅延_RequiredDateがNULLは対象外()
        {
            AddOrder(custA, today.AddDays(-10), requiredIsNull: true, shipped: null);
            AddOrder(custA, today.AddDays(-10), requiredIsNull: true, shipped: today.AddDays(-5));

            var rv = Search(pv => { pv.CustomerID = custA; pv.ShipStatus = "納期遅延"; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(0));
        }

        #endregion

        #region 2.2 一覧の内容（EV-ORD-101 手順 4〜7、EV-ORD-102 手順 3〜4）

        [Test, Basis("EV-ORD-101"), Basis("SC-ORD-01-I06")]
        public void OrdTfSearch_既定の並びは受注日の降順_同日はID降順()
        {
            int s1 = AddOrder(custA, today);               // 同日の ID 小
            int s2 = AddOrder(custA, today);               // 同日の ID 大
            int old = AddOrder(custA, today.AddDays(-1));

            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(OrderIds(rv), Is.EqualTo(new List<int> { s2, s1, old }));
        }

        [Test, Basis("EV-ORD-101"), Basis("EV-ORD-102")]
        public void OrdTfSearch_1ページは20件_総件数は全体()
        {
            for (int i = 0; i < 25; i++) AddOrder(custA, today);

            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(20));
            Assert.That(rv.TotalCount, Is.EqualTo(25));
        }

        [Test, Basis("EV-ORD-101")]
        public void OrdTfSearch_ちょうど20件()
        {
            for (int i = 0; i < 20; i++) AddOrder(custA, today);

            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(20));
            Assert.That(rv.TotalCount, Is.EqualTo(20));
        }

        [Test, Basis("EV-ORD-101")]
        public void OrdTfSearch_該当なしはエラーにしない()
        {
            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows, Is.Not.Null);
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(0));
            Assert.That(rv.TotalCount, Is.EqualTo(0));
        }

        [Test, Basis("状況の判定"), Basis("ORD-T7")]
        public void OrdTfSearch_状況_未出荷()
        {
            AddOrder(custA, today, required: today.AddDays(1), shipped: null);
            Assert.That(OneStatus(Search(pv => pv.CustomerID = custA)), Is.EqualTo("未出荷"));
        }

        [Test, Basis("状況の判定"), Basis("ORD-T7")]
        public void OrdTfSearch_状況_納期遅延_未出荷()
        {
            AddOrder(custA, today.AddDays(-10), required: today.AddDays(-1), shipped: null);
            Assert.That(OneStatus(Search(pv => pv.CustomerID = custA)), Is.EqualTo("納期遅延"));
        }

        [Test, Basis("状況の判定"), Basis("ORD-T7")]
        public void OrdTfSearch_状況_納期遅延_出荷済()
        {
            AddOrder(custA, today.AddDays(-20), required: today.AddDays(-10), shipped: today.AddDays(-5));
            Assert.That(OneStatus(Search(pv => pv.CustomerID = custA)), Is.EqualTo("納期遅延"));
        }

        [Test, Basis("状況の判定"), Basis("ORD-T7")]
        public void OrdTfSearch_状況_出荷済()
        {
            AddOrder(custA, today.AddDays(-20), required: today.AddDays(-10), shipped: today.AddDays(-10));
            Assert.That(OneStatus(Search(pv => pv.CustomerID = custA)), Is.EqualTo("出荷済"));
        }

        [Test, Basis("状況の判定"), Basis("ORD-T7")]
        public void OrdTfSearch_状況_RequiredDateがNULLは遅延にしない()
        {
            int unshipped = AddOrder(custA, today.AddDays(-10), requiredIsNull: true, shipped: null);
            int shippedId = AddOrder(custA, today.AddDays(-9), requiredIsNull: true, shipped: today.AddDays(-5));

            // 出荷状況では絞らず、顧客だけで検索する。
            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(2));
            Assert.That(StatusOf(rv, unshipped), Is.EqualTo("未出荷"));
            Assert.That(StatusOf(rv, shippedId), Is.EqualTo("出荷済"));
        }

        [Test, Basis("受注検索クエリ"), Basis("Common §3 金額の算出式")]
        public void OrdTfSearch_合計金額()
        {
            int id = AddOrder(custA, today, freight: 5.00m);
            AddDetail(id, 1, 10.00m, 0.15f, 3);   // 10 × 0.85 × 3 = 25.50
            AddDetail(id, 2, 7.50m, 0f, 2);       // 7.5 × 1 × 2 = 15.00

            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(1));
            Assert.That(Convert.ToDecimal(rv.Rows.Rows[0]["TotalAmount"]), Is.EqualTo(45.50m));
        }

        [Test, Basis("Common §3 金額の算出式")]
        public void OrdTfSearch_合計金額_割引率realの誤差を出さない()
        {
            int id = AddOrder(custA, today, freight: 0m);
            AddDetail(id, 1, 19.99m, 0.1f, 3);    // 19.99 × 0.9 × 3 = 53.973 → 53.97

            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(1));
            Assert.That(Convert.ToDecimal(rv.Rows.Rows[0]["TotalAmount"]), Is.EqualTo(53.97m));
        }

        [Test, Basis("受注検索クエリ"), Basis("SC-ORD-01-I06 列 7")]
        public void OrdTfSearch_外部結合_配送業者がNULL()
        {
            AddOrder(custA, today, shipVia: null);

            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(1));
            Assert.That(Text(rv.Rows.Rows[0]["ShipperName"]), Is.EqualTo(""));
        }

        [Test, Basis("受注検索クエリ"), Basis("SC-ORD-01-I06 列 3")]
        public void OrdTfSearch_外部結合_担当者がNULL()
        {
            AddOrder(custA, today, employeeId: null);

            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(1));
            Assert.That(Text(rv.Rows.Rows[0]["EmployeeName"]), Is.EqualTo(""));
        }

        [Test, Basis("受注検索クエリ"), Basis("SC-ORD-01-I06 列 2・3")]
        public void OrdTfSearch_顧客名と担当者名を返す()
        {
            AddOrder(custA, today, employeeId: 1);
            string expected = QueryText("SELECT LastName + N' ' + FirstName FROM Employees WHERE EmployeeID = 1");

            var rv = Search(pv => pv.CustomerID = custA);

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(1));
            Assert.That(Text(rv.Rows.Rows[0]["CustomerName"]), Is.EqualTo("TestCo-" + custA));
            Assert.That(Text(rv.Rows.Rows[0]["EmployeeName"]), Is.EqualTo(expected));
        }

        #endregion

        #region 2.3 入力の検証（EV-ORD-102 手順 1、VAL-DATE）

        [Test, Basis("VAL-DATE"), Basis("ERR-VALIDATION")]
        public void OrdTfSearch_開始が終了より後はエラー()
        {
            AddOrder(custA, today);

            var rv = Search(pv =>
            {
                pv.CustomerID = custA; pv.OrderDateFrom = today.AddDays(1); pv.OrderDateTo = today;
            });

            // ErrorMessageID は確かめない（ID は後で決める。spec 質問 1）。
            Assert.That(rv.ErrorFlag, Is.True);
            Assert.That(rv.Rows == null || rv.Rows.Rows.Count == 0, Is.True);
            Assert.That(rv.TotalCount, Is.EqualTo(0));
        }

        [Test, Basis("VAL-DATE")]
        public void OrdTfSearch_開始のみ指定は検証を通る()
        {
            AddOrder(custA, today);

            var rv = Search(pv => { pv.CustomerID = custA; pv.OrderDateFrom = today; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(1)); // 検索まで進むこと（検証で弾かれていない）
        }

        [Test, Basis("VAL-DATE")]
        public void OrdTfSearch_終了のみ指定は検証を通る()
        {
            AddOrder(custA, today);

            var rv = Search(pv => { pv.CustomerID = custA; pv.OrderDateTo = today; });

            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.TotalCount, Is.EqualTo(1)); // 検索まで進むこと（検証で弾かれていない）
        }

        #endregion

        #region ヘルパ

        /// <summary>DoBusinessLogic の境界で呼ぶ（UOC_ は直接呼ばない）</summary>
        private static OrdTfReturnValue Search(Action<OrdTfParameterValue> fill)
        {
            var pv = new OrdTfParameterValue("OrdTfBusinessTests", "-", Method, "SQL", new MyUserInfo("test", "127.0.0.1"));
            fill(pv);
            return (OrdTfReturnValue)new OrdTfBusiness().DoBusinessLogic(pv, DbEnum.IsolationLevelEnum.User);
        }

        private static string NewCustomerId()
        {
            // 英大文字 5 桁：Z + ランダム 4 桁
            return "Z" + new string(Enumerable.Range(0, 4).Select(_ => (char)('A' + Rnd.Next(26))).ToArray());
        }

        private static List<int> OrderIds(OrdTfReturnValue rv)
        {
            return rv.Rows.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r["OrderID"])).ToList();
        }

        private static List<string> CustomerNames(OrdTfReturnValue rv)
        {
            return rv.Rows.Rows.Cast<DataRow>().Select(r => Text(r["CustomerName"])).ToList();
        }

        private static List<string> EmployeeNames(OrdTfReturnValue rv)
        {
            return rv.Rows.Rows.Cast<DataRow>().Select(r => Text(r["EmployeeName"])).ToList();
        }

        private static string OneStatus(OrdTfReturnValue rv)
        {
            Assert.That(rv.ErrorFlag, Is.False);
            Assert.That(rv.Rows.Rows.Count, Is.EqualTo(1));
            return Text(rv.Rows.Rows[0]["Status"]);
        }

        private static string StatusOf(OrdTfReturnValue rv, int orderId)
        {
            var row = rv.Rows.Rows.Cast<DataRow>().FirstOrDefault(r => Convert.ToInt32(r["OrderID"]) == orderId);
            Assert.That(row, Is.Not.Null, "OrderID = " + orderId + " の行が一覧に無い");
            return Text(row["Status"]);
        }

        private static string Text(object o)
        {
            return (o == null || o == DBNull.Value) ? "" : o.ToString();
        }

        /// <summary>受注を 1 件入れて OrderID を返す（既定：担当者 1・配送業者 1・未出荷・要求納期は受注日の 30 日後）</summary>
        private int AddOrder(string customerId, DateTime orderDate,
            DateTime? required = null, DateTime? shipped = null, int? employeeId = 1, int? shipVia = 1, decimal freight = 0m,
            bool requiredIsNull = false)
        {
            // required の省略 = 受注日の 30 日後。NULL にしたいときは requiredIsNull: true。
            object req = requiredIsNull ? null : (object)(required ?? orderDate.AddDays(30));
            return InsertOrder(customerId, orderDate, req, shipped, employeeId, shipVia, freight);
        }

        private int InsertOrder(string customerId, DateTime orderDate, object required, object shipped,
            object employeeId, object shipVia, decimal freight)
        {
            return Convert.ToInt32(ScalarObj(
                "INSERT INTO Orders (CustomerID, EmployeeID, OrderDate, RequiredDate, ShippedDate, ShipVia, Freight) " +
                "OUTPUT INSERTED.OrderID VALUES (@c, @e, @od, @rd, @sd, @sv, @f)",
                P("@c", customerId), P("@e", employeeId), P("@od", orderDate),
                P("@rd", required), P("@sd", shipped), P("@sv", shipVia), P("@f", freight)));
        }

        private void AddDetail(int orderId, int productId, decimal unitPrice, float discount, short qty)
        {
            Exec("INSERT INTO [Order Details] (OrderID, ProductID, UnitPrice, Quantity, Discount) VALUES (@o, @p, @u, @q, @d)",
                P("@o", orderId), P("@p", productId), P("@u", unitPrice), P("@q", qty), P("@d", discount));
        }

        private static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        private static SqlConnection Open()
        {
            var cn = new SqlConnection(GetConfigParameter.GetConnectionString("ConnectionString_SQL"));
            cn.Open();
            return cn;
        }

        private static int Exec(string sql, params SqlParameter[] ps)
        {
            using (var cn = Open())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddRange(ps);
                return cmd.ExecuteNonQuery();
            }
        }

        private static object ScalarObj(string sql, params SqlParameter[] ps)
        {
            using (var cn = Open())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddRange(ps);
                return cmd.ExecuteScalar();
            }
        }

        private static int Scalar(string sql, params SqlParameter[] ps)
        {
            return Convert.ToInt32(ScalarObj(sql, ps));
        }

        private static string QueryText(string sql, params SqlParameter[] ps)
        {
            return Text(ScalarObj(sql, ps));
        }

        #endregion
    }
}
