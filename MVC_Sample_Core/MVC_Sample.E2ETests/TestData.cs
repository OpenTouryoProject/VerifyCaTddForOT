using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text.Json;

namespace MVC_Sample.E2ETests
{
    /// <summary>
    /// テストが自分で入れて自分で消すデータ（Northwind の元の行は触らない）。
    /// 準備・後始末は B層を通さず、直接 SQL で行う。
    /// </summary>
    internal sealed class TestData
    {
        private static readonly Random Rnd = new Random();

        private readonly List<string> customers = new List<string>();
        private readonly List<int> employees = new List<int>();
        private readonly List<int> shippers = new List<int>();
        private readonly List<int> orders = new List<int>();

        /// <summary>顧客の名前の記録（CustomerID → CompanyName）</summary>
        public readonly Dictionary<string, string> CustomerNames = new Dictionary<string, string>();

        /// <summary>配送業者の名前の記録（ShipperID → CompanyName）</summary>
        public readonly Dictionary<int, string> ShipperNames = new Dictionary<int, string>();

        private static string connectionString;

        /// <summary>接続文字列は Web の appsettings.json の connectionStrings から読む（コメント付きの JSON）</summary>
        private static string ConnectionString
        {
            get
            {
                if (connectionString == null)
                {
                    string path = Path.Combine(AppHost.WebDir, "appsettings.json");
                    JsonDocumentOptions opt = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
                    using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path), opt))
                    {
                        connectionString = doc.RootElement.GetProperty("connectionStrings")
                            .GetProperty("ConnectionString_SQL").GetString();
                    }
                }
                return connectionString;
            }
        }

        private static string Guid8() { return Guid.NewGuid().ToString("N").Substring(0, 8); }

        private static string Guid4() { return Guid.NewGuid().ToString("N").Substring(0, 4); }

        private static SqlParameter P(string name, SqlDbType type, object value)
        {
            return new SqlParameter(name, type) { Value = value == null ? DBNull.Value : value };
        }

        /// <summary>IDENTITY の値を返す INSERT などを 1 つのコマンドで実行する</summary>
        private static object ExecScalar(string sql, params SqlParameter[] ps)
        {
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddRange(ps);
                cn.Open();
                return cmd.ExecuteScalar();
            }
        }

        // ---------------------------------------------------------------- 顧客

        /// <summary>英大文字 5 桁の顧客 ID（DB に無いもの）</summary>
        public static string NewCustomerId()
        {
            while (true)
            {
                string id = new string(Enumerable.Range(0, 5).Select(_ => (char)('A' + Rnd.Next(26))).ToArray());
                object exists = ExecScalar("SELECT COUNT(*) FROM Customers WHERE CustomerID = @id", P("@id", SqlDbType.NChar, id));
                if (Convert.ToInt32(exists) == 0) return id;
            }
        }

        /// <summary>顧客を入れる。会社名は "Tf" + 目印 + GUID 8 桁</summary>
        public string NewCustomer(char mark = 'C', string id = null)
        {
            if (id == null) id = NewCustomerId();
            string name = "Tf" + mark + Guid8();
            customers.Add(id);   // 入れる前に記録する（途中で落ちても後始末できる）
            ExecScalar("INSERT INTO Customers (CustomerID, CompanyName) VALUES (@id, @n)",
                P("@id", SqlDbType.NChar, id), P("@n", SqlDbType.NVarChar, name));
            CustomerNames[id] = name;
            return id;
        }

        // ---------------------------------------------------------------- 担当者・配送業者

        /// <summary>担当者を入れる（姓・名を指定）。EmployeeID を返す</summary>
        public int NewEmployee(string last, string first)
        {
            int id = Convert.ToInt32(ExecScalar(
                "INSERT INTO Employees (LastName, FirstName) VALUES (@l, @f); SELECT CAST(SCOPE_IDENTITY() AS int);",
                P("@l", SqlDbType.NVarChar, last), P("@f", SqlDbType.NVarChar, first)));
            employees.Add(id);
            return id;
        }

        /// <summary>姓の目印付きで作る名前（姓は nvarchar(20) に収める）</summary>
        public static string NewLastName(char mark) { return "Tf" + mark + Guid8(); }

        /// <summary>名の目印付きで作る名前（名は nvarchar(10) に収める）</summary>
        public static string NewFirstName(char mark) { return mark.ToString() + Guid4(); }

        /// <summary>配送業者を入れる。ShipperID を返す</summary>
        public int NewShipper(string name = null)
        {
            if (name == null) name = "TfSh" + Guid8();
            int id = Convert.ToInt32(ExecScalar(
                "INSERT INTO Shippers (CompanyName) VALUES (@n); SELECT CAST(SCOPE_IDENTITY() AS int);",
                P("@n", SqlDbType.NVarChar, name)));
            shippers.Add(id);
            ShipperNames[id] = name;
            return id;
        }

        // ---------------------------------------------------------------- 受注・明細

        /// <summary>受注を入れる。OrderID を返す（送料は既定 0。null で NULL）</summary>
        public int NewOrder(string customerId = null, int? employeeId = null, DateTime? orderDate = null,
            DateTime? requiredDate = null, DateTime? shippedDate = null, int? shipVia = null, decimal? freight = 0m)
        {
            int id = Convert.ToInt32(ExecScalar(
                "INSERT INTO Orders (CustomerID, EmployeeID, OrderDate, RequiredDate, ShippedDate, ShipVia, Freight) " +
                "VALUES (@c, @e, @od, @rd, @sd, @sv, @fr); SELECT CAST(SCOPE_IDENTITY() AS int);",
                P("@c", SqlDbType.NChar, customerId), P("@e", SqlDbType.Int, employeeId),
                P("@od", SqlDbType.DateTime, orderDate), P("@rd", SqlDbType.DateTime, requiredDate),
                P("@sd", SqlDbType.DateTime, shippedDate), P("@sv", SqlDbType.Int, shipVia),
                P("@fr", SqlDbType.Money, freight)));
            orders.Add(id);
            return id;
        }

        /// <summary>明細を入れる（割引は real で格納される）</summary>
        public void NewDetail(int orderId, int productId, decimal unitPrice, short quantity, float discount)
        {
            ExecScalar(
                "INSERT INTO [Order Details] (OrderID, ProductID, UnitPrice, Quantity, Discount) VALUES (@o, @p, @u, @q, @d)",
                P("@o", SqlDbType.Int, orderId), P("@p", SqlDbType.Int, productId),
                P("@u", SqlDbType.Money, unitPrice), P("@q", SqlDbType.SmallInt, quantity),
                P("@d", SqlDbType.Real, discount));
        }

        /// <summary>
        /// 顧客に受注を n 件入れる。i 番目（1 から）の受注日 = start + i 日。OrderID を i の昇順で返す。
        /// </summary>
        public List<int> NewOrdersSequence(string customerId, int n, DateTime start)
        {
            List<int> ids = new List<int>();
            for (int i = 1; i <= n; i++)
            {
                ids.Add(NewOrder(customerId: customerId, orderDate: start.AddDays(i)));
            }
            return ids;
        }

        /// <summary>入れた受注を消す（画面の操作の途中で、DB の受注を減らすために使う）</summary>
        public void DeleteOrders(IEnumerable<int> ids)
        {
            foreach (int o in ids)
            {
                ExecScalar("DELETE FROM [Order Details] WHERE OrderID = @o", P("@o", SqlDbType.Int, o));
                ExecScalar("DELETE FROM Orders WHERE OrderID = @o", P("@o", SqlDbType.Int, o));
            }
        }

        // ---------------------------------------------------------------- 後始末

        /// <summary>入れたデータを消す（親子は子から：明細 → 受注 → 顧客・担当者・配送業者）</summary>
        public void CleanUp()
        {
            Exception first = null;
            Action<string, SqlParameter> run = (sql, p) =>
            {
                try { ExecScalar(sql, p); }
                catch (Exception ex) { if (first == null) first = ex; }
            };

            foreach (string c in customers)
            {
                run("DELETE FROM [Order Details] WHERE OrderID IN (SELECT OrderID FROM Orders WHERE CustomerID = @c)",
                    P("@c", SqlDbType.NChar, c));
                run("DELETE FROM Orders WHERE CustomerID = @c", P("@c", SqlDbType.NChar, c));
            }
            foreach (int o in orders)
            {
                run("DELETE FROM [Order Details] WHERE OrderID = @o", P("@o", SqlDbType.Int, o));
                run("DELETE FROM Orders WHERE OrderID = @o", P("@o", SqlDbType.Int, o));
            }
            foreach (string c in customers)
                run("DELETE FROM Customers WHERE CustomerID = @c", P("@c", SqlDbType.NChar, c));
            foreach (int e in employees)
                run("DELETE FROM Employees WHERE EmployeeID = @e", P("@e", SqlDbType.Int, e));
            foreach (int s in shippers)
                run("DELETE FROM Shippers WHERE ShipperID = @s", P("@s", SqlDbType.Int, s));

            customers.Clear(); employees.Clear(); shippers.Clear(); orders.Clear();
            CustomerNames.Clear(); ShipperNames.Clear();

            if (first != null) throw first;
        }
    }
}
