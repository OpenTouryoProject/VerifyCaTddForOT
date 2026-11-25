# 計画：受注帳票プレビュー OrdTfReport（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

テスト仕様：[test3-testfirst-OrdTfReport-mvc-core-tests.md](../spec/test3-testfirst-OrdTfReport-mvc-core-tests.md)（NorthwindDbSampleSpec@b60c760。決定は「決定 1」と DEC-ORD-64〜72）。決定のレポート：[test3-testfirst-OrdTfReport-mvc-core-decisions.md](../spec/test3-testfirst-OrdTfReport-mvc-core-decisions.md)。
B層 16 メソッド・22 ケース（`Shared\WSServer_sample.Tests\`。net10.0 の `MVC_Sample_Core\MVC_Sample.Tests` と net48 の `MVC_Sample\MVC_Sample.Tests` で流す。既存と合わせて 325 ケース）・E2E 16 メソッド・20 ケース（`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfReportScreenTests`）。
Red のコミット：2bd5ee6。前の回の計画：[test3-testfirst-OrdTfShip-mvc-core.md](./test3-testfirst-OrdTfShip-mvc-core.md)。

## 使うスキル
`opentouryo-app-design`（設計事項の確認）・`opentouryo-layer-b`・`opentouryo-layer-d`・`opentouryo-dao-common`（`CmnDao`）・`opentouryo-query-definition`（SQL 定義の再利用）・`opentouryo-exception`（業務例外）・`opentouryo-layer-p-mvc`・`opentouryo-mvc-crud-screens`（MVC の画面の作り。フッタのボタンは `@section`）・`opentouryo-comment-convention`（ファイルヘッダ・XML doc・BOM）・`opentouryo-test-layer-b`・`opentouryo-test-e2e-web`（流し方）。

## 設計事項（`opentouryo-app-design`）
| 設計事項 | 決め |
| --- | --- |
| レイヤ分割 | P層（`OrdTfController.Report`）→ B層 `OrdTfBusiness`（`MethodName` = `OrdTfReport`。`DoBusinessLogicAsync(pv, ReadCommitted)`）→ D層（`CmnDao` ＋ 既存の `resource\Sql\OrdTfDetailHeader.sql`・`OrdTfDetailLines.sql`）。P層から D層を呼ばない。親クラス 1・2 は変えない |
| トランザクション・データアクセス | 読むだけ（`EV-ORD-501` 手順 8）。更新しない。分離レベルは `ReadCommitted`。**SQL は新しく足さない**（受注詳細の SQL を再利用。ヘッダは顧客も外部結合で読むが、宛先は出荷先情報だけを使い、顧客の値は使わない）。SQL はパラメタ化済み（`@OrderID`）で、文字列を連結しない |
| 例外・エラー | `ERR-NOTFOUND` は B層が既存の `MsgNotFound` で `BusinessApplicationException` を投げる → `ErrorFlag = true`・`ErrorMessage`。P層は `BackWithMessage`（一覧へ。メッセージはエラー）。呼び出し側で `catch` しない。`ERR-DB` は DEC-CMN-07（定めない）で基盤に任せる |
| 状態管理 | 一覧状態（Session の `OrdTfListState`）は**書き換えない**。`ERR-NOTFOUND` の後は既存の `BackWithMessage` → `Back` が保持された一覧状態で戻す（DEC-ORD-70）。帳票は GET だけで、サーバに状態を持たない |
| 画面から送られる値（引き継ぎ 4） | 画面から受け取るのは URL の受注 ID（`id`）と帳票種別（`type`）だけ。**B層は受注 ID で `Orders` を読み直し**、画面が送る値で業務の検証を外せない（hidden・Session は使わない）。`type` が `delivery`・`invoice` 以外のときは、表示の切替にだけ効く値なので納品書として扱う（DEC-ORD-71：定めない＝テストの期待にしない。DB の検証には関係しない）。`id` が数値でないときは、MVC のモデル バインドで 0 になり `ERR-NOTFOUND`（同じく定めない） |
| 画面設計 | ビュー `Views\OrdTf\Report.cshtml`。種別の切替は**クライアント側だけ**（`wwwroot\js\app\ordtf-report.js`。`#reportType` の `change` で、タイトルと `#rowShippedDate`・`#rowShipper`・`#rowDueDate` の `d-none` を切り替える。サーバへ要求を出さない＝`EV-ORD-502` 手順 3）。初期表示は、サーバが `type` に応じて同じ状態にする。印刷は `#btnPrint` の `onclick="window.print();"`（呼ぶ時点で `window.print` を引く）。**印刷の除外はこのビューの `HeaderScriptsSection` の `<style>`（`@media print`）**：`#reportType`・`#btnPrint`・`#btnBack`・`.page-header`・`.nav-side-menu`・`#footer-buttons` を `display: none !important`。**共通のレイアウト・共通の CSS は変えない**（テスト仕様 1.2。`QUALITY.md` 1 節） |
| メインボタン（引き継ぎ 1。規約 `cmn-framework.md`「画面共通」との整合） | 規約は、メインボタンを「機能トランザクションを起動するボタン（検索・登録・更新・削除など）」とし、画面の全部にコンテンツのフッタ部へ 5 つ置く。**この画面はダイアログではなく通常の画面**で、機能トランザクションを起動するボタンが無い（印刷は `window.print()` を呼ぶだけ、戻るは遷移だけ＝決定 1）。したがって、**フッタの 5 つ（`#btnMain1`〜`#btnMain5`）はすべて非活性（キャプションなし）で置き、印刷（`#btnPrint`）・戻る（`#btnBack`）はメインボタンに含めず画面定義書の配置（画面上部）に置く**。ダイアログ用に足された読み（閉じるは含めない）は、この画面には当たらない。フッタの 5 つは印刷の対象から除く（DEC-ORD-64）。規約の文は変えない |
| 認証・CSRF | コントローラ全体が `[Authorize(Cookie)]`。帳票は状態を変えない GET なので CSRF のトークンは要らない（`QUALITY.md` 4 節）。リダイレクトの行き先に利用者の入力を使わない（一覧は `Back` 固定） |
| メッセージ・書式 | 文言は仕様のまま。日付 `yyyy/MM/dd`・金額は千区切り＋小数 2 桁（小数第 3 位を `AwayFromZero` で四捨五入）・割引率は百分率（`0.##` ＋ `%`。DEC-ORD-68）・NULL は空文字・`InvariantCulture` |
| 設定・SQL 定義 | 構成ファイルは変えない。SQL は足さない |

## 実装の決定（振る舞いは変えない）
- 宛先の連結は、既存の `JoinAddress`（空の要素を詰めて、区切りは半角空白 `AddressSeparator`）を使う（DEC-ORD-65 は区切りを定めない。既存の住所の連結と揃える）。
- 明細の読み込み・金額の算出（受注詳細の手順 2〜3）は、`UOC_OrdTfDetail` から private メソッド `FillLines(rv, orderId, markDiscontinued)` に切り出して、`UOC_OrdTfReport` と共用する（受注詳細は付記あり、帳票は付記なし＝DEC-ORD-67。金額の算出の規則を 2 か所に書かない）。切り出しは振る舞いを変えない（受注詳細の既存のテストで確かめる）。
- 支払期限は `new DateTime(発行日の年, 月, 1).AddMonths(2).AddDays(-1)`（月末・閏年・年またぎを標準のカレンダーに任せる）。発行日は `(pv.IssueDate ?? DateTime.Today).Date`。
- 帳票の ViewModel `OrdTfReportViewModel`（`Data` ＝ B層の戻り値、`Type` ＝ `delivery`｜`invoice`）。
- 受注一覧のテスト仕様 `test3-testfirst-OrdTf-mvc-core-tests.md` の見出しが 9 列のまま（前の回で一覧に 10 列目「操作」を足した）。**この回は受注一覧に触れないので直さない**。触れることになったら、直すべきものとして申告する。

## 対象ファイル
- B層（`Shared\`。ケース1・3 と共用）
  - `Shared\Business\OrdTfBusiness.cs`（`UOC_OrdTfReport` の本実装、`FillLines` の切り出し）
  - `Shared\IfType\`（段 2 で追加済み。変えない）
  - `resource\Sql\`：足さない（既存の `OrdTfDetailHeader.sql`・`OrdTfDetailLines.sql` を再利用）
- P層（`MVC_Sample_Core\MVC_Sample\`）
  - `Controllers\OrdTfController.cs`（`Report`、`CallReport`）
  - `Models\ViewModels\OrdTfReportViewModel.cs`（新規）
  - `Views\OrdTf\Report.cshtml`（本実装。BOM 付き）・`Detail.cshtml`（`#btnDelivery`・`#btnInvoice` を足す）
  - `wwwroot\js\app\ordtf-report.js`（新規）
- 親クラス 1・2・`Startup`・`_Layout.cshtml`・共通の CSS（`site.css`）・テストのファイルは変えない。

## 手順
1. 計画（本書）。
2. B層：`FillLines` の切り出し → `UOC_OrdTfReport`。net10.0 の B層を全件。
3. P層：ViewModel → コントローラ → ビュー・script → 詳細のボタン。`dotnet build`（Core のビューはビルドでコンパイルされる）。
4. net10.0 の E2E を全件。**Red で手前の Expect で落ちていたケースが、後ろの期待まで通ることを、全件で確かめる**（引き継ぎ 3。落ちたら、テストの側の誤りかを見て、直さずに理由を書いて止まる）。
5. **net48 の B層**：Shared を共用するので、net10.0 の後に（同時にでなく）`MVC_Sample\MVC_Sample.Tests` を MSBuild で組み、`dotnet test --no-build` で全件。
6. **後始末の確認**：`Northwind2` の受注 830 件・明細 2155 件・`Customers` 91 件・`Products` 77 件、**既存の商品（ProductID 1〜77）の `UnitsInStock` の合計が 3119**、`Products.RowVersion` の合計が始めと同じ。テストのデータ（`Tf` 始まり・2198 年以降の受注）とプロセスが残っていないこと。
7. Red のコミット（2bd5ee6）からテストのファイルが変わっていないこと（`git diff 2bd5ee6 -- <テストの場所>` が空）。

## ケース1・ケース3 への影響
`Shared\` の B層は、`OrdTfReport` の追加と、`UOC_OrdTfDetail` の明細の読み込みの切り出し（振る舞いは変えない）。ケース1（MVC_Sample net48）・ケース3（WebForms）の P層は、この回は作らない。ケース1 のテストは net48 で `Shared\` の B層のテストを流すので、net48 の全件で確かめる（受注詳細の既存のテストが、切り出しの後も通ること）。
