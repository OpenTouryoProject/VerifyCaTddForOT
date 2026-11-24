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
//*  2026/11/24  実装役            EV-ORD-301〜308（OrdTfEditInit・OrdTfCustomer・OrdTfProduct・OrdTfSave）の空の UOC（テスト先行の Red 用）
//*  2026/11/24  実装役            実装（Green）：EV-ORD-301〜308（選択肢・編集の読み込み・顧客・商品・保存。保存は 1 トランザクション）
//*  2026/11/25  実装役            読み込み時から存在する行は DB で決める（OriginalProductID を信じない。DEC-ORD-49）。削除の印と残す行の食い違い（ORD-T5）
//**********************************************************************************

using WSIFType_sample;

using System;
using System.Collections.Generic;
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
        public const string MsgIdNotFound = "ORDTF003";

        /// <summary>メッセージ ID（DEC-CMN-01）</summary>
        public const string MsgIdConflict = "ORDTF004";

        /// <summary>ERR-NOTFOUND（共通仕様 7 節）</summary>
        public const string MsgNotFound = "対象のデータは存在しません。";

        /// <summary>ERR-CONFLICT（共通仕様 7 節）</summary>
        public const string MsgConflict = "他の利用者によって更新されています。最新の内容を読み込み直してください。";

        /// <summary>メッセージ ID（DEC-CMN-01）</summary>
        public const string MsgIdShipped = "ORDTF005";

        /// <summary>メッセージ ID（DEC-CMN-01）</summary>
        public const string MsgIdValidation = "ORDTF006";

        /// <summary>出荷済は編集できない（EV-ORD-301 手順 3-2。ORD-T4）</summary>
        public const string MsgShipped = "出荷済の受注は編集できません。";

        /// <summary>受注日の形式違反（DEC-ORD-29。文言は DEC-ORD-30 により定めない）</summary>
        private const string MsgOrderDateFormat = "受注日は yyyy/MM/dd の形式で指定してください。";

        /// <summary>要求納期の形式違反（DEC-ORD-29）</summary>
        private const string MsgRequiredDateFormat = "要求納期は yyyy/MM/dd の形式で指定してください。";

        /// <summary>受注日 > 要求納期（ORD-T3。文言は DEC-ORD-31 により定めない）</summary>
        private const string MsgDateRelation = "要求納期は受注日以降の日付を指定してください。";

        /// <summary>商品の重複（VAL-DUP・OD-T1）</summary>
        private const string MsgDuplicated = "同一商品の明細が重複しています。";

        /// <summary>明細が 0 行（ORD-T5）</summary>
        private const string MsgNoLine = "明細を 1 行以上入力してください。";

        /// <summary>新規の廃番商品（OD-T3）</summary>
        private const string MsgDiscontinued = "廃番商品は指定できません。";

        /// <summary>金額（money）の上限（DEC-ORD-38：定めない。DB の範囲を超えないための実装の上限）</summary>
        private const decimal MaxMoney = 922337203685477m;

        /// <summary>数量（smallint）の上限（DEC-ORD-38：定めない。DB の範囲を超えないための実装の上限）</summary>
        private const int MaxQuantity = 32767;

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

        /// <summary>受注詳細（登録・編集）のページ読み込み（EV-ORD-301）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfEditInit(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            // 手順 1：選択肢の構築（顧客・担当者・配送業者・商品。先頭に空白の選択肢。商品は廃番でないものだけ＝OD-T3）
            rv.CustomerOptions = this.BuildOptions("OrdTfCustomers.sql", true, "CustomerID", "CompanyName", null);
            rv.EmployeeOptions = this.BuildOptions("OrdTfEmployees.sql", true, "EmployeeID", "LastName", "FirstName");
            rv.ShipperOptions = this.BuildOptions("OrdTfShippers.sql", true, "ShipperID", "CompanyName", null);
            rv.ProductOptions = this.BuildOptions("OrdTfProducts.sql", true, "ProductID", "ProductName", null);

            if (pv.Mode != "edit")
            {
                // 手順 2（登録モード）：初期値は画面（P層）が持つ。選択肢だけ返す
                return;
            }

            // 手順 3-1：受注 1 件。0 件は ERR-NOTFOUND
            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfEditHeader.sql";
            dao.SetParameter("OrderID", pv.OrderID);
            DataTable dtHeader = new DataTable();
            dao.ExecSelectFill_DT(dtHeader);
            if (dtHeader.Rows.Count == 0)
            {
                throw new BusinessApplicationException(MsgIdNotFound, MsgNotFound, "");
            }
            DataRow h = dtHeader.Rows[0];

            // 手順 3-2：出荷済は編集させない（ORD-T4。ERR-BIZ）
            if (h["ShippedDate"] != DBNull.Value)
            {
                throw new BusinessApplicationException(MsgIdShipped, MsgShipped, "");
            }

            // 手順 3-3・4：受注の値と顧客情報、行バージョン
            rv.OrderID = Convert.ToInt32(h["OrderID"]);
            rv.EditCustomerID = h["CustomerID"] == DBNull.Value ? null : Convert.ToString(h["CustomerID"]).Trim();
            rv.EditEmployeeID = h["EmployeeID"] == DBNull.Value ? (int?)null : Convert.ToInt32(h["EmployeeID"]);
            rv.EditShipVia = h["ShipVia"] == DBNull.Value ? (int?)null : Convert.ToInt32(h["ShipVia"]);
            rv.CustomerName = ToText(h["CustomerName"]);
            rv.CustomerPhone = ToText(h["CustomerPhone"]);
            rv.CustomerAddress = JoinAddress(h["CustomerAddress"], h["CustomerCity"], h["CustomerRegion"],
                h["CustomerPostalCode"], h["CustomerCountry"]);
            rv.OrderDate = ToDate(h["OrderDate"]);
            rv.RequiredDate = ToDate(h["RequiredDate"]);
            rv.ShipName = ToText(h["ShipName"]);
            rv.ShipAddress = ToText(h["ShipAddress"]);
            rv.ShipCity = ToText(h["ShipCity"]);
            rv.ShipRegion = ToText(h["ShipRegion"]);
            rv.ShipPostalCode = ToText(h["ShipPostalCode"]);
            rv.ShipCountry = ToText(h["ShipCountry"]);
            rv.Freight = h["Freight"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(h["Freight"]);
            rv.RowVersion = Convert.ToInt32(h["RowVersion"]);

            // 手順 3-4・3-5：明細（商品名の昇順。各行の行バージョンも。廃番の行は Discontinued = true で返す）
            dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfEditLines.sql";
            dao.SetParameter("OrderID", pv.OrderID);
            DataTable dtLines = new DataTable();
            dao.ExecSelectFill_DT(dtLines);

            decimal subtotal = 0m;
            foreach (DataRow dr in dtLines.Rows)
            {
                OrdTfEditLine line = new OrdTfEditLine();
                line.ProductID = Convert.ToInt32(dr["ProductID"]);
                line.ProductName = ToText(dr["ProductName"]);
                line.Discontinued = Convert.ToBoolean(dr["Discontinued"]);
                line.UnitPrice = Convert.ToDecimal(dr["UnitPrice"]);
                line.Discount = Convert.ToDecimal(dr["Discount"]);
                line.Quantity = Convert.ToInt32(dr["Quantity"]);
                line.UnitsInStock = dr["UnitsInStock"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["UnitsInStock"]);
                line.RowVersion = Convert.ToInt32(dr["RowVersion"]);
                subtotal += Amount(line.UnitPrice, line.Discount, line.Quantity);
                rv.EditLines.Add(line);
            }

            // 手順 5：金額の再計算
            rv.Subtotal = subtotal;
            rv.Total = subtotal + (rv.Freight ?? 0m);

            // ↑業務処理-----------------------------------------------------
        }

        /// <summary>顧客選択の変更（EV-ORD-302 手順 2〜4）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfCustomer(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfCustomer.sql";
            dao.SetParameter("CustomerID", pv.CustomerID);
            DataTable dt = new DataTable();
            dao.ExecSelectFill_DT(dt);
            if (dt.Rows.Count == 0)
            {
                throw new BusinessApplicationException(MsgIdNotFound, MsgNotFound, "");
            }
            DataRow c = dt.Rows[0];

            // 手順 3：電話・住所（空の要素を詰めて連結）
            rv.CustomerName = ToText(c["CompanyName"]);
            rv.CustomerPhone = ToText(c["Phone"]);
            rv.CustomerAddress = JoinAddress(c["Address"], c["City"], c["Region"], c["PostalCode"], c["Country"]);

            // 手順 4：出荷先の既定値（ORD-T6。住所の連結でなく各項目）
            rv.ShipName = ToText(c["CompanyName"]);
            rv.ShipAddress = ToText(c["Address"]);
            rv.ShipCity = ToText(c["City"]);
            rv.ShipRegion = ToText(c["Region"]);
            rv.ShipPostalCode = ToText(c["PostalCode"]);
            rv.ShipCountry = ToText(c["Country"]);

            // ↑業務処理-----------------------------------------------------
        }

        /// <summary>明細の商品選択の変更（EV-ORD-306 手順 3・4）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfProduct(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfProduct.sql";
            dao.SetParameter("ProductID", pv.ProductID);
            DataTable dt = new DataTable();
            dao.ExecSelectFill_DT(dt);
            if (dt.Rows.Count == 0)
            {
                throw new BusinessApplicationException(MsgIdNotFound, MsgNotFound, "");
            }
            DataRow p = dt.Rows[0];

            // 手順 4：単価（OD-T2）・在庫
            rv.ProductName = ToText(p["ProductName"]);
            rv.UnitPrice = p["UnitPrice"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(p["UnitPrice"]);
            rv.UnitsInStock = p["UnitsInStock"] == DBNull.Value ? (int?)null : Convert.ToInt32(p["UnitsInStock"]);

            // ↑業務処理-----------------------------------------------------
        }

        /// <summary>受注の保存（EV-ORD-308）。検証 → 1 トランザクションで登録または更新（業務例外でフレームワークがロールバックする）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfSave(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            bool isEdit = pv.Mode == "edit";
            List<OrdTfSaveLine> lines = pv.SaveLines ?? new List<OrdTfSaveLine>();

            // 手順 1：全項目の検証をまとめて実施する（1 件でも違反があれば更新しない）
            // 読み込み時から存在する行は、画面が送る値（OriginalProductID）でなく、保存のトランザクションの中の DB で決める（OD-T3・DEC-ORD-49）。
            // 登録モードに読み込み時の行は無い（OriginalProductID は無視する）
            HashSet<int> existing = isEdit ? this.LoadExistingProducts(pv.OrderID) : null;

            SaveValues v = this.ValidateSave(pv, lines, rv, existing);
            if (rv.ErrorFields.Count > 0 || rv.ErrorMessages.Count > 0)
            {
                string msg = rv.ErrorMessages.Count > 0 ? rv.ErrorMessages[0] : rv.ErrorFieldMessages[0];
                throw new BusinessApplicationException(MsgIdValidation, msg, "");
            }

            if (!isEdit)
            {
                // 手順 3：登録（受注 → 採番された OrderID → 明細）
                CmnDao dao = new CmnDao(this.GetDam());
                dao.SQLFileName = "OrdTfInsertOrder.sql";
                SetOrderParameters(dao, v);
                int newId = Convert.ToInt32(dao.ExecSelectScalar());
                rv.OrderID = newId;
                rv.RowVersion = 1;

                foreach (SaveLineValues l in v.Lines) this.InsertDetail(newId, l);
                return;
            }

            // 手順 4-1：受注を、主キーと保持していた行バージョンで更新する（RowVersion + 1。出荷日は更新しない）
            CmnDao upd = new CmnDao(this.GetDam());
            upd.SQLFileName = "OrdTfUpdateOrder.sql";
            SetOrderParameters(upd, v);
            upd.SetParameter("OrderID", pv.OrderID);
            upd.SetParameter("RowVersion", pv.RowVersion);
            int updated = upd.ExecInsUpDel_NonQuery();
            if (updated == 0)
            {
                // 手順 4-2：0 件はロールバック（業務例外でフレームワークが行う）し、受注の有無で ERR-CONFLICT と ERR-NOTFOUND を分ける
                CmnDao cnt = new CmnDao(this.GetDam());
                cnt.SQLFileName = "OrdTfOrderCount.sql";
                cnt.SetParameter("OrderID", pv.OrderID);
                if (Convert.ToInt32(cnt.ExecSelectScalar()) > 0)
                {
                    throw new BusinessApplicationException(MsgIdConflict, MsgConflict, "");
                }
                throw new BusinessApplicationException(MsgIdNotFound, MsgNotFound, "");
            }

            // 手順 4-3：明細を行ごとに適用する（全削除・再登録にしない。OD-T6）。削除 → 更新 → 挿入の順
            // （商品を入れ替えた行の主キーが衝突しないよう、削除を先に行う）
            // 削除の印は、同じ行が残る（商品が同じ既存の行）ときは無視する。印と残す行が食い違う入力でも、明細が 0 行になる・残す行が消えることは無い（ORD-T5・DEC-ORD-49）
            HashSet<int> kept = new HashSet<int>();
            foreach (SaveLineValues l in v.Lines)
            {
                if (l.OriginalProductID.HasValue && l.OriginalProductID.Value == l.ProductID) kept.Add(l.ProductID);
            }
            foreach (int deleted in v.DeletedProductIDs)
            {
                if (!kept.Contains(deleted)) this.DeleteDetail(pv.OrderID, deleted);
            }
            foreach (SaveLineValues l in v.Lines)
            {
                if (l.OriginalProductID.HasValue && l.OriginalProductID.Value != l.ProductID)
                {
                    this.DeleteDetail(pv.OrderID, l.OriginalProductID.Value);   // 商品が変更された既存行：変更前を削除
                }
            }
            foreach (SaveLineValues l in v.Lines)
            {
                if (l.OriginalProductID.HasValue && l.OriginalProductID.Value == l.ProductID && l.Changed)
                {
                    CmnDao ud = new CmnDao(this.GetDam());
                    ud.SQLFileName = "OrdTfUpdateDetail.sql";
                    ud.SetParameter("OrderID", pv.OrderID);
                    ud.SetParameter("ProductID", l.ProductID);
                    ud.SetParameter("UnitPrice", l.UnitPrice);
                    ud.SetParameter("Quantity", l.Quantity);
                    ud.SetParameter("Discount", l.Discount);
                    ud.ExecInsUpDel_NonQuery();
                }
            }
            foreach (SaveLineValues l in v.Lines)
            {
                if (!l.OriginalProductID.HasValue || l.OriginalProductID.Value != l.ProductID)
                {
                    this.InsertDetail(pv.OrderID, l);   // 読み込み時に無かった行・商品が変更された行
                }
            }

            rv.OrderID = pv.OrderID;
            rv.RowVersion = pv.RowVersion + 1;

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

        #region 受注詳細（登録・編集）の検証・部品

        /// <summary>保存の検証を通った受注の値</summary>
        private sealed class SaveValues
        {
            public string CustomerID;
            public int EmployeeID;
            public DateTime OrderDate;
            public DateTime? RequiredDate;
            public int? ShipVia;
            public decimal Freight;
            public string ShipName, ShipAddress, ShipCity, ShipRegion, ShipPostalCode, ShipCountry;
            public List<SaveLineValues> Lines = new List<SaveLineValues>();
            public List<int> DeletedProductIDs = new List<int>();
        }

        /// <summary>保存の検証を通った明細 1 行の値</summary>
        private sealed class SaveLineValues
        {
            public int ProductID;
            public decimal UnitPrice;
            public decimal Discount;
            public int Quantity;
            public int? OriginalProductID;

            /// <summary>読み込み時から値が変わっているか（商品が同じ行の単価・割引率・数量）</summary>
            public bool Changed;
        }

        /// <summary>選択肢を構築する（会社名・商品名などの昇順の SQL 定義。先頭に空白の選択肢）</summary>
        private List<OrdTfOption> BuildOptions(string sqlFile, bool blankFirst, string idColumn, string textColumn, string textColumn2)
        {
            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = sqlFile;
            DataTable dt = new DataTable();
            dao.ExecSelectFill_DT(dt);

            List<OrdTfOption> list = new List<OrdTfOption>();
            if (blankFirst) list.Add(new OrdTfOption { Value = "", Text = "" });
            foreach (DataRow dr in dt.Rows)
            {
                string text = ToText(dr[textColumn]);
                if (textColumn2 != null) text = text + " " + ToText(dr[textColumn2]);
                list.Add(new OrdTfOption { Value = Convert.ToString(dr[idColumn]), Text = text });
            }
            return list;
        }

        /// <summary>明細金額（価格 × 数量を小数第 3 位で四捨五入。共通仕様 3 節）</summary>
        private static decimal Amount(decimal unitPrice, decimal discount, int quantity)
        {
            return Math.Round(unitPrice * (1m - discount) * quantity, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>検証の違反（項目単位）を足す</summary>
        private static void AddFieldError(OrdTfReturnValue rv, string field, string message)
        {
            rv.ErrorFields.Add(field);
            rv.ErrorFieldMessages.Add(message);
        }

        /// <summary>件数を返す SQL（存在の確認）を実行して 0 より大きいか</summary>
        private bool Exists(string sqlFile, string paramName, object value)
        {
            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = sqlFile;
            dao.SetParameter(paramName, value);
            return Convert.ToInt32(dao.ExecSelectScalar()) > 0;
        }

        /// <summary>数値（10 進数）の解釈。符号と小数点だけを許す（通貨記号・桁区切りは受け付けない）</summary>
        private static bool TryParseDecimal(string text, out decimal value)
        {
            return decimal.TryParse((text ?? "").Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value);
        }

        /// <summary>整数の解釈</summary>
        private static bool TryParseInt(string text, out int value)
        {
            return int.TryParse((text ?? "").Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// 保存の入力の検証（EV-ORD-308 検証内容。VAL-REQUIRED・VAL-DATE・VAL-NUMERIC・VAL-LENGTH・VAL-EXISTS・VAL-DUP・ORD-T5・OD-T3）。
        /// 項目単位の違反は ErrorFields（と ErrorFieldMessages）、関連チェック・個別の違反は ErrorMessages に集める（DEC-CMN-04・05）。
        /// </summary>
        private SaveValues ValidateSave(OrdTfParameterValue pv, List<OrdTfSaveLine> lines, OrdTfReturnValue rv, HashSet<int> existing)
        {
            SaveValues v = new SaveValues();
            int?[] origins = ResolveOrigins(lines, existing);

            // 顧客（必須・存在）
            if (string.IsNullOrWhiteSpace(pv.CustomerID)) AddFieldError(rv, "CustomerID", "顧客は必須です。");
            else if (!this.Exists("OrdTfExistsCustomer.sql", "CustomerID", pv.CustomerID.Trim())) AddFieldError(rv, "CustomerID", "指定された顧客は存在しません。");
            else v.CustomerID = pv.CustomerID.Trim();

            // 担当者（必須・存在）
            int employeeId;
            if (string.IsNullOrWhiteSpace(pv.EmployeeIDText)) AddFieldError(rv, "EmployeeID", "担当者は必須です。");
            else if (!TryParseInt(pv.EmployeeIDText, out employeeId) || !this.Exists("OrdTfExistsEmployee.sql", "EmployeeID", employeeId))
                AddFieldError(rv, "EmployeeID", "指定された担当者は存在しません。");
            else v.EmployeeID = employeeId;

            // 受注日（必須・形式）・要求納期（形式）
            DateTime? orderDate = null, requiredDate = null;
            if (string.IsNullOrWhiteSpace(pv.OrderDateText)) AddFieldError(rv, "OrderDate", "受注日は必須です。");
            else if (!TryParseDate(pv.OrderDateText, out orderDate) || !orderDate.HasValue) AddFieldError(rv, "OrderDate", MsgOrderDateFormat);
            if (!TryParseDate(pv.RequiredDateText, out requiredDate)) AddFieldError(rv, "RequiredDate", MsgRequiredDateFormat);
            if (orderDate.HasValue) v.OrderDate = orderDate.Value;
            v.RequiredDate = requiredDate;

            // 受注日 <= 要求納期（関連チェック。共通メッセージ領域だけ。ORD-T3・DEC-CMN-04）
            if (orderDate.HasValue && requiredDate.HasValue && orderDate.Value > requiredDate.Value)
            {
                rv.ErrorMessages.Add(MsgDateRelation);
            }

            // 配送業者（任意・存在）
            if (!string.IsNullOrWhiteSpace(pv.ShipViaText))
            {
                int shipVia;
                if (!TryParseInt(pv.ShipViaText, out shipVia) || !this.Exists("OrdTfExistsShipper.sql", "ShipperID", shipVia))
                    AddFieldError(rv, "ShipVia", "指定された配送業者は存在しません。");
                else v.ShipVia = shipVia;
            }

            // 送料（空は 0。DEC-ORD-01。0 以上の数値）
            if (string.IsNullOrWhiteSpace(pv.FreightText)) v.Freight = 0m;
            else
            {
                decimal freight;
                if (!TryParseDecimal(pv.FreightText, out freight) || freight < 0m || freight > MaxMoney)
                    AddFieldError(rv, "Freight", "送料は 0 以上の数値で指定してください。");
                else v.Freight = freight;
            }

            // 出荷先（桁数。空は NULL。DEC-ORD-36）
            v.ShipName = this.CheckLength(rv, "ShipName", pv.ShipName, 40);
            v.ShipAddress = this.CheckLength(rv, "ShipAddress", pv.ShipAddress, 60);
            v.ShipCity = this.CheckLength(rv, "ShipCity", pv.ShipCity, 15);
            v.ShipRegion = this.CheckLength(rv, "ShipRegion", pv.ShipRegion, 15);
            v.ShipPostalCode = this.CheckLength(rv, "ShipPostalCode", pv.ShipPostalCode, 10);
            v.ShipCountry = this.CheckLength(rv, "ShipCountry", pv.ShipCountry, 15);

            // 明細（各行：商品・単価・割引率・数量）
            HashSet<int> seen = new HashSet<int>();
            bool duplicated = false, discontinued = false;
            for (int i = 0; i < lines.Count; i++)
            {
                OrdTfSaveLine l = lines[i];
                string key = "Lines[" + i + "].";
                SaveLineValues lv = new SaveLineValues();
                bool ok = true;

                int productId;
                if (string.IsNullOrWhiteSpace(l.ProductIDText)) { AddFieldError(rv, key + "ProductID", "商品は必須です。"); ok = false; }
                else if (!TryParseInt(l.ProductIDText, out productId)) { AddFieldError(rv, key + "ProductID", "指定された商品は存在しません。"); ok = false; }
                else
                {
                    CmnDao dao = new CmnDao(this.GetDam());
                    dao.SQLFileName = "OrdTfProduct.sql";
                    dao.SetParameter("ProductID", productId);
                    DataTable dt = new DataTable();
                    dao.ExecSelectFill_DT(dt);
                    if (dt.Rows.Count == 0) { AddFieldError(rv, key + "ProductID", "指定された商品は存在しません。"); ok = false; }
                    else
                    {
                        lv.ProductID = productId;
                        if (!seen.Add(productId)) duplicated = true;   // VAL-DUP（OD-T1）
                        // 新規の廃番商品は指定できない。読み込み時から存在する行（商品が同じ）は対象外（OD-T3）
                        if (Convert.ToBoolean(dt.Rows[0]["Discontinued"]) && !(origins[i].HasValue && origins[i].Value == productId))
                            discontinued = true;
                    }
                }

                decimal unitPrice, discount;
                int quantity;
                if (string.IsNullOrWhiteSpace(l.UnitPriceText)) { AddFieldError(rv, key + "UnitPrice", "単価は必須です。"); ok = false; }
                else if (!TryParseDecimal(l.UnitPriceText, out unitPrice) || unitPrice < 0m || unitPrice > MaxMoney)
                { AddFieldError(rv, key + "UnitPrice", "単価は 0 以上の数値で指定してください。"); ok = false; }
                else lv.UnitPrice = unitPrice;

                if (string.IsNullOrWhiteSpace(l.DiscountText)) { AddFieldError(rv, key + "Discount", "割引率は必須です。"); ok = false; }
                else if (!TryParseDecimal(l.DiscountText, out discount) || discount < 0m || discount > 1m)
                { AddFieldError(rv, key + "Discount", "割引率は 0 以上 1 以下の数値で指定してください。"); ok = false; }
                else lv.Discount = discount;

                if (string.IsNullOrWhiteSpace(l.QuantityText)) { AddFieldError(rv, key + "Quantity", "数量は必須です。"); ok = false; }
                else if (!TryParseInt(l.QuantityText, out quantity) || quantity < 1 || quantity > MaxQuantity)
                { AddFieldError(rv, key + "Quantity", "数量は 1 以上の整数で指定してください。"); ok = false; }
                else lv.Quantity = quantity;

                lv.OriginalProductID = origins[i];
                lv.Changed = !(l.OriginalUnitPrice.HasValue && l.OriginalDiscount.HasValue && l.OriginalQuantity.HasValue)
                    || lv.UnitPrice != l.OriginalUnitPrice.Value || lv.Discount != l.OriginalDiscount.Value || lv.Quantity != l.OriginalQuantity.Value;
                if (ok) v.Lines.Add(lv);
            }

            if (duplicated) rv.ErrorMessages.Add(MsgDuplicated);
            if (lines.Count == 0) rv.ErrorMessages.Add(MsgNoLine);          // ORD-T5
            if (discontinued) rv.ErrorMessages.Add(MsgDiscontinued);        // OD-T3

            if (pv.DeletedProductIDs != null) v.DeletedProductIDs.AddRange(pv.DeletedProductIDs);
            return v;
        }

        /// <summary>
        /// 各行が「読み込み時から存在する行」として名乗る商品 ID を、DB に今ある行（existing）と照らして決める。
        /// 登録モード（existing = null）・DB に無い商品 ID・既に別の行が名乗った商品 ID は、名乗りを認めない（null＝新規の行として扱う）。
        /// 商品が同じ行（変更の無い行）の名乗りを先に認め、次に商品を変えた行を、先に来た順に認める（DEC-ORD-49：行の扱いは定めないが、検証は外さない）
        /// </summary>
        private static int?[] ResolveOrigins(List<OrdTfSaveLine> lines, HashSet<int> existing)
        {
            int?[] result = new int?[lines.Count];
            if (existing == null) return result;
            HashSet<int> claimed = new HashSet<int>();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (result[i].HasValue || !lines[i].OriginalProductID.HasValue) continue;
                    int orig = lines[i].OriginalProductID.Value;
                    int product;
                    bool same = TryParseInt(lines[i].ProductIDText, out product) && product == orig;
                    if (pass == 0 && !same) continue;
                    if (!existing.Contains(orig) || claimed.Contains(orig)) continue;
                    claimed.Add(orig);
                    result[i] = orig;
                }
            }
            return result;
        }

        /// <summary>受注が今持っている明細の商品 ID（編集の保存のトランザクションの中で読む）</summary>
        private HashSet<int> LoadExistingProducts(int orderId)
        {
            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfDetailProducts.sql";
            dao.SetParameter("OrderID", orderId);
            DataTable dt = new DataTable();
            dao.ExecSelectFill_DT(dt);
            HashSet<int> set = new HashSet<int>();
            foreach (DataRow dr in dt.Rows) set.Add(Convert.ToInt32(dr["ProductID"]));
            return set;
        }

        /// <summary>桁数の検査（VAL-LENGTH）。空は NULL（null）を返す。空白だけは入力のまま（DEC-ORD-36 が決めているのは空のとき）</summary>
        private string CheckLength(OrdTfReturnValue rv, string field, string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (value.Length > max)
            {
                AddFieldError(rv, field, "最大 " + max + " 文字で入力してください。");
                return null;
            }
            return value;
        }

        /// <summary>受注の列のパラメタを設定する（NULL は DBNull）</summary>
        private static void SetOrderParameters(CmnDao dao, SaveValues v)
        {
            dao.SetParameter("CustomerID", v.CustomerID);
            dao.SetParameter("EmployeeID", v.EmployeeID);
            dao.SetParameter("OrderDate", v.OrderDate);
            dao.SetParameter("RequiredDate", v.RequiredDate.HasValue ? (object)v.RequiredDate.Value : DBNull.Value);
            dao.SetParameter("ShipVia", v.ShipVia.HasValue ? (object)v.ShipVia.Value : DBNull.Value);
            dao.SetParameter("Freight", v.Freight);
            dao.SetParameter("ShipName", (object)v.ShipName ?? DBNull.Value);
            dao.SetParameter("ShipAddress", (object)v.ShipAddress ?? DBNull.Value);
            dao.SetParameter("ShipCity", (object)v.ShipCity ?? DBNull.Value);
            dao.SetParameter("ShipRegion", (object)v.ShipRegion ?? DBNull.Value);
            dao.SetParameter("ShipPostalCode", (object)v.ShipPostalCode ?? DBNull.Value);
            dao.SetParameter("ShipCountry", (object)v.ShipCountry ?? DBNull.Value);
        }

        /// <summary>明細 1 行を挿入する（RowVersion は列に含めない）</summary>
        private void InsertDetail(int orderId, SaveLineValues l)
        {
            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfInsertDetail.sql";
            dao.SetParameter("OrderID", orderId);
            dao.SetParameter("ProductID", l.ProductID);
            dao.SetParameter("UnitPrice", l.UnitPrice);
            dao.SetParameter("Quantity", l.Quantity);
            dao.SetParameter("Discount", l.Discount);
            dao.ExecInsUpDel_NonQuery();
        }

        /// <summary>明細 1 行を、受注 ID と商品 ID の一致で削除する</summary>
        private void DeleteDetail(int orderId, int productId)
        {
            CmnDao dao = new CmnDao(this.GetDam());
            dao.SQLFileName = "OrdTfDeleteDetail.sql";
            dao.SetParameter("OrderID", orderId);
            dao.SetParameter("ProductID", productId);
            dao.ExecInsUpDel_NonQuery();
        }

        #endregion
    }
}
