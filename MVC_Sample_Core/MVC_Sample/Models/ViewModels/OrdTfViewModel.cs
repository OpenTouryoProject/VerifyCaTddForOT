//**********************************************************************************
//* サンプル アプリ・モデル
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfViewModel
//* クラス日本語名  ：受注一覧（SC-ORD-01）のビュー モデル
//*
//* 作成日時        ：2026/11/13
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/13  実装役            新規作成（net48 版 MVC_Sample と同じ構成。Core 用に名前空間を直した）
//**********************************************************************************

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

using WSIFType_sample;

namespace MVC_Sample.Models.ViewModels
{
    /// <summary>受注一覧（SC-ORD-01）のビュー モデル</summary>
    public class OrdTfViewModel
    {
        #region 入力（SC-ORD-01-I01〜I05。画面の今の値）

        /// <summary>顧客（I01）</summary>
        public string CustomerID { get; set; }

        /// <summary>担当者（I02）</summary>
        public string EmployeeID { get; set; }

        /// <summary>受注日（開始）（I03）</summary>
        public string OrderDateFrom { get; set; }

        /// <summary>受注日（終了）（I04）</summary>
        public string OrderDateTo { get; set; }

        /// <summary>出荷状況（I05）</summary>
        public string ShippedStatus { get; set; }

        #endregion

        #region 一覧状態の保持（hidden。最後に実行した検索の条件・ページ。DEC-ORD-04）

        /// <summary>保持：顧客</summary>
        public string PCustomerID { get; set; }

        /// <summary>保持：担当者</summary>
        public string PEmployeeID { get; set; }

        /// <summary>保持：受注日（開始）</summary>
        public string POrderDateFrom { get; set; }

        /// <summary>保持：受注日（終了）</summary>
        public string POrderDateTo { get; set; }

        /// <summary>保持：出荷状況</summary>
        public string PShippedStatus { get; set; }

        /// <summary>保持：現在ページ</summary>
        public int PPageNo { get; set; }

        /// <summary>保持：総ページ数（ページ移動の判定に使う。EV-ORD-104 手順 1）</summary>
        public int PTotalPages { get; set; }

        #endregion

        #region 出力

        /// <summary>顧客の選択肢</summary>
        public List<SelectListItem> CustomerItems { get; set; }

        /// <summary>担当者の選択肢</summary>
        public List<SelectListItem> EmployeeItems { get; set; }

        /// <summary>出荷状況の選択肢（固定値）</summary>
        public List<SelectListItem> StatusItems { get; set; }

        /// <summary>受注一覧の行（I06）</summary>
        public List<OrdTfRow> Rows { get; set; }

        /// <summary>総件数（I07）</summary>
        public int TotalCount { get; set; }

        /// <summary>現在ページ（I08）</summary>
        public int PageNo { get; set; }

        /// <summary>総ページ数（I08）</summary>
        public int TotalPages { get; set; }

        /// <summary>共通メッセージ領域の文言</summary>
        public string Message { get; set; }

        /// <summary>共通メッセージ領域がエラーか（false は情報）</summary>
        public bool MessageIsError { get; set; }

        /// <summary>受注日（開始）の項目単位のエラー（直下に表示）</summary>
        public string ErrorFrom { get; set; }

        /// <summary>受注日（終了）の項目単位のエラー（直下に表示）</summary>
        public string ErrorTo { get; set; }

        #endregion

        /// <summary>コンストラクタ</summary>
        public OrdTfViewModel()
        {
            this.Rows = new List<OrdTfRow>();
            this.CustomerItems = new List<SelectListItem>();
            this.EmployeeItems = new List<SelectListItem>();
            this.StatusItems = new List<SelectListItem>();
        }
    }
}
