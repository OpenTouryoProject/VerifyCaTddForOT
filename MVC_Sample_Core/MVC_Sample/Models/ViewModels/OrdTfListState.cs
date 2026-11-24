//**********************************************************************************
//* サンプル アプリ・モデル
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfListState
//* クラス日本語名  ：受注一覧（SC-ORD-01）の一覧状態（Session に保持）
//*
//* 作成日時        ：2026/11/24
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/24  実装役            新規作成（EV-ORD-106・107・206 の「一覧状態の保持」）
//**********************************************************************************

namespace MVC_Sample.Models.ViewModels
{
    /// <summary>
    /// 受注一覧の一覧状態（最後に実行した検索の条件・現在ページ・並べ替え。DEC-ORD-04・DEC-ORD-19）。
    /// 一覧の操作（初期表示・検索・ページ切替・並べ替え）のたびに Session へ保持し、
    /// 詳細・新規登録から戻る（/OrdTf/Back）ときに復元する。
    /// </summary>
    public class OrdTfListState
    {
        /// <summary>顧客</summary>
        public string CustomerID { get; set; }

        /// <summary>担当者</summary>
        public string EmployeeID { get; set; }

        /// <summary>受注日（開始）</summary>
        public string OrderDateFrom { get; set; }

        /// <summary>受注日（終了）</summary>
        public string OrderDateTo { get; set; }

        /// <summary>出荷状況</summary>
        public string ShippedStatus { get; set; }

        /// <summary>並べ替えの列のキー（空は既定の並び）</summary>
        public string SortColumn { get; set; }

        /// <summary>並べ替えの方向（true = 降順）</summary>
        public bool SortDescending { get; set; }

        /// <summary>現在ページ</summary>
        public int PageNo { get; set; }
    }
}
