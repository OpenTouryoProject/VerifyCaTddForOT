//**********************************************************************************
//* 受注一覧（テスト先行）・引数クラス（Ｂ層）
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfParameterValue
//* クラス日本語名  ：受注一覧の検索の引数クラス
//*
//* 作成日時        ：2026/11/06
//* 作成者          ：Claude Code
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/06  Claude Code       新規作成（docs/spec/test6-testfirst-mvc-core-tests.md）
//**********************************************************************************

using System;

using Touryo.Infrastructure.Business.Common;
using Touryo.Infrastructure.Business.Util;

namespace MVC_Sample.Logic.Common
{
    /// <summary>受注一覧の検索の引数クラス（SC-ORD-01-I01〜I05）</summary>
    public class OrdTfParameterValue : MyParameterValue
    {
        /// <summary>顧客 ID（SC-ORD-01-I01。空 = 未指定）</summary>
        public string CustomerID { get; set; }

        /// <summary>担当者 ID（SC-ORD-01-I02。null = 未指定）</summary>
        public int? EmployeeID { get; set; }

        /// <summary>受注日（開始）（SC-ORD-01-I03。null = 未指定）</summary>
        public DateTime? OrderDateFrom { get; set; }

        /// <summary>受注日（終了）（SC-ORD-01-I04。null = 未指定）</summary>
        public DateTime? OrderDateTo { get; set; }

        /// <summary>出荷状況（SC-ORD-01-I05）：すべて／未出荷／出荷済／納期遅延</summary>
        public string ShipStatus { get; set; } = "すべて";

        /// <summary>ページ番号（EV-ORD-101・102 では 1）</summary>
        public int PageNo { get; set; } = 1;

        #region コンストラクタ

        /// <summary>コンストラクタ</summary>
        public OrdTfParameterValue(string screenId, string controlId, string methodName, string actionType, MyUserInfo user)
            : base(screenId, controlId, methodName, actionType, user)
        {
            // Baseのコンストラクタに引数を渡すために必要。
        }

        #endregion
    }
}
