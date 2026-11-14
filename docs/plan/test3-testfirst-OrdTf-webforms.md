# 計画：受注一覧 OrdTf（ケース3：WebForms_Sample ＝ net48 ASP.NET Web Forms）

テスト仕様：[test3-testfirst-OrdTf-webforms-tests.md](../spec/test3-testfirst-OrdTf-webforms-tests.md)（NorthwindDbSampleSpec@2240ab3）。
E2E 19 件（`WebForms_Sample\WebForms_Sample.E2ETests\`）を通す。**テストは変えない。**
B層（`OrdTfBusiness`・SQL 定義・引数/戻り値クラス）は**ケース1 で作成済み。このケースでは作らない・触らない。**
ケース1 の計画：[test3-testfirst-OrdTf-mvc.md](./test3-testfirst-OrdTf-mvc.md)（設計事項はそれと同じで、下は Web Forms の違いだけを書く）。

## 使うスキル
`opentouryo-app-design`（設計事項の確認）・`opentouryo-layer-p-webforms-screen`（画面・マスタページ・csproj 登録・designer）・
`opentouryo-layer-p-webforms-event`（接頭辞による自動結線・UOC の名前）・`opentouryo-p-call-business`（B層の呼び出し）・
`opentouryo-exception`・`opentouryo-comment-convention`（ヘッダ・BOM）。B層・D層は作らないので `opentouryo-layer-b`・`opentouryo-dao-*` は使わない。

## 設計事項（`opentouryo-app-design`。ケース1 と同じもの）
| 設計事項 | 決め |
| --- | --- |
| レイヤ分割 | P層（`OrdTf.aspx.cs`）→ B層 `new OrdTfBusiness().DoBusinessLogic(pv, ReadCommitted)` 直呼び（Web の既定。`WebForms_Sample.csproj` が `WSServer_sample`・`WSIFType_sample` を既に `ProjectReference`）。P層から D層は呼ばない |
| トランザクション・データアクセス | 読み取りだけ・B層のみ（ケース1 で作成済み）。分離レベルは B層のテストと同じ `ReadCommitted` |
| 引数クラス | `new OrdTfParameterValue(this.ContentPageFileNoEx, "-" または fxEventArgs.ButtonID, "OrdTfInit" / "OrdTfSearch", "SQL", this.UserInfo)`（`opentouryo-p-call-business`。メソッド名は文字列で明示） |
| 例外・エラー | 検証違反は B層が `ErrorFlag = true`・`ErrorMessage`・`ErrorFields` で返す。P層は `catch` しない。表示先：形式違反は項目直下、関連チェックは共通メッセージ領域（`DEC-CMN-04`・`05`）。**`ERR-DB`（DB への接続・実行の失敗）は、`DEC-CMN-07`（定めない）により、基盤のエラー処理に任せる**：「DB 接続エラー」は業務続行不可能なエラーとして、基盤の異常終了の処理（`UOC_ABEND`）に任せ、`OrdTf.aspx.cs` は例外を扱わない。`ERR-DB` の文言の表示と画面の状態を変えないことは、この範囲では求めず、テストの期待にしない |
| 国際化・書式 | 日付 `yyyy/MM/dd`・金額 `#,##0.00`（`CultureInfo.InvariantCulture`）、空値は空文字（P層で文字列にしてから GridView へ） |

## 実装の決定（作り方だけが変わること。仕様の振る舞いは変えない）

1. **状態管理（一覧状態の保持）：ViewState**。最後に実行した検索の条件 5 つ＋現在ページ＋総ページ数をページの `ViewState` に持つ（Session・hidden は使わない。ケース1・2 は hidden。タブごとに独立し、Session タイムアウトの影響が無い点は同じ）。`DEC-ORD-04`：ページ移動は保持した条件を使う。`DEC-ORD-03`：検証エラーでは保持を更新しない。
2. **id の出し方：`ClientIDMode="Static"`**。画面の id（テスト仕様 2 章）のまま出すため、入力欄・一覧・メッセージ・ボタンの各サーバ コントロールに `ClientIDMode="Static"` を付け、`ID` を表の id にする（`CustomerID`・`OrderDateFrom`・`messageArea`・`orderList` など）。
   - 入力欄（`DropDownList`・`TextBox`）・一覧（`GridView`）の `ID` には**接頭辞（`ddl`・`txt`・`gvw` など）を付けない**。接頭辞は自動結線の機能で、付けると不要な `SelectedIndexChanged`・`TextChanged`・`RowCommand` が結線される。この画面は入力欄の変更・行のイベントを使わない。
   - ボタンは `btn` を付ける（`btnMain1`〜`btnMain5`・`btnPageFirst/Prev/Next/Last`。`ID` が表の id と一致する）。`WebCustomButton`（`cc1`）を使う。
3. **GridView の出力**（テスト仕様 2 章の決め）：
   - `UseAccessibleHeader="true"` にし、バインド後に `HeaderRow.TableSection = TableRowSection.TableHeader` で見出しの行を `thead` に出す（既定は `thead` を出さず、見出しも `tr`）。
   - `EmptyDataText`・`EmptyDataTemplate` を指定しない（0 件のとき行・表を出さない。`#orderList tbody tr` が 0 になる。0 件の知らせは `#messageArea`）。
   - `AllowPaging`・`AllowSorting` は使わない。毎回 B層から取り直してバインドする（`EnableViewState="false"`。行は ViewState に持たない）。
   - 行の値は、P層で表示の書式の文字列にした `OrdTfListRow`（受注 ID・日付 `yyyy/MM/dd`・金額 `#,##0.00`・空は空文字）を `BoundField` にバインドする。納期遅延の行は、バインド後に `Rows[i].CssClass = "table-danger"` を付ける（他の行は class を持たない）。
4. **マスタページ：新しいマスタ `footerScreen.master`**（`BaseMasterController` 派生）を作る。既存の `sampleScreen.master` は、サンプルのボタン・ラベル・ユーザ コントロール（共通イベント エリアなど）を持つので、持ち込まない（`id` の衝突を避ける）。
   - ヘッダ・左メニュー・コンテンツ（`#contents`）の構成と Fx 隠しフィールド一式・`ScriptManager`・バンドル（`~/bundles/touryo`・`~/bundles/app`）は `sampleScreen.master` から写す。左メニューは「メニュートップ」「受注一覧」。
   - **フッタのメイン 5 ボタンは、マスタの `#contents` の中のコンテンツ プレースホルダの後ろに置く**（`btnMain1`〜`btnMain5`。`cmn-framework.md` の WebForms：マスタにレイアウト、画面ごとの制御は各ページの初期処理）。既定値は `.master` の宣言（`Text="－" Enabled="false"`）で与え、マスタの `Page_Load` では初期化しない（スキル：マスタの `Page_Load` はコンテンツの `UOC_FormInit` より後で、上書きしてしまう）。
   - `<form defaultbutton="btnMain1">`：入力欄で Enter を押したときの暗黙の送信が検索になる（ページ移動のボタンが受けない。ケース1・2 の見えない submit と同じ目的）。
   - マスタ上の `btnMain1` のハンドラは、マスタのファイル名が接頭辞になる：`UOC_footerScreen_btnMain1_Click`（画面コードクラスに実装）。
   - 画面（`OrdTf.aspx.cs`）が `UOC_FormInit`・`UOC_FormInit_PostBack` の両方で `GetMasterWebControl` から検索ボタンのキャプション「検索」・活性を決め、`btnMain2`〜`btnMain5` は非活性（非表示にしない）。
5. **画面遷移の定義（`SCDefinition.xml`）には登録しない**。`app.config` は `FxScreenTransitionMode = off` で、画面遷移のチェックは走らない（`FxScreenTransitionCheck = on` でも主スイッチが off）。この画面は遷移しない（`UOC_*` は `string.Empty` を返す）。主スイッチを有効にするなら `directLink="allow"` で登録する（`opentouryo-screen-transition`）。
6. **URL・名前**：`~/Aspx/OrdTf/OrdTf.aspx`、名前空間 `WebForms_Sample.Aspx.OrdTf`、画面コードクラス `OrdTf`（`MyBaseController` 派生）。コードビハインドは `OrdTfBusiness` の `MethodName`（`OrdTfInit`・`OrdTfSearch`）だけを呼ぶ。
7. **ログイン・認証**：既存の Forms 認証（`web.config`）のまま。`menu.aspx` と `footerScreen.master` のメニューに「受注一覧」への導線を足す。
8. **ビューの検証**：`.aspx`・`.master` は msbuild では検証されないので、`aspnet_compiler` で確かめる。新規の `.cs`・`.aspx`・`.master` は UTF-8 BOM を付ける。
9. 親クラス 1・2・B層（`Shared\`）・テストのファイルは編集しない。

## 対象ファイル（すべて `WebForms_Sample\WebForms_Sample\`）
- `Aspx\OrdTf\OrdTf.aspx`・`OrdTf.aspx.cs`・`OrdTf.aspx.designer.cs`（新規）
- `Aspx\Common\Master\footerScreen.master`・`footerScreen.master.cs`・`footerScreen.master.designer.cs`（新規）
- `Aspx\start\menu.aspx`（「受注一覧」への導線）
- `WebForms_Sample.csproj`（`Content`・`Compile`（`DependentUpon`・`SubType`）の追加）

## 手順
1. 計画（本書）。
2. マスタページ → 画面（`.aspx`・コードビハインド・designer）→ csproj → メニュー。
3. Web を MSBuild でビルドし、`aspnet_compiler` で `.aspx`・`.master` を検証する。
4. E2E 19 件を通す（`dotnet test WebForms_Sample\WebForms_Sample.E2ETests`。アプリは空きポートで起動）。
5. 実装を 1 か所ずつ一時的に壊して、落ちるケースと落ち方を見て戻す（戻したファイルの更新時刻を今にしてから流す）。後始末の確認（アプリのプロセス・DB のテスト データ）。
- B層は触らないので、B層のテストは流さない（B層に変更が入ったら、net48 の `MVC_Sample.Tests` と net10.0 の `MVC_Sample_Core\MVC_Sample.Tests` を順に流す）。
