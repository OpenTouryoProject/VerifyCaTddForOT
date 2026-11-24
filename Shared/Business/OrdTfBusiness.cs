//**********************************************************************************
//* フレームワーク・テストクラス（Ｂ層）
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfBusiness
//* クラス日本語名  ：受注一覧（OrdTf）のＢ層
//*
//* 作成日時        ：2026/11/13
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/13  実装役            新規作成（テスト先行の Red 用。戻り値を設定するだけの空の UOC）
//*  2026/11/13  実装役            実装（Green）：EV-ORD-101 の選択肢、EV-ORD-102・EV-ORD-104 の検索・ページ
//*  2026/11/15  実装役            EV-ORD-105 の並べ替え（SortColumn・SortDescending を SQL 定義の ORDER BY へ）
//*  2026/11/24  実装役            EV-ORD-201・203（OrdTfDetail・OrdTfDelete）の空の UOC（テスト先行の Red 用）
//*  2026/11/24  実装役            実装（Green）：EV-ORD-201 の詳細の読み込み、EV-ORD-203 の削除（明細 → 受注。業務例外でロールバック）
//**********************************************************************************

using WSIFType_sample;

using System;
using System.Data;
using System.Globalization;

using Touryo.Infrastructure.Business.Business;
using Touryo.Infrastructure.Business.Dao;
using Touryo.Infrastructure.Framework.Exceptions;

namespace WSServer_sample.Business
{
    /// <summary>受注一覧（OrdTf）のＢ層</summary>
    public class OrdTfBusiness : MyFcBaseLogic
    {
        #region 定数

        /// <summary>1 ページの件数（共通仕様 4 節）</summary>
        private const int PageSize = 20;

        /// <summary>受注日の形式（共通仕様 4 節）</summary>
        private const string DateFormat = "yyyy/MM/dd";

        /// <summary>メッセージ ID（DEC-CMN-01：文言で定め、ID は実装で決める）</summary>
        private const string MsgIdFormat = "ORDTF001";

        /// <summary>メッセージ ID（DEC-CMN-01）</summary>
        private const string MsgIdRelation = "ORDTF002";

        /// <summary>日付の形式違反（DEC-ORD-09。開始・終了で同じ文言）</summary>
        private const string MsgFormat = "受注日は yyyy/MM/dd の形式で指定してください。";

        /// <summary>日付の前後関係の違反（EV-ORD-102 検証内容）</summary>
        private const string MsgRelation = "受注日の開始は終了以前を指定してください。";

        /// <summary>メッセージ ID（DEC-CMN-01）</summary>
        private const string MsgIdNotFound = "ORDTF003";

        /// <summary>メッセージ ID（DEC-CMN-01）</summary>
        private const string MsgIdConflict = "ORDTF004";

        /// <summary>ERR-NOTFOUND（共通仕様 7 節）</summary>
        private const string MsgNotFound = "対象のデータは存在しません。";

        /// <summary>ERR-CONFLICT（共通仕様 7 節）</summary>
        private const string MsgConflict = "他の利用者によって更新されています。最新の内容を読み込み直してください。";

        /// <summary>廃番商品の商品名への付記（EV-ORD-201 手順 6）</summary>
        private const string DiscontinuedMark = "（廃番）";

        /// <summary>顧客住所の連結の区切り（DEC-ORD-18：区切りは定めない。実装は半角空白）</summary>
        private const string AddressSeparator = " ";

        /// <summary>項目名（ErrorFields）：受注日（開始）</summary>
        private const string FieldFrom = "OrderDateFrom";

        /// <summary>項目名（ErrorFields）：受注日（終了）</summary>
        private const string FieldTo = "OrderDateTo";

        #endregion

        #region UOCメソッド

        /// <summary>選択肢の構築（EV-ORD-101 手順 1）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfInit(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            // 顧客：会社名の昇順。先頭に空白の選択肢（＝すべて）
            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfCustomers.sql";
            DataTable dtCustomer = new DataTable();
            dao.ExecSelectFill_DT(dtCustomer);

            rv.CustomerOptions.Add(new OrdTfOption { Value = "", Text = "" });
            foreach (DataRow dr in dtCustomer.Rows)
            {
                rv.CustomerOptions.Add(new OrdTfOption
                {
                    Value = Convert.ToString(dr["CustomerID"]),
                    Text = Convert.ToString(dr["CompanyName"])
                });
            }

            // 担当者：姓・名の昇順。先頭に空白の選択肢（＝すべて）
            dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfEmployees.sql";
            DataTable dtEmployee = new DataTable();
            dao.ExecSelectFill_DT(dtEmployee);

            rv.EmployeeOptions.Add(new OrdTfOption { Value = "", Text = "" });
            foreach (DataRow dr in dtEmployee.Rows)
            {
                rv.EmployeeOptions.Add(new OrdTfOption
                {
                    Value = Convert.ToString(dr["EmployeeID"]),
                    Text = Convert.ToString(dr["LastName"]) + " " + Convert.ToString(dr["FirstName"])
                });
            }

            // ↑業務処理-----------------------------------------------------
        }

        /// <summary>受注の検索・ページ切替（EV-ORD-102・EV-ORD-104）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfSearch(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            // 1. 入力値の検証（VAL-DATE）。違反があれば中断する。
            DateTime? from;
            DateTime? to;
            this.Validate(pv, rv, out from, out to);

            // 2. 抽出条件（未指定の条件は設定しない＝動的 SQL で消える）
            string status = MapStatus(pv.ShippedStatus);
            DateTime today = DateTime.Today;   // 当日はアプリの日付（DB の時計は使わない）

            // 総件数
            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfCount.xml";
            SetConditions(dao, pv, from, to, status, today);
            int total = Convert.ToInt32(dao.ExecSelectScalar());

            // 総ページ数（最小 1＝DEC-ORD-02）と、移動先のページ（総ページ数を超えるなら最終ページ＝DEC-ORD-06）
            int totalPages = Math.Max(1, (total + PageSize - 1) / PageSize);
            int pageNo = Math.Min(Math.Max(1, pv.PageNo), totalPages);

            rv.TotalCount = total;
            rv.TotalPages = totalPages;
            rv.PageNo = pageNo;

            // 当該ページの行
            dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfSearch.xml";
            SetConditions(dao, pv, from, to, status, today);
            dao.SetParameter("Offset", (pageNo - 1) * PageSize);
            // 並べ替え（EV-ORD-105）。列が空なら SQL 定義の既定の並び（受注日の降順、同日は受注 ID の降順）。
            // 値は SQL 定義の許可リスト（SELECT/CASE）で照合され、列名は連結されない。許可リスト外は既定の並びになる（DEFAULT）。
            // 最後の並べ替えキーは受注 ID（同じ値が続いてもページが重ならず抜けないため。DEC-ORD-12）。日付は日付の単位（DEC-ORD-14）。
            // SELECT タグはパラメタが未設定だと何も出力しない（DEFAULT も出ない）ので、列が空のときも必ず設定する（どの CASE にも一致しない値）。
            dao.SetParameter("SortKey", string.IsNullOrEmpty(pv.SortColumn)
                ? "Default"
                : pv.SortColumn + (pv.SortDescending ? "_DESC" : "_ASC"));
            DataTable dt = new DataTable();
            dao.ExecSelectFill_DT(dt);

            foreach (DataRow dr in dt.Rows)
            {
                OrdTfRow row = new OrdTfRow();
                row.OrderID = Convert.ToInt32(dr["OrderID"]);
                row.CustomerName = ToText(dr["CustomerName"]);
                row.EmployeeName = (dr["LastName"] == DBNull.Value && dr["FirstName"] == DBNull.Value)
                    ? "" : ToText(dr["LastName"]) + " " + ToText(dr["FirstName"]);
                row.ShipperName = ToText(dr["ShipperName"]);
                row.OrderDate = ToDate(dr["OrderDate"]);
                row.RequiredDate = ToDate(dr["RequiredDate"]);
                row.ShippedDate = ToDate(dr["ShippedDate"]);
                row.TotalAmount = Convert.ToDecimal(dr["TotalAmount"]);
                row.Status = JudgeStatus(row.RequiredDate, row.ShippedDate, today);
                rv.Rows.Add(row);
            }

            // ↑業務処理-----------------------------------------------------
        }

        /// <summary>受注詳細（照会）の読み込み（EV-ORD-201）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfDetail(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            // 1. 受注 1 件（顧客・担当者・配送業者は外部結合）。0 件は ERR-NOTFOUND（手順 1・2）
            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfDetailHeader.sql";
            dao.SetParameter("OrderID", pv.OrderID);
            DataTable dtHeader = new DataTable();
            dao.ExecSelectFill_DT(dtHeader);
            if (dtHeader.Rows.Count == 0)
            {
                throw new BusinessApplicationException(MsgIdNotFound, MsgNotFound, "");
            }
            DataRow h = dtHeader.Rows[0];

            rv.OrderID = Convert.ToInt32(h["OrderID"]);
            rv.CustomerName = ToText(h["CustomerName"]);
            rv.CustomerPhone = ToText(h["CustomerPhone"]);
            rv.CustomerAddress = JoinAddress(h["CustomerAddress"], h["CustomerCity"], h["CustomerRegion"],
                h["CustomerPostalCode"], h["CustomerCountry"]);
            rv.EmployeeName = (h["LastName"] == DBNull.Value && h["FirstName"] == DBNull.Value)
                ? "" : ToText(h["LastName"]) + " " + ToText(h["FirstName"]);
            rv.OrderDate = ToDate(h["OrderDate"]);
            rv.RequiredDate = ToDate(h["RequiredDate"]);
            rv.ShippedDate = ToDate(h["ShippedDate"]);
            rv.ShipperName = ToText(h["ShipperName"]);
            rv.ShipName = ToText(h["ShipName"]);
            rv.ShipAddress = ToText(h["ShipAddress"]);
            rv.ShipCity = ToText(h["ShipCity"]);
            rv.ShipRegion = ToText(h["ShipRegion"]);
            rv.ShipPostalCode = ToText(h["ShipPostalCode"]);
            rv.ShipCountry = ToText(h["ShipCountry"]);
            rv.Freight = h["Freight"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(h["Freight"]);
            rv.RowVersion = Convert.ToInt32(h["RowVersion"]);                  // 手順 7
            rv.Status = JudgeStatus(rv.RequiredDate, rv.ShippedDate, DateTime.Today);   // 手順 5
            rv.CanEdit = !rv.ShippedDate.HasValue;                              // 手順 8（ORD-T4）

            // 2. 明細（商品名の昇順。Products は内部結合＝商品マスタに無い明細は含めない。DEC-ORD-24）
            dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfDetailLines.sql";
            dao.SetParameter("OrderID", pv.OrderID);
            DataTable dtLines = new DataTable();
            dao.ExecSelectFill_DT(dtLines);

            decimal subtotal = 0m;
            foreach (DataRow dr in dtLines.Rows)
            {
                OrdTfDetailLine line = new OrdTfDetailLine();
                line.ProductName = ToText(dr["ProductName"]) + (Convert.ToBoolean(dr["Discontinued"]) ? DiscontinuedMark : "");   // 手順 6
                line.UnitPrice = Convert.ToDecimal(dr["UnitPrice"]);
                line.Discount = Convert.ToDecimal(dr["Discount"]);   // real は SQL 側で 10 進数へ変換済み
                line.Price = line.UnitPrice * (1m - line.Discount);   // 丸めない
                line.Quantity = Convert.ToInt32(dr["Quantity"]);
                line.UnitsInStock = dr["UnitsInStock"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["UnitsInStock"]);
                // 明細金額は、価格に数量を掛けた後で小数第 3 位を四捨五入する（共通仕様 3 節）
                line.Amount = Math.Round(line.Price * line.Quantity, 2, MidpointRounding.AwayFromZero);
                subtotal += line.Amount;
                rv.Lines.Add(line);
            }

            // 3. 小計・合計（明細が無ければ小計 0＝DEC-CMN-02、送料 NULL は 0＝DEC-CMN-03）
            rv.Subtotal = subtotal;
            rv.Total = subtotal + (rv.Freight ?? 0m);

            // ↑業務処理-----------------------------------------------------
        }

        /// <summary>受注の削除（EV-ORD-203）。1 トランザクション（業務例外でフレームワークがロールバックする）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfDelete(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            // 手順 3：明細をすべて削除（ORD-T8：明細 → 受注の順）
            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfDeleteDetails.sql";
            dao.SetParameter("OrderID", pv.OrderID);
            dao.ExecInsUpDel_NonQuery();

            // 手順 4：受注を、主キーと保持していた行バージョンで削除（削除では RowVersion を更新しない）
            dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfDeleteOrder.sql";
            dao.SetParameter("OrderID", pv.OrderID);
            dao.SetParameter("RowVersion", pv.RowVersion);
            int deleted = dao.ExecInsUpDel_NonQuery();

            if (deleted == 0)
            {
                // 0 件：ロールバック（業務例外でフレームワークが行う）し、受注の有無で ERR-CONFLICT と ERR-NOTFOUND を分ける
                dao = new CmnDao(this.GetDam());
                dao.SQLFileName = "OrdTfOrderCount.sql";
                dao.SetParameter("OrderID", pv.OrderID);
                int exists = Convert.ToInt32(dao.ExecSelectScalar());
                if (exists > 0)
                {
                    throw new BusinessApplicationException(MsgIdConflict, MsgConflict, "");
                }
                throw new BusinessApplicationException(MsgIdNotFound, MsgNotFound, "");
            }

            // ↑業務処理-----------------------------------------------------
        }

        #endregion

        #region 検証・共通処理

        /// <summary>
        /// 受注日の検証（VAL-DATE・DEC-ORD-09・DEC-CMN-04・DEC-CMN-05）。
        /// 形式違反は項目名を ErrorFields に入れて投げる。前後関係の違反は ErrorFields を空で投げる。
        /// </summary>
        private void Validate(OrdTfParameterValue pv, OrdTfReturnValue rv, out DateTime? from, out DateTime? to)
        {
            bool okFrom = TryParseDate(pv.OrderDateFrom, out from);
            bool okTo = TryParseDate(pv.OrderDateTo, out to);

            if (!okFrom) rv.ErrorFields.Add(FieldFrom);
            if (!okTo) rv.ErrorFields.Add(FieldTo);
            if (!okFrom || !okTo)
            {
                throw new BusinessApplicationException(MsgIdFormat, MsgFormat, "");
            }

            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                throw new BusinessApplicationException(MsgIdRelation, MsgRelation, "");
            }
        }

        /// <summary>日付の解釈。空白だけ・null は未指定（true・null）。yyyy/MM/dd の存在する日付だけを受け付ける</summary>
        private static bool TryParseDate(string text, out DateTime? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text)) return true;

            DateTime d;
            if (DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            {
                value = d;
                return true;
            }
            return false;
        }

        /// <summary>出荷状況（画面の値）を SQL 定義の分岐値に写す。「すべて」・未指定は null（条件に加えない）</summary>
        private static string MapStatus(string status)
        {
            switch (status)
            {
                case "未出荷": return "NOTSHIPPED";
                case "出荷済": return "SHIPPED";
                case "納期遅延": return "LATE";
                default: return null;
            }
        }

        /// <summary>抽出条件のパラメタを設定する（未指定の条件は設定しない）</summary>
        private static void SetConditions(CmnDao dao, OrdTfParameterValue pv,
            DateTime? from, DateTime? to, string status, DateTime today)
        {
            if (!string.IsNullOrEmpty(pv.CustomerID)) dao.SetParameter("CustomerID", pv.CustomerID);
            if (pv.EmployeeID.HasValue) dao.SetParameter("EmployeeID", pv.EmployeeID.Value);
            if (from.HasValue) dao.SetParameter("OrderDateFrom", from.Value);
            // 終了日は日付の単位で比べ、当日の時刻付きを含める（DEC-ORD-07）。翌日を作らない（9999/12/31 でも例外にならない）
            if (to.HasValue) dao.SetParameter("OrderDateTo", to.Value);
            if (status != null)
            {
                dao.SetParameter("ShippedStatus", status);
                if (status == "LATE") dao.SetParameter("Today", today);
            }
        }

        /// <summary>状況の判定（EventSpec「状況の判定」・ORD-T7）</summary>
        private static string JudgeStatus(DateTime? required, DateTime? shipped, DateTime today)
        {
            if (!shipped.HasValue)
            {
                // 未出荷：要求納期が当日より前なら納期遅延（要求納期が NULL なら遅延としない）
                return (required.HasValue && required.Value < today) ? "納期遅延" : "未出荷";
            }

            // 出荷済：出荷日 > 要求納期なら納期遅延
            return (required.HasValue && shipped.Value > required.Value) ? "納期遅延" : "出荷済";
        }

        /// <summary>顧客住所：Address・City・Region・PostalCode・Country の空（NULL・空白だけ）の要素を詰めて連結（SC-ORD-02-I05）</summary>
        private static string JoinAddress(params object[] parts)
        {
            System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
            foreach (object p in parts)
            {
                string s = ToText(p);
                if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
            }
            return string.Join(AddressSeparator, list);
        }

        /// <summary>NULL は空文字（共通仕様 4 節）</summary>
        private static string ToText(object o)
        {
            return o == DBNull.Value ? "" : Convert.ToString(o);
        }

        /// <summary>NULL は null</summary>
        private static DateTime? ToDate(object o)
        {
            return o == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(o);
        }

        #endregion
    }
}
