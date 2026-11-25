# 計画：出荷ステータス更新ダイアログ OrdTfShip（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

テスト仕様：[test3-testfirst-OrdTfShip-mvc-core-tests.md](../spec/test3-testfirst-OrdTfShip-mvc-core-tests.md)（NorthwindDbSampleSpec@2962688。決定は「決定 1」と DEC-ORD-50〜63）。決定のレポート：[test3-testfirst-OrdTfShip-mvc-core-decisions.md](../spec/test3-testfirst-OrdTfShip-mvc-core-decisions.md)。
B層 60 メソッド（`Shared\WSServer_sample.Tests\`。net10.0 の `MVC_Sample_Core\MVC_Sample.Tests` と net48 の `MVC_Sample\MVC_Sample.Tests` で流す。既存と合わせて 303 ケース）・E2E 40 メソッド（`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfShipScreenTests`）。
Red のコミット：4d30d17。受注詳細（登録・編集）の計画：[test3-testfirst-OrdTfEdit-mvc-core.md](./test3-testfirst-OrdTfEdit-mvc-core.md)。

## 使うスキル
`opentouryo-app-design`（設計事項の確認）・`opentouryo-layer-b`・`opentouryo-layer-d`・`opentouryo-dao-common`（`CmnDao`）・`opentouryo-query-definition`（SQL 定義）・`opentouryo-exception`（業務例外）・`opentouryo-transaction-control`（B層が 1 トランザクション）・`opentouryo-layer-p-mvc`・`opentouryo-mvc-crud-screens`（`<form>`・`AntiForgeryToken`・ボタンの `form` 属性・JavaScript のダイアログ）。

## 設計事項（`opentouryo-app-design`）
| 設計事項 | 決め |
| --- | --- |
| レイヤ分割 | P層（`OrdTfController` の `Ship`・`ShipSave`・`ShipCancel`）→ B層 `OrdTfBusiness`（`MethodName` = `OrdTfShipInit`・`OrdTfShipSave`・`OrdTfShipCancel`。`DoBusinessLogicAsync(pv, ReadCommitted)`）→ D層（`CmnDao` ＋ `resource\Sql\` の SQL 定義）。P層から D層を呼ばない。親クラス 1・2 は変えない |
| トランザクション・データアクセス | 確定・取消は B層が 1 トランザクション（`DoBusinessLogic` の境界。業務例外でロールバック）。順は、**受注を `UPDLOCK` で取得（`OrderID`＋`RowVersion`）→ 在庫の検査・更新 → 受注の更新（`RowVersion + 1`）**（`EV-ORD-402` 手順 3〜6）。**在庫は `UnitsInStock = UnitsInStock ± @Quantity` の SQL の加減算で更新し（読んだ値で書き戻さない）、`Products.RowVersion` を +1 する（DEC-ORD-59。条件は `ProductID` の一致だけ）**。引き落としは `UnitsInStock IS NULL OR UnitsInStock >= @Quantity` の条件付き更新で、検査の後に他の要求が在庫を変えても負にしない（0 件なら在庫不足の `ERR-BIZ`）。パラメタ化し、文字列で連結しない |
| 検証の順 | **検証 → 受注の取得（`ERR-CONFLICT`・`ERR-NOTFOUND`）→ 更新**（手順 1 → 3）。検証は全項目をまとめて行い、違反は受注が無い・行バージョンが違うときも先に返る。`受注日 <= 出荷日` は、受注があり受注日が NULL でないときだけ比べる（DEC-ORD-63：定めない）。出荷日は `yyyy/MM/dd` の存在する日付だけ・空白だけは未指定（DEC-ORD-56）、未来の日付も受け付ける（DEC-ORD-58）。送料は空を 0（DEC-ORD-01）、0 以上の数値 |
| 例外・エラー | `ERR-NOTFOUND`・`ERR-CONFLICT`・在庫不足・未出荷の取消（`ERR-BIZ`）・検証の違反は B層が `BusinessApplicationException`（メッセージ ID `ORDTF003`〜`ORDTF008`。文言は仕様のとおり）を投げる → フレームワークがロールバックして `ErrorFlag = true`・`ErrorMessage` に変換する。呼び出し側で `catch` しない。検証の違反は `ErrorFields`・`ErrorFieldMessages`（項目単位。出荷日の受注日以前の文言も同じ。DEC-ORD-57）。在庫不足は全商品名を列挙（区切りは「、」。並びと区切りは定めない＝DEC-ORD-61）。**在庫が NULL の商品**は、不足にしない。在庫は NULL のまま、`Products.RowVersion` だけ +1 する（DEC-ORD-62 は定めない。実装の選択で、テストにしない。取消の加算も同じ） |
| 楽観排他と、画面から送られる値（引き継ぎ 1） | ダイアログが `Orders.RowVersion` を hidden で保持し、確定・取消の `WHERE` に付ける（共通仕様 9.1 節。更新は `RowVersion = RowVersion + 1`）。**画面から送られる「表示時点の出荷済／未出荷の別」（`displayedShipped`）は B層に渡すだけで、B層は使わない。在庫を引き落とすかは、ロックして取得した受注の `ShippedDate`（DB）で決める（DEC-ORD-60。DEC-ORD-49 の考え方）**。画面の主張で在庫の検査・引き落としを省かせない・二重に引かせない。受注日・配送業者の存在・在庫の検査も DB で行い、画面が送る値を信じない |
| 状態管理（引き継ぎ 2） | 一覧状態は受注一覧・詳細の回の **Session**（キー `OrdTfListState`）をそのまま使う。**この回の画面は一覧状態を書き換えない**（一覧は `GET /OrdTf/Back` が読むだけ。ダイアログのアクションは Session に触れない）。ダイアログの入力・`RowVersion` は、ダイアログの `<form>` の hidden／入力欄に持つ（Session に持たない）。呼び出し元（`from`＝`list`／`detail`）も hidden |
| 画面設計（実装の決定。重ねる作り） | ダイアログは**部分ビュー `Views\OrdTf\Ship.cshtml`**（レイアウトなし。`#shipDialog`＝画面全体を覆うオーバーレイ＋中身）。**元画面の script（`wwwroot\js\app\ordtf-ship.js`。一覧・詳細だけが読む）が `fetch` で部分ビューを取り、`body` 直下の `#shipHost` に差し込む**（元画面の URL は変わらず、元画面の DOM が残る＝F-5。入力欄は開くまで `#contents` に置かない＝テスト仕様 1.2・2.1・2.3 の条件。既存の詳細の照会モードの確かめ〔`#contents` の入力欄が 0〕を変えない）。ダイアログの id は `ship` で始め、元画面（`#shipName` など）と重ねない。ボタンは決定 1：ダイアログ下部に専用の 5 つ（`#shipMain1` 出荷確定／出荷情報を更新、`#shipMain2` 出荷取消、`#shipMain3`〜`5` は非活性）と `#btnShipClose`。元画面のフッタは変えない |
| 画面遷移 | 一覧の各行 `button.btn-ship`（`data-ship-url`＝`/OrdTf/Ship?id=&from=list`）・詳細の `#btnShip`（`from=detail`）→ ダイアログ。**確定・取消の POST は JSON を返す**：成功・`ERR-NOTFOUND` は `{ redirect }`（script が `location.href`）。成功は元画面を再読み込み：一覧は `GET /OrdTf/Back`（保持された一覧状態＝検索条件・ページ・並べ替えで再検索。ページが無ければ B層が最終ページ。DEC-ORD-51）・詳細は `GET /OrdTf/Detail?id=`（`EV-ORD-201`。保持する `RowVersion` も最新）。完了メッセージ（情報）は `TempData`（一覧は既存の `OrdTfMessage`、詳細は `OrdTfDetailMessage`）。`ERR-NOTFOUND` は呼び出し元によらず `Back`（保持された一覧状態の一覧）＋エラー（DEC-ORD-52）。**検証の違反・`ERR-BIZ`・`ERR-CONFLICT` は部分ビューの HTML を返し、ダイアログの中身を差し替える**：検証の違反と `ERR-BIZ` は入力値・保持した `RowVersion` を残し（DEC-ORD-54）、`ERR-CONFLICT` は `OrdTfShipInit` を呼び直して DB の最新で再表示する（入力は捨てる。`RowVersion`・モード・ボタンの表示名・活性・内訳も最新。DEC-ORD-53）。閉じる（`EV-ORD-404`）は script だけ：入力を破棄し `Back`／`Detail` へ移る（変更がなくても再読み込み。DB は更新しない） |
| 認証・CSRF | コントローラ全体が `[Authorize(Cookie)]`。確定・取消は `[HttpPost]`・`[ValidateAntiForgeryToken]`（ダイアログの `<form>` に `@Html.AntiForgeryToken()`。script が `FormData` で送る）。ダイアログの表示（`GET Ship`）は状態を変えない |
| メッセージ・確認 | 取消の確認は script の `window.confirm`（「出荷を取り消し、在庫を戻します。よろしいですか？」）。確定には確認を出さない。完了メッセージは「受注 ID nnn の出荷情報を更新しました。」「受注 ID nnn の出荷を取り消しました。」（情報＝DEC-ORD-55）。ダイアログに留まるエラーは `#shipMessage`（`text-danger`）。項目単位の違反は項目の直下（`#shipShippedDateError` など。`#shipMessage` に出さない＝DEC-CMN-05） |
| 国際化・書式 | 日付 `yyyy/MM/dd`・送料 `0.00`（入力欄）・NULL は空文字・`InvariantCulture`。出荷日は日付だけ（時刻を持たない。DEC-ORD-27） |
| 設定・SQL 定義 | `resource\Sql\` に `.sql` を 7 足す（下の対象ファイル）。構成ファイルは変えない |

## 実装の決定（振る舞いは変えない）
- ダイアログを重ねる作り：`fetch` ＋ 部分ビュー ＋ `body` 直下の差し込み先（上の画面設計）。`<dialog>`・iframe にしない（ビューのコンパイルと CSS の影響を避ける）。
- 在庫の更新は、検査（SELECT）と条件付き更新（UPDATE）の二段にする。検査は全不足商品名を列挙するため。更新は `ProductID` の昇順（同じ商品を逆順に更新する要求とのデッドロックを避ける）。
- 受注の取得は `WITH (UPDLOCK, ROWLOCK)` で、同じ受注の確定・取消の同時実行を直列にする（二重に在庫を引かない・戻さない）。
- 受注日の検証のための受注の取得（`OrdTfShipHeader.sql`）は、`OrdTfShipInit` と共用する（顧客は外部結合）。

## 対象ファイル
- B層・D層（`Shared\`・`resource\`。ケース1・3 と共用）
  - `Shared\Business\OrdTfBusiness.cs`（`UOC_OrdTfShipInit`・`UOC_OrdTfShipSave`・`UOC_OrdTfShipCancel`、`ReadShipHeader`・`ReadShipLines`・`LockShipOrder`・`ThrowConflictOrNotFound`・`DecreaseStock`、メッセージ・項目名の定数。`MsgNotFound`・`MsgConflict`・`MsgShort`・`MsgNotShipped` は `public const`）
  - `Shared\IfType\OrdTfParameterValue.cs`・`OrdTfReturnValue.cs`（段 2 で追加済み）
  - `resource\Sql\`（新規 7）：`OrdTfShipHeader.sql`・`OrdTfShipStockLines.sql`・`OrdTfShipLockOrder.sql`・`OrdTfShipUpdateOrder.sql`・`OrdTfShipClearOrder.sql`・`OrdTfShipDecreaseStock.sql`・`OrdTfShipIncreaseStock.sql`（ASCII。既存の `OrdTfShippers.sql`・`OrdTfExistsShipper.sql`・`OrdTfOrderCount.sql` を再利用）
- P層（`MVC_Sample_Core\MVC_Sample\`）
  - `Controllers\OrdTfController.cs`（`Ship`・`ShipSave`・`ShipCancel`、`CallShipInit`・`BuildShipModel`・`ShipDone`・`ShipNotFound`）
  - `Models\ViewModels\OrdTfShipViewModel.cs`（新規）
  - `Views\OrdTf\Ship.cshtml`（本実装。BOM 付き。部分ビュー）・`Index.cshtml`（「操作」列・`button.btn-ship`・script の読み込み）・`Detail.cshtml`（`#btnShip`・script の読み込み）
  - `wwwroot\js\app\ordtf-ship.js`（新規）
- 親クラス 1・2・`Startup`・`_Layout.cshtml`・テストのファイルは変えない。

## 手順
1. 計画（本書）。
2. B層・D層：SQL 定義 → `OrdTfBusiness`。net10.0 の B層を全件。
3. P層：ViewModel → コントローラ → 部分ビュー・script → 一覧・詳細。`dotnet build`（Core のビューはビルドでコンパイルされる）。
4. net10.0 の E2E を全件。
5. **net48 の B層**：Shared を共用するので、net10.0 の後に（同時にでなく）`MVC_Sample\MVC_Sample.Tests` を MSBuild で組み、`dotnet test --no-build` で全件。
6. **後始末の確認**：`Northwind2` の受注 830 件・明細 2155 件・`Customers` 91 件・`Products` 77 件、**既存の商品（ProductID 1〜77）の `UnitsInStock` の合計が 3119**、`Products.RowVersion` の合計が始めと同じ。
7. Red のコミット（4d30d17）からテストのファイルが変わっていないこと（`git diff 4d30d17 -- <場所>` が空）。

## ケース1・ケース3 への影響
`Shared\` の B層は追加だけ（`OrdTfShipInit`・`OrdTfShipSave`・`OrdTfShipCancel`）。ケース1（MVC_Sample net48）・ケース3（WebForms）の P層は、この回は作らない。ケース1 のテストは net48 で `Shared\` の B層のテストを流すので、net48 の全件で確かめる。
