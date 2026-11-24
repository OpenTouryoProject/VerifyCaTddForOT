//**********************************************************************************
//* サンプル アプリ・モデル
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfDetailViewModel
//* クラス日本語名  ：受注詳細・照会（SC-ORD-02）のビュー モデル
//*
//* 作成日時        ：2026/11/24
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/24  実装役            新規作成（EV-ORD-201・203）
//**********************************************************************************

using WSIFType_sample;

namespace MVC_Sample.Models.ViewModels
{
    /// <summary>受注詳細・照会（SC-ORD-02）のビュー モデル</summary>
    public class OrdTfDetailViewModel
    {
        /// <summary>B層（OrdTfDetail）の戻り値（I01〜I20・RowVersion・CanEdit）</summary>
        public OrdTfReturnValue Data { get; set; }

        /// <summary>共通メッセージ領域の文言</summary>
        public string Message { get; set; }

        /// <summary>共通メッセージ領域がエラーか（false は情報）</summary>
        public bool MessageIsError { get; set; }
    }
}
