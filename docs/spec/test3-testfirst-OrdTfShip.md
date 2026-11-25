# 出荷ステータス更新ダイアログ（OrdTfShip）
[共通の決まり](./cmn-testfirst.md)に従い、出荷ステータス更新ダイアログを、テスト先行で作る（OpenTouryoProject/OpenTouryoCodingAgentAssets#22 の 3 回目）。
受注一覧（[OrdTf](./test3-testfirst-OrdTf.md)）・受注詳細の照会・削除（[OrdTfDetail](./test3-testfirst-OrdTfDetail.md)）・登録・編集（[OrdTfEdit](./test3-testfirst-OrdTfEdit.md)）は作成済みで、その上に足す。

## 作るケース
**ケース2（MVC_Sample_Core）だけ。** ケース1・ケース3 には作らない（#22 の進め方 1）。B層は `Shared\` を共用するので、B層とそのテスト（`Shared\WSServer_sample.Tests\`）はこのケースで作り、
net10.0 → net48 の順に（同時にでなく）全件流す（`docs/plan/cmn-testfirst.md`「B層を共用する構成」）。

## 作る範囲（仕様の ID）
- 画面：`SC-ORD-04`（`I01`〜`I08`、`B01`〜`B03`）。`SC-ORD-01` に `B04`（一覧の各行の出荷更新）、`SC-ORD-02` に `B03`（出荷更新）を足す。
- イベント：
  - `EV-ORD-401`（ダイアログの表示。未出荷・出荷済の両方。`ERR-NOTFOUND` を含む）
  - `EV-ORD-402`（出荷の確定・情報更新。検証の全部、在庫の引き落とし `OD-T5`・`PRD-T5`、在庫不足の `ERR-BIZ`、`ERR-CONFLICT`・`ERR-NOTFOUND`）
  - `EV-ORD-403`（出荷の取消。確認、在庫を戻す、未出荷の `ERR-BIZ`、`ERR-CONFLICT`・`ERR-NOTFOUND`）
  - `EV-ORD-404`（閉じる。変更がなくても呼び出し元を再読み込みする）
  - `EV-ORD-108`（一覧からの表示。閉じた後に同じ条件で再検索。F-5）・`EV-ORD-205`（詳細からの表示。閉じた後に詳細を最新化）
- あわせて読む：`LLD/EventSpec/Orders.md` の「共通の処理」（状況の判定・選択肢の構築・一覧状態の保持）、`LLD/TableSchema/Orders.md` の業務ルール（`ORD-T2`・`T3`・`T7`、`OD-T5`・`OD-T6`）、
  `LLD/TableSchema/Products.md`（`UnitsInStock` の制約、`PRD-T5`）、`HLD/Common.md`（4 節、5〜7 節の `PRE-*`・`VAL-*`・`ERR-*`、9.1 節の楽観排他の定型文）、`HLD/UI_FlowList.md`（F-5）、
  `LLD/Decisions/Common.md`・`LLD/Decisions/Orders.md`

範囲外（作らない）：`SC-ORD-05`（帳票）と、そこへの遷移（`EV-ORD-204`。詳細の `B04`・`B05` は置かない）。配送管理の出荷追跡画面（`SC-SHP-04`）からの表示。

## 前の回から引き継ぐこと
- 決定のレポート `docs/spec/test3-testfirst-OrdTfEdit-mvc-core-decisions.md` の「終わり 4」：
  - **画面から送られる値で、B層の業務の検証を外させない**（`DEC-ORD-49` の考え方）。この画面では、`EV-ORD-402` の入力の「表示時点の出荷済／未出荷の別」が当たる（在庫を引き落とすかどうかが、これで決まる）。
  - 一覧状態は Session（キー `OrdTfListState`）。この回の画面は一覧状態を書き換えない。
- メインボタン：規約（`cmn-framework.md`「画面共通」）は「すべての画面のメインボタンはフッタ部」とするが、ダイアログ（F-5。元画面の上に重ねて表示）を書いていない。
  画面定義書はダイアログの `B01`〜`B03` を「ダイアログ下部」に置く。**ダイアログのボタンをどこに置くかは、規約の解釈として段 1 で仕様の決定に上げる**（前の回の決定 4 と同じく、決定記録には書かない）。
  一覧の `B04`（各行）・詳細の `B03` は、ダイアログを開くだけ（遷移だけ）なので、前の回の決定 4 の読みで画面定義書の配置にする。

## データを書き換えるテスト
- 出荷の確定・取消は `Orders` と `Products.UnitsInStock` を書き換える。**テストは、テストが自分で作った受注・明細・商品（と顧客）だけを書き換える・消す**。既存の受注（Northwind2 の 830 件）・商品（77 件）は書き換えない。
- 在庫の不足・ちょうど 0 になる・明細が複数の商品にまたがる、なども、テストが作った商品で作る。
- 作った行は、テストが落ちても後片付けで消す（B層・E2E とも）。
- 後片付けの確かめは計画の手順に入れる。終わりに受注 830 件・明細 2155 件・顧客 91 件・商品 77 件で、**既存の商品の `UnitsInStock` の合計が始めと同じ**（この回の始めで 3119）か確かめる。
接続先は RowVersion 付きの `Northwind2`（[規約](./cmn-framework.md)「データベース」）。

## 名前（固定）
ProgramID は受注一覧と同じ **`OrdTf`**（B層・引数・戻り値のクラスは受注一覧のものに足す）。

| 対象 | 名前 |
| --- | --- |
| 業務コードクラス | `OrdTfBusiness`（作成済み。メソッドを足す） |
| B層のルート メソッド | `MethodName` = `OrdTfShipInit`（ダイアログの表示。`EV-ORD-401`）・`OrdTfShipSave`（出荷の確定・情報更新。`EV-ORD-402`）・`OrdTfShipCancel`（出荷の取消。`EV-ORD-403`） |
| 引数クラス・戻り値クラス | `OrdTfParameterValue`・`OrdTfReturnValue`（作成済み。項目を足す） |
| P層（MVC） | `OrdTfController` にアクションを足す。ダイアログの中身のビューは `Views\OrdTf\Ship.cshtml`（重ねて表示する作りは計画で決める） |
| テストのクラス | B層 `OrdTfBusinessTests`（`Shared\WSServer_sample.Tests\`。作成済み。ケースを足す）、E2E `OrdTfShipScreenTests`（`MVC_Sample_Core\MVC_Sample.E2ETests\`） |
| テスト仕様 | `docs/spec/test3-testfirst-OrdTfShip-mvc-core-tests.md` |
| 計画 | `docs/plan/test3-testfirst-OrdTfShip-mvc-core.md` |
| 決定のレポート | `docs/spec/test3-testfirst-OrdTfShip-mvc-core-decisions.md`（司令役が書き、判断は[代行の取り決め](../plan/cmn-testfirst-proxy.md)で受ける） |
