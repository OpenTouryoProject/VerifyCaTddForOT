# 計画：受注一覧 OrdTf（ケース1：MVC_Sample ＝ net48 ASP.NET MVC）

テスト仕様：[test3-testfirst-OrdTf-mvc-tests.md](../spec/test3-testfirst-OrdTf-mvc-tests.md)（NorthwindDbSampleSpec@e391dca）。
B層 56 件（`Shared\WSServer_sample.Tests\`）と E2E 19 件（`MVC_Sample.E2ETests\`）を通す。**テストは変えない。**

## 使うスキル
`opentouryo-app-design`（設計事項の確認）・`opentouryo-layer-b`・`opentouryo-dao-common`（CmnDao）・`opentouryo-query-definition`（動的 SQL）・
`opentouryo-exception`・`opentouryo-layer-p-mvc`・`opentouryo-p-call-business`・`opentouryo-comment-convention`。

## 設計事項（`opentouryo-app-design` のチェックリストから、この画面に関わるもの）

| 設計事項 | 決め |
| --- | --- |
| レイヤ分割 | P層（`OrdTfController`）→ B層 `new OrdTfBusiness().DoBusinessLogic(pv, ReadCommitted)` 直呼び（Web の既定）→ D層は `CmnDao`（`GetDam()` 渡し）。P層から D層は呼ばない |
| トランザクション | 読み取りだけ。分離レベルは `ReadCommitted`（テストと P層で同じ値）。更新は無い |
| データアクセス | CmnDao ＋ SQL 定義ファイル（`resource\Sql\OrdTf*.sql / .xml`）。条件の有無で SQL の形が変わるので検索だけ動的（`.xml`）、選択肢は静的（`.sql`）。値は `@パラメタ`（`SetUserParameter` は使わない） |
| 一覧ページ制御 | SQL の `OFFSET … FETCH NEXT 20 ROWS ONLY`（アプリでページを切らない）。総件数は別の `COUNT(*)`（同じ条件） |
| 例外・エラー | 検証違反は `BusinessApplicationException`（文言は仕様どおり。メッセージ ID は実装で決める＝`DEC-CMN-01`：`ORDTF001`・`ORDTF002`）。DB の失敗は基盤任せ（`ERR-DB` は範囲外） |
| 状態管理（一覧状態の保持） | **P層が hidden で持つ**（最後に実行した検索の条件 5 つ＋現在ページ。実装の決定）。Session は使わない（タブごとに独立し、Session タイムアウトの影響が無い）。`DEC-ORD-04`：ページ移動は hidden の条件を使う。`DEC-ORD-03`：検証エラーでは hidden を更新しない |
| 画面設計 | `_Layout.cshtml` に `@RenderSection("FooterButtonsSection", required: false)`（コンテンツ フッタ部）を足し、画面が 5 つのメインボタンを `<button form="OrdTfForm">` で置く。`@section` は `<form>` の外に描画されるため `form` 属性で紐付ける（`cmn-framework.md`） |
| 入力チェック | 日付の形式・前後関係は B層（`3.6`）。P層は入力をそのまま渡す。違反の表示先：形式は項目直下、関連チェックは共通メッセージ領域（`DEC-CMN-04`・`DEC-CMN-05`） |
| 認証 | 既存の Forms 認証（`[Authorize]`）。メニューに「受注一覧」の導線を足す |
| キャッシュ・セキュリティ | `FxCacheControl` は既存のまま。POST は `[ValidateAntiForgeryToken]` |
| 国際化・書式 | 日付 `yyyy/MM/dd`、金額 `#,##0.00`（`CultureInfo.InvariantCulture`）、空値は空文字 |

## 実装の決定（作り方だけが変わること。仕様の振る舞いは変えない）
1. B層の契約は、テスト仕様 1 章のとおり（クラスは `Shared\IfType\` に作成済み）。
2. 受注検索 SQL：`Orders` を主表に `Customers`・`Employees`・`Shippers` を LEFT JOIN。合計金額は `OUTER APPLY` で受注ごとに `SUM(ROUND(CAST(UnitPrice AS decimal(19,4)) * (1 - CAST(Discount AS decimal(5,4))) * Quantity, 2))`（10 進で計算・明細ごとに丸め。`LLD/TableSchema.md` の `Discount` の扱い）、`+ ISNULL(Freight, 0)`。
3. 並び：`ORDER BY CAST(OrderDate AS date) DESC, OrderID DESC`（時刻を見ない＝`DEC-ORD-07`。NULL は降順で末尾＝`DEC-ORD-08`）。
4. 受注日の範囲：開始は `OrderDate >= 開始日`、終了は `CAST(OrderDate AS date) <= 終了日`（日付の単位で比べ、当日の時刻付きを含む。翌日を作らないので `9999/12/31` でも例外にならない）。
5. 出荷状況：B層で `ALL`／`NOTSHIPPED`／`SHIPPED`／`LATE` に写し、動的 SQL の `<SELECT>` で分岐（`ALL` は未設定）。`LATE` のときだけ `@Today`（`DateTime.Today`）を渡す。状況の表示も `DateTime.Today` で C# が判定。
6. 受注日の入力は `DateTime.TryParseExact("yyyy/MM/dd", InvariantCulture)`。空白だけ・null は未指定。
7. 項目単位の違反は `ErrorFields` に項目名を入れてから投げる（形式違反）。関連チェックは `ErrorFields` 空。
8. P層の部品：`OrdTfController`（`Index` GET／`Search`・`Move` POST。PRG はしない。ページ移動は `Move?dir=first|prev|next|last`）、`OrdTfViewModel`、`Views\OrdTf\Index.cshtml`（UTF-8 BOM。`aspnet_compiler` で検証）。
9. 入力欄で Enter を押したときは、フォームの先頭に置いた見えない submit ボタン（`Search` へ送る）が受けるので、検索になる（ページ移動のボタンが受けない）。
10. B層の例外：形式違反の項目名を持った戻り値が `ErrorFlag = true` で返ることは、基盤の変換（`ErrorFields` は戻り値のメンバなので保たれる）を前提とし、テストで確かめる。

## 対象ファイル
- `Shared\Business\OrdTfBusiness.cs`（`UOC_OrdTfInit`・`UOC_OrdTfSearch`）
- `resource\Sql\OrdTfCustomers.sql`・`OrdTfEmployees.sql`・`OrdTfCount.xml`・`OrdTfSearch.xml`
- `MVC_Sample\MVC_Sample\Controllers\OrdTfController.cs`、`Models\ViewModels\OrdTfViewModel.cs`、`Views\OrdTf\Index.cshtml`、`Views\Shared\_Layout.cshtml`（フッタ ボタンのセクションとメニューの導線）、`MVC_Sample.csproj`（`Compile`・`Content` の追加）
- 親クラス 1・2 は修正しない。テストのファイルは編集しない。

## 手順
1. 計画（本書）。
2. B層：SQL 定義 → `OrdTfBusiness` → `MVC_Sample.Tests`（net48）で 56 件が通るまで。
3. P層：ViewModel → コントローラ → ビュー → `_Layout`・メニュー → csproj。Web を MSBuild でビルドし、`aspnet_compiler` でビューを確かめる。
4. E2E 19 件を通す（IIS Express・8090）。
5. 全件（B層・E2E）。壊す確かめ（実装を 1 か所ずつ一時的に壊し、落ちるケースと落ち方を見て戻す。戻したファイルの更新時刻を更新してから流す）。後始末（DB・iisexpress）の確認。
