//**********************************************************************************
//* 受注一覧（テスト先行）・業務コードクラス（Ｂ層）
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfBusiness
//* クラス日本語名  ：受注一覧の検索（Ｂ層）
//*
//* 作成日時        ：2026/11/06
//* 作成者          ：Claude Code
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/06  Claude Code       新規作成（段 2：戻り値を設定するだけの空の UOC）
//*  2026/11/06  Claude Code       段 4：UOC_OrdTfSearch を実装（EV-ORD-101・102）
//*  2026/11/06  Claude Code       メッセージ ID のコメントを決定に合わせた（NorthwindDbSampleSpec@fe574ce）
//*  2026/11/07  Claude Code       2 回目 段 2：戻り値を設定するだけの空の UOC_OrdTfInit を追加
//**********************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using MVC_Sample.Logic.Common;

using Touryo.Infrastructure.Business.Business;
using Touryo.Infrastructure.Business.Dao;
using Touryo.Infrastructure.Framework.Exceptions;

namespace MVC_Sample.Logic.Business
{
    /// <summary>受注一覧の検索（Ｂ層）</summary>
    public class OrdTfBusiness : MyFcBaseLogic
    {
        /// <summary>1 ページの件数（HLD/Common.md 4 節）</summary>
        private const int PageSize = 20;

        #region UOCメソッド

        /// <summary>受注の検索（EV-ORD-101・EV-ORD-102）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfSearch(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            // 検証・業務エラーの場合も、空の一覧（0 件）が戻る。
            rv.Rows = CreateEmptyTable();
            rv.TotalCount = 0;

            // 検証（VAL-DATE）：両方が指定された場合は 開始 <= 終了
            if (pv.OrderDateFrom.HasValue && pv.OrderDateTo.HasValue
                && pv.OrderDateFrom.Value.Date > pv.OrderDateTo.Value.Date)
            {
                // メッセージは文言で定まる。メッセージ ID は実装で決める（HLD/Common.md 7 節）
                throw new BusinessApplicationException(
                    "ORD_VAL_DATE", "受注日の開始は終了以前を指定してください。", "");
            }

            DateTime today = DateTime.Today;

            // 抽出条件の組み立て（固定の断片だけを連結し、値は必ずパラメタで渡す）
            StringBuilder where = new StringBuilder();
            List<KeyValuePair<string, object>> prms = new List<KeyValuePair<string, object>>();

            if (!string.IsNullOrEmpty(pv.CustomerID))
            {
                prms.Add(new KeyValuePair<string, object>("P" + (prms.Count + 1), pv.CustomerID));
                where.Append(" AND o.CustomerID = @P" + prms.Count);
            }
            if (pv.EmployeeID.HasValue)
            {
                prms.Add(new KeyValuePair<string, object>("P" + (prms.Count + 1), pv.EmployeeID.Value));
                where.Append(" AND o.EmployeeID = @P" + prms.Count);
            }
            if (pv.OrderDateFrom.HasValue)
            {
                prms.Add(new KeyValuePair<string, object>("P" + (prms.Count + 1), pv.OrderDateFrom.Value.Date));
                where.Append(" AND o.OrderDate >= @P" + prms.Count);
            }
            if (pv.OrderDateTo.HasValue)
            {
                // 当該日を含む（時刻部があっても含めるため、翌日未満で比較）
                prms.Add(new KeyValuePair<string, object>("P" + (prms.Count + 1), pv.OrderDateTo.Value.Date.AddDays(1)));
                where.Append(" AND o.OrderDate < @P" + prms.Count);
            }
            switch (pv.ShipStatus)
            {
                case "未出荷":
                    where.Append(" AND o.ShippedDate IS NULL");
                    break;
                case "出荷済":
                    where.Append(" AND o.ShippedDate IS NOT NULL");
                    break;
                case "納期遅延":
                    prms.Add(new KeyValuePair<string, object>("P" + (prms.Count + 1), today));
                    where.Append(" AND o.RequiredDate IS NOT NULL AND (" +
                        "(o.ShippedDate IS NULL AND o.RequiredDate < @P" + prms.Count + ")" +
                        " OR (o.ShippedDate IS NOT NULL AND o.ShippedDate > o.RequiredDate))");
                    break;
                default: // すべて（未指定）
                    break;
            }

            // 総件数
            CmnDao countDao = new CmnDao(this.GetDam());
            countDao.SQLText = "SELECT COUNT(*) FROM Orders o WHERE 1 = 1" + where;
            foreach (KeyValuePair<string, object> p in prms) countDao.SetParameter(p.Key, p.Value);
            rv.TotalCount = Convert.ToInt32(countDao.ExecSelectScalar());

            if (rv.TotalCount == 0) return; // 0 件はエラーにしない（EV-ORD-101 手順 7）

            // 当該ページの 20 件（既定の並び：受注日の降順、同日は受注 ID の降順）
            // 合計金額は、明細ごとに小数第 3 位を四捨五入（10 進数で計算。Discount は real のため decimal へ変換）
            CmnDao selDao = new CmnDao(this.GetDam());
            selDao.SQLText =
                "SELECT o.OrderID, c.CompanyName AS CustomerName," +
                " e.LastName + N' ' + e.FirstName AS EmployeeName," +
                " o.OrderDate, o.RequiredDate, o.ShippedDate, s.CompanyName AS ShipperName," +
                " (SELECT ISNULL(SUM(ROUND(CAST(d.UnitPrice AS decimal(19,4))" +
                " * (1 - CAST(d.Discount AS decimal(9,4))) * d.Quantity, 2)), 0)" +
                " FROM [Order Details] d WHERE d.OrderID = o.OrderID)" +
                " + CAST(ISNULL(o.Freight, 0) AS decimal(19,4)) AS TotalAmount" +
                " FROM Orders o" +
                " LEFT JOIN Customers c ON c.CustomerID = o.CustomerID" +
                " LEFT JOIN Employees e ON e.EmployeeID = o.EmployeeID" +
                " LEFT JOIN Shippers s ON s.ShipperID = o.ShipVia" +
                " WHERE 1 = 1" + where +
                " ORDER BY o.OrderDate DESC, o.OrderID DESC" +
                " OFFSET " + (Math.Max(pv.PageNo, 1) - 1) * PageSize + " ROWS FETCH NEXT " + PageSize + " ROWS ONLY";
            foreach (KeyValuePair<string, object> p in prms) selDao.SetParameter(p.Key, p.Value);

            DataTable dt = new DataTable();
            selDao.ExecSelectFill_DT(dt);

            // 状況の判定（ORD-T7）
            dt.Columns.Add("Status", typeof(string));
            foreach (DataRow row in dt.Rows)
            {
                row["Status"] = JudgeStatus(row["RequiredDate"], row["ShippedDate"], today);
            }
            rv.Rows = dt;

            // ↑業務処理-----------------------------------------------------
        }

        /// <summary>選択肢の構築（EV-ORD-101 手順 1：顧客・担当者）</summary>
        /// <param name="pv">引数クラス（入力なし）</param>
        private void UOC_OrdTfInit(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            // 段 2（Red）：戻り値を設定するだけ。実装は段 4。
            rv.CustomerOptions = CreateEmptyOptions();
            rv.EmployeeOptions = CreateEmptyOptions();

            // ↑業務処理-----------------------------------------------------
        }

        #endregion

        #region 部品

        /// <summary>列（Value・Text）だけを持つ空の選択肢</summary>
        private static DataTable CreateEmptyOptions()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Value", typeof(string));
            dt.Columns.Add("Text", typeof(string));
            return dt;
        }

        /// <summary>状況の判定（HLD 共通の処理・ORD-T7）</summary>
        private static string JudgeStatus(object requiredDate, object shippedDate, DateTime today)
        {
            bool hasRequired = requiredDate != null && requiredDate != DBNull.Value;
            bool shipped = shippedDate != null && shippedDate != DBNull.Value;

            if (!shipped)
            {
                // RequiredDate が NULL のときは遅延と判定しない
                return (hasRequired && (DateTime)requiredDate < today) ? "納期遅延" : "未出荷";
            }
            return (hasRequired && (DateTime)shippedDate > (DateTime)requiredDate) ? "納期遅延" : "出荷済";
        }

        /// <summary>列だけを持つ空の一覧</summary>
        private static DataTable CreateEmptyTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("OrderID", typeof(int));
            dt.Columns.Add("CustomerName", typeof(string));
            dt.Columns.Add("EmployeeName", typeof(string));
            dt.Columns.Add("OrderDate", typeof(DateTime));
            dt.Columns.Add("RequiredDate", typeof(DateTime));
            dt.Columns.Add("ShippedDate", typeof(DateTime));
            dt.Columns.Add("ShipperName", typeof(string));
            dt.Columns.Add("TotalAmount", typeof(decimal));
            dt.Columns.Add("Status", typeof(string));
            return dt;
        }

        #endregion
    }
}
