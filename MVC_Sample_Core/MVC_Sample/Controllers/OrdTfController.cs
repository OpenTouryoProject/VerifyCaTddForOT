//**********************************************************************************
//* 受注一覧（テスト先行）・コントローラ（Ｐ層）
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfController
//* クラス日本語名  ：受注一覧（SC-ORD-01）のコントローラ
//*
//* 作成日時        ：2026/11/06
//* 作成者          ：Claude Code
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/06  Claude Code       新規作成（EV-ORD-101・EV-ORD-102）
//**********************************************************************************

using System;
using System.Data;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MVC_Sample.Logic.Business;
using MVC_Sample.Logic.Common;
using MVC_Sample.Models.ViewModels;

using Touryo.Infrastructure.Business.Presentation;
using Touryo.Infrastructure.Public.Db;

namespace MVC_Sample.Controllers
{
    /// <summary>受注一覧（SC-ORD-01）のコントローラ</summary>
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class OrdTfController : MyBaseMVControllerCore
    {
        /// <summary>ページ読み込み（EV-ORD-101）：既定条件で一覧を表示する</summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            OrdTfViewModel model = new OrdTfViewModel();
            await this.SearchCore(model);
            return View(model);
        }

        /// <summary>受注の検索（EV-ORD-102）</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(OrdTfViewModel model)
        {
            // 日付として解釈できない入力は、モデル バインドで弾く（VAL-DATE の前半。B層には届かない）
            if (!ModelState.IsValid)
            {
                model.Rows.Clear();
                model.TotalCount = 0;
                model.Message = "受注日は yyyy/MM/dd の形式で入力してください。";
                model.IsError = true;
                return View("Index", model);
            }

            await this.SearchCore(model);
            return View("Index", model);
        }

        /// <summary>B層を呼んで、結果をビューモデルへ設定する</summary>
        private async Task<OrdTfViewModel> SearchCore(OrdTfViewModel model)
        {
            OrdTfParameterValue pv = new OrdTfParameterValue(
                this.ControllerName, "-", "OrdTfSearch", "SQL", this.UserInfo)
            {
                CustomerID = model.CustomerID,
                EmployeeID = model.EmployeeID,
                OrderDateFrom = model.OrderDateFrom,
                OrderDateTo = model.OrderDateTo,
                ShipStatus = model.ShipStatus ?? "すべて",
                PageNo = 1
            };

            // Ｂ層呼出し（Web：直呼び）
            OrdTfReturnValue rv = (OrdTfReturnValue)await new OrdTfBusiness()
                .DoBusinessLogicAsync(pv, DbEnum.IsolationLevelEnum.ReadCommitted);

            model.Rows.Clear();
            model.TotalCount = 0;
            model.PageNo = 1;
            model.Message = null;
            model.IsError = false;

            if (rv.ErrorFlag)
            {
                // 業務エラー（VAL-DATE など）：メッセージを表示し、画面に留まる（ERR-VALIDATION）
                model.Message = rv.ErrorMessage;
                model.IsError = true;
                return model;
            }

            model.TotalCount = rv.TotalCount;
            foreach (DataRow r in rv.Rows.Rows)
            {
                model.Rows.Add(new OrdTfRowViewModel
                {
                    OrderID = Convert.ToInt32(r["OrderID"]),
                    CustomerName = Text(r["CustomerName"]),
                    EmployeeName = Text(r["EmployeeName"]),
                    OrderDate = Date(r["OrderDate"]),
                    RequiredDate = Date(r["RequiredDate"]),
                    ShippedDate = Date(r["ShippedDate"]),
                    ShipperName = Text(r["ShipperName"]),
                    TotalAmount = Convert.ToDecimal(r["TotalAmount"]).ToString("N2"),
                    Status = Text(r["Status"])
                });
            }

            if (model.TotalCount == 0)
            {
                // 0 件はエラーにしない（EV-ORD-101 手順 7）
                model.Message = "該当するデータがありません。";
            }
            return model;
        }

        private static string Text(object o)
        {
            return (o == null || o == DBNull.Value) ? "" : o.ToString();
        }

        /// <summary>日付は yyyy/MM/dd、NULL は空（HLD/Common.md 4 節）</summary>
        private static string Date(object o)
        {
            return (o == null || o == DBNull.Value) ? "" : ((DateTime)o).ToString("yyyy/MM/dd");
        }
    }
}
