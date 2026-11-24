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
//*  2026/11/24  実装役            Red 用の空の土台（Edit。EV-ORD-301 は段 4 で実装する）
//*  2026/11/24  実装役            実装（段 4。EV-ORD-202・301〜309：Edit・Save・EditStart・CustomerInfo・ProductInfo・Missing。保存は PRG）
//**********************************************************************************

using WSIFType_sample;
using WSServer_sample.Business;
using MVC_Sample.Models.ViewModels;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            // 保存の完了・出荷済で戻ってきたときのメッセージ（PRG。EV-ORD-308 手順 6・EV-ORD-301 手順 3-2）
            OrdTfDetailViewModel dm = new OrdTfDetailViewModel { Data = rv };
            string dmsg = TempData[TempDetailMessage] as string;
            if (dmsg != null)
            {
                dm.Message = dmsg;
                dm.MessageIsError = TempData[TempDetailIsError] is bool db && db;
            }
            return View("Detail", dm);
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

        #region 受注詳細（登録・編集。Edit・Save・EditStart・CustomerInfo・ProductInfo・Missing）

        /// <summary>TempData のキー：編集の画面へ戻ったときの共通メッセージ領域の文言・種類（ERR-CONFLICT の再表示）</summary>
        private const string TempEditMessage = "OrdTfEditMessage";
        private const string TempEditIsError = "OrdTfEditMessageIsError";

        /// <summary>TempData のキー：詳細（照会）へ戻ったときの共通メッセージ領域の文言・種類（保存の完了・出荷済）</summary>
        private const string TempDetailMessage = "OrdTfDetailMessage";
        private const string TempDetailIsError = "OrdTfDetailMessageIsError";

        /// <summary>出荷済は編集できない（詳細の「編集」。EV-ORD-202 手順 1）</summary>
        private const string MsgShippedFromDetail = "出荷済の受注は編集できません。出荷を取り消してから編集してください。";

        /// <summary>
        /// 受注詳細（登録・編集）のページ読み込み（EV-ORD-301）
        /// GET: /OrdTf/Edit?mode=new または /OrdTf/Edit?mode=edit&amp;id=受注 ID
        /// </summary>
        /// <param name="mode">モード（new = 登録、edit = 編集。それ以外は登録）</param>
        /// <param name="id">受注 ID（編集のみ）</param>
        /// <returns>画面（IActionResult）。受注が無ければ一覧へ、出荷済なら詳細へ戻る</returns>
        [HttpGet]
        public async Task<IActionResult> Edit(string mode, int? id)
        {
            bool isEdit = mode == "edit";
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfEditInit", "SQL", this.UserInfo);
            pv.Mode = isEdit ? "edit" : "new";
            pv.OrderID = id ?? 0;
            OrdTfReturnValue rv = (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(pv, Iso);

            if (rv.ErrorFlag)
            {
                if (rv.ErrorMessageID == OrdTfBusiness.MsgIdShipped)
                {
                    // 出荷済は編集させず、詳細（照会）へ戻す（手順 3-2。ERR-BIZ）
                    TempData[TempDetailMessage] = rv.ErrorMessage;
                    TempData[TempDetailIsError] = true;
                    return this.RedirectToDetail(id ?? 0);
                }
                return this.BackWithMessage(rv.ErrorMessage, true);   // ERR-NOTFOUND（一覧状態は保持された状態。DEC-ORD-28）
            }

            OrdTfEditViewModel model = this.BuildEditModel(rv, isEdit);
            string msg = TempData[TempEditMessage] as string;
            if (msg != null)
            {
                model.Message = msg;
                model.MessageIsError = TempData[TempEditIsError] is bool b && b;
            }
            return this.DisplayEdit(model);
        }

        /// <summary>
        /// 受注の保存（EV-ORD-308）。成功・ERR-CONFLICT・ERR-NOTFOUND は PRG（リダイレクト）にし、
        /// 検証エラーだけは入力値を保持して POST の応答で再描画する
        /// POST: /OrdTf/Save
        /// </summary>
        /// <returns>詳細への遷移・編集の再表示・一覧への遷移・検証エラーの再描画</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save()
        {
            OrdTfEditViewModel model = this.ReadEditForm();
            bool isEdit = model.Mode == "edit";

            OrdTfParameterValue pv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfSave", "SQL", this.UserInfo);
            pv.Mode = model.Mode;
            pv.OrderID = model.OrderID;
            pv.RowVersion = model.RowVersion;
            pv.CustomerID = model.CustomerID;
            pv.EmployeeIDText = model.EmployeeID;
            pv.OrderDateText = model.OrderDate;
            pv.RequiredDateText = model.RequiredDate;
            pv.ShipViaText = model.ShipVia;
            pv.FreightText = model.Freight;
            pv.ShipName = model.ShipName;
            pv.ShipAddress = model.ShipAddress;
            pv.ShipCity = model.ShipCity;
            pv.ShipRegion = model.ShipRegion;
            pv.ShipPostalCode = model.ShipPostalCode;
            pv.ShipCountry = model.ShipCountry;
            foreach (OrdTfEditRow r in model.Lines)
            {
                OrdTfSaveLine l = new OrdTfSaveLine();
                l.ProductIDText = r.ProductID;
                l.UnitPriceText = r.UnitPrice;
                l.DiscountText = r.Discount;
                l.QuantityText = r.Quantity;
                int op; decimal ou, od; int oq;
                l.OriginalProductID = int.TryParse(r.OrigProductID, out op) ? (int?)op : null;
                l.OriginalUnitPrice = decimal.TryParse(r.OrigUnitPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out ou) ? (decimal?)ou : null;
                l.OriginalDiscount = decimal.TryParse(r.OrigDiscount, NumberStyles.Number, CultureInfo.InvariantCulture, out od) ? (decimal?)od : null;
                l.OriginalQuantity = int.TryParse(r.OrigQuantity, out oq) ? (int?)oq : null;
                pv.SaveLines.Add(l);
            }
            foreach (string d in model.Deleted)
            {
                int p;
                if (int.TryParse(d, out p)) pv.DeletedProductIDs.Add(p);
            }
            OrdTfReturnValue rv = (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(pv, Iso);

            if (!rv.ErrorFlag)
            {
                // 手順 6：完了メッセージ（情報。DEC-ORD-35）を出し、受注 ID を引き渡して詳細（照会）へ遷移する（PRG）
                TempData[TempDetailMessage] = "受注 ID " + rv.OrderID + " を保存しました。";
                TempData[TempDetailIsError] = false;
                return this.RedirectToDetail(rv.OrderID);
            }

            if (rv.ErrorMessageID == OrdTfBusiness.MsgIdConflict)
            {
                // ERR-CONFLICT：更新を取り消し、入力を捨てて DB の最新を再表示する（DEC-ORD-34）。編集の画面を読み直す（PRG）
                TempData[TempEditMessage] = rv.ErrorMessage;
                TempData[TempEditIsError] = true;
                return this.RedirectToEdit(model.OrderID);
            }
            if (rv.ErrorMessageID == OrdTfBusiness.MsgIdNotFound)
            {
                return this.BackWithMessage(rv.ErrorMessage, true);   // ERR-NOTFOUND：保持された一覧状態の一覧へ（DEC-ORD-28）
            }

            // ERR-VALIDATION：更新せず、入力値を保持して画面に留まる。項目単位は項目の直下、関連チェック・個別は共通メッセージ領域（DEC-CMN-04・05）
            OrdTfParameterValue ipv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfEditInit", "SQL", this.UserInfo);
            ipv.Mode = "new";   // 選択肢だけを取る
            OrdTfReturnValue init = (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(ipv, Iso);
            model.Customers = init.CustomerOptions;
            model.Employees = init.EmployeeOptions;
            model.Shippers = init.ShipperOptions;
            model.Products = init.ProductOptions;
            for (int i = 0; i < rv.ErrorFields.Count; i++)
            {
                model.Errors[rv.ErrorFields[i]] = i < rv.ErrorFieldMessages.Count ? rv.ErrorFieldMessages[i] : "入力内容を確認してください。";
            }
            if (rv.ErrorMessages.Count > 0)
            {
                model.Message = string.Join(" ", rv.ErrorMessages);
                model.MessageIsError = true;
            }
            await this.FillCustomerInfo(model);
            return this.DisplayEdit(model);
        }

        /// <summary>
        /// 詳細（照会）の「編集」（EV-ORD-202）：出荷日を読み直して判定し、編集の画面へ遷移する。出荷済なら遷移せず詳細へ戻してエラーを出す
        /// GET: /OrdTf/EditStart?id=受注 ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditStart(int id)
        {
            OrdTfReturnValue rv = await this.CallDetail(id);
            if (rv.ErrorFlag)
            {
                return this.BackWithMessage(rv.ErrorMessage, true);   // ERR-NOTFOUND（PRE-EXIST）
            }
            if (!rv.CanEdit)
            {
                TempData[TempDetailMessage] = MsgShippedFromDetail;   // 手順 1（ORD-T4。ERR-BIZ）
                TempData[TempDetailIsError] = true;
                return this.RedirectToDetail(id);
            }
            return this.RedirectToEdit(id);   // 手順 2
        }

        /// <summary>
        /// 顧客選択の変更（EV-ORD-302 手順 2〜4）：顧客情報と出荷先の既定値を JSON で返す。無ければ ok = false
        /// GET: /OrdTf/CustomerInfo?id=顧客 ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CustomerInfo(string id)
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfCustomer", "SQL", this.UserInfo);
            pv.CustomerID = id;
            OrdTfReturnValue rv = (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(pv, Iso);
            if (rv.ErrorFlag) return Json(new { ok = false });
            return Json(new
            {
                ok = true,
                phone = rv.CustomerPhone,
                address = rv.CustomerAddress,
                shipName = rv.ShipName,
                shipAddress = rv.ShipAddress,
                shipCity = rv.ShipCity,
                shipRegion = rv.ShipRegion,
                shipPostalCode = rv.ShipPostalCode,
                shipCountry = rv.ShipCountry
            });
        }

        /// <summary>
        /// 明細の商品選択の変更（EV-ORD-306 手順 3・4）：単価・在庫を JSON で返す。無ければ ok = false
        /// GET: /OrdTf/ProductInfo?id=商品 ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ProductInfo(int id)
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfProduct", "SQL", this.UserInfo);
            pv.ProductID = id;
            OrdTfReturnValue rv = (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(pv, Iso);
            if (rv.ErrorFlag) return Json(new { ok = false });
            return Json(new
            {
                ok = true,
                unitPrice = rv.UnitPrice.HasValue ? rv.UnitPrice.Value.ToString("0.00", CultureInfo.InvariantCulture) : "",
                stock = rv.UnitsInStock.HasValue ? rv.UnitsInStock.Value.ToString(CultureInfo.InvariantCulture) : ""
            });
        }

        /// <summary>
        /// 入力画面の中のイベント（顧客選択・商品選択）で ERR-NOTFOUND になったとき（DEC-ORD-40）：保持された一覧状態の一覧へ戻る
        /// GET: /OrdTf/Missing
        /// </summary>
        [HttpGet]
        public IActionResult Missing()
        {
            return this.BackWithMessage(OrdTfBusiness.MsgNotFound, true);
        }

        /// <summary>B層（OrdTfEditInit）の戻り値から、画面のビュー モデルを作る</summary>
        private OrdTfEditViewModel BuildEditModel(OrdTfReturnValue rv, bool isEdit)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            OrdTfEditViewModel m = new OrdTfEditViewModel();
            m.Mode = isEdit ? "edit" : "new";
            m.Customers = rv.CustomerOptions;
            m.Employees = rv.EmployeeOptions;
            m.Shippers = rv.ShipperOptions;
            m.Products = rv.ProductOptions;

            if (!isEdit)
            {
                // 登録モード（手順 2）：受注日は当日、送料は 0、明細は空行 1 行（割引率 0・数量 1。DEC-ORD-39）。その他は未入力
                m.OrderDate = DateTime.Today.ToString("yyyy/MM/dd", inv);
                m.Freight = "0";
                m.CustomerID = m.EmployeeID = m.RequiredDate = m.ShipVia = "";
                m.ShipName = m.ShipAddress = m.ShipCity = m.ShipRegion = m.ShipPostalCode = m.ShipCountry = "";
                m.CustomerPhone = m.CustomerAddress = "";
                m.Lines.Add(new OrdTfEditRow());
                return m;
            }

            // 編集モード（手順 3）
            m.OrderID = rv.OrderID;
            m.RowVersion = rv.RowVersion;
            m.CustomerID = rv.EditCustomerID ?? "";
            m.EmployeeID = rv.EditEmployeeID.HasValue ? rv.EditEmployeeID.Value.ToString(inv) : "";
            m.OrderDate = rv.OrderDate.HasValue ? rv.OrderDate.Value.ToString("yyyy/MM/dd", inv) : "";
            m.RequiredDate = rv.RequiredDate.HasValue ? rv.RequiredDate.Value.ToString("yyyy/MM/dd", inv) : "";
            m.ShipVia = rv.EditShipVia.HasValue ? rv.EditShipVia.Value.ToString(inv) : "";
            m.Freight = rv.Freight.HasValue ? rv.Freight.Value.ToString("0.00", inv) : "";
            m.ShipName = rv.ShipName;
            m.ShipAddress = rv.ShipAddress;
            m.ShipCity = rv.ShipCity;
            m.ShipRegion = rv.ShipRegion;
            m.ShipPostalCode = rv.ShipPostalCode;
            m.ShipCountry = rv.ShipCountry;
            m.CustomerPhone = rv.CustomerPhone;
            m.CustomerAddress = rv.CustomerAddress;
            foreach (OrdTfEditLine l in rv.EditLines)
            {
                OrdTfEditRow r = new OrdTfEditRow();
                r.ProductID = l.ProductID.ToString(inv);
                r.UnitPrice = l.UnitPrice.ToString("0.00", inv);
                r.Discount = l.Discount.ToString("0.####", inv);
                r.Quantity = l.Quantity.ToString(inv);
                r.Stock = l.UnitsInStock.HasValue ? l.UnitsInStock.Value.ToString(inv) : "";
                r.OrigProductID = r.ProductID;
                r.OrigUnitPrice = r.UnitPrice;
                r.OrigDiscount = r.Discount;
                r.OrigQuantity = r.Quantity;
                r.OrigName = l.ProductName;
                m.Lines.Add(r);
            }
            if (m.Lines.Count == 0) m.Lines.Add(new OrdTfEditRow());   // 明細が無い受注（DB は妥当性を保証しない）：空行 1 行
            return m;
        }

        /// <summary>POST されたフォームからビュー モデルを組み立てる（明細は同じ名前の入力が行の順に並ぶ）</summary>
        private OrdTfEditViewModel ReadEditForm()
        {
            IFormCollection f = Request.Form;
            Func<string, string> g = k => f.ContainsKey(k) ? (f[k].FirstOrDefault() ?? "") : "";
            OrdTfEditViewModel m = new OrdTfEditViewModel();
            m.Mode = g("Mode") == "edit" ? "edit" : "new";
            int oid, rver;
            m.OrderID = int.TryParse(g("OrderID"), out oid) ? oid : 0;
            m.RowVersion = int.TryParse(g("RowVersion"), out rver) ? rver : 0;
            m.CustomerID = g("CustomerID");
            m.EmployeeID = g("EmployeeID");
            m.OrderDate = g("OrderDate");
            m.RequiredDate = g("RequiredDate");
            m.ShipVia = g("ShipVia");
            m.Freight = g("Freight");
            m.ShipName = g("ShipName");
            m.ShipAddress = g("ShipAddress");
            m.ShipCity = g("ShipCity");
            m.ShipRegion = g("ShipRegion");
            m.ShipPostalCode = g("ShipPostalCode");
            m.ShipCountry = g("ShipCountry");
            m.InitialSnapshot = g("InitialSnapshot");

            string[] products = f["line_product"].ToArray();
            Func<string, int, string> at = (k, i) => { string[] a = f[k].ToArray(); return i < a.Length ? a[i] : ""; };
            for (int i = 0; i < products.Length; i++)
            {
                OrdTfEditRow r = new OrdTfEditRow();
                r.ProductID = products[i];
                r.UnitPrice = at("line_price", i);
                r.Discount = at("line_discount", i);
                r.Quantity = at("line_qty", i);
                r.Stock = at("line_stock", i);
                r.OrigProductID = at("line_origProduct", i);
                r.OrigUnitPrice = at("line_origPrice", i);
                r.OrigDiscount = at("line_origDiscount", i);
                r.OrigQuantity = at("line_origQty", i);
                r.OrigName = at("line_origName", i);
                m.Lines.Add(r);
            }
            m.Deleted = f["deleted"].ToList();
            return m;
        }

        /// <summary>
        /// 検証エラーの再描画で、選ばれている顧客の電話・住所（表示のみで POST されない）を B層（OrdTfCustomer）で読み直す。
        /// 登録モードでは、その顧客の既定の出荷先を画面の「自動で入れた値」の初期値として渡す（描き直しの後も、自動で入った出荷先を変えていなければ上書きの確認が出ない。DEC-ORD-32）
        /// </summary>
        private async Task FillCustomerInfo(OrdTfEditViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerID)) return;
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfCustomer", "SQL", this.UserInfo);
            pv.CustomerID = model.CustomerID;
            OrdTfReturnValue rv = (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(pv, Iso);
            if (rv.ErrorFlag) return;
            model.CustomerPhone = rv.CustomerPhone;
            model.CustomerAddress = rv.CustomerAddress;
            if (model.Mode != "edit")
            {
                model.AutoShip["ShipName"] = rv.ShipName;
                model.AutoShip["ShipAddress"] = rv.ShipAddress;
                model.AutoShip["ShipCity"] = rv.ShipCity;
                model.AutoShip["ShipRegion"] = rv.ShipRegion;
                model.AutoShip["ShipPostalCode"] = rv.ShipPostalCode;
                model.AutoShip["ShipCountry"] = rv.ShipCountry;
            }
        }

        /// <summary>詳細（照会）へ移る（URL の約束：/OrdTf/Detail?id=受注 ID）</summary>
        private IActionResult RedirectToDetail(int id)
        {
            return LocalRedirect("~/OrdTf/Detail?id=" + id);
        }

        /// <summary>編集の画面へ移る（URL の約束：/OrdTf/Edit?mode=edit&amp;id=受注 ID）</summary>
        private IActionResult RedirectToEdit(int id)
        {
            return LocalRedirect("~/OrdTf/Edit?mode=edit&id=" + id);
        }

        /// <summary>再描画。POST の入力値が ModelState に残らないよう、ModelState を消す</summary>
        private IActionResult DisplayEdit(OrdTfEditViewModel model)
        {
            ModelState.Clear();
            return View("Edit", model);
        }

        #endregion

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
