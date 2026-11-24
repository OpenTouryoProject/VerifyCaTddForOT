# 受注詳細（照会・削除）と一覧からの遷移（OrdTfDetail）
[共通の決まり](./cmn-testfirst.md)に従い、受注詳細画面（照会）と、受注一覧からの遷移を、テスト先行で作る（OpenTouryoProject/OpenTouryoCodingAgentAssets#22 の 1 回目）。
受注一覧（[OrdTf](./test3-testfirst-OrdTf.md)）は作成済みで、その上に足す。

## 作るケース
**ケース2（MVC_Sample_Core）だけ。** ケース1・ケース3 には作らない（#22 の進め方 1）。B層は `Shared\` を共用するので、B層とそのテスト（`Shared\WSServer_sample.Tests\`）はこのケースで作り、
net10.0 → net48 の順に（同時にでなく）全件流す（`docs/plan/cmn-testfirst.md`「B層を共用する構成」）。

## 作る範囲（仕様の ID）
- 画面：`SC-ORD-02`（`I01`〜`I20`、`B01`、`B02`、`B06`）。`SC-ORD-01` に `B03` と、`I06` の受注 ID 列の選択（`EV-ORD-106`）を足す。
- イベント：
  - `EV-ORD-106`（受注詳細への遷移）・`EV-ORD-107`（受注新規登録への遷移。遷移先は下の「まだ無い画面への遷移」）
  - `EV-ORD-201`（受注詳細のページ読み込み。`ERR-NOTFOUND` を含む）・`EV-ORD-203`（受注の削除。`ERR-CONFLICT`・`ERR-NOTFOUND` を含む）・`EV-ORD-206`（受注一覧への復帰）
  - 受注一覧に戻ったときの一覧状態の復元（画面遷移 F-2、`EV-ORD-101` の復元、「共通の処理」一覧状態の保持）。受注一覧の範囲で範囲外にしていたもの
- あわせて読む：`LLD/EventSpec/Orders.md` の「共通の処理」（受注検索クエリ・状況の判定・金額の再計算・一覧状態の保持）、`LLD/TableSchema/Orders.md` の業務ルール（`ORD-T4`・`ORD-T7`・`ORD-T8`）、
  `HLD/Common.md`（3 節の金額の算出式・メッセージ表示・`PRE-*`・`ERR-*`・楽観排他の 9.1 節）、`HLD/UI_FlowList.md`（F-2・F-4）、`LLD/Decisions/Common.md`・`LLD/Decisions/Orders.md`

## まだ無い画面への遷移
`SC-ORD-03`（登録・編集）は次の回で作る。この回では：
- `SC-ORD-01-B03`（新規登録）を置き、`EV-ORD-107` は「一覧状態を保持し、登録モードで `SC-ORD-03` の URL へ遷移する」ところまでを作って確かめる（遷移先の URL・パラメータは計画で決める）。
- `SC-ORD-02-B01`（編集）を置き、`EV-ORD-201` 手順 8 の活性（出荷済で非活性。`ORD-T4`）を確かめる。**`EV-ORD-202`（編集への遷移）は作らない**（次の回）。押したときの振る舞いはテストの期待にしない。

範囲外（作らない）：`EV-ORD-108`（出荷ダイアログ。3 回目）、`EV-ORD-202`・`204`・`205`、`SC-ORD-03`〜`05`。
`SC-ORD-02-B03`（出荷更新）・`B04`（納品書）・`B05`（請求書）は置かない。他のモジュールから `SC-ORD-02` へ来る遷移（`SC-CUS-04` など）も作らない。

## データを書き換えるテスト
削除（`EV-ORD-203`）のテストは、**テストが自分で作った受注（と明細）だけを消す**。既存の受注（Northwind2 の 830 件）は消さない・書き換えない。
作った行は、テストが落ちても後片付けで消す（B層・E2E とも）。後片付けの確かめ（終わりに受注の件数が 830 件か）は計画の手順に入れる。
接続先は RowVersion 付きの `Northwind2`（[規約](./cmn-framework.md)「データベース」）。

## 名前（固定）
ProgramID は受注一覧と同じ **`OrdTf`**（B層・引数・戻り値のクラスは受注一覧のものに足す）。

| 対象 | 名前 |
| --- | --- |
| 業務コードクラス | `OrdTfBusiness`（作成済み。メソッドを足す） |
| B層のルート メソッド | `MethodName` = `OrdTfDetail`（受注 1 件と明細）・`OrdTfDelete`（削除） |
| 引数クラス・戻り値クラス | `OrdTfParameterValue`・`OrdTfReturnValue`（作成済み。項目を足す） |
| P層（MVC） | `OrdTfController` にアクションを足し、ビューは `Views\OrdTf\Detail.cshtml` |
| テストのクラス | B層 `OrdTfBusinessTests`（`Shared\WSServer_sample.Tests\`。作成済み。ケースを足す）、E2E `OrdTfDetailScreenTests`（`MVC_Sample_Core\MVC_Sample.E2ETests\`） |
| テスト仕様 | `docs/spec/test3-testfirst-OrdTfDetail-mvc-core-tests.md` |
| 計画 | `docs/plan/test3-testfirst-OrdTfDetail-mvc-core.md` |
| 決定のレポート | `docs/spec/test3-testfirst-OrdTfDetail-mvc-core-decisions.md`（司令役が書き、判断は[代行の取り決め](../plan/cmn-testfirst-proxy.md)で受ける） |
