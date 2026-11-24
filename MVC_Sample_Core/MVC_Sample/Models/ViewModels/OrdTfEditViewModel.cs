//**********************************************************************************
//* サンプル アプリ・モデル
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfEditViewModel
//* クラス日本語名  ：受注詳細・登録・編集（SC-ORD-03）のビュー モデル
//*
//* 作成日時        ：2026/11/24
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/24  実装役            新規作成（EV-ORD-301〜309）
//**********************************************************************************

using System.Collections.Generic;

using WSIFType_sample;

namespace MVC_Sample.Models.ViewModels
{
    /// <summary>受注詳細（登録・編集。SC-ORD-03）のビュー モデル。入力は画面の入力のままの文字列で持つ</summary>
    public class OrdTfEditViewModel
    {
        /// <summary>"new"（登録）・"edit"（編集）</summary>
        public string Mode { get; set; }

        /// <summary>受注 ID（編集のみ）</summary>
        public int OrderID { get; set; }

        /// <summary>画面が保持している Orders.RowVersion（編集のみ）</summary>
        public int RowVersion { get; set; }

        /// <summary>顧客（I02）</summary>
        public string CustomerID { get; set; }

        /// <summary>担当者（I05）</summary>
        public string EmployeeID { get; set; }

        /// <summary>受注日（I06）</summary>
        public string OrderDate { get; set; }

        /// <summary>要求納期（I07）</summary>
        public string RequiredDate { get; set; }

        /// <summary>配送業者（I08）</summary>
        public string ShipVia { get; set; }

        /// <summary>送料（I09）</summary>
        public string Freight { get; set; }

        /// <summary>出荷先名称・住所・市区町村・地域・郵便番号・国（I10〜I15）</summary>
        public string ShipName { get; set; }
        public string ShipAddress { get; set; }
        public string ShipCity { get; set; }
        public string ShipRegion { get; set; }
        public string ShipPostalCode { get; set; }
        public string ShipCountry { get; set; }

        /// <summary>顧客電話番号・顧客住所（I03・I04。表示のみ）</summary>
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }

        /// <summary>選択肢（先頭に空白の選択肢）</summary>
        public List<OrdTfOption> Customers { get; set; } = new List<OrdTfOption>();
        public List<OrdTfOption> Employees { get; set; } = new List<OrdTfOption>();
        public List<OrdTfOption> Shippers { get; set; } = new List<OrdTfOption>();
        public List<OrdTfOption> Products { get; set; } = new List<OrdTfOption>();

        /// <summary>明細入力表の行</summary>
        public List<OrdTfEditRow> Lines { get; set; } = new List<OrdTfEditRow>();

        /// <summary>削除対象として印を付けた、読み込み時の明細行の商品 ID</summary>
        public List<string> Deleted { get; set; } = new List<string>();

        /// <summary>登録モードの再描画で、選ばれている顧客の既定の出荷先（ShipName〜ShipCountry のキー）。画面の「自動で入れた値」の初期値（DEC-ORD-32）</summary>
        public Dictionary<string, string> AutoShip { get; set; } = new Dictionary<string, string>();

        /// <summary>読み込み時の入力の状態（未保存の変更の判定に使う。DEC-ORD-37）</summary>
        public string InitialSnapshot { get; set; }

        /// <summary>共通メッセージ領域の文言</summary>
        public string Message { get; set; }

        /// <summary>共通メッセージ領域がエラーか（false は情報）</summary>
        public bool MessageIsError { get; set; }

        /// <summary>項目単位の違反の文言（ErrorFields のキー → 文言）</summary>
        public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();

        /// <summary>項目の違反の文言を返す（無ければ空）</summary>
        public string Error(string key)
        {
            string v;
            return Errors.TryGetValue(key, out v) ? v : "";
        }
    }

    /// <summary>明細入力表の 1 行（画面の入力のままの文字列）</summary>
    public class OrdTfEditRow
    {
        /// <summary>選択された商品 ID（空は未選択）</summary>
        public string ProductID { get; set; } = "";

        /// <summary>単価</summary>
        public string UnitPrice { get; set; } = "";

        /// <summary>割引率</summary>
        public string Discount { get; set; } = "0";

        /// <summary>数量</summary>
        public string Quantity { get; set; } = "1";

        /// <summary>在庫（商品選択時に取得。NULL・未選択は空）</summary>
        public string Stock { get; set; } = "";

        /// <summary>読み込み時に存在した行の商品 ID・単価・割引率・数量（画面で足した行は空）</summary>
        public string OrigProductID { get; set; } = "";
        public string OrigUnitPrice { get; set; } = "";
        public string OrigDiscount { get; set; } = "";
        public string OrigQuantity { get; set; } = "";

        /// <summary>読み込み時の商品名（廃番の商品は選択肢に無いので、その行の選択肢にだけ足す。OD-T3）</summary>
        public string OrigName { get; set; } = "";
    }
}
