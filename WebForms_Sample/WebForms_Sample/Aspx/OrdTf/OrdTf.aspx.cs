//**********************************************************************************
//* サンプル アプリ・ＵＩ層
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTf
//* クラス日本語名  ：受注一覧（SC-ORD-01）の画面コード
//*
//* 作成日時        ：2026/11/14
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/14  実装役            新規作成
//**********************************************************************************

using WSIFType_sample;
using WSServer_sample.Business;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;

using Touryo.Infrastructure.Business.Presentation;
using Touryo.Infrastructure.CustomControl;
using Touryo.Infrastructure.Framework.Presentation;
using Touryo.Infrastructure.Public.Db;

namespace WebForms_Sample.Aspx.OrdTf
{
    /// <summary>
    /// 受注一覧（SC-ORD-01）の画面コード
    /// 一覧状態（最後に実行した検索の条件・現在ページ・総ページ数）は ViewState で持つ（Session は使わない）。
    /// </summary>
    public partial class OrdTf : MyBaseController
    {
        #region 定数

        /// <summary>0 件の知らせ（EV-ORD-101 手順 7。エラーとしない）</summary>
        private const string MsgNone = "該当するデータがありません。";

        /// <summary>出荷状況の選択肢（固定値。SC-ORD-01-I05）</summary>
        private static readonly string[] StatusValues = { "すべて", "未出荷", "出荷済", "納期遅延" };

        /// <summary>B層呼び出しの分離レベル（読み取りだけ。B層のテストと同じ値）</summary>
        private const DbEnum.IsolationLevelEnum Iso = DbEnum.IsolationLevelEnum.ReadCommitted;

        #endregion

        #region 一覧状態の保持（ViewState。最後に実行した検索の条件・ページ。DEC-ORD-04）

        /// <summary>保持：顧客</summary>
        private string PCustomerID { get { return (string)this.ViewState["PCustomerID"] ?? ""; } set { this.ViewState["PCustomerID"] = value; } }

        /// <summary>保持：担当者</summary>
        private string PEmployeeID { get { return (string)this.ViewState["PEmployeeID"] ?? ""; } set { this.ViewState["PEmployeeID"] = value; } }

        /// <summary>保持：受注日（開始）</summary>
        private string POrderDateFrom { get { return (string)this.ViewState["POrderDateFrom"] ?? ""; } set { this.ViewState["POrderDateFrom"] = value; } }

        /// <summary>保持：受注日（終了）</summary>
        private string POrderDateTo { get { return (string)this.ViewState["POrderDateTo"] ?? ""; } set { this.ViewState["POrderDateTo"] = value; } }

        /// <summary>保持：出荷状況</summary>
        private string PShippedStatus { get { return (string)this.ViewState["PShippedStatus"] ?? StatusValues[0]; } set { this.ViewState["PShippedStatus"] = value; } }

        /// <summary>保持：現在ページ</summary>
        private int PPageNo { get { object o = this.ViewState["PPageNo"]; return o == null ? 1 : (int)o; } set { this.ViewState["PPageNo"] = value; } }

        /// <summary>保持：総ページ数（ページ移動の判定に使う。EV-ORD-104 手順 1）</summary>
        private int PTotalPages { get { object o = this.ViewState["PTotalPages"]; return o == null ? 1 : (int)o; } set { this.ViewState["PTotalPages"] = value; } }

        #endregion

        #region Page LoadのUOCメソッド

        /// <summary>Page LoadのUOCメソッド（個別：初回Load）。受注一覧のページ読み込み（EV-ORD-101）</summary>
        /// <remarks>実装必須</remarks>
        protected override void UOC_FormInit()
        {
            this.SetFooterButtons();

            // 選択肢の構築（手順 1・2）。先頭は空白（すべて）。出荷状況は「すべて」を選択
            this.BuildChoices();

            // 検索条件は未指定、ページは 1（保持された一覧状態の復元は範囲外）。手順 3〜7
            this.Retain(this.CustomerID.SelectedValue, this.EmployeeID.SelectedValue,
                this.OrderDateFrom.Text, this.OrderDateTo.Text, this.ShippedStatus.SelectedValue);
            this.ExecuteSearch(1);
        }

        /// <summary>Page LoadのUOCメソッド（個別：Post Back）</summary>
        /// <remarks>実装必須</remarks>
        protected override void UOC_FormInit_PostBack()
        {
            // マスタ上のボタンの状態は ViewState で復元されるが、画面の初期処理の決めを毎回明示する
            this.SetFooterButtons();
        }

        #endregion

        #region イベント ハンドラ

        /// <summary>
        /// 受注の検索（EV-ORD-102）。マスタ上の btnMain1（検索）の Click
        /// </summary>
        /// <param name="fxEventArgs">Event Handlerの共通引数</param>
        /// <returns>URL</returns>
        protected string UOC_footerScreen_btnMain1_Click(FxEventArgs fxEventArgs)
        {
            this.ClearMessages();

            OrdTfReturnValue rv = this.CallSearch(
                this.CustomerID.SelectedValue, this.EmployeeID.SelectedValue,
                this.OrderDateFrom.Text, this.OrderDateTo.Text, this.ShippedStatus.SelectedValue, 1);

            if (rv.ErrorFlag)
            {
                // 検証エラー：入力値は入力のまま。一覧・総件数・ページ情報・保持した一覧状態は更新せず、直前のままにする（DEC-ORD-03）
                this.ShowError(rv);
                OrdTfReturnValue prev = this.CallSearch(
                    this.PCustomerID, this.PEmployeeID, this.POrderDateFrom, this.POrderDateTo, this.PShippedStatus, this.PPageNo);
                this.ShowList(prev);
            }
            else
            {
                // 成功：実行した検索の条件を保持する（EV-ORD-102 手順 5）
                this.Retain(this.CustomerID.SelectedValue, this.EmployeeID.SelectedValue,
                    this.OrderDateFrom.Text, this.OrderDateTo.Text, this.ShippedStatus.SelectedValue);
                this.ShowList(rv);
                this.RetainPage(rv);
            }

            return string.Empty;
        }

        /// <summary>先頭ページ（EV-ORD-104）</summary>
        /// <param name="fxEventArgs">Event Handlerの共通引数</param>
        /// <returns>URL</returns>
        protected string UOC_btnPageFirst_Click(FxEventArgs fxEventArgs) { return this.MovePage(1); }

        /// <summary>前ページ（EV-ORD-104）</summary>
        /// <param name="fxEventArgs">Event Handlerの共通引数</param>
        /// <returns>URL</returns>
        protected string UOC_btnPagePrev_Click(FxEventArgs fxEventArgs) { return this.MovePage(this.PPageNo - 1); }

        /// <summary>次ページ（EV-ORD-104）</summary>
        /// <param name="fxEventArgs">Event Handlerの共通引数</param>
        /// <returns>URL</returns>
        protected string UOC_btnPageNext_Click(FxEventArgs fxEventArgs) { return this.MovePage(this.PPageNo + 1); }

        /// <summary>末尾ページ（EV-ORD-104）</summary>
        /// <param name="fxEventArgs">Event Handlerの共通引数</param>
        /// <returns>URL</returns>
        protected string UOC_btnPageLast_Click(FxEventArgs fxEventArgs) { return this.MovePage(this.PTotalPages); }

        #endregion

        #region 部品

        /// <summary>一覧のページ切替（EV-ORD-104）</summary>
        /// <param name="target">移動先のページ番号</param>
        /// <returns>URL（遷移しないので空）</returns>
        private string MovePage(int target)
        {
            this.ClearMessages();

            // 移動先のページ番号を求める。1 未満または総ページ数を超える場合は移動しない（手順 1）
            if (target < 1 || target > this.PTotalPages) target = this.PPageNo;

            // 保持された（最後に実行した検索の）条件で検索する（DEC-ORD-04）。入力欄は入力のまま
            OrdTfReturnValue rv = this.CallSearch(
                this.PCustomerID, this.PEmployeeID, this.POrderDateFrom, this.POrderDateTo, this.PShippedStatus, target);
            this.ShowList(rv);
            this.RetainPage(rv);   // 取り直した総ページ数での最終ページを含む（DEC-ORD-06）

            return string.Empty;
        }

        /// <summary>フッタ部のメインボタンのキャプションと活性を決める（検索だけ活性。残りは非活性のまま）</summary>
        private void SetFooterButtons()
        {
            WebCustomButton main1 = (WebCustomButton)this.GetMasterWebControl("btnMain1");
            main1.Text = "検索";
            main1.Enabled = true;
            for (int i = 2; i <= 5; i++)
            {
                WebCustomButton b = (WebCustomButton)this.GetMasterWebControl("btnMain" + i);
                b.Enabled = false;
            }
        }

        /// <summary>選択肢を構築する（顧客・担当者は B層、出荷状況は固定値）</summary>
        private void BuildChoices()
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ContentPageFileNoEx, "-", "OrdTfInit", "SQL", this.UserInfo);
            OrdTfReturnValue rv = (OrdTfReturnValue)new OrdTfBusiness().DoBusinessLogic(pv, Iso);

            Bind(this.CustomerID, rv.CustomerOptions);
            Bind(this.EmployeeID, rv.EmployeeOptions);

            this.ShippedStatus.Items.Clear();
            foreach (string s in StatusValues)
            {
                this.ShippedStatus.Items.Add(new System.Web.UI.WebControls.ListItem(s, s));
            }
            this.ShippedStatus.SelectedValue = StatusValues[0];
        }

        /// <summary>B層の選択肢をドロップダウンへ（先頭は B層が置く空白の選択肢）</summary>
        private static void Bind(DropDownList ddl, List<OrdTfOption> options)
        {
            ddl.Items.Clear();
            foreach (OrdTfOption o in options)
            {
                ddl.Items.Add(new System.Web.UI.WebControls.ListItem(o.Text, o.Value));
            }
        }

        /// <summary>B層（OrdTfSearch）を呼ぶ</summary>
        private OrdTfReturnValue CallSearch(string customerId, string employeeId,
            string from, string to, string status, int pageNo)
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ContentPageFileNoEx, "-", "OrdTfSearch", "SQL", this.UserInfo);
            pv.CustomerID = customerId;
            int e;
            pv.EmployeeID = int.TryParse(employeeId, out e) ? (int?)e : null;
            pv.OrderDateFrom = from;
            pv.OrderDateTo = to;
            pv.ShippedStatus = status;
            pv.PageNo = pageNo;
            return (OrdTfReturnValue)new OrdTfBusiness().DoBusinessLogic(pv, Iso);
        }

        /// <summary>保持された条件で検索して表示する（初期表示用）</summary>
        private void ExecuteSearch(int pageNo)
        {
            OrdTfReturnValue rv = this.CallSearch(
                this.PCustomerID, this.PEmployeeID, this.POrderDateFrom, this.POrderDateTo, this.PShippedStatus, pageNo);
            this.ShowList(rv);
            this.RetainPage(rv);
        }

        /// <summary>検索条件を一覧状態として保持する</summary>
        private void Retain(string customerId, string employeeId, string from, string to, string status)
        {
            this.PCustomerID = customerId;
            this.PEmployeeID = employeeId;
            this.POrderDateFrom = from;
            this.POrderDateTo = to;
            this.PShippedStatus = status;
        }

        /// <summary>現在ページ・総ページ数を一覧状態として保持する</summary>
        private void RetainPage(OrdTfReturnValue rv)
        {
            this.PPageNo = rv.PageNo;
            this.PTotalPages = rv.TotalPages;
        }

        /// <summary>共通メッセージ領域・項目単位のエラーを空にする（今回の操作の知らせだけを出す）</summary>
        private void ClearMessages()
        {
            this.messageArea.Text = "";
            this.messageArea.CssClass = "";
            this.OrderDateFromError.Text = "";
            this.OrderDateToError.Text = "";
        }

        /// <summary>一覧・総件数・ページ情報・ページ移動の活性を表示する。0 件なら知らせ（情報）を出す</summary>
        private void ShowList(OrdTfReturnValue rv)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            List<OrdTfListRow> rows = new List<OrdTfListRow>();
            foreach (OrdTfRow r in rv.Rows)
            {
                OrdTfListRow v = new OrdTfListRow();
                v.OrderID = r.OrderID.ToString(inv);
                v.CustomerName = r.CustomerName ?? "";
                v.EmployeeName = r.EmployeeName ?? "";
                v.OrderDate = r.OrderDate.HasValue ? r.OrderDate.Value.ToString("yyyy/MM/dd", inv) : "";
                v.RequiredDate = r.RequiredDate.HasValue ? r.RequiredDate.Value.ToString("yyyy/MM/dd", inv) : "";
                v.ShippedDate = r.ShippedDate.HasValue ? r.ShippedDate.Value.ToString("yyyy/MM/dd", inv) : "";
                v.ShipperName = r.ShipperName ?? "";
                v.TotalAmount = r.TotalAmount.ToString("#,##0.00", inv);
                v.Status = r.Status;
                rows.Add(v);
            }

            this.orderList.DataSource = rows;
            this.orderList.DataBind();
            if (this.orderList.HeaderRow != null)
            {
                // 見出しの行を thead に出す（既定は thead を出さない）
                this.orderList.HeaderRow.TableSection = TableRowSection.TableHeader;
            }
            for (int i = 0; i < this.orderList.Rows.Count; i++)
            {
                // 納期遅延の行は強調（他の行は class を持たない）
                if (rows[i].Status == "納期遅延") this.orderList.Rows[i].CssClass = "table-danger";
            }

            this.totalCount.Text = "全 " + rv.TotalCount.ToString(inv) + " 件";
            this.pageInfo.Text = rv.PageNo.ToString(inv) + " / " + rv.TotalPages.ToString(inv) + " ページ";

            bool hasPrev = rv.PageNo > 1;
            bool hasNext = rv.PageNo < rv.TotalPages;
            this.btnPageFirst.Enabled = hasPrev;
            this.btnPagePrev.Enabled = hasPrev;
            this.btnPageNext.Enabled = hasNext;
            this.btnPageLast.Enabled = hasNext;

            if (!rv.ErrorFlag && rv.TotalCount == 0 && string.IsNullOrEmpty(this.messageArea.Text))
            {
                this.messageArea.Text = MsgNone;
                this.messageArea.CssClass = "text-info";
            }
        }

        /// <summary>検証エラーの表示：項目単位は該当項目の直下だけ、関連チェックは共通メッセージ領域だけ（DEC-CMN-04・05）</summary>
        private void ShowError(OrdTfReturnValue rv)
        {
            if (rv.ErrorFields != null && rv.ErrorFields.Count > 0)
            {
                if (rv.ErrorFields.Contains("OrderDateFrom")) this.OrderDateFromError.Text = rv.ErrorMessage;
                if (rv.ErrorFields.Contains("OrderDateTo")) this.OrderDateToError.Text = rv.ErrorMessage;
            }
            else
            {
                this.messageArea.Text = rv.ErrorMessage;
                this.messageArea.CssClass = "text-danger";
            }
        }

        #endregion
    }

    /// <summary>受注一覧の 1 行の表示用（GridView へバインドする。値は表示の書式の文字列）</summary>
    public class OrdTfListRow
    {
        /// <summary>受注 ID</summary>
        public string OrderID { get; set; }

        /// <summary>顧客名</summary>
        public string CustomerName { get; set; }

        /// <summary>担当者名</summary>
        public string EmployeeName { get; set; }

        /// <summary>受注日（yyyy/MM/dd）</summary>
        public string OrderDate { get; set; }

        /// <summary>要求納期（yyyy/MM/dd）</summary>
        public string RequiredDate { get; set; }

        /// <summary>出荷日（yyyy/MM/dd）</summary>
        public string ShippedDate { get; set; }

        /// <summary>配送業者</summary>
        public string ShipperName { get; set; }

        /// <summary>合計金額（#,##0.00）</summary>
        public string TotalAmount { get; set; }

        /// <summary>状況</summary>
        public string Status { get; set; }
    }
}
