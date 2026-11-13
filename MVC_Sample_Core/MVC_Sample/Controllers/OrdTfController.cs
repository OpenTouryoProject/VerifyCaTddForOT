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
//**********************************************************************************

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

using Touryo.Infrastructure.Business.Presentation;

namespace MVC_Sample.Controllers
{
    /// <summary>受注一覧（SC-ORD-01）のコントローラ（空の土台）</summary>
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class OrdTfController : MyBaseMVControllerCore
    {
        /// <summary>
        /// GET: /OrdTf
        /// </summary>
        /// <returns>要素の無い画面（段 4 で実装する）</returns>
        [HttpGet]
        public IActionResult Index()
        {
            return this.View();
        }
    }
}
