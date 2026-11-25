//**********************************************************************************
//* サンプル アプリ・モデル
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfShipViewModel
//* クラス日本語名  ：出荷ステータス更新ダイアログ（SC-ORD-04）のビュー モデル
//*
//* 作成日時        ：2026/11/25
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/25  実装役            新規作成（EV-ORD-401〜403）
//**********************************************************************************

using WSIFType_sample;

using System.Collections.Generic;

namespace MVC_Sample.Models.ViewModels
{
    /// <summary>出荷ステータス更新ダイアログ（SC-ORD-04）のビュー モデル</summary>
    public class OrdTfShipViewModel
    {
        /// <summary>呼び出し元（"list"＝受注一覧、"detail"＝受注詳細）</summary>
        public string From { get; set; }

        /// <summary>受注 ID（SC-ORD-04-I01）</summary>
        public int OrderID { get; set; }

        /// <summary>ダイアログが保持している Orders.RowVersion（EV-ORD-401 手順 7）</summary>
        public int RowVersion { get; set; }

        /// <summary>出荷済か（表示時点。ボタンの表示名・活性・内訳の表示を決める。B層は DB の ShippedDate を正とし、この値を業務の検証に使わない＝DEC-ORD-60）</summary>
        public bool Shipped { get; set; }

        /// <summary>顧客名（SC-ORD-04-I02）</summary>
        public string CustomerName { get; set; }

        /// <summary>現在の状況（SC-ORD-04-I03）</summary>
        public string Status { get; set; }

        /// <summary>要求納期（SC-ORD-04-I04。yyyy/MM/dd。NULL は空）</summary>
        public string RequiredDate { get; set; }

        /// <summary>出荷日の入力（SC-ORD-04-I05。yyyy/MM/dd）</summary>
        public string ShippedDate { get; set; }

        /// <summary>配送業者の選択（SC-ORD-04-I06。ShipperID。未選択は空）</summary>
        public string ShipVia { get; set; }

        /// <summary>送料の入力（SC-ORD-04-I07）</summary>
        public string Freight { get; set; }

        /// <summary>配送業者の選択肢（先頭は空白）</summary>
        public List<OrdTfOption> ShipperOptions { get; set; } = new List<OrdTfOption>();

        /// <summary>在庫引落の内訳（SC-ORD-04-I08。未出荷だけ）</summary>
        public List<OrdTfShipStockLine> StockLines { get; set; } = new List<OrdTfShipStockLine>();

        /// <summary>共通メッセージ領域の文言（ダイアログに留まったときのエラー）</summary>
        public string Message { get; set; }

        /// <summary>項目の直下のエラー：出荷日</summary>
        public string ErrorShippedDate { get; set; }

        /// <summary>項目の直下のエラー：配送業者</summary>
        public string ErrorShipVia { get; set; }

        /// <summary>項目の直下のエラー：送料</summary>
        public string ErrorFreight { get; set; }
    }
}
