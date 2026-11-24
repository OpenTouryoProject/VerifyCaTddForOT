# 計画：受注詳細（照会・削除）と一覧からの遷移 OrdTfDetail（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

テスト仕様：[test3-testfirst-OrdTfDetail-mvc-core-tests.md](../spec/test3-testfirst-OrdTfDetail-mvc-core-tests.md)（NorthwindDbSampleSpec@ec66d6e）。
B層 33 ケースの追加（`Shared\WSServer_sample.Tests\`。net10.0 の `MVC_Sample_Core\MVC_Sample.Tests` と net48 の `MVC_Sample\MVC_Sample.Tests` で流す。既存と合わせて 115 ケース）と、E2E 20 ケースの追加（`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfDetailScreenTests`。既存と合わせて 56 ケース）を通す。**テストは変えない。**
Red のコミット：38df555。受注一覧の計画：[test3-testfirst-OrdTf-mvc-core.md](./test3-testfirst-OrdTf-mvc-core.md)（Core 固有の点〔`ModelState` を消す・`@section` は `<form>` の外・Razor の落とし穴〕はそれと同じ）。

## 使うスキル
`opentouryo-app-design`（設計事項の確認）・`opentouryo-layer-b`・`opentouryo-layer-d`・`opentouryo-dao-common`（`CmnDao`）・`opentouryo-query-definition`（SQL 定義）・`opentouryo-exception`（業務例外・ロールバック）・`opentouryo-transaction-control`・`opentouryo-layer-p-mvc`・`opentouryo-mvc-crud-screens`（MVC の一覧・詳細の作り）・`opentouryo-p-call-business`・`opentouryo-comment-convention`（ヘッダ・BOM）。

## 設計事項（`opentouryo-app-design`）
| 設計事項 | 決め |
| --- | --- |
| レイヤ分割 | P層（`OrdTfController` の `Detail`・`Delete`・`Back`）→ B層 `OrdTfBusiness`（`MethodName` = `OrdTfDetail`・`OrdTfDelete`。`DoBusinessLogicAsync(pv, ReadCommitted)`）→ D層（`CmnDao` ＋ SQL 定義 `.sql`）。P層から D層は呼ばない。金額の計算・状況の判定・廃番の付記・住所の連結は B層 |
| トランザクション・データアクセス | 詳細は読み取りだけ。削除は B層が 1 トランザクション（`DoBusinessLogic` の境界）：明細 → 受注（`RowVersion` 条件）の順（ORD-T8）。受注が 0 件ならロールバックし、受注の有無で ERR-CONFLICT・ERR-NOTFOUND を分ける。分離レベルは `ReadCommitted`（テストの呼び出しと同じ）。SQL は静的 `.sql`（値はすべて `@パラメタ`。文字列の連結なし）。自動生成 Dao は使わない（`Orders`・`Order Details` を絞る単発の SQL で、業務の判断を持たないため `CmnDao` で足りる） |
| 例外・エラー | ERR-NOTFOUND・ERR-CONFLICT は B層が `BusinessApplicationException`（メッセージ ID `ORDTF003`・`ORDTF004`。文言は共通仕様 7 節）を投げる → フレームワークがロールバックし、`ErrorFlag = true` の戻り値にする。P層は `catch` しない。DB の失敗は基盤任せ（`DEC-CMN-07`） |
| 楽観排他 | 画面が `Orders.RowVersion` を hidden で保持し、削除の `WHERE` に付ける（共通仕様 9.1 節）。削除では `RowVersion` を更新しない。ERR-CONFLICT のときは詳細を読み直して再表示する（保持する `RowVersion` も更新される） |
| 状態管理（一覧状態の保持） | **Session**（`OrdTfListState` の JSON。キー `OrdTfListState`）。一覧の操作（初期表示・検索の成功・ページ切替・並べ替え・復帰）のたびに、最後に実行した検索の条件・ページ・並べ替えを保持する（`DEC-ORD-04`・`DEC-ORD-19`）。詳細へのリンク（GET）・新規登録のリンクは、保持済みの状態をそのまま使う。検証エラーでは更新しない（`DEC-ORD-03`）。復帰（`Back`）だけが復元し、`/OrdTf` を直接開いたら既定の一覧（`DEC-ORD-20`）。`Session` は `Startup` で構成済み（`AddSession`・`UseSession`） |
| 画面設計 | 詳細は `Views\OrdTf\Detail.cshtml`（読み取り専用のラベルと表。入力欄なし）。フッタのメインボタンは削除だけ（`#btnMain1`）、編集（`#btnEdit`・画面上部）・一覧へ戻る（`#btnBack`・画面下部）・一覧の新規登録（`#btnNew`）は遷移だけなのでメインボタンに含めない（決定 4）。削除の確認は `window.confirm`（メッセージ「受注 ID nnn を削除します。よろしいですか？」） |
| 画面遷移 | 一覧 → 詳細：行の受注 ID の `<a href="/OrdTf/Detail?id=">`。詳細 → 一覧：`<a href="/OrdTf/Back">`。削除の完了・ERR-NOTFOUND：`TempData` にメッセージを入れて `/OrdTf/Back` へリダイレクト（PRG）。新規登録：`/OrdTf/Edit?mode=new`（`SC-ORD-03` は次の回。この回は画面が無く、URL までを作る。テスト仕様 1.2 で固めた URL） |
| 認証・CSRF | コントローラ全体が `[Authorize(Cookie)]`。削除は `[HttpPost]`・`[ValidateAntiForgeryToken]`（フォームに `@Html.AntiForgeryToken()`） |
| 国際化・書式 | 日付 `yyyy/MM/dd`・金額 `#,##0.00`（`InvariantCulture`、小数第 3 位は四捨五入＝`DEC-ORD-26`）・割引率は百分率（端数は小数で表示＝`DEC-ORD-25`）・NULL は空文字 |
| 設定・SQL 定義 | `resource\Sql\` に `.sql` を 5 つ足す（下の対象ファイル）。構成ファイルは変えない |

## 実装の決定（振る舞いは変えない）
1. **顧客住所の区切りは半角空白**（`DEC-ORD-18` は区切りを定めない。テストは区切りを期待にしない）。空の要素は NULL・空白だけ。
2. **明細金額・価格の計算は B層の C#（`decimal`）**：`Discount` は SQL で `CAST(... AS decimal(5,4))`（`real` のまま計算しない＝TableSchema の注意）、`Price = UnitPrice × (1 − Discount)`（丸めない）、`Amount = Math.Round(Price × Quantity, 2, AwayFromZero)`、小計 = Σ Amount。一覧の合計金額の SQL（`OrdTfSearch.xml`）と同じ丸め規則。
3. **商品名の昇順は SQL（`ORDER BY ProductName, ProductID`）**。`Products` は内部結合＝商品マスタに無い明細は明細一覧にも小計・合計にも含めない（`DEC-ORD-24`）。
4. **削除失敗の分類**：P層は ErrorMessageID に頼らず、失敗のあとに `OrdTfDetail` を呼び直す。受注が無ければ ERR-NOTFOUND（一覧へ）、あれば ERR-CONFLICT（詳細を最新で再表示。メッセージは削除の `ErrorMessage`）。
5. **一覧状態を操作のたびに Session へ保持する**（リンクが GET のままで、遷移の時点で保持するのと同じ振る舞いになる）。`Back` は復元したページが総ページ数を超えれば B層が最終ページにする（`DEC-ORD-23`＝`DEC-ORD-06` の B層の挙動）。
6. **SQL 定義に書くコメントは先頭の 1 行だけ**（`OrdTfSearch.xml` の知見：動的 SQL の組み立てで改行が詰められる。静的 `.sql` でも先頭 1 行だけに限る）。
7. **ビューは BOM 付き UTF-8・CRLF**。Core のビューはビルドでコンパイルされる（`aspnet_compiler` は不要）。`@Html.AntiForgeryToken()` を使う。

## 対象ファイル
- B層・D層（`Shared\`・`resource\`。ケース1・3 と共用）
  - `Shared\Business\OrdTfBusiness.cs`（`UOC_OrdTfDetail`・`UOC_OrdTfDelete`・住所の連結・メッセージの定数）
  - `Shared\IfType\OrdTfParameterValue.cs`・`OrdTfReturnValue.cs`（段 2 で追加済み）
  - `resource\Sql\OrdTfDetailHeader.sql`・`OrdTfDetailLines.sql`・`OrdTfDeleteDetails.sql`・`OrdTfDeleteOrder.sql`・`OrdTfOrderCount.sql`
- P層（`MVC_Sample_Core\MVC_Sample\`）
  - `Controllers\OrdTfController.cs`（`Detail`・`Delete`・`Back`・一覧状態の保持。`Index`・`Search`・`Move`・`Sort` に保持の呼び出しを足す。`Edit` は `SC-ORD-03` への遷移先の**仮の土台**：404 と本文の 1 行だけを返す〔本文が空だと Chrome がエラー画面に変わり URL を確かめられない〕。次の回で本実装に置き換える）
  - `Models\ViewModels\OrdTfListState.cs`・`OrdTfDetailViewModel.cs`（新規）
  - `Views\OrdTf\Detail.cshtml`（新規の本実装）・`Views\OrdTf\Index.cshtml`（受注 ID のリンク・`#btnNew`）
- 親クラス 1・2・`Startup`・`_Layout.cshtml`・テストのファイルは変えない。

## 手順
1. 計画（本書）。
2. B層・D層：SQL 定義 → `OrdTfBusiness`。net10.0 の B層（115 ケース）を流す。
3. P層：ViewModel → コントローラ → ビュー。`dotnet build`（Core のビューはビルドでコンパイルされる）。
4. net10.0 の E2E を全件（56 ケース。アプリは空きポートで起動）。
5. **net48 の B層**：Shared を共用するので、net10.0 の後に（同時にでなく）`MVC_Sample\MVC_Sample.Tests` を MSBuild で組み、`dotnet test --no-build` で全件（115 ケース）。
6. **後始末の確認**：アプリのプロセスが残っていないこと、`Northwind2` の受注が **830 件**・明細 2155 件で、`Tf` で始まるテストのデータ（顧客・商品・担当者・配送業者）が 0 件であること。
7. Red のコミット（38df555）からテストのファイル（`Shared\WSServer_sample.Tests\`・`MVC_Sample_Core\MVC_Sample.Tests\`・`MVC_Sample_Core\MVC_Sample.E2ETests\`）が変わっていないこと（`git diff 38df555 -- <場所>` が空）。

## ケース1・ケース3 への影響
`Shared\` の B層は追加だけ（`OrdTfDetail`・`OrdTfDelete` と戻り値・引数のメンバ）。既存の `OrdTfInit`・`OrdTfSearch` は変えない。ケース1（net48 MVC）・ケース3（WebForms）は、この回は作らない。
