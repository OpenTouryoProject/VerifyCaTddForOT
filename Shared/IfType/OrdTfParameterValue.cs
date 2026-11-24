//**********************************************************************************
//* フレームワーク・テストクラス（Ｉ／Ｆ）
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfParameterValue
//* クラス日本語名  ：受注一覧（OrdTf）の引数クラス
//*
//* 作成日時        ：2026/11/13
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/13  実装役            新規作成（テスト先行の Red 用。契約はテスト仕様 1.2）
//*  2026/11/15  実装役            並べ替えのメンバ（SortColumn・SortDescending）を追加（EV-ORD-105。Red の足場）
//*  2026/11/24  実装役            受注詳細・削除のメンバ（OrderID・RowVersion）を追加（EV-ORD-201・203。Red の足場）
//*  2026/11/24  実装役            受注詳細（登録・編集）のメンバ（EV-ORD-301〜308。Red の足場）
//**********************************************************************************

using System;
using System.Collections.Generic;
using Touryo.Infrastructure.Business.Common;
using Touryo.Infrastructure.Business.Util;

namespace WSIFType_sample
{
    /// <summary>受注一覧（OrdTf）の引数クラス</summary>
    /// <remarks>シリアライズ可能にする（WS対応）</remarks>
    [Serializable()]
    public class OrdTfParameterValue : MyParameterValue
    {
        /// <summary>顧客 ID（null・空文字は未指定）</summary>
        public string CustomerID;

        /// <summary>担当者 ID（null は未指定）</summary>
        public int? EmployeeID;

        /// <summary>受注日（開始）。画面の入力のまま（null・空白だけは未指定）</summary>
        public string OrderDateFrom;

        /// <summary>受注日（終了）。画面の入力のまま（null・空白だけは未指定）</summary>
        public string OrderDateTo;

        /// <summary>出荷状況（すべて・未出荷・出荷済・納期遅延。null・空文字は「すべて」）</summary>
        public string ShippedStatus;

        /// <summary>表示するページ（1 始まり）</summary>
        public int PageNo;

        /// <summary>
        /// 並べ替える列のキー（OrderID・CustomerName・EmployeeName・OrderDate・RequiredDate・ShippedDate・ShipperName）。
        /// null・空文字は既定の並び（EV-ORD-105。テスト仕様 8.1）
        /// </summary>
        public string SortColumn;

        /// <summary>true = 降順、false = 昇順（SortColumn が空のときは無視する）</summary>
        public bool SortDescending;

        /// <summary>対象の受注 ID（OrdTfDetail・OrdTfDelete）</summary>
        public int OrderID;

        /// <summary>画面が保持していた Orders.RowVersion（OrdTfDelete）</summary>
        public int RowVersion;

        #region 受注詳細（登録・編集。OrdTfEditInit・OrdTfCustomer・OrdTfProduct・OrdTfSave。EV-ORD-301〜308）

        /// <summary>"new"（登録）・"edit"（編集）（OrdTfEditInit・OrdTfSave）</summary>
        public string Mode;

        /// <summary>担当者の入力値（選択肢の value。空・null は未選択。OrdTfSave）</summary>
        public string EmployeeIDText;

        /// <summary>配送業者の入力値（選択肢の value。空・null は未選択。OrdTfSave）</summary>
        public string ShipViaText;

        /// <summary>受注日の入力（画面の入力のまま。OrdTfSave）</summary>
        public string OrderDateText;

        /// <summary>要求納期の入力（画面の入力のまま。OrdTfSave）</summary>
        public string RequiredDateText;

        /// <summary>送料の入力（画面の入力のまま。空は 0。DEC-ORD-01。OrdTfSave）</summary>
        public string FreightText;

        /// <summary>出荷先名称（OrdTfSave）</summary>
        public string ShipName;

        /// <summary>出荷先住所（OrdTfSave）</summary>
        public string ShipAddress;

        /// <summary>出荷先市区町村（OrdTfSave）</summary>
        public string ShipCity;

        /// <summary>出荷先地域（OrdTfSave）</summary>
        public string ShipRegion;

        /// <summary>出荷先郵便番号（OrdTfSave）</summary>
        public string ShipPostalCode;

        /// <summary>出荷先国（OrdTfSave）</summary>
        public string ShipCountry;

        /// <summary>商品 ID（OrdTfProduct）</summary>
        public int ProductID;

        /// <summary>明細入力表の行（画面の並びのまま。OrdTfSave）</summary>
        public List<OrdTfSaveLine> SaveLines = new List<OrdTfSaveLine>();

        /// <summary>削除対象として印を付けた、読み込み時の明細行の商品 ID（OrdTfSave・編集）</summary>
        public List<int> DeletedProductIDs = new List<int>();

        #endregion

        /// <summary>コンストラクタ</summary>
        public OrdTfParameterValue(string screenId, string controlId, string methodName, string actionType, MyUserInfo user)
            : base(screenId, controlId, methodName, actionType, user)
        {
        }
    }

    /// <summary>受注の保存（OrdTfSave）の明細入力表の 1 行。入力の文字列は画面の入力のまま</summary>
    [Serializable()]
    public class OrdTfSaveLine
    {
        /// <summary>選択された商品 ID（空は未選択）</summary>
        public string ProductIDText;

        /// <summary>単価</summary>
        public string UnitPriceText;

        /// <summary>割引率</summary>
        public string DiscountText;

        /// <summary>数量</summary>
        public string QuantityText;

        /// <summary>読み込み時に存在した行の商品 ID（画面で足した行は null）</summary>
        public int? OriginalProductID;

        /// <summary>読み込み時の単価</summary>
        public decimal? OriginalUnitPrice;

        /// <summary>読み込み時の割引率</summary>
        public decimal? OriginalDiscount;

        /// <summary>読み込み時の数量</summary>
        public int? OriginalQuantity;
    }}
