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
//**********************************************************************************

using WSIFType_sample;
using WSServer_sample.Business;
using MVC_Sample.Models.ViewModels;

using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
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
                model.CustomerID, model.EmployeeID, model.OrderDateFrom, model.OrderDateTo, model.ShippedStatus, 1);

            if (rv.ErrorFlag)
            {
                // 検証エラー：入力値は入力のまま。一覧・総件数・ページ情報・保持した一覧状態は更新せず、直前のままにする（DEC-ORD-03）
                this.ShowError(model, rv);
                OrdTfReturnValue prev = await this.CallSearch(
                    model.PCustomerID, model.PEmployeeID, model.POrderDateFrom, model.POrderDateTo, model.PShippedStatus, model.PPageNo);
                this.ShowList(model, prev);
            }
            else
            {
                // 成功：実行した検索の条件を保持する（EV-ORD-102 手順 5）
                this.Retain(model, model.CustomerID, model.EmployeeID, model.OrderDateFrom, model.OrderDateTo, model.ShippedStatus);
                this.ShowList(model, rv);
                this.RetainPage(model, rv);
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
                model.PCustomerID, model.PEmployeeID, model.POrderDateFrom, model.POrderDateTo, model.PShippedStatus, target);
            this.ShowList(model, rv);
            this.RetainPage(model, rv);   // 取り直した総ページ数での最終ページを含む（DEC-ORD-06）

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
            string from, string to, string status, int pageNo)
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(this.ControllerName, "-", "OrdTfSearch", "SQL", this.UserInfo);
            pv.CustomerID = customerId;
            int e;
            pv.EmployeeID = int.TryParse(employeeId, out e) ? (int?)e : null;
            pv.OrderDateFrom = from;
            pv.OrderDateTo = to;
            pv.ShippedStatus = status;
            pv.PageNo = pageNo;
            return (OrdTfReturnValue)await new OrdTfBusiness().DoBusinessLogicAsync(pv, Iso);
        }

        /// <summary>保持された条件で検索して表示する（初期表示用）</summary>
        private async Task ExecuteSearch(OrdTfViewModel model, int pageNo)
        {
            OrdTfReturnValue rv = await this.CallSearch(
                model.PCustomerID, model.PEmployeeID, model.POrderDateFrom, model.POrderDateTo, model.PShippedStatus, pageNo);
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
    }
}
