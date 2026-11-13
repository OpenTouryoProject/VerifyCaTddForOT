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
//**********************************************************************************

using System;
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

        /// <summary>コンストラクタ</summary>
        public OrdTfParameterValue(string screenId, string controlId, string methodName, string actionType, MyUserInfo user)
            : base(screenId, controlId, methodName, actionType, user)
        {
        }
    }
}
