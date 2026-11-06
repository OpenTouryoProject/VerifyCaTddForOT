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
//**********************************************************************************

using System.Data;

using MVC_Sample.Logic.Common;

using Touryo.Infrastructure.Business.Business;

namespace MVC_Sample.Logic.Business
{
    /// <summary>受注一覧の検索（Ｂ層）</summary>
    public class OrdTfBusiness : MyFcBaseLogic
    {
        #region UOCメソッド

        /// <summary>受注の検索（EV-ORD-101・EV-ORD-102）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfSearch(OrdTfParameterValue pv)
        {
            // 戻り値クラスを生成して、事前に戻り値に設定しておく。
            OrdTfReturnValue rv = new OrdTfReturnValue();
            this.ReturnValue = rv;

            // ↓業務処理-----------------------------------------------------

            // 段 2（Red）：戻り値を設定するだけ。実装は段 4。
            DataTable dt = new DataTable();
            foreach (string col in new string[] {
                "OrderID", "CustomerName", "EmployeeName", "OrderDate", "RequiredDate",
                "ShippedDate", "ShipperName", "TotalAmount", "Status" })
            {
                dt.Columns.Add(col);
            }
            rv.Rows = dt;
            rv.TotalCount = -1; // 未実装を示す値（空の結果 = 0 と区別する）

            // ↑業務処理-----------------------------------------------------
        }

        #endregion
    }
}
