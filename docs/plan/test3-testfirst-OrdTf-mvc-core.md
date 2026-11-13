# 計画：受注一覧 OrdTf（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

テスト仕様：[test3-testfirst-OrdTf-mvc-core-tests.md](../spec/test3-testfirst-OrdTf-mvc-core-tests.md)（NorthwindDbSampleSpec@72e156e）。
B層 57 件（`Shared\WSServer_sample.Tests\`。`MVC_Sample_Core\MVC_Sample.Tests` で net10.0 で流す）と E2E 19 件（`MVC_Sample_Core\MVC_Sample.E2ETests\`）を通す。**テストは変えない。**
B層（`OrdTfBusiness`・SQL 定義・引数/戻り値クラス）は**ケース1 で作成済み。このケースでは作らない・触らない。**
ケース1 の計画：[test3-testfirst-OrdTf-mvc.md](./test3-testfirst-OrdTf-mvc.md)（設計事項はそれと同じで、下は Core の違いだけを書く）。

## 使うスキル
`opentouryo-app-design`（設計事項の確認）・`opentouryo-layer-p-mvc`・`opentouryo-p-call-business`・`opentouryo-exception`・`opentouryo-comment-convention`（ヘッダ・BOM）。B層・D層は作らないので `opentouryo-layer-b`・`opentouryo-dao-*` は使わない。

## 設計事項（`opentouryo-app-design`。ケース1 と同じもの）
| 設計事項 | 決め |
| --- | --- |
| レイヤ分割 | P層（`OrdTfController`）→ B層 `new OrdTfBusiness().DoBusinessLogicAsync(pv, ReadCommitted)`（Web の既定＝直呼び。Core は非同期版。サンプルの `Crud1Controller` と同じ）。P層から D層は呼ばない |
| トランザクション・データアクセス | 読み取りだけ・B層のみ（ケース1 で作成済み）。分離レベルは B層のテストと同じ `ReadCommitted` |
| 例外・エラー | 検証違反は B層が `ErrorFlag = true`・`ErrorMessage`・`ErrorFields` で返す。P層は `catch` しない（`opentouryo-exception`）。表示先：形式違反は項目直下、関連チェックは共通メッセージ領域（`DEC-CMN-04`・`05`） |
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
