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
//*  2026/11/07  Claude Code       選択肢の構築（OrdTfInit）の戻り値を追加
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

        /// <summary>顧客の選択肢（SC-ORD-01-I01。OrdTfInit）。列：Value（CustomerID）・Text（CompanyName）。
        /// 会社名の昇順。先頭の空白の選択肢は画面が置く</summary>
        public DataTable CustomerOptions { get; set; }

        /// <summary>担当者の選択肢（SC-ORD-01-I02。OrdTfInit）。列：Value（EmployeeID の文字列）・Text（姓 + 半角空白 + 名）。
        /// 姓・名の昇順。先頭の空白の選択肢は画面が置く</summary>
        public DataTable EmployeeOptions { get; set; }
    }
}
