# 計画：受注一覧 OrdTf（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

テスト仕様：[test3-testfirst-OrdTf-mvc-core-tests.md](../spec/test3-testfirst-OrdTf-mvc-core-tests.md)（NorthwindDbSampleSpec@d65135e）。
B層 82 件（`Shared\WSServer_sample.Tests\`。`MVC_Sample_Core\MVC_Sample.Tests` で net10.0、`MVC_Sample\MVC_Sample.Tests` で net48 で流す）と E2E 36 件（`MVC_Sample_Core\MVC_Sample.E2ETests\`）を通す。**テストは変えない。**
B層（`OrdTfBusiness`・SQL 定義・引数/戻り値クラス）は**ケース1 で作成済み。1〜7 章の範囲（EV-ORD-101・102・104）では作らない・触らない。** `EV-ORD-105`（一覧の並べ替え。テスト仕様 8 章）は、B層に不足があるため、下の「EV-ORD-105（一覧の並べ替え）」で B層（`Shared\`）も足す。
ケース1 の計画：[test3-testfirst-OrdTf-mvc.md](./test3-testfirst-OrdTf-mvc.md)（設計事項はそれと同じで、下は Core の違いだけを書く）。

## 使うスキル
`opentouryo-app-design`（設計事項の確認）・`opentouryo-layer-p-mvc`・`opentouryo-p-call-business`・`opentouryo-exception`・`opentouryo-comment-convention`（ヘッダ・BOM）。B層・D層は作らないので `opentouryo-layer-b`・`opentouryo-dao-*` は使わない。

## 設計事項（`opentouryo-app-design`。ケース1 と同じもの）
| 設計事項 | 決め |
| --- | --- |
| レイヤ分割 | P層（`OrdTfController`）→ B層 `new OrdTfBusiness().DoBusinessLogicAsync(pv, ReadCommitted)`（Web の既定＝直呼び。Core は非同期版。サンプルの `Crud1Controller` と同じ）。P層から D層は呼ばない |
| トランザクション・データアクセス | 読み取りだけ・B層のみ（ケース1 で作成済み）。分離レベルは B層のテストと同じ `ReadCommitted` |
| 例外・エラー | 検証違反は B層が `ErrorFlag = true`・`ErrorMessage`・`ErrorFields` で返す。P層は `catch` しない（`opentouryo-exception`）。表示先：形式違反は項目直下、関連チェックは共通メッセージ領域（`DEC-CMN-04`・`05`）。DB の失敗は基盤任せ（`ERR-DB` は `DEC-CMN-07`：定めない） |
| 状態管理（一覧状態の保持） | P層が hidden で持つ（最後に実行した検索の条件 5 つ＋現在ページ＋総ページ数）。Session は使わない（ケース1 と同じ実装の決定）。`DEC-ORD-03`：検証エラーでは更新しない。`DEC-ORD-04`：ページ移動は hidden の条件を使う |
| 画面設計 | `_Layout.cshtml` の `#contents` の中に `@RenderSection("FooterButtonsSection", required: false)`。画面が 5 つのメインボタンを `<button form="OrdTfForm">` で置く |
| 認証・CSRF | `[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]`。POST は `[ValidateAntiForgeryToken]`（`Html.BeginForm` が token を出す） |
| 国際化・書式 | 日付 `yyyy/MM/dd`・金額 `#,##0.00`（`CultureInfo.InvariantCulture`）、空値は空文字 |

## Core 固有の点（実装の決定。振る舞いは変えない）
1. **`ModelState` を消してから再描画する**：Core の `Html.HiddenFor`・`TextBoxFor`・`DropDownListFor` は、POST で受けた値（`ModelState`）をモデルの新しい値より優先する。ページ・保持条件を更新しても古い値が出るので、`ModelState.Clear()` してからビューを返す（`Display` 部品。ページ切替のケースが確かめる）。
2. **`@section` は `<form>` の外**：フッタのボタンは `@section FooterButtonsSection` に置き、`form="OrdTfForm"` で紐付ける（`cmn-framework.md`）。Enter での暗黙の送信が検索になるよう、フォームの先頭に見えない submit（`Search` へ送る）を置く。
3. **Core のビューは `dotnet build` 時にコンパイルされる**（net48 の `aspnet_compiler` は不要）。構文の誤りはビルドで分かる。BOM は付ける（`opentouryo-comment-convention`。MVC_Sample_Core の他のビューに合わせる）。
4. **名前空間・型**：`SelectListItem` は `Microsoft.AspNetCore.Mvc.Rendering`、`[Serializable]` は不要（Session に入れない）。`this.ControllerName`・`this.UserInfo` は `MyBaseMVControllerCore` が設定済み。
5. **Razor の落とし穴**：`全@Model.TotalCount件` は式にならない（`全@(Model.TotalCount)件` と括弧で囲む）。`disabled="@(!hasPrev)"` は false で属性ごと消え、true で `disabled="disabled"` になる（Core の Razor の仕様）。
6. 親クラス 1・2・`Startup`・B層（`Shared\`）・テストのファイルは編集しない。

## 対象ファイル（すべて `MVC_Sample_Core\MVC_Sample\`）
- `Controllers\OrdTfController.cs`（`Index` GET・`Search`・`Move` POST。PRG はしない。`Move` は `dir=first|prev|next|last`）
- `Models\ViewModels\OrdTfViewModel.cs`
- `Views\OrdTf\Index.cshtml`（フッタ ボタンの section・条件欄・一覧・ページ移動）
- `Views\Shared\_Layout.cshtml`（フッタ ボタンのセクションの差し込みと、メニューの「受注一覧」への導線）
- csproj は SDK 形式で `Controllers\`・`Views\` を自動で含むので変更しない。

## 手順
1. 計画（本書）。
2. P層：ViewModel → コントローラ → ビュー → `_Layout`。`dotnet build` で確かめる。
3. E2E 19 件を通す（`dotnet test MVC_Sample_Core\MVC_Sample.E2ETests`。アプリは空きポートで起動）。
4. B層 57 件（net10.0）。続けて、**net48 の B層テストを net10.0 の後に（同時にでなく）流して**、Shared を共用しても通ることを確かめる。
5. 後始末の確認（アプリのプロセス・DB のテスト データ）。

## EV-ORD-105（一覧の並べ替え。テスト仕様 8 章）

### 使うスキル
`opentouryo-app-design`・`opentouryo-layer-p-mvc`・`opentouryo-layer-b`・`opentouryo-query-definition`（ORDER BY の動的化）・`opentouryo-p-call-business`・`opentouryo-exception`・`opentouryo-comment-convention`（ヘッダ・BOM）。

### 設計事項（`opentouryo-app-design`）
| 設計事項 | 決め |
| --- | --- |
| レイヤ分割 | P層（`OrdTfController.Sort`）が列・方向を決め、B層 `OrdTfSearch` へ `SortColumn`・`SortDescending` で渡す。B層は状態を持たず、毎回「列・方向・ページ・抽出条件」を受けて返す。並びの決定（ORDER BY）は SQL 定義。P層から D層は呼ばない |
| トランザクション・データアクセス | 読み取りだけ（`ReadCommitted`）。トランザクション境界は変わらない |
| 例外・エラー | 並べ替えは検証が無い（`EV-ORD-105` 検証内容：なし）。`ERR-DB` は `DEC-CMN-07`（定めない） |
| 状態管理 | 並べ替えの列・方向は hidden（`PSortColumn`・`PSortDescending`）で持つ。Session は使わない（検索条件・ページと同じ）。**Core の `ModelState` は `Display` で消す**（消さないと hidden が前の値のまま出る） |
| 画面設計 | 並べ替え可の 7 列の `th` にボタンを 1 つ（`#sort` + 列のキー）。合計金額・状況の `th` には何も置かない。現在の並べ替え列・方向は `th` に `aria-sort` を付け、CSS（`::after`）で ▲▼ を見せる（`th` のテキストには入れない。`DEC-ORD-16`）。**見出しをまだ押していない（保持された並べ替えが無い）既定の並びのときは、受注日に ▼（降順）を出す**（`EV-ORD-101` 手順 3。表示だけの扱いで、hidden の `PSortColumn` とコントローラの反転の判定は変えない）。見出しのボタンには `form="OrdTfForm"` を付ける（テスト仕様 8.3） |
| 認証・CSRF | `Sort` は `[HttpPost]`・`[ValidateAntiForgeryToken]`（`Html.BeginForm` の token）。`Authorize` はコントローラ全体のまま |
| 国際化・書式 | 変更なし |

### 実装の決定（テスト仕様 8.1・8.3 のとおり。振る舞いは変えない）
1. **許可リストの ORDER BY**：`resource\Sql\OrdTfSearch.xml` の ORDER BY を `<SELECT name="SortKey">` の `CASE`（`<列>_ASC` / `<列>_DESC` の 14 値）で切り替える。列名は文字列で連結しない。`DEFAULT` は既定の並び（受注日の日付の降順、同日は受注 ID の降順）。
2. **許可リスト外のキーは既定の並びで返す**（どの `CASE` にも一致せず `DEFAULT`）。仕様の決定ではなく実装の決定で、ケースは作らない（テスト仕様 8.1）。
3. **`SortKey` は必ず設定する**：`SELECT` タグはパラメタが未設定だと何も出力しない（`DEFAULT` も出ない＝ORDER BY が空になり OFFSET が構文エラー）。列が空のときは、どの `CASE` にも一致しない値（`Default`）を設定する。
4. **最後の並べ替えキーは受注 ID**（降順）。並べ替えの列に同じ値が続いても、ページが重ならず抜けないため（`DEC-ORD-12`：同じ値の並びは定めない）。日付の列は日付の単位（`CAST(... AS date)`。`DEC-ORD-14`）。担当者名は `LastName`・`FirstName` の順で同じ方向（`DEC-ORD-15`）。
5. **SQL 定義の中にコメント（`--`）を書かない**：動的 SQL の組み立てで改行が詰められ、コメントが後ろの文を巻き込んで構文エラーになる（実測。ファイル先頭の 1 行コメントだけは SELECT の前なので無事）。説明は C# のコメントと本計画に書く。
6. **P層：`Sort` アクション**：`sort` が 7 つのキーのどれかなら、`PSortColumn` と同じなら方向を反転、違えば当該列の昇順、ページを 1 に戻す。保持された検索条件（`P*`）で検索し直す（入力欄だけ変えても最後に検索した条件。`DEC-ORD-04`）。7 つ以外のキーは何もせず、保持した状態のまま取り直す（ボタンが無いので来ない）。`Search`・`Move`・`Index` は保持した並べ替えをそのまま B層へ渡す（検索・ページ切替でも維持。`Index` は空＝既定）。検証エラー（`DEC-ORD-03`）の「直前の一覧」も、保持した並べ替えで取り直す。
7. **0 件**：並べ替えでも `ShowList` が「該当するデータがありません。」（情報）を出す（`DEC-ORD-17`）。

### ケース1・ケース3 への影響（変えない）
- `Shared\` の B層は共用。**`SortColumn` が空（既定値 null）のときの振る舞い＝既定の並びは変えない**（`SortKey = "Default"` で `DEFAULT`＝従来と同じ ORDER BY）。ケース1（net48 MVC）・ケース3（WebForms）は `SortColumn` を設定しないので、従来どおり。
- 確かめ：B層のテストを net10.0 → net48 の順に 82 件全件流す（既存の 57 件が通ること）。

### 対象ファイル
- B層（`Shared\`・`resource\`）：`Shared\Business\OrdTfBusiness.cs`（`SortKey` の設定）・`resource\Sql\OrdTfSearch.xml`（ORDER BY の `SELECT`/`CASE`）・`Shared\IfType\OrdTfParameterValue.cs`（段 2 で追加済み）。`OrdTfCount.xml` は変えない。
- P層（`MVC_Sample_Core\MVC_Sample\`）：`Controllers\OrdTfController.cs`（`Sort`・保持した並べ替えを渡す）・`Models\ViewModels\OrdTfViewModel.cs`（`PSortColumn`・`PSortDescending`）・`Views\OrdTf\Index.cshtml`（見出しのボタン・hidden・`aria-sort`・CSS）。

### 手順
1. 計画（本節）。
2. B層：SQL 定義 → `OrdTfBusiness`。B層のテスト（net10.0）で並びのケースを通す。
3. P層：ViewModel → コントローラ → ビュー。`dotnet build`（Core のビューはビルドでコンパイルされる）。
4. E2E 36 件（net10.0）。
5. B層 82 件を net10.0 → net48 の順に（同時にでなく）。
6. 壊す確かめ（テスト仕様 8.7 の (a)〜(h) と画面の壊し方）。受注 ID を外す壊し方 (d) は、並べ替えが実装された状態で流して記録する。
7. 後始末（アプリのプロセス・DB のテスト データ）。
