//**********************************************************************************
//* サンプル アプリ・ＵＩ層
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：footerScreen
//* クラス日本語名  ：フッタ部にメインボタン 5 つを持つ画面用のMaster Page
//*
//* 作成日時        ：2026/11/14
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/14  実装役            新規作成
//**********************************************************************************

using Touryo.Infrastructure.Business.Util;
using Touryo.Infrastructure.Framework.Presentation;
using Touryo.Infrastructure.Framework.Util;

namespace WebForms_Sample.Aspx.Common.Master
{
    /// <summary>フッタ部にメインボタン 5 つを持つ画面用のMaster Page</summary>
    /// <remarks>
    /// マスタ上のボタンのハンドラは、画面コードクラスに
    /// UOC_footerScreen_（コントロール名）_（イベント名）で実装する（接頭辞はマスタのファイル名）。
    /// </remarks>
    public partial class footerScreen : BaseMasterController
    {
        /// <summary>サインイン中の利用者名</summary>
        public string UserName
        {
            get
            {
                var user = (MyUserInfo)UserInfoHandle.GetUserInformation();

                if (user == null)
                {
                    return "anonymous";
                }
                else
                {
                    return user.UserName;
                }
            }
        }
    }
}
