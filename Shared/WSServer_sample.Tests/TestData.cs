using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using Touryo.Infrastructure.Public.Util;

namespace WSServer_sample.Tests
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
        private readonly List<int> products = new List<int>();
        private readonly List<string> shipNameMarks = new List<string>();

        /// <summary>顧客の名前の記録（CustomerID → CompanyName）</summary>
        public readonly Dictionary<string, string> CustomerNames = new Dictionary<string, string>();

        /// <summary>配送業者の名前の記録（ShipperID → CompanyName）</summary>
        public readonly Dictionary<int, string> ShipperNames = new Dictionary<int, string>();

        /// <summary>商品の名前の記録（ProductID → ProductName）</summary>
        public readonly Dictionary<int, string> ProductNames = new Dictionary<int, string>();

        private static string ConnectionString
        {
            get { return GetConfigParameter.GetConnectionString("ConnectionString_SQL"); }
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
        public string NewCustomer(char mark = 'C', string id = null, string phone = null, string address = null,
            string city = null, string region = null, string postalCode = null, string country = null)
        {
            if (id == null) id = NewCustomerId();
            string name = "Tf" + mark + Guid8();
            customers.Add(id);   // 入れる前に記録する（途中で落ちても後始末できる）
            ExecScalar("INSERT INTO Customers (CustomerID, CompanyName, Phone, Address, City, Region, PostalCode, Country) " +
                "VALUES (@id, @n, @ph, @ad, @ci, @re, @po, @co)",
                P("@id", SqlDbType.NChar, id), P("@n", SqlDbType.NVarChar, name),
                P("@ph", SqlDbType.NVarChar, phone), P("@ad", SqlDbType.NVarChar, address), P("@ci", SqlDbType.NVarChar, city),
                P("@re", SqlDbType.NVarChar, region), P("@po", SqlDbType.NVarChar, postalCode), P("@co", SqlDbType.NVarChar, country));
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

        /// <summary>配送業者を入れる。ShipperID を返す（mark 指定時の会社名は "TfSh" + 目印 + GUID 7 桁。並べ替えのケース用）</summary>
        public int NewShipper(string name = null, char? mark = null)
        {
            if (name == null) name = mark.HasValue ? "TfSh" + mark.Value + Guid.NewGuid().ToString("N").Substring(0, 7) : "TfSh" + Guid8();
            int id = Convert.ToInt32(ExecScalar(
                "INSERT INTO Shippers (CompanyName) VALUES (@n); SELECT CAST(SCOPE_IDENTITY() AS int);",
                P("@n", SqlDbType.NVarChar, name)));
            shippers.Add(id);
            ShipperNames[id] = name;
            return id;
        }

        // ---------------------------------------------------------------- 受注・明細

        /// <summary>受注を入れる。OrderID を返す（送料は既定 0。null で NULL。Ship* は既定 NULL）</summary>
        public int NewOrder(string customerId = null, int? employeeId = null, DateTime? orderDate = null,
            DateTime? requiredDate = null, DateTime? shippedDate = null, int? shipVia = null, decimal? freight = 0m,
            string shipName = null, string shipAddress = null, string shipCity = null, string shipRegion = null,
            string shipPostalCode = null, string shipCountry = null)
        {
            int id = Convert.ToInt32(ExecScalar(
                "INSERT INTO Orders (CustomerID, EmployeeID, OrderDate, RequiredDate, ShippedDate, ShipVia, Freight, " +
                "ShipName, ShipAddress, ShipCity, ShipRegion, ShipPostalCode, ShipCountry) " +
                "VALUES (@c, @e, @od, @rd, @sd, @sv, @fr, @sn, @sa, @sc, @sr, @sp, @sy); SELECT CAST(SCOPE_IDENTITY() AS int);",
                P("@c", SqlDbType.NChar, customerId), P("@e", SqlDbType.Int, employeeId),
                P("@od", SqlDbType.DateTime, orderDate), P("@rd", SqlDbType.DateTime, requiredDate),
                P("@sd", SqlDbType.DateTime, shippedDate), P("@sv", SqlDbType.Int, shipVia),
                P("@fr", SqlDbType.Money, freight),
                P("@sn", SqlDbType.NVarChar, shipName), P("@sa", SqlDbType.NVarChar, shipAddress),
                P("@sc", SqlDbType.NVarChar, shipCity), P("@sr", SqlDbType.NVarChar, shipRegion),
                P("@sp", SqlDbType.NVarChar, shipPostalCode), P("@sy", SqlDbType.NVarChar, shipCountry)));
            orders.Add(id);
            return id;
        }

        /// <summary>商品を入れる。ProductID を返す（名前の既定は "Tf" + GUID 8 桁。mark 指定時は "Tf" + 目印 + GUID 8 桁。在庫 null で NULL）</summary>
        public int NewProduct(string name = null, bool discontinued = false, int? unitsInStock = 0, char? mark = null, decimal? unitPrice = null)
        {
            if (name == null) name = mark.HasValue ? "Tf" + mark.Value + Guid8() : "Tf" + Guid8();
            int id = Convert.ToInt32(ExecScalar(
                "INSERT INTO Products (ProductName, UnitPrice, UnitsInStock, Discontinued) VALUES (@n, ISNULL(@u, 0), @s, @d); SELECT CAST(SCOPE_IDENTITY() AS int);",
                P("@n", SqlDbType.NVarChar, name), P("@u", SqlDbType.Money, unitPrice), P("@s", SqlDbType.SmallInt, unitsInStock), P("@d", SqlDbType.Bit, discontinued)));
            products.Add(id);
            ProductNames[id] = name;
            return id;
        }

        /// <summary>商品を消す（明細は消さない。商品マスタに無い明細を作る）</summary>
        public void DeleteProduct(int productId)
        {
            ExecScalar("DELETE FROM Products WHERE ProductID = @p", P("@p", SqlDbType.Int, productId));
        }

        // ---------------------------------------------------------------- DB の状態を読む・他の利用者の操作を模す

        /// <summary>受注の件数（OrderID 指定）</summary>
        public static int CountOrders(int orderId)
        {
            return Convert.ToInt32(ExecScalar("SELECT COUNT(*) FROM Orders WHERE OrderID = @o", P("@o", SqlDbType.Int, orderId)));
        }

        /// <summary>明細の件数（OrderID 指定）</summary>
        public static int CountDetails(int orderId)
        {
            return Convert.ToInt32(ExecScalar("SELECT COUNT(*) FROM [Order Details] WHERE OrderID = @o", P("@o", SqlDbType.Int, orderId)));
        }

        /// <summary>受注の行バージョン</summary>
        public static int GetRowVersion(int orderId)
        {
            return Convert.ToInt32(ExecScalar("SELECT RowVersion FROM Orders WHERE OrderID = @o", P("@o", SqlDbType.Int, orderId)));
        }

        /// <summary>他の利用者の更新を模す（送料を変え、行バージョンを +1）</summary>
        public static void BumpOrder(int orderId, decimal freight)
        {
            ExecScalar("UPDATE Orders SET Freight = @f, RowVersion = RowVersion + 1 WHERE OrderID = @o",
                P("@f", SqlDbType.Money, freight), P("@o", SqlDbType.Int, orderId));
        }

        /// <summary>他の利用者の削除を模す（明細 → 受注。記録は残す＝後始末は、無い行の削除でも落ちない）</summary>
        public static void DeleteOrder(int orderId)
        {
            ExecScalar("DELETE FROM [Order Details] WHERE OrderID = @o", P("@o", SqlDbType.Int, orderId));
            ExecScalar("DELETE FROM Orders WHERE OrderID = @o", P("@o", SqlDbType.Int, orderId));
        }

        // ---------------------------------------------------------------- 受注詳細（登録・編集）の確かめ

        /// <summary>Orders の 1 行（DB を直接読んだ値）</summary>
        public sealed class OrderRow
        {
            public string CustomerID;
            public int? EmployeeID;
            public DateTime? OrderDate;
            public DateTime? RequiredDate;
            public DateTime? ShippedDate;
            public int? ShipVia;
            public decimal? Freight;
            public string ShipName;
            public string ShipAddress;
            public string ShipCity;
            public string ShipRegion;
            public string ShipPostalCode;
            public string ShipCountry;
            public int RowVersion;
        }

        /// <summary>Order Details の 1 行（DB を直接読んだ値）</summary>
        public sealed class DetailRow
        {
            public int ProductID;
            public decimal UnitPrice;
            public int Quantity;
            public decimal Discount;
            public int RowVersion;
        }

        private static T Nz<T>(object v) { return v == null || v is DBNull ? default(T) : (T)v; }

        /// <summary>受注 1 行を読む（無ければ null）</summary>
        public static OrderRow GetOrder(int orderId)
        {
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT CustomerID, EmployeeID, OrderDate, RequiredDate, ShippedDate, ShipVia, Freight, ShipName, ShipAddress, " +
                "ShipCity, ShipRegion, ShipPostalCode, ShipCountry, RowVersion FROM Orders WHERE OrderID = @o", cn))
            {
                cmd.Parameters.Add(P("@o", SqlDbType.Int, orderId));
                cn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    OrderRow o = new OrderRow();
                    o.CustomerID = r.IsDBNull(0) ? null : r.GetString(0).Trim();
                    o.EmployeeID = r.IsDBNull(1) ? (int?)null : r.GetInt32(1);
                    o.OrderDate = r.IsDBNull(2) ? (DateTime?)null : r.GetDateTime(2);
                    o.RequiredDate = r.IsDBNull(3) ? (DateTime?)null : r.GetDateTime(3);
                    o.ShippedDate = r.IsDBNull(4) ? (DateTime?)null : r.GetDateTime(4);
                    o.ShipVia = r.IsDBNull(5) ? (int?)null : r.GetInt32(5);
                    o.Freight = r.IsDBNull(6) ? (decimal?)null : r.GetDecimal(6);
                    o.ShipName = r.IsDBNull(7) ? null : r.GetString(7);
                    o.ShipAddress = r.IsDBNull(8) ? null : r.GetString(8);
                    o.ShipCity = r.IsDBNull(9) ? null : r.GetString(9);
                    o.ShipRegion = r.IsDBNull(10) ? null : r.GetString(10);
                    o.ShipPostalCode = r.IsDBNull(11) ? null : r.GetString(11);
                    o.ShipCountry = r.IsDBNull(12) ? null : r.GetString(12);
                    o.RowVersion = r.GetInt32(13);
                    return o;
                }
            }
        }

        /// <summary>明細を ProductID の昇順で読む</summary>
        public static List<DetailRow> GetDetails(int orderId)
        {
            List<DetailRow> list = new List<DetailRow>();
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ProductID, UnitPrice, Quantity, Discount, RowVersion FROM [Order Details] WHERE OrderID = @o ORDER BY ProductID", cn))
            {
                cmd.Parameters.Add(P("@o", SqlDbType.Int, orderId));
                cn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        DetailRow d = new DetailRow();
                        d.ProductID = r.GetInt32(0);
                        d.UnitPrice = r.GetDecimal(1);
                        d.Quantity = r.GetInt16(2);
                        d.Discount = Convert.ToDecimal(r.GetFloat(3));
                        d.RowVersion = r.GetInt32(4);
                        list.Add(d);
                    }
                }
            }
            return list;
        }

        /// <summary>他の利用者が明細だけを更新したことを模す（Orders.RowVersion は変えない）</summary>
        public static void BumpDetail(int orderId, int productId)
        {
            ExecScalar("UPDATE [Order Details] SET RowVersion = RowVersion + 1 WHERE OrderID = @o AND ProductID = @p",
                P("@o", SqlDbType.Int, orderId), P("@p", SqlDbType.Int, productId));
        }

        /// <summary>他の利用者の出荷確定を模す（出荷日を設定し、行バージョンを +1）</summary>
        public static void ShipOrder(int orderId)
        {
            ExecScalar("UPDATE Orders SET ShippedDate = @d, RowVersion = RowVersion + 1 WHERE OrderID = @o",
                P("@d", SqlDbType.DateTime, DateTime.Today), P("@o", SqlDbType.Int, orderId));
        }

        /// <summary>商品の在庫を DB から読む（NULL は null）</summary>
        public static int? GetUnitsInStock(int productId)
        {
            object v = ExecScalar("SELECT UnitsInStock FROM Products WHERE ProductID = @p", P("@p", SqlDbType.Int, productId));
            return (v == null || v is DBNull) ? (int?)null : Convert.ToInt32(v);
        }

        /// <summary>他の利用者が在庫を変えたことを模す（UnitsInStock だけを更新する。RowVersion は変えない）</summary>
        public static void SetUnitsInStock(int productId, int? value)
        {
            ExecScalar("UPDATE Products SET UnitsInStock = @s WHERE ProductID = @p",
                P("@s", SqlDbType.SmallInt, value), P("@p", SqlDbType.Int, productId));
        }

        /// <summary>商品の行バージョンを読む</summary>
        public static int GetProductRowVersion(int productId)
        {
            return Convert.ToInt32(ExecScalar("SELECT RowVersion FROM Products WHERE ProductID = @p", P("@p", SqlDbType.Int, productId)));
        }

        /// <summary>顧客で受注を数える</summary>
        public static int CountOrdersOfCustomer(string customerId)
        {
            return Convert.ToInt32(ExecScalar("SELECT COUNT(*) FROM Orders WHERE CustomerID = @c", P("@c", SqlDbType.NChar, customerId)));
        }

        /// <summary>出荷先名称が一致する受注を数える（他のテストと並行しても干渉しない目印として使う）</summary>
        public static int CountOrdersOfShipName(string shipName)
        {
            return Convert.ToInt32(ExecScalar("SELECT COUNT(*) FROM Orders WHERE ShipName = @s", P("@s", SqlDbType.NVarChar, shipName)));
        }

        /// <summary>出荷先名称の目印を作って記録する（"Tf" + GUID 8 桁。後始末でその名称の受注と明細を消す）</summary>
        public string NewMark()
        {
            string m = "Tf" + Guid8();
            shipNameMarks.Add(m);
            return m;
        }

        /// <summary>出荷先名称の目印として記録する（後始末で、その名称で始まる受注と明細を消す）</summary>
        public void TrackShipName(string shipName)
        {
            shipNameMarks.Add(shipName);
        }

        /// <summary>保存で作られた受注を記録する（後始末で消す。顧客が K でない誤保存も消すため）</summary>
        public void Track(int orderId)
        {
            orders.Add(orderId);
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
        public List<int> NewOrdersSequence(string customerId, int n, DateTime start, int? employeeId = null)
        {
            List<int> ids = new List<int>();
            for (int i = 1; i <= n; i++)
            {
                ids.Add(NewOrder(customerId: customerId, employeeId: employeeId, orderDate: start.AddDays(i)));
            }
            return ids;
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
            foreach (string m in shipNameMarks)
            {
                run("DELETE FROM [Order Details] WHERE OrderID IN (SELECT OrderID FROM Orders WHERE ShipName LIKE @s)",
                    P("@s", SqlDbType.NVarChar, m + "%"));
                run("DELETE FROM Orders WHERE ShipName LIKE @s", P("@s", SqlDbType.NVarChar, m + "%"));
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
            foreach (int p in products)
                run("DELETE FROM Products WHERE ProductID = @p", P("@p", SqlDbType.Int, p));
            foreach (int s in shippers)
                run("DELETE FROM Shippers WHERE ShipperID = @s", P("@s", SqlDbType.Int, s));

            customers.Clear(); employees.Clear(); shippers.Clear(); orders.Clear(); products.Clear(); shipNameMarks.Clear();
            CustomerNames.Clear(); ShipperNames.Clear(); ProductNames.Clear();

            if (first != null) throw first;
        }
    }
}
