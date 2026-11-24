# 計画：受注詳細（登録・編集）OrdTfEdit（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

テスト仕様：[test3-testfirst-OrdTfEdit-mvc-core-tests.md](../spec/test3-testfirst-OrdTfEdit-mvc-core-tests.md)（NorthwindDbSampleSpec@a724ca5。決定は DEC-ORD-29〜49）。
B層 106 ケースの追加（`Shared\WSServer_sample.Tests\`。net10.0 の `MVC_Sample_Core\MVC_Sample.Tests` と net48 の `MVC_Sample\MVC_Sample.Tests` で流す。既存と合わせて 221 ケース）と、E2E 42 ケースの追加（`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfEditScreenTests`。既存と合わせて 98 ケース）を通す。**テストは変えない。**
Red のコミット：8266d54（その後、テスト仕様の追随でテストを足したコミット：1dc07ff）。受注一覧の計画：[test3-testfirst-OrdTf-mvc-core.md](./test3-testfirst-OrdTf-mvc-core.md)、受注詳細の計画：[test3-testfirst-OrdTfDetail-mvc-core.md](./test3-testfirst-OrdTfDetail-mvc-core.md)（Core 固有の点〔`ModelState` を消す・`@section` は `<form>` の外・Razor の落とし穴〕はそれと同じ）。

## 使うスキル
`opentouryo-app-design`（設計事項の確認）・`opentouryo-layer-b`・`opentouryo-layer-d`・`opentouryo-dao-common`（`CmnDao`）・`opentouryo-query-definition`（SQL 定義）・`opentouryo-exception`（業務例外・ロールバック）・`opentouryo-transaction-control`・`opentouryo-layer-p-mvc`・`opentouryo-mvc-crud-screens`（MVC の一覧・詳細の作り。一覧＆更新の RowState バッチは使わない：明細は行ごとの適用＝OD-T6）・`opentouryo-batch-update`（Added・Modified・Deleted の考え方だけ参照）・`opentouryo-p-call-business`・`opentouryo-comment-convention`（ヘッダ・BOM）。

## 設計事項（`opentouryo-app-design`）
| 設計事項 | 決め |
| --- | --- |
| レイヤ分割 | P層（`OrdTfController` の `Edit`・`Save`・`EditStart`・`CustomerInfo`・`ProductInfo`・`Missing`）→ B層 `OrdTfBusiness`（`MethodName` = `OrdTfEditInit`・`OrdTfCustomer`・`OrdTfProduct`・`OrdTfSave`。`DoBusinessLogicAsync(pv, ReadCommitted)`）→ D層（`CmnDao` ＋ SQL 定義 `.sql`）。P層から D層は呼ばない。検証・金額（`decimal`）・廃番の判定・行ごとの適用は B層 |
| トランザクション・データアクセス | 保存は B層が 1 トランザクション（`DoBusinessLogic` の境界）。登録：受注の INSERT（`SELECT CAST(SCOPE_IDENTITY() AS int)` を `ExecSelectScalar`）→ 明細の INSERT。編集：受注の UPDATE（`RowVersion` 条件・`RowVersion = RowVersion + 1`）→ 0 件ならロールバックして受注の有無で ERR-CONFLICT・ERR-NOTFOUND → 明細を**削除 → 更新 → 挿入**の順に行ごとに適用（商品を入れ替えた行の主キーが衝突しない。全削除・再登録にしない）。SQL は静的 `.sql`（値はすべて `@パラメタ`。NULL は `DBNull`） |
| 検証の順 | **検証 → 存在の確認 → 更新**（EV-ORD-308 手順 1 → 4-2）。検証の違反は、受注が消されていても先に返る（`OrdTfSave_編集で消された受注でも検証の違反が先に返る`）。編集でも顧客・担当者・受注日は必須（DEC-ORD-48。ORD-T1 の「表示側は NULL を許容」は表示の決まり） |
| 例外・エラー | ERR-NOTFOUND・ERR-CONFLICT・出荷済（ERR-BIZ）・検証の違反は B層が `BusinessApplicationException`（メッセージ ID `ORDTF003`〜`ORDTF006`。文言は共通仕様 7 節・EventSpec）を投げる → フレームワークがロールバックし、`ErrorFlag = true` の戻り値にする。P層は `ErrorMessageID` で分類する（`catch` しない）。検証の違反は `ErrorFields`（キー）・`ErrorFieldMessages`（項目単位の文言。DEC-ORD-30＝文言は期待にしない）・`ErrorMessages`（関連チェック・個別。共通メッセージ領域） |
| 楽観排他 | 画面が `Orders.RowVersion` を hidden で保持し、更新の `WHERE` に付ける（共通仕様 9.1 節）。明細の `RowVersion` は更新のたびに +1、挿入は列に含めない（DEFAULT 1）。明細は親の `RowVersion` で判定する（OD-T6） |
| 状態管理 | 一覧状態は受注詳細の回の **Session**（キー `OrdTfListState`）をそのまま使う。**編集の画面は一覧状態を書き換えない**（読むだけ）。入力中の値・未保存の判定用の読み込み時の状態（`InitialSnapshot`）は画面の hidden で持つ（Session に持たない） |
| 画面設計 | `Views\OrdTf\Edit.cshtml`（登録・編集の共用。単一の `<form id="OrdTfEditForm">`）。フッタのメインボタンは保存（`#btnMain1`）だけ（`@section FooterButtonsSection`。`form="OrdTfEditForm"` で紐付ける）。明細行追加（`#btnAddLine`）・行削除・キャンセル（`#btnCancel`・画面下部）はメインボタンに含めない（1 回目の決定 4）。明細表は `table` を自前で生成し `tr` をループする |
| 画面遷移 | 詳細の「編集」（`#btnEdit`）→ `GET /OrdTf/EditStart?id=`（出荷日を `OrdTfDetail` の `CanEdit` で読み直す。出荷済なら詳細へ戻してエラー）→ `/OrdTf/Edit?mode=edit&id=`。保存の成功 → `/OrdTf/Detail?id=`（PRG。完了メッセージを TempData）。ERR-NOTFOUND → 保持された一覧状態の一覧（`BackWithMessage`）。出荷済（編集の読み込み）→ 詳細。キャンセル → 編集は詳細、登録は `/OrdTf/Back`。URL はテスト仕様 1.2 の約束どおり（`LocalRedirect("~/…?…")` で組む。`RedirectToAction` は `id` をパスに入れるので使わない） |
| 認証・CSRF | コントローラ全体が `[Authorize(Cookie)]`。保存は `[HttpPost]`・`[ValidateAntiForgeryToken]`（フォームに `@Html.AntiForgeryToken()`）。顧客・商品の取得は GET の JSON（状態を変えない） |
| 国際化・書式 | 日付 `yyyy/MM/dd`・金額 `0.00`（入力欄）／`#,##0.00`（表示。`InvariantCulture`、小数第 3 位は四捨五入）・割引率は `0.####` の数値・NULL は空文字。出荷先・要求納期・配送業者の空は NULL（DEC-ORD-36） |
| 設定・SQL 定義 | `resource\Sql\` に `.sql` を 15 足す（下の対象ファイル）。構成ファイルは変えない |

## 実装の決定（振る舞いは変えない）
1. **金額の再計算・行の追加削除・顧客と商品の反映は画面内の script（`Edit.cshtml`）**。B層のメソッドは 4 つのまま（テスト仕様 1.2「実装は自由」）。金額は `BigInt` の 10 進数（単価・割引率は 1e4 倍）で、明細金額は `価格 × 数量` を小数第 3 位で四捨五入（B層の `decimal` と同じ規則。価格を先に丸めない）。顧客・商品は `fetch`（GET の JSON）で B層（`OrdTfCustomer`・`OrdTfProduct`）を呼ぶ。見つからなければ `/OrdTf/Missing`（ERR-NOTFOUND の一覧へ。DEC-ORD-40）へ移る。
2. **数値・日付の入力欄は `type="text"`（`inputmode` を付ける）**。`type="number"` だと E2E の `abc` などの入力（`FillAsync`）が拒まれ、不正値の検証（`#FreightError`・`.line-*-error`）を確かめられない（段 2 の申告）。形式の検査は B層（保存）と script（入力の確定時）が行う。
3. **明細の行は、同じ名前の入力が行の順に並ぶ**（`line_product`・`line_price`・`line_discount`・`line_qty`・`line_orig*`・`line_stock`・`line_origName`）。行の追加・削除で添字を振り直さないため、モデル バインドでなく `Request.Form` から組み立てる（`ReadEditForm`）。読み込み時に存在した行の削除は `deleted` の hidden に商品 ID を足す。
4. **保存の「値が変更された行」は、画面が保持する読み込み時の値（`line_orig*`）との比較**で決める（B層。DB の今の値と比べない＝他の利用者の更新を巻き戻さない）。商品が変わった行は、変更前の行を削除して挿入する。
5. **ERR-NOTFOUND の判定は B層の `ErrorMessageID`**（`OrdTfBusiness` の `public const MsgId*`）。受注詳細の回は削除の後に詳細を読み直して分けたが、保存は B層の例外が受注の有無を区別して投げるので不要。
6. **登録の `SELECT CAST(SCOPE_IDENTITY() AS int)` は `ExecSelectScalar`**。数量の上限（smallint）・金額の上限（money）は DEC-ORD-38（定めない）で、DB の範囲を超えて落ちないための実装の上限だけを置く（違反は項目単位の違反）。
7. **SQL 定義に書くコメントは先頭の 1 行だけ**（受注詳細の計画と同じ）。
8. **編集の詳細へ戻したときの文言は、詳細の `ViewModel` の `Message` に載せる**（TempData `OrdTfDetailMessage`。`Detail` が読んで消す）。
9. **「読み込み時から存在する行」は、画面が送る値（`OriginalProductID` など `Original*`）でなく、DB で決める（DEC-ORD-49。決定 21＝行の扱いは定めない。ただし保存の検証を `Original*` を信じて外さない）**。
   - 編集の保存の最初に、保存のトランザクションの中で受注の今の明細の商品 ID を読む（`OrdTfDetailProducts.sql`）。**登録モードは `Original*` を無視**（読み込み時の行が無い）。
   - 各行の名乗り（`OriginalProductID`）は、DB に今ある商品 ID のときだけ認める（`ResolveOrigins`）。DB に無い・別の行が既に名乗った商品 ID の名乗りは認めず、**新規の行として扱う**（廃番の検査・挿入を行う）。商品が同じ行（変更の無い行・値だけ変えた行）の名乗りを先に、次に商品を変えた行を、先に来た順に認める。
   - 廃番の検査（OD-T3）は、認めた名乗りが商品と同じ行だけを対象外にする。重複（OD-T1）・存在（VAL-EXISTS）・必須・数値は、`Original*` を見ずに画面の入力だけで判定する。
   - **削除の印と、同じ商品を残す行が食い違う入力**（印と、変更の無い既存の行の両方）は、**印を無視して行を残す**（ORD-T5：明細が 0 行にならない。残す行の値を変えていれば UPDATE する）。商品を付け替えた行（変更前の商品が印の対象）は、印どおり削除してから挿入する。
   - 理由：どの扱い（エラー・無視・挿入）も DEC-ORD-49 は許す。利用者が見ている行が黙って消える・明細が 0 行になることがない扱いとして、「画面に残っている行を優先する」を選んだ。DB に無いのに名乗る行を挿入するのは、「読み込み時に存在しなかった行は挿入」（EV-ORD-308 手順 4-3）に沿う。名乗りを認めない結果の二重（名乗りの重複）は、後の行を新規として挿入し、主キーが衝突するときはシステム エラーでロールバックする（黙って壊れない）。
10. **検証エラーの再描画で、自動で入れた出荷先の値を引き継ぐ**（DEC-ORD-32）：登録モードで顧客が選ばれていれば、その顧客の既定の出荷先（`OrdTfCustomer` の戻り値）を `form` の `data-auto-*` に渡し、画面の `autoVals` の初期値にする。自動で入った出荷先を変えていなければ、顧客を選び直しても上書きの確認が出ない。

## 前の回から引き継いだ 2 点（`test3-testfirst-OrdTfDetail-mvc-core-decisions.md`「終わり 3」）
1. **一覧状態を Session に持つので、ブラウザの複数タブで共用になる**。
   - 決め：**この回も Session のまま**。登録・編集の画面からキャンセル（登録のみ。`/OrdTf/Back`）・ERR-NOTFOUND（`BackWithMessage` → `/OrdTf/Back`）・新規登録からの復帰で一覧へ戻るとき、**最後に一覧を操作したタブの状態**で戻る。編集の画面（`Edit`・`Save`）は一覧状態を**書き換えない**（読むだけ）ので、登録・編集に費やした時間で状態が変わらない。
   - 理由：仕様は複数タブを扱っておらず（F-2 は「直前の検索条件・ページ・並び順」＝画面ごとでなく利用者ごとの 1 つ）、タブごとの状態を持つ（URL・hidden に一覧状態を載せる）には、一覧から詳細・編集・キャンセルまで全部の遷移に状態を引き回す必要がある。サンプルの範囲では過剰で、複数タブの操作はテストの対象外（テスト仕様 5 章）。
   - 影響：タブ A で一覧を検索し、タブ B で別の条件に検索し直した後、タブ A の詳細・編集からキャンセルすると、タブ B の状態の一覧になる。利用者に見える差はこの 1 点だけ。
2. **ERR-CONFLICT の再表示と POST の再送（PRG に揃えるか）**。
   - 決め：**保存の POST の応答は、検証エラーを除いてすべて PRG（リダイレクト）にする**。
     - 成功 → `GET /OrdTf/Detail?id=`（完了メッセージは TempData）。
     - ERR-CONFLICT → `GET /OrdTf/Edit?mode=edit&id=` へリダイレクト（メッセージは TempData `OrdTfEditMessage`）。**編集の画面を DB から読み直す**ので、利用者の入力は捨てられ、保持する `RowVersion` も最新になる（DEC-ORD-34）。
     - ERR-NOTFOUND → `GET /OrdTf/Back`（メッセージは TempData）。
     - **検証エラーだけは POST の応答で再描画**する（入力値を保持して画面に留まる＝`ERR-VALIDATION`。PRG にすると入力値の持ち運び先〔TempData・Session〕が要る）。この応答を再読み込みすると POST の再送の確認が出るが、再送されるのは同じ入力の検証（書き込みを伴わない）だけで、二重登録にならない。
   - 理由：成功・ERR-CONFLICT の後は、再読み込みでブラウザが POST を再送すると、登録なら二重登録、編集なら古い `RowVersion` で再び ERR-CONFLICT になる。PRG なら再読み込みは GET を読み直すだけ。
   - テスト `保存_登録の後に詳細を再読み込みしても二重登録しない` との関係：成功が PRG（302 → 詳細の GET）なので、`page.ReloadAsync` は詳細の GET を読み直すだけで、K の受注は 1 件のまま。
   - テスト `保存_他の利用者が更新していたらERR_CONFLICTで最新を再表示する`：1 回目の保存の後、リダイレクト先の編集の画面が最新（送料 99・`ShipCity` は元の値）で出てメッセージが `text-danger`。2 回目の保存は最新の `RowVersion` で成功する。

## 対象ファイル
- B層・D層（`Shared\`・`resource\`。ケース1・3 と共用）
  - `Shared\Business\OrdTfBusiness.cs`（`UOC_OrdTfEditInit`・`UOC_OrdTfCustomer`・`UOC_OrdTfProduct`・`UOC_OrdTfSave`、検証・部品、メッセージの定数。`MsgId*` を `public const` に）
  - `Shared\IfType\OrdTfParameterValue.cs`・`OrdTfReturnValue.cs`（段 2 で追加済み。`ErrorFieldMessages` を追加）
  - `resource\Sql\`：`OrdTfShippers.sql`・`OrdTfProducts.sql`・`OrdTfEditHeader.sql`・`OrdTfEditLines.sql`・`OrdTfCustomer.sql`・`OrdTfProduct.sql`・`OrdTfExistsCustomer.sql`・`OrdTfExistsEmployee.sql`・`OrdTfExistsShipper.sql`・`OrdTfInsertOrder.sql`・`OrdTfUpdateOrder.sql`・`OrdTfInsertDetail.sql`・`OrdTfUpdateDetail.sql`・`OrdTfDeleteDetail.sql`・`OrdTfDetailProducts.sql`
- P層（`MVC_Sample_Core\MVC_Sample\`）
  - `Controllers\OrdTfController.cs`（`Edit`・`Save`・`EditStart`・`CustomerInfo`・`ProductInfo`・`Missing`・フォームの読み取り。`Detail` が TempData のメッセージを読む）
  - `Models\ViewModels\OrdTfEditViewModel.cs`（新規）
  - `Views\OrdTf\Edit.cshtml`（本実装。BOM 付き・CRLF）・`Detail.cshtml`（`#btnEdit` を `EditStart` へ）
- 親クラス 1・2・`Startup`・`_Layout.cshtml`・テストのファイルは変えない。

## 手順
1. 計画（本書）。
2. B層・D層：SQL 定義 → `OrdTfBusiness`。net10.0 の B層（221 ケース）を流す。
3. P層：ViewModel → コントローラ → ビュー。`dotnet build`（Core のビューはビルドでコンパイルされる）。
4. net10.0 の E2E を全件（98 ケース。アプリは空きポートで起動）。
5. **net48 の B層**：Shared を共用するので、net10.0 の後に（同時にでなく）`MVC_Sample\MVC_Sample.Tests` を MSBuild で組み、`dotnet test --no-build` で全件（221 ケース）。
6. **並行の確認**：net10.0 の B層と E2E を並行して流して干渉しないこと（日付の範囲が違う。保存で作られる受注は顧客 K の受注として後片付けで消える）。
7. **後始末の確認**：アプリのプロセスが残っていないこと、`Northwind2` の受注が **830 件**・明細 2155 件・`Customers` 91 件・`Products` 77 件で、`Tf` で始まるテストのデータが 0 件であること。
8. Red のコミット（8266d54）からテストのファイルが変わっていないこと（`git diff 8266d54 -- <場所>` が空）。

## ケース1・ケース3 への影響
`Shared\` の B層は追加だけ（`OrdTfEditInit`・`OrdTfCustomer`・`OrdTfProduct`・`OrdTfSave` と戻り値・引数のメンバ。`MsgId*` の可視性を `public` にした）。既存の `OrdTfInit`・`OrdTfSearch`・`OrdTfDetail`・`OrdTfDelete` は変えない。ケース1（net48 MVC）・ケース3（WebForms）は、この回は作らない。
