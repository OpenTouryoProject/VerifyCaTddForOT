//**********************************************************************************
//* 受注一覧（テスト先行）・戻り値クラス（Ｂ層）
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfReturnValue
//* クラス日本語名  ：受注一覧の検索の戻り値クラス
//*
//* 作成日時        ：2026/11/06
//* 作成者          ：Claude Code
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/06  Claude Code       新規作成（docs/spec/test6-testfirst-mvc-core-tests.md）
//**********************************************************************************

using System.Data;

using Touryo.Infrastructure.Business.Common;

namespace MVC_Sample.Logic.Common
{
    /// <summary>受注一覧の検索の戻り値クラス</summary>
    public class OrdTfReturnValue : MyReturnValue
    {
        /// <summary>受注一覧（SC-ORD-01-I06）。
        /// 列：OrderID・CustomerName・EmployeeName・OrderDate・RequiredDate・ShippedDate・ShipperName・TotalAmount・Status</summary>
        public DataTable Rows { get; set; }

        /// <summary>総件数（SC-ORD-01-I07）。ページ分ではなく全体</summary>
        public int TotalCount { get; set; }
    }
}
