# 受注詳細（登録・編集）（OrdTfEdit）
[共通の決まり](./cmn-testfirst.md)に従い、受注詳細画面（登録・編集）を、テスト先行で作る（OpenTouryoProject/OpenTouryoCodingAgentAssets#22 の 2 回目）。
受注一覧（[OrdTf](./test3-testfirst-OrdTf.md)）と受注詳細（照会・削除。[OrdTfDetail](./test3-testfirst-OrdTfDetail.md)）は作成済みで、その上に足す。

## 作るケース
**ケース2（MVC_Sample_Core）だけ。** ケース1・ケース3 には作らない（#22 の進め方 1）。B層は `Shared\` を共用するので、B層とそのテスト（`Shared\WSServer_sample.Tests\`）はこのケースで作り、
net10.0 → net48 の順に（同時にでなく）全件流す（`docs/plan/cmn-testfirst.md`「B層を共用する構成」）。

## 作る範囲（仕様の ID）
- 画面：`SC-ORD-03`（`I01`〜`I18`、`B01`〜`B04`）。
- イベント：
  - `EV-ORD-301`（ページ読み込み。登録・編集の両モード。`ERR-NOTFOUND`・出荷済の `ERR-BIZ` を含む）
  - `EV-ORD-302`（顧客選択の変更。登録モードの出荷先の既定値と、手入力があるときの上書きの確認を含む）・`EV-ORD-303`（送料の変更）
  - `EV-ORD-304`（明細行の追加）・`EV-ORD-305`（明細行の削除）・`EV-ORD-306`（明細の商品選択の変更。重複・在庫の警告を含む）・`EV-ORD-307`（明細の単価・割引率・数量の変更）
  - `EV-ORD-308`（受注の保存。登録・編集、検証の全部、`ERR-CONFLICT`・`ERR-NOTFOUND`・`ERR-BIZ`、明細の行ごとの適用 `OD-T6`）
  - `EV-ORD-309`（キャンセル。未保存の変更があるときの確認 F-3）
  - `EV-ORD-202`（受注詳細の「編集」から編集モードへの遷移。手順 1 の出荷済の `ERR-BIZ` を含む）。1 回目に作らなかったもの
- あわせて読む：`LLD/EventSpec/Orders.md` の「共通の処理」（金額の再計算・選択肢の構築・一覧状態の保持）、`LLD/TableSchema/Orders.md` の業務ルール（`ORD-T1`・`T3`〜`T6`、`OD-T1`〜`T4`・`OD-T6`）と桁数、
  `HLD/Common.md`（3 節の金額の算出式、4 節、5〜7 節の `PRE-*`・`VAL-*`・`ERR-*`、9.1 節の楽観排他の定型文）、`HLD/UI_FlowList.md`（F-3・F-4）、`LLD/Decisions/Common.md`・`LLD/Decisions/Orders.md`

## 1 回目から引き継ぐこと
- **URL の約束**：登録モードは `/OrdTf/Edit?mode=new`（受注 ID を付けない）。1 回目の E2E（`OrdTfDetailScreenTests` の `新規登録_…`）が確かめているので変えない（変えるなら期待値の変更）。編集モードの URL は計画で決める。
- `OrdTfController.Edit` の仮の土台（404 と本文 1 行）を、この回の画面で置き換える。
- 決定のレポート `docs/spec/test3-testfirst-OrdTfDetail-mvc-core-decisions.md` の「終わり 3」の残り 2 点（一覧状態の Session と複数タブ、`ERR-CONFLICT` の再表示と POST の再送）を、この回の計画で扱う。
- メインボタン：規約（`cmn-framework.md`「画面共通」）と 1 回目の決定 4（遷移だけのボタンは含めない）の読みに従う。**保存（`B03`）はメインボタン**（登録・更新のトランザクションを起動する）。
  キャンセル（`B04`）は遷移だけなので画面定義書の配置（画面下部）、明細行追加（`B01`）・行削除（`B02`）は入力支援なので画面定義書の配置。

範囲外（作らない）：`SC-ORD-04`（出荷ダイアログ）・`SC-ORD-05`（帳票）と、そこへの遷移（`EV-ORD-108`・`204`・`205`）。
F-3 の確認は、キャンセル（`EV-ORD-309`）だけに作る。ヘッダのメニューなど、ほかの離脱の操作の確認（`HLD/Common.md` 8 節）は作らない。

## データを書き換えるテスト
- 登録・編集のテストは、**テストが自分で作った受注（と明細）だけを書き換える・消す**。既存の受注（Northwind2 の 830 件）は書き換えない。
- テストが作る顧客・商品が要るときも、テストが作って後で消す（既存の `Customers`・`Products` を書き換えない）。廃番の商品・在庫の少ない商品なども、テストが作る。
- 作った行は、テストが落ちても後片付けで消す（B層・E2E とも）。登録のテストは、採番された受注 ID を控えて消す。
- 後片付けの確かめ（終わりに受注の件数が 830 件、`Customers`・`Products` の件数が始めと同じか）は計画の手順に入れる。
接続先は RowVersion 付きの `Northwind2`（[規約](./cmn-framework.md)「データベース」）。

## 名前（固定）
ProgramID は受注一覧と同じ **`OrdTf`**（B層・引数・戻り値のクラスは受注一覧のものに足す）。

| 対象 | 名前 |
| --- | --- |
| 業務コードクラス | `OrdTfBusiness`（作成済み。メソッドを足す） |
| B層のルート メソッド | `MethodName` = `OrdTfEditInit`（選択肢と、編集モードの受注 1 件・明細）・`OrdTfCustomer`（顧客 1 件。`EV-ORD-302`）・`OrdTfProduct`（商品 1 件。`EV-ORD-306`）・`OrdTfSave`（保存。登録・編集） |
| 引数クラス・戻り値クラス | `OrdTfParameterValue`・`OrdTfReturnValue`（作成済み。項目を足す） |
| P層（MVC） | `OrdTfController` の `Edit` を置き換え、アクションを足す。ビューは `Views\OrdTf\Edit.cshtml` |
| テストのクラス | B層 `OrdTfBusinessTests`（`Shared\WSServer_sample.Tests\`。作成済み。ケースを足す）、E2E `OrdTfEditScreenTests`（`MVC_Sample_Core\MVC_Sample.E2ETests\`） |
| テスト仕様 | `docs/spec/test3-testfirst-OrdTfEdit-mvc-core-tests.md` |
| 計画 | `docs/plan/test3-testfirst-OrdTfEdit-mvc-core.md` |
| 決定のレポート | `docs/spec/test3-testfirst-OrdTfEdit-mvc-core-decisions.md`（司令役が書き、判断は[代行の取り決め](../plan/cmn-testfirst-proxy.md)で受ける） |
