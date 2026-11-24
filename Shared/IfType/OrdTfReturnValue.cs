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
//*  2026/11/24  実装役            受注詳細（OrdTfDetailLine と詳細のメンバ）を追加（EV-ORD-201。Red の足場。契約はテスト仕様 1.1）
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

    /// <summary>受注詳細の明細 1 行（EV-ORD-201）</summary>
    [Serializable()]
    public class OrdTfDetailLine
    {
        /// <summary>商品名（廃番は「（廃番）」を付記した表示名）</summary>
        public string ProductName;

        /// <summary>単価</summary>
        public decimal UnitPrice;

        /// <summary>割引率（0〜1）</summary>
        public decimal Discount;

        /// <summary>価格（単価 × (1 − 割引率)。丸めない）</summary>
        public decimal Price;

        /// <summary>数量</summary>
        public int Quantity;

        /// <summary>在庫（NULL は null）</summary>
        public int? UnitsInStock;

        /// <summary>明細金額（価格 × 数量を小数第 3 位で四捨五入）</summary>
        public decimal Amount;
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

        #region 受注詳細（OrdTfDetail。EV-ORD-201）

        /// <summary>受注 ID（I01）</summary>
        public int OrderID;

        /// <summary>状況（I02）：「未出荷」「出荷済」「納期遅延」</summary>
        public string Status;

        /// <summary>顧客名（I03）。無いときは空文字</summary>
        public string CustomerName;

        /// <summary>顧客電話番号（I04）</summary>
        public string CustomerPhone;

        /// <summary>顧客住所（I05）：空の要素を詰めて連結</summary>
        public string CustomerAddress;

        /// <summary>担当者名（I06）：姓 + 半角空白 + 名</summary>
        public string EmployeeName;

        /// <summary>受注日（I07）</summary>
        public DateTime? OrderDate;

        /// <summary>要求納期（I08）</summary>
        public DateTime? RequiredDate;

        /// <summary>出荷日（I09）</summary>
        public DateTime? ShippedDate;

        /// <summary>配送業者（I10）</summary>
        public string ShipperName;

        /// <summary>出荷先名称（I11）</summary>
        public string ShipName;

        /// <summary>出荷先住所（I12）</summary>
        public string ShipAddress;

        /// <summary>出荷先市区町村（I13）</summary>
        public string ShipCity;

        /// <summary>出荷先地域（I14）</summary>
        public string ShipRegion;

        /// <summary>出荷先郵便番号（I15）</summary>
        public string ShipPostalCode;

        /// <summary>出荷先国（I16）</summary>
        public string ShipCountry;

        /// <summary>明細（I17）：商品名の昇順</summary>
        public List<OrdTfDetailLine> Lines = new List<OrdTfDetailLine>();

        /// <summary>小計（I18）</summary>
        public decimal Subtotal;

        /// <summary>送料（I19）：NULL は null</summary>
        public decimal? Freight;

        /// <summary>合計（I20）</summary>
        public decimal Total;

        /// <summary>Orders.RowVersion</summary>
        public int RowVersion;

        /// <summary>編集ボタンの活性（ShippedDate が NULL のとき true）</summary>
        public bool CanEdit;

        #endregion
    }
}
