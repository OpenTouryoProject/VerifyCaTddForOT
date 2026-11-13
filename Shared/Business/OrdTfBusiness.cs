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
