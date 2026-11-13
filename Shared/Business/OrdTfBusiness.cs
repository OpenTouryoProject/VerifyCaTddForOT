//**********************************************************************************
//* フレームワーク・テストクラス（Ｂ層）
//**********************************************************************************

//**********************************************************************************
//* クラス名        ：OrdTfBusiness
//* クラス日本語名  ：受注一覧（OrdTf）のＢ層
//*
//* 作成日時        ：2026/11/13
//* 作成者          ：実装役
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2026/11/13  実装役            新規作成（テスト先行の Red 用。戻り値を設定するだけの空の UOC）
//**********************************************************************************

using WSIFType_sample;

using Touryo.Infrastructure.Business.Business;

namespace WSServer_sample.Business
{
    /// <summary>受注一覧（OrdTf）のＢ層</summary>
    public class OrdTfBusiness : MyFcBaseLogic
    {
        /// <summary>選択肢の構築（EV-ORD-101）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfInit(OrdTfParameterValue pv)
        {
            // 段 2（Red）：戻り値を設定するだけ。実装は段 4。
            this.ReturnValue = new OrdTfReturnValue();
        }

        /// <summary>受注の検索・ページ切替（EV-ORD-102・EV-ORD-104）</summary>
        /// <param name="pv">引数クラス</param>
        private void UOC_OrdTfSearch(OrdTfParameterValue pv)
        {
            // 段 2（Red）：戻り値を設定するだけ。実装は段 4。
            this.ReturnValue = new OrdTfReturnValue();
        }
    }
}
