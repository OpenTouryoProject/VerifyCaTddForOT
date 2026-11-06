# 計画：受注一覧の検索（MVC_Sample_Core / net10.0）

仕様：`docs/spec/test6-testfirst.md`、テスト仕様：`docs/spec/test6-testfirst-mvc-core-tests.md`（NorthwindDbSampleSpec@fe574ce）。
段 3（計画）。段 4 はこの計画に沿って実施した。

## 使用スキル
`opentouryo-layer-b`・`opentouryo-dao-common`（CmnDao）・`opentouryo-p-call-business`・`opentouryo-layer-p-mvc`・`opentouryo-exception`・`opentouryo-comment-convention`・`opentouryo-test-layer-b`

## 対象ファイル
| 層 | ファイル | 内容 |
| --- | --- | --- |
| B層 | `Logic/Business/OrdTfBusiness.cs` | `UOC_OrdTfSearch`：検証 → 総件数 → 当該ページ 20 件 → 状況の判定 |
| 引数・戻り値 | `Logic/Common/OrdTfParameterValue.cs`・`OrdTfReturnValue.cs` | 段 2 で作成済み（変更なし） |
| P層 | `Controllers/OrdTfController.cs` | `Index`（GET、EV-ORD-101）・`Search`（POST、EV-ORD-102） |
| P層 | `Models/ViewModels/OrdTfViewModel.cs`・`Views/OrdTf/Index.cshtml` | 検索条件・一覧・総件数・ページ情報 |
| P層 | `Views/Shared/_Layout.cshtml` | フッタ ボタン用の `FooterButtonsSection`（任意）とメニュー項目を追加 |

テスト（`MVC_Sample.Tests/`）は編集しない。

## B層（CmnDao の SQL）
- D層は CmnDao（`SQLText`）。**SQL は固定の断片だけを連結し、値は必ず `SetParameter`（`@P1`…）で渡す**（`SetUserParameter` は使わない）。
- 総件数：`SELECT COUNT(*) FROM Orders o WHERE 1 = 1 <条件>`。
- 一覧：`Orders` を主表に `Customers`・`Employees`・`Shippers` を LEFT JOIN。`ORDER BY OrderDate DESC, OrderID DESC` ＋ `OFFSET … FETCH NEXT 20`。
- 合計金額：`Σ ROUND(CAST(UnitPrice AS decimal(19,4)) × (1 − CAST(Discount AS decimal(9,4))) × Quantity, 2)` ＋ `Freight`。
  `Discount` は `real` なので `decimal` に変換して計算する（Common §3）。明細が無い受注の小計は 0（`ISNULL(SUM(...), 0)`）、`Freight` が NULL のときは 0（`ISNULL(o.Freight, 0)`）として足す（@fe574ce で決定）。
- 受注日（終了）は「翌日未満」で比較する（当該日を含む。時刻部があっても含める）。
- 納期遅延の絞り込みは、当日（`DateTime.Today`）をパラメタで渡す。状況の判定（ORD-T7）は取得後に C# で行う。
- 検証（`VAL-DATE`）は B層：開始 > 終了なら `BusinessApplicationException`（フレームワークが `ErrorFlag = true` に変換）。
  メッセージは文言で定まる（「受注日の開始は終了以前を指定してください。」）。**ID は実装で決める**（Common 7 節）ので、`ORD_VAL_DATE` を採用する（仮ではない）。テストは文言を確かめ、ID は確かめない。

## 画面
- ページ読み込み（`EV-ORD-101`）：`Index` が条件なしで `OrdTfSearch` を呼ぶ。
- 検索（`EV-ORD-102`）：`Search` が条件を引数クラスに詰めて呼ぶ。日付として解釈できない入力はモデル バインドで弾く（テスト仕様 質問 3＝画面）。
- B層呼び出しは直呼び（`new OrdTfBusiness().DoBusinessLogicAsync`、分離レベル `ReadCommitted`）。`ErrorFlag` ならメッセージを表示して画面に留まる。
- 一覧は table を自前で生成し、納期遅延の行を強調（`table-danger`）。0 件は「該当するデータがありません。」。
- フッタのメインボタンは 5 つ（`@section FooterButtonsSection`、`form="formOrdTf"` で紐付け）。使うのは「検索」のみで、他は非活性。
- 顧客・担当者は、選択肢の構築（`OrdTfInit`、今回は範囲外）が無いため **ID を入力するテキスト ボックス**にした。
- ダイアログは使わない（確認・通知が要るイベントが範囲内に無い）。

## 範囲外
`EV-ORD-103`〜`108`、ページ 2 以降、並べ替え列の切替、`OrdTfInit`（選択肢の構築）。
