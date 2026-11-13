//**********************************************************************************
//* フレームワーク・テストクラス（Ｉ／Ｆ）
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfReturnValue
//* クラス日本語名  ：受注一覧（OrdTf）の戻り値クラス
//*
//* 作成日時        ：2026/11/13
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/13  実装役            新規作成（テスト先行の Red 用。契約はテスト仕様 1.3）
//**********************************************************************************

using System;
using System.Collections.Generic;
using Touryo.Infrastructure.Business.Common;

namespace WSIFType_sample
{
    /// <summary>選択肢（値と表示）</summary>
    [Serializable()]
    public class OrdTfOption
    {
        /// <summary>値</summary>
        public string Value;

        /// <summary>表示</summary>
        public string Text;
    }

    /// <summary>受注一覧の行</summary>
    [Serializable()]
    public class OrdTfRow
    {
        /// <summary>受注 ID</summary>
        public int OrderID;

        /// <summary>顧客名</summary>
        public string CustomerName;

        /// <summary>担当者名（姓 + " " + 名）</summary>
        public string EmployeeName;

        /// <summary>配送業者名</summary>
        public string ShipperName;

        /// <summary>受注日</summary>
        public DateTime? OrderDate;

        /// <summary>要求納期</summary>
        public DateTime? RequiredDate;

        /// <summary>出荷日</summary>
        public DateTime? ShippedDate;

        /// <summary>合計金額</summary>
        public decimal TotalAmount;

        /// <summary>状況（未出荷・出荷済・納期遅延）</summary>
        public string Status;
    }

    /// <summary>受注一覧（OrdTf）の戻り値クラス</summary>
    /// <remarks>シリアライズ可能にする（WS対応）</remarks>
    [Serializable()]
    public class OrdTfReturnValue : MyReturnValue
    {
        /// <summary>顧客の選択肢（先頭は空白）</summary>
        public List<OrdTfOption> CustomerOptions = new List<OrdTfOption>();

        /// <summary>担当者の選択肢（先頭は空白）</summary>
        public List<OrdTfOption> EmployeeOptions = new List<OrdTfOption>();

        /// <summary>当該ページの行（最大 20 件）</summary>
        public List<OrdTfRow> Rows = new List<OrdTfRow>();

        /// <summary>総件数</summary>
        public int TotalCount;

        /// <summary>総ページ数（最小 1）</summary>
        public int TotalPages;

        /// <summary>実際に返したページ</summary>
        public int PageNo;

        /// <summary>項目単位の検証の違反の項目（"OrderDateFrom"・"OrderDateTo"）</summary>
        public List<string> ErrorFields = new List<string>();
    }
}
