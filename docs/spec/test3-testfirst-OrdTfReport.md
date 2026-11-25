# 受注帳票プレビュー（OrdTfReport）
[共通の決まり](./cmn-testfirst.md)に従い、受注帳票プレビュー画面を、テスト先行で作る（OpenTouryoProject/OpenTouryoCodingAgentAssets#22 の 4 回目）。
受注一覧（[OrdTf](./test3-testfirst-OrdTf.md)）・受注詳細の照会・削除（[OrdTfDetail](./test3-testfirst-OrdTfDetail.md)）・登録・編集（[OrdTfEdit](./test3-testfirst-OrdTfEdit.md)）・
出荷ステータス更新ダイアログ（[OrdTfShip](./test3-testfirst-OrdTfShip.md)）は作成済みで、その上に足す。この回で、受注管理の画面（`SC-ORD-01`〜`05`）がそろう。

## 作るケース
**ケース2（MVC_Sample_Core）だけ。** ケース1・ケース3 には作らない（#22 の進め方 1）。B層は `Shared\` を共用するので、B層とそのテスト（`Shared\WSServer_sample.Tests\`）はこのケースで作り、
net10.0 → net48 の順に（同時にでなく）全件流す（`docs/plan/cmn-testfirst.md`「B層を共用する構成」）。

## 作る範囲（仕様の ID）
- 画面：`SC-ORD-05`（`I01`〜`I14`、`B01`・`B02`）。`SC-ORD-02` に `B04`（納品書）・`B05`（請求書）を足す。
- イベント：
  - `EV-ORD-501`（ページ読み込み。納品書・請求書の両方。`ERR-NOTFOUND` を含む）
  - `EV-ORD-502`（帳票種別の切替。DB を読み直さない）
  - `EV-ORD-503`（印刷。操作用の要素とヘッダ・メニューを印刷の対象から除き、ブラウザの印刷機能を呼ぶ）
  - `EV-ORD-504`（受注詳細への復帰）
  - `EV-ORD-204`（受注詳細の「納品書」「請求書」から、帳票種別を渡して遷移）
- あわせて読む：`LLD/EventSpec/Orders.md` の「共通の処理」（金額の再計算）、`LLD/TableSchema/Orders.md` の業務ルール（`OD-T4`）、
  `HLD/Common.md`（1 節の帳票は同期処理、3 節の金額の算出式、4 節、5〜7 節の `PRE-*`・`ERR-*`）、`HLD/UI_FlowList.md`、`LLD/Decisions/Common.md`・`LLD/Decisions/Orders.md`

範囲外（作らない）：ほかのモジュールの帳票（`SC-SUP-05` など）。ブラウザの印刷のダイアログそのものの操作（印刷を呼んだことまでを確かめる）。

## 前の回から引き継ぐこと
- 決定のレポート `docs/spec/test3-testfirst-OrdTfShip-mvc-core-decisions.md` の「終わり 5」を読む。
- 受注詳細の決定の読みが、この画面にも当たりうる（担当者名 `EMP-T1`、明細の商品名の昇順、商品マスタに無い明細〔`DEC-ORD-24`〕、住所の連結〔`DEC-ORD-18`〕など）。広げるかは、推測で揃えず、段 1 で仕様の決定に上げる。
- メインボタン：規約（`cmn-framework.md`「画面共通」）は、メインボタンを「機能トランザクションを起動するボタン（検索・登録・更新・削除など）」とする。
  画面定義書は `B01`（印刷）・`B02`（戻る）を「画面上部（印刷時は非表示）」に置く。**印刷をメインボタンに含めるかは、規約の解釈として段 1 で仕様の決定に上げる**（前の回までの決定と同じく、決定記録には書かない）。
  戻る（`B02`）と、詳細の `B04`・`B05` は遷移だけなので、前の回の決定 4 の読みで画面定義書の配置にする。

## データを書き換えるテスト
- この回の画面は DB を書き換えない（`EV-ORD-501` 手順 8）。テストが期待を決めるための受注・明細・商品などを作るときは、**テストが自分で作り、後片付けで消す**。既存の行は書き換えない。
- 終わりに受注 830 件・明細 2155 件・顧客 91 件・商品 77 件で、既存の商品の在庫の合計（3119）が始めと同じか確かめる手順を、計画に入れる。
接続先は RowVersion 付きの `Northwind2`（[規約](./cmn-framework.md)「データベース」）。

## 名前（固定）
ProgramID は受注一覧と同じ **`OrdTf`**（B層・引数・戻り値のクラスは受注一覧のものに足す）。

| 対象 | 名前 |
| --- | --- |
| 業務コードクラス | `OrdTfBusiness`（作成済み。メソッドを足す） |
| B層のルート メソッド | `MethodName` = `OrdTfReport`（帳票の受注 1 件・明細。`EV-ORD-501`） |
| 引数クラス・戻り値クラス | `OrdTfParameterValue`・`OrdTfReturnValue`（作成済み。項目を足す） |
| P層（MVC） | `OrdTfController` にアクションを足す。ビューは `Views\OrdTf\Report.cshtml` |
| テストのクラス | B層 `OrdTfBusinessTests`（`Shared\WSServer_sample.Tests\`。作成済み。ケースを足す）、E2E `OrdTfReportScreenTests`（`MVC_Sample_Core\MVC_Sample.E2ETests\`） |
| テスト仕様 | `docs/spec/test3-testfirst-OrdTfReport-mvc-core-tests.md` |
| 計画 | `docs/plan/test3-testfirst-OrdTfReport-mvc-core.md` |
| 決定のレポート | `docs/spec/test3-testfirst-OrdTfReport-mvc-core-decisions.md`（司令役が書き、判断は[代行の取り決め](../plan/cmn-testfirst-proxy.md)で受ける） |
