//**********************************************************************************
//* 受注一覧（テスト先行）・ビューモデル
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfViewModel
//* クラス日本語名  ：受注一覧（SC-ORD-01）のビューモデル
//*
//* 作成日時        ：2026/11/06
//* 作成者          ：Claude Code
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/06  Claude Code       新規作成
//**********************************************************************************

using System;
using System.Collections.Generic;

namespace MVC_Sample.Models.ViewModels
{
    /// <summary>受注一覧の 1 行（SC-ORD-01-I06）</summary>
    public class OrdTfRowViewModel
    {
        public int OrderID { get; set; }
        public string CustomerName { get; set; }
        public string EmployeeName { get; set; }
        public string OrderDate { get; set; }
        public string RequiredDate { get; set; }
        public string ShippedDate { get; set; }
        public string ShipperName { get; set; }
        public string TotalAmount { get; set; }
        public string Status { get; set; }
    }

    /// <summary>受注一覧（SC-ORD-01）のビューモデル</summary>
    public class OrdTfViewModel
    {
        /// <summary>顧客 ID（SC-ORD-01-I01）</summary>
        public string CustomerID { get; set; }

        /// <summary>担当者 ID（SC-ORD-01-I02）</summary>
        public int? EmployeeID { get; set; }

        /// <summary>受注日（開始）（SC-ORD-01-I03）</summary>
        public DateTime? OrderDateFrom { get; set; }

        /// <summary>受注日（終了）（SC-ORD-01-I04）</summary>
        public DateTime? OrderDateTo { get; set; }

        /// <summary>出荷状況（SC-ORD-01-I05）</summary>
        public string ShipStatus { get; set; } = "すべて";

        /// <summary>出荷状況の選択肢（固定値）</summary>
        public static readonly string[] ShipStatusItems = { "すべて", "未出荷", "出荷済", "納期遅延" };

        /// <summary>受注一覧（SC-ORD-01-I06）</summary>
        public List<OrdTfRowViewModel> Rows { get; set; } = new List<OrdTfRowViewModel>();

        /// <summary>総件数（SC-ORD-01-I07）</summary>
        public int TotalCount { get; set; }

        /// <summary>現在ページ（EV-ORD-101・102 では 1）</summary>
        public int PageNo { get; set; } = 1;

        /// <summary>総ページ数</summary>
        public int PageCount { get { return Math.Max(1, (TotalCount + 19) / 20); } }

        /// <summary>メッセージ（エラー・該当なし）</summary>
        public string Message { get; set; }

        /// <summary>メッセージがエラーか</summary>
        public bool IsError { get; set; }
    }
}
