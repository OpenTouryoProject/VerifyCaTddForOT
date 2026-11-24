//**********************************************************************************
//* サンプル アプリ・コントローラ
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfController
//* クラス日本語名  ：受注一覧（SC-ORD-01）のコントローラ
//*
//* 作成日時        ：2026/11/13
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/13  実装役            Red 用の空の土台（段 2。画面の要素は段 4 で実装する）
//*  2026/11/13  実装役            実装（段 4。EV-ORD-101・102・104）
//*  2026/11/15  実装役            EV-ORD-105（一覧の並べ替え）を追加（Sort アクション。並べ替えの列・方向を hidden で保持）
//*  2026/11/24  実装役            Red 用の空の土台（Detail・Back。段 2。EV-ORD-201・206 は段 4 で実装する）
//*  2026/11/24  実装役            実装（段 4。EV-ORD-201・203・206：Detail・Delete・Back。一覧状態は Session に保持）
//**********************************************************************************

using WSIFType_sample;
using WSServer_sample.Business;
using MVC_Sample.Models.ViewModels;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using Touryo.Infrastructure.Business.Presentation;
using Touryo.Infrastructure.Public.Db;

namespace MVC_Sample.Controllers
{
    /// <summary>
    /// 受注一覧（SC-ORD-01）のコントローラ
    /// 一覧状態（最後に実行した検索の条件・現在ページ）は hidden で保持する。
    /// </summary>
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class OrdTfController : MyBaseMVControllerCore
    {
        #region 定数

        /// <summary>0 件の知らせ（EV-ORD-101 手順 7。エラーとしない）</summary>
        private const string MsgNone = "該当するデータがありません。";

        /// <summary>並べ替えられる列のキー（SC-ORD-01-I06 の列の表の「並べ替え」が「可」の 7 列。B層の許可リストと同じ）</summary>
        private static readonly string[] SortableColumns =
            { "OrderID", "CustomerName", "EmployeeName", "OrderDate", "RequiredDate", "ShippedDate", "ShipperName" };

        /// <summary>出荷状況の選択肢（固定値。SC-ORD-01-I05）</summary>
        private static readonly string[] StatusValues = { "すべて", "未出荷", "出荷済", "納期遅延" };

        /// <summary>B層呼び出しの分離レベル（読み取りだけ。B層のテストと同じ値）</summary>
        private const DbEnum.IsolationLevelEnum Iso = DbEnum.IsolationLevelEnum.ReadCommitted;

        #endregion

        #region 画面（Index・Search・Move）

        /// <summary>
        /// 受注一覧のページ読み込み（EV-ORD-101）
        /// GET: /OrdTf
        /// </summary>
        /// <returns>初期表示状態の画面（IActionResult）</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            OrdTfViewModel model = new OrdTfViewModel();
            model.ShippedStatus = StatusValues[0];
            await this.BuildChoices(model);

            // 検索条件は未指定、ページは 1（保持された一覧状態の復元は範囲外）
            this.Retain(model, model.CustomerID, model.EmployeeID, model.OrderDateFrom, model.OrderDateTo, model.ShippedStatus);
            await this.ExecuteSearch(model, 1);
            this.SaveState(model);   // 既定の一覧状態（直接開いたときは保持した状態を使わない。DEC-ORD-20）

            return this.Display(model);
        }

        /// <summary>
        /// 受注の検索（EV-ORD-102）
        /// POST: /OrdTf/Search
        /// </summary>
        /// <param name="model">ビュー モデル</param>
        /// <returns>再描画（IActionResult）</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(OrdTfViewModel model)
        {
            await this.BuildChoices(model);

            OrdTfReturnValue rv = await this.CallSearch(
                model.CustomerID, model.EmployeeID, model.OrderDateFrom, model.OrderDateTo, model.ShippedStatus, 1,
                model.PSortColumn, model.PSortDescending);

            if (rv.ErrorFlag)
            {
                // 検証エラー：入力値は入力のまま。一覧・総件数・ページ情報・保持した一覧状態は更新せず、直前のままにする（DEC-ORD-03）
                this.ShowError(model, rv);
                OrdTfReturnValue prev = await this.CallSearch(
                    model.PCustomerID, model.PEmployeeID, model.POrderDateFrom, model.POrderDateTo, model.PShippedStatus, model.PPageNo,
                    model.PSortColumn, model.PSortDescending);
                this.ShowList(model, prev);
            }
            else
            {
                // 成功：実行した検索の条件を保持する（EV-ORD-102 手順 5）
                this.Retain(model, model.CustomerID, model.EmployeeID, model.OrderDateFrom, model.OrderDateTo, model.ShippedStatus);
                this.ShowList(model, rv);
                this.RetainPage(model, rv);
                this.SaveState(model);
            }

            return this.Display(model);
        }

        /// <summary>
        /// 一覧のページ切替（EV-ORD-104）
        /// POST: /OrdTf/Move
        /// </summary>
        /// <param name="model">ビュー モデル</param>
        /// <param name="dir">移動方向（first・prev・next・last）</param>
        /// <returns>再描画（IActionResult）</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(OrdTfViewModel model, string dir)
        {
            await this.BuildChoices(model);

            // 移動先のページ番号を求める。1 未満または総ページ数を超える場合は移動しない（手順 1）
            int target;
            switch (dir)
            {
                case "first": target = 1; break;
                case "prev": target = model.PPageNo - 1; break;
                case "next": target = model.PPageNo + 1; break;
                case "last": target = model.PTotalPages; break;
                default: target = model.PPageNo; break;
            }
            if (target < 1 || target > model.PTotalPages) target = model.PPageNo;

            // 保持された（最後に実行した検索の）条件で検索する（DEC-ORD-04）。入力欄は入力のまま
            OrdTfReturnValue rv = await this.CallSearch(
                model.PCustomerID, model.PEmployeeID, model.POrderDateFrom, model.POrderDateTo, model.PShippedStatus, target,
                model.PSortColumn, model.PSortDescending);
            this.ShowList(model, rv);
            this.RetainPage(model, rv);   // 取り直した総ページ数での最終ページを含む（DEC-ORD-06）
            this.SaveState(model);

            return this.Display(model);
        }

        /// <summary>
        /// 一覧の並べ替え（EV-ORD-105）
        /// POST: /OrdTf/Sort
        /// </summary>
        /// <param name="model">ビュー モデル</param>
        /// <param name="sort">選ばれた列のキー（並べ替え可の 7 列。見出しのボタンの value）</param>
        /// <returns>再描画（IActionResult）</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sort(OrdTfViewModel model, string sort)
        {
            await this.BuildChoices(model);

            if (Array.IndexOf(SortableColumns, sort) >= 0)
            {
                // 現在の並べ替え列と同じなら方向を反転、異なれば当該列の昇順（手順 1）。ページは 1 に戻す（手順 3）
                if (sort == model.PSortColumn)
                {
                    model.PSortDescending = !model.PSortDescending;
                }
                else
                {
                    model.PSortColumn = sort;
                    model.PSortDescending = false;
                }
                model.PPageNo = 1;
            }
            // 並べ替え対象外の列（合計金額・状況は見出しにボタンが無く、ここへは来ない）は処理しない：
            // 保持された状態のまま一覧を取り直す（手順 2）

            // 保持された検索条件（最後に実行した検索の条件）と新しい並べ替えで検索し直す（手順 4。入力欄は入力のまま）
            OrdTfReturnValue rv = await this.CallSearch(
                model.PCustomerID, model.PEmployeeID, model.POrderDateFrom, model.POrderDateTo, model.PShippedStatus, model.PPageNo,
                model.PSortColumn, model.PSortDescending);
            this.ShowList(model, rv);
            this.RetainPage(model, rv);
            this.SaveState(model);

            return this.Display(model);
        }

        #endregion

        #region 部品

        /// <summary>選択肢を構築する（顧客・担当者は B層、出荷状況は固定値）</summary>
        private async Task BuildChoices(OrdTfViewModel model)
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfInit", "SQL", this.UserInfo);
            OrdTfReturnValue rv = (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(pv, Iso);

            model.CustomerItems = ToItems(rv.CustomerOptions);
            model.EmployeeItems = ToItems(rv.EmployeeOptions);

            model.StatusItems = new List<SelectListItem>();
            foreach (string s in StatusValues)
            {
                model.StatusItems.Add(new SelectListItem { Value = s, Text = s });
            }
        }

        /// <summary>B層の選択肢を SelectListItem へ</summary>
        private static List<SelectListItem> ToItems(List<OrdTfOption> options)
        {
            List<SelectListItem> items = new List<SelectListItem>();
            foreach (OrdTfOption o in options)
            {
                items.Add(new SelectListItem { Value = o.Value, Text = o.Text });
            }
            return items;
        }

        /// <summary>B層（OrdTfSearch）を呼ぶ</summary>
        private async Task<OrdTfReturnValue> CallSearch(string customerId, string employeeId,
            string from, string to, string status, int pageNo, string sortColumn, bool sortDescending)
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfSearch", "SQL", this.UserInfo);
            pv.CustomerID = customerId;
            int e;
            pv.EmployeeID = int.TryParse(employeeId, out e) ? (int?)e : null;
            pv.OrderDateFrom = from;
            pv.OrderDateTo = to;
            pv.ShippedStatus = status;
            pv.PageNo = pageNo;
            pv.SortColumn = sortColumn;
            pv.SortDescending = sortDescending;
            return (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(pv, Iso);
        }

        /// <summary>保持された条件で検索して表示する（初期表示用）</summary>
        private async Task ExecuteSearch(OrdTfViewModel model, int pageNo)
        {
            OrdTfReturnValue rv = await this.CallSearch(
                model.PCustomerID, model.PEmployeeID, model.POrderDateFrom, model.POrderDateTo, model.PShippedStatus, pageNo,
                model.PSortColumn, model.PSortDescending);
            this.ShowList(model, rv);
            this.RetainPage(model, rv);
        }

        /// <summary>検索条件を一覧状態として保持する</summary>
        private void Retain(OrdTfViewModel model, string customerId, string employeeId, string from, string to, string status)
        {
            model.PCustomerID = customerId;
            model.PEmployeeID = employeeId;
            model.POrderDateFrom = from;
            model.POrderDateTo = to;
            model.PShippedStatus = status;
        }

        /// <summary>現在ページ・総ページ数を一覧状態として保持する</summary>
        private void RetainPage(OrdTfViewModel model, OrdTfReturnValue rv)
        {
            model.PPageNo = rv.PageNo;
            model.PTotalPages = rv.TotalPages;
        }

        /// <summary>一覧・総件数・ページ情報を表示する。0 件なら知らせ（情報）を出す</summary>
        private void ShowList(OrdTfViewModel model, OrdTfReturnValue rv)
        {
            model.Rows = rv.Rows;
            model.TotalCount = rv.TotalCount;
            model.PageNo = rv.PageNo;
            model.TotalPages = rv.TotalPages;

            if (!rv.ErrorFlag && rv.TotalCount == 0 && model.Message == null)
            {
                model.Message = MsgNone;
                model.MessageIsError = false;
            }
        }

        /// <summary>検証エラーの表示：項目単位は該当項目の直下だけ、関連チェックは共通メッセージ領域だけ（DEC-CMN-04・05）</summary>
        private void ShowError(OrdTfViewModel model, OrdTfReturnValue rv)
        {
            if (rv.ErrorFields != null && rv.ErrorFields.Count > 0)
            {
                if (rv.ErrorFields.Contains("OrderDateFrom")) model.ErrorFrom = rv.ErrorMessage;
                if (rv.ErrorFields.Contains("OrderDateTo")) model.ErrorTo = rv.ErrorMessage;
            }
            else
            {
                model.Message = rv.ErrorMessage;
                model.MessageIsError = true;
            }
        }

        /// <summary>再描画。POST の入力値が ModelState に残って hidden・入力欄を上書きしないよう、ModelState を消す</summary>
        private IActionResult Display(OrdTfViewModel model)
        {
            ModelState.Clear();
            return View("Index", model);
        }

        #endregion

        #region 受注詳細・一覧への復帰（Detail・Delete・Back）

        /// <summary>Session のキー：一覧状態（OrdTfListState の JSON）</summary>
        private const string StateKey = "OrdTfListState";

        /// <summary>TempData のキー：一覧へ戻ったときの共通メッセージ領域の文言・種類</summary>
        private const string TempMessage = "OrdTfMessage";
        private const string TempIsError = "OrdTfMessageIsError";

        /// <summary>
        /// 受注詳細（照会）のページ読み込み（EV-ORD-201）
        /// GET: /OrdTf/Detail?id=
        /// </summary>
        /// <param name="id">受注 ID</param>
        /// <returns>画面（IActionResult）。受注が無ければ一覧へ戻る（ERR-NOTFOUND）</returns>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            OrdTfReturnValue rv = await this.CallDetail(id);
            if (rv.ErrorFlag)
            {
                return this.BackWithMessage(rv.ErrorMessage, true);
            }
            return View("Detail", new OrdTfDetailViewModel { Data = rv });
        }

        /// <summary>
        /// 受注の削除（EV-ORD-203。確認ダイアログはビューの window.confirm）
        /// POST: /OrdTf/Delete
        /// </summary>
        /// <param name="id">受注 ID</param>
        /// <param name="rowVersion">画面が保持していた Orders.RowVersion</param>
        /// <returns>一覧（完了・ERR-NOTFOUND）または詳細の再表示（ERR-CONFLICT）</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int rowVersion)
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfDelete", "SQL", this.UserInfo);
            pv.OrderID = id;
            pv.RowVersion = rowVersion;
            OrdTfReturnValue rv = (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(pv, Iso);

            if (!rv.ErrorFlag)
            {
                // 手順 6：完了メッセージ（情報）を出し、保持された一覧状態で一覧へ（一覧は再検索する）
                return this.BackWithMessage("受注 ID " + id + " を削除しました。", false);
            }

            // 失敗：最新を読み直す。受注が無ければ ERR-NOTFOUND（一覧へ）、あれば ERR-CONFLICT（最新を再表示）
            OrdTfReturnValue latest = await this.CallDetail(id);
            if (latest.ErrorFlag)
            {
                return this.BackWithMessage(latest.ErrorMessage, true);
            }
            return View("Detail", new OrdTfDetailViewModel { Data = latest, Message = rv.ErrorMessage, MessageIsError = true });
        }

        /// <summary>
        /// 一覧への復帰（EV-ORD-206・EV-ORD-101 手順 3）：保持された一覧状態を復元して再検索する。無ければ既定の一覧
        /// GET: /OrdTf/Back
        /// </summary>
        /// <returns>一覧（IActionResult）</returns>
        [HttpGet]
        public async Task<IActionResult> Back()
        {
            OrdTfViewModel model = new OrdTfViewModel();
            await this.BuildChoices(model);

            OrdTfListState st = this.LoadState();
            if (st == null)
            {
                st = new OrdTfListState { ShippedStatus = StatusValues[0], PageNo = 1 };
            }

            // 入力欄と保持する状態を、保持された一覧状態で復元する（最後に実行した検索の条件。DEC-ORD-19）
            model.CustomerID = st.CustomerID;
            model.EmployeeID = st.EmployeeID;
            model.OrderDateFrom = st.OrderDateFrom;
            model.OrderDateTo = st.OrderDateTo;
            model.ShippedStatus = st.ShippedStatus;
            this.Retain(model, st.CustomerID, st.EmployeeID, st.OrderDateFrom, st.OrderDateTo, st.ShippedStatus);
            model.PSortColumn = st.SortColumn;
            model.PSortDescending = st.SortDescending;

            // 戻ったときのメッセージ（削除の完了・ERR-NOTFOUND）
            string msg = TempData[TempMessage] as string;
            if (msg != null)
            {
                model.Message = msg;
                model.MessageIsError = TempData[TempIsError] is bool b && b;
            }

            // 再検索する（復元するページが無くなっていたら B層が最終ページにする。DEC-ORD-23）
            await this.ExecuteSearch(model, st.PageNo < 1 ? 1 : st.PageNo);
            this.SaveState(model);

            return this.Display(model);
        }

        /// <summary>
        /// 受注登録・編集（SC-ORD-03）への遷移先（EV-ORD-107）。この回は画面が無く、URL までを作る（次の回で実装する）。
        /// 一覧状態は一覧の操作のたびに Session へ保持済み
        /// GET: /OrdTf/Edit?mode=new
        /// </summary>
        /// <param name="mode">モード（new = 登録）</param>
        /// <returns>未実装の知らせ（404。本文を持たせる＝ブラウザのエラー画面にしない）</returns>
        [HttpGet]
        public IActionResult Edit(string mode)
        {
            return StatusCode(404, "受注の登録・編集（SC-ORD-03）は、次の回で作成します。");
        }

        /// <summary>B層（OrdTfDetail）を呼ぶ</summary>
        private async Task<OrdTfReturnValue> CallDetail(int id)
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfDetail", "SQL", this.UserInfo);
            pv.OrderID = id;
            return (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(pv, Iso);
        }

        /// <summary>メッセージを持たせて、一覧への復帰（/OrdTf/Back）へ移る</summary>
        private IActionResult BackWithMessage(string message, bool isError)
        {
            TempData[TempMessage] = message;
            TempData[TempIsError] = isError;
            return RedirectToAction("Back");
        }

        /// <summary>一覧状態（最後に実行した検索の条件・現在ページ・並べ替え）を Session に保持する</summary>
        private void SaveState(OrdTfViewModel model)
        {
            OrdTfListState st = new OrdTfListState
            {
                CustomerID = model.PCustomerID,
                EmployeeID = model.PEmployeeID,
                OrderDateFrom = model.POrderDateFrom,
                OrderDateTo = model.POrderDateTo,
                ShippedStatus = model.PShippedStatus,
                SortColumn = model.PSortColumn,
                SortDescending = model.PSortDescending,
                PageNo = model.PPageNo
            };
            HttpContext.Session.SetString(StateKey, JsonSerializer.Serialize(st));
        }

        /// <summary>保持された一覧状態を読む（無ければ null）</summary>
        private OrdTfListState LoadState()
        {
            string json = HttpContext.Session.GetString(StateKey);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<OrdTfListState>(json);
        }

        #endregion
    }
}
