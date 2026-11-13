# テスト仕様：受注一覧 OrdTf（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

起こした仕様の版：NorthwindDbSampleSpec@72e156e（main の先頭。本文 `HLD/Common.md`・`LLD/EventSpec/Orders.md`・`LLD/UI_ElementsAndEventList/Orders.md`・`LLD/TableSchema/Orders.md` と、決定記録 `LLD/Decisions/Common.md`・`LLD/Decisions/Orders.md` を合わせて読んだ。ケース1 と同じ版）
規約：`docs/spec/cmn-framework.md` の「画面共通」「MVC」（VerifyCaTddForOT@aac8bc1）、`docs/spec/cmn-testfirst.md`、`docs/spec/test3-testfirst-OrdTf.md`、`docs/plan/cmn-testfirst.md` の「B層を共用する構成」
ケース1 のテスト仕様：[test3-testfirst-OrdTf-mvc-tests.md](./test3-testfirst-OrdTf-mvc-tests.md)（以下「ケース1」。VerifyCaTddForOT@c87426b で承認・実装済み）。決定のレポート：[test3-testfirst-OrdTf-mvc-decisions.md](./test3-testfirst-OrdTf-mvc-decisions.md)

対象：`SC-ORD-01`（`I01`〜`I08`、`B01`、`B05`）、`EV-ORD-101`・`EV-ORD-102`・`EV-ORD-104`。範囲外は対象の仕様のとおり（`EV-ORD-103`・`105`〜`108`、新規登録、出荷更新、詳細への遷移、一覧状態の復元）。

## 0. 進め方の前提

- **B層（`OrdTfBusiness`・`OrdTfParameterValue`・`OrdTfReturnValue`・SQL 定義）とそのテスト（`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`）は、ケース1 で作成済み。このケースでは B層を作らず、テストのコードも足さない。**
  B層の契約（引数クラス・戻り値クラス・`MethodName`・分離レベル `ReadCommitted`・「当日」は `DateTime.Today`）はケース1 の 1 章のとおり。
- このケースで作るのは次の 3 つだけ。
  1. **net10.0 の `MVC_Sample.Tests`**：`Shared\WSServer_sample.Tests\` をリンクして、B層のテストを net10.0 で流す（`opentouryo-test-layer-b`）。
  2. **P層**（コントローラ・ViewModel・ビュー・レイアウトの差し込み。段 4）。
  3. **net10.0 の `MVC_Sample.E2ETests`**（`OrdTfScreenTests`。`opentouryo-test-e2e-web`）。
- B層の不足がP層の作業中に見つかったら、ケース1 のテスト仕様と `Shared\WSServer_sample.Tests\` に足し（レビューを通し）、net48・net10.0 の両方で B層のテストを流す。
- 画面は MVC_Sample_Core の既存のログイン（`/Home/Login`。利用者名 `#UserName` を入れて `input[name=normal]` を押す。パスワードは要らない。成功してもリダイレクトせず同じ画面に戻るが、認証クッキーは付く）の後に、`/OrdTf`（`OrdTfController`・`Index`）で開く。
- 表示のメッセージ・文言は仕様の文言そのまま（`DEC-CMN-01`：メッセージ ID は実装で決め、テストは文言で確かめる）。

## 1. テストのプロジェクト（実装の決定。テストの境界の形）

| 項目 | 値 |
| --- | --- |
| 置き場所 | Web のプロジェクト（`MVC_Sample_Core\MVC_Sample\`）と**同じ階層**：`MVC_Sample_Core\MVC_Sample.Tests\`・`MVC_Sample_Core\MVC_Sample.E2ETests\`（`opentouryo-test-layer-b` の決まり。net48 の `MVC_Sample\MVC_Sample.Tests` と同じ並び）。名前は net48 と同じ（フォルダが別なので衝突しない） |
| `MVC_Sample.Tests`（B層） | SDK 形式・`net10.0`。`Microsoft.NET.Test.Sdk` 18.10.1・`NUnit` 4.6.1・`NUnit3TestAdapter` 6.3.0。`OpenTouryo.Business/Framework/Public` を `..\..\OpenTouryoAssemblies\Build_netcore100\net10.0\` から `Reference`。`ProjectReference` は Web（`..\MVC_Sample\MVC_Sample.csproj`）と `WS_sample_Core` の `WSIFType_sample`・`WSServer_sample`。`..\..\Shared\WSServer_sample.Tests\**\*.cs` を `Link` で取り込む。`TestSetup.cs`（`[SetUpFixture]`。Web の `appsettings.json` を `GetConfigParameter.InitConfiguration` に渡す。名前空間は B層のテストと同じ `WSServer_sample.Tests`。`[SetUpFixture]` は同じ名前空間のテストにだけ効くため）を置く。`MVC_Sample_Core\MVC_Sample.sln` に `dotnet sln add` する |
| `MVC_Sample.E2ETests` | SDK 形式・`net10.0`。`Microsoft.Playwright` 1.63.0・`Microsoft.Data.SqlClient` 7.0.0・`System.Security.Cryptography.Pkcs` 10.0.13（Web の csproj と同じ版）。Web・OpenTouryo は参照しない。名前空間 `MVC_Sample.E2ETests`。sln に `dotnet sln add` する |
| アプリの起動 | `AppHost`（`[SetUpFixture]`）が `dotnet run --no-launch-profile --urls http://127.0.0.1:<空きポート>`（作業ディレクトリ = Web のフォルダ。`ASPNETCORE_ENVIRONMENT=Development`。`OT_RESOURCE_ROOT` は引き継ぐ）で起動し、`/Ping`（認証不要・200）を待つ。固定のポートは使わない（net48 の E2E の 8090 と衝突しない）。終了時は `Kill(entireProcessTree: true)` |
| ブラウザ | Edge のチャネル・ヘッドレス |
| テストのデータ | `TestData`（E2E 側）はケース1 と同じ中身。接続文字列だけ、Web の `appsettings.json` の `connectionStrings:ConnectionString_SQL`（コメント付きの JSON）から読む。B層のテストの `TestData` は共用（`GetConfigParameter`） |
| `OrdTfScreenTests` | ケース1 の E2E と**同じケース・同じ id**（画面の仕様が同じため）。ログインとアプリの起動だけが違う。テストのコードはコピーして別ファイルとする（共用にしない：フレームワークごとに id・操作が変わりうる） |
| 日付の範囲 | B層のテスト 2199 年・E2E 2198 年（ケース1 と同じ） |
| 並行実行 | net48 と net10.0 の**B層のテストを同時に流さない**（「条件なしは全件の先頭ページ」の `2199/12/30` の行どうしが先頭を取り合う）。順に流す。E2E どうし・B層と E2E は並行してよい（日付の範囲が違う） |

画面の id・class は**ケース1 の 4.1 のとおり**（下に写す）。Core の P層で押さえること（実装の決定。計画に書く）：レイアウトは `_Layout.cshtml` の `#contents` の中に `@RenderSection("FooterButtonsSection", required: false)` を足す。`@section` は `<form>` の外に出るので、ボタンに `form="OrdTfForm"` を付ける。Core MVC は、再描画で `Html.HiddenFor` などが `ModelState` の入力値を優先する（モデルに設定した新しい値が出ない）ので、保持する条件・ページを更新するときは `ModelState` を消す（ページ切替のケースが確かめる）。

## 2. 画面の id・class（ケース1 の 4.1 と同じ。段 4 の実装が付ける）

| 対象 | 指定 |
| --- | --- |
| 顧客（I01）・担当者（I02） | `<select id="CustomerID">`・`<select id="EmployeeID">`。先頭の `<option value="">` が空白（すべて）。表示は顧客 = 会社名、担当者 = `姓 + " " + 名` |
| 受注日（I03）・（I04） | `<input id="OrderDateFrom">`・`<input id="OrderDateTo">`（テキスト。`yyyy/MM/dd`）。直下の項目単位のエラー：`#OrderDateFromError`・`#OrderDateToError`（違反が無いときは空） |
| 出荷状況（I05） | `<select id="ShippedStatus">`。option は「すべて」「未出荷」「出荷済」「納期遅延」の順で、value は表示と同じ。既定は「すべて」 |
| 受注一覧（I06） | `<table id="orderList">`（`thead` に見出し、`tbody` に行）。列は左から 受注 ID・顧客名・担当者名・受注日・要求納期・出荷日・配送業者・合計金額・状況（9 列。範囲外の「操作」列は置かない）。**納期遅延の行（`tr`）は class に `table-danger` を持ち、それ以外の行は持たない** |
| 総件数（I07）・ページ情報（I08） | `#totalCount`（「全 nnn 件」）・`#pageInfo`（「n / m ページ」） |
| 共通メッセージ領域 | `#messageArea`。メッセージが無いときは空。エラーは自分自身に class `text-danger`、情報は `text-info`（`cmn-framework.md` の MVC） |
| メインボタン（フッタに 5 つ） | `#btnMain1`〜`#btnMain5`（コンテンツのフッタ部＝`#contents` の中）。`#btnMain1` は「検索」で活性、`#btnMain2`〜`#btnMain5` は非活性（機能トランザクションの範囲外のため）。`@section` に置くので `form="<フォーム ID>"` で紐付ける |
| ページ移動（B05） | `#btnPageFirst`・`#btnPagePrev`・`#btnPageNext`・`#btnPageLast`（先頭・前・次・末尾の 4 つ。ページ番号の選択は作らない。`DEC-ORD-05`）。活性は「該当方向にページが存在するとき」 |

## 3. B層のルート メソッドのテスト（net10.0 で流す）

B層のテストはケース1 の 3 章のとおりで、**新しいケースは足さない**。`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`（39 メソッド・57 テストケース。`3.1` 選択肢・`3.2` 抽出条件・`3.3` 並びと行・`3.4` 合計金額・`3.5` ページング・`3.6` 検証）を `MVC_Sample_Core\MVC_Sample.Tests` から net10.0 で流す。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| （ケース1 の 3.1〜3.6 の全ケース。`OrdTfBusinessTests` の全メソッド） | ケース1 の各ケースの根拠のとおり（`EV-ORD-101`・`102`・`104`、`SC-ORD-01-I01`〜`I08`、`VAL-DATE`、`DEC-ORD-02`〜`09`、`DEC-CMN-02`〜`05` ほか） | ケース1 のとおり | ケース1 のとおり | ケース1 のとおり。net10.0 でも net48 と同じ結果（`decimal`・日付・文字列の並びが同じ） |

- B層が既に実装済みなので、**これらのテストは最初から通る**（Red にならない）。段 2 では、net10.0 で流したテストが B層の実装を見ていること（テストのコードがリンクされ、`TestSetup` の設定・SQL 定義が読めて DB に届くこと）を、**B層の実装を 1 か所ずつ一時的に壊して落ちることを確かめ、戻す**（`TESTING.md` 4。壊した箇所・落ちたケース・落ち方の例を段 2 の報告に書く）。

### 壊す確かめの方針（段 2 の報告に書くこと）

- (a) **net10.0 で流す 57 テストケースのどれもが、いずれかの壊し方で落ちる**ように、B層の実装（`OrdTfBusiness`・SQL 定義）の壊し方の一覧を組む。壊し方は 1 か所ずつ一時的に壊して戻す（戻したファイルの更新時刻を今にしてから流す）。**ケースと壊し方の対応を表にして、段 2 の報告に書く**（どの壊し方でも落ちないケースが残ったら、そのケースは実装を見ていない＝テストの誤りとして報告する）。
- (b) **壊し方ごとに、落ち方の例（`But was:` などの実際の値）を記録し、意図した壊し方の値になっていること**を確かめる（壊し方が効かず別の形で壊れても「落ちた」になるため）。戻した後の全件の結果も書く。

## 4. 画面の E2E（`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfScreenTests`）

B層で確かめることは繰り返さない（抽出条件・並び・金額の計算・状況の判定は B層のテスト）。画面の表示・活性・条件の保持・メッセージの区別を確かめる。ケースはケース1 の 4.2 と同じ（表の 17 行。ケース1 の E2E のテストは 17 メソッド・19 テストケース）。

### 4.1 ケース

共通の前提：`[SetUp]` でログインし、テストが入れたデータを使う。一覧の件数・並びに頼るのはテストが入れた顧客で絞った結果だけ（初期表示のケースを除く）。

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 初期表示_選択肢と既定の条件 | EV-ORD-101 手順 1〜3, SC-ORD-01-I01〜I05 | テスト用の顧客 K・担当者 E | `/OrdTf` を開く | `#CustomerID` の先頭 option が value = ""・text = ""、K の会社名の option を含む。`#EmployeeID` の先頭が空白で、E の「姓 名」の option を含む。`#OrderDateFrom`・`#OrderDateTo` が空。`#ShippedStatus` の option が「すべて」「未出荷」「出荷済」「納期遅延」の順で、選択は「すべて」 |
| 初期表示_一覧とページ情報 | EV-ORD-101 手順 4〜6, SC-ORD-01-I06〜I08, 共通仕様 4 節 | **テストが受注を 21 件入れる**（受注日は `2198/01/01` 以降。既存データの件数には頼らない） | `/OrdTf` を開く | `#orderList tbody tr` が 20 行（21 件以上あるので）。`#totalCount` が `全 \d+ 件`（21 以上）、`#pageInfo` が `1 / \d+ ページ`。見出し（`thead th`）が 9 列で、表示名が「受注 ID」「顧客名」「担当者名」「受注日」「要求納期」「出荷日」「配送業者」「合計金額」「状況」 |
| 初期表示_ボタン | cmn-framework.md の画面共通, SC-ORD-01-B01, B05 | **テストが受注を 21 件入れる**（2198 年。既存データに頼らない。21 件以上あるので次ページが存在する） | `/OrdTf` を開く | `#contents` の中に `#btnMain1`〜`#btnMain5` が 5 つ。`#btnMain1` は活性でキャプション「検索」、`#btnMain2`〜`#btnMain5` は非活性。1 ページ目なので `#btnPageFirst`・`#btnPagePrev` は非活性、`#btnPageNext`・`#btnPageLast` は活性 |
| 検索_顧客で絞った一覧の表示 | EV-ORD-102 手順 4, SC-ORD-01-I06〜I08, 共通仕様 3・4 節（日付・空値・金額の表示） | 顧客 K の受注 2 件：新しい方 = 受注日 `2198/03/05 15:30`・要求納期 `2198/04/01`・出荷日 NULL・担当者 E・配送業者 S・明細（単価 100.00・数量 12・割引 0）・送料 34.50、古い方 = 受注日 `2198/03/01`・担当者・配送業者 NULL・明細なし・送料 0 | `#CustomerID` で K を選び、`#btnMain1` を押す | `#orderList tbody tr` が 2 行で、新しい方が先頭。先頭行：受注 ID = 登録した ID、顧客名 = K の会社名、担当者名 = 「姓 名」、受注日 = `2198/03/05`（時刻は出ない）、要求納期 = `2198/04/01`、出荷日 = 空、配送業者 = S の会社名、合計金額 = `1,234.50`、状況 = 「未出荷」。2 行目：担当者名・配送業者・要求納期・出荷日 = 空、合計金額 = `0.00`。`#totalCount` = 「全 2 件」、`#pageInfo` = 「1 / 1 ページ」。`#CustomerID` は K の選択のまま。`#messageArea` は空。ページ移動の 4 つのボタンが非活性（ページが 1 つ） |
| 検索_すべての条件の指定 | EV-ORD-102 手順 2, SC-ORD-01-I01〜I05 | 顧客 K の受注：(E1, `2198/05/10`, 未出荷)・(E1, `2198/05/20`, 未出荷)・(E1, `2198/05/20`, 出荷済 = 出荷日あり)・(E2, `2198/05/20`, 未出荷)・(E1, `2198/06/01`, 未出荷) | K・E1・開始 `2198/05/15`・終了 `2198/05/20`・「未出荷」を選び（入力し）、`#btnMain1` を押す | 一覧が 1 行（(E1, `2198/05/20`, 未出荷)）。`#totalCount` = 「全 1 件」。5 つの入力欄が操作後も入力のまま |
| 検索_納期遅延の行は強調 | EV-ORD-101 手順 5, SC-ORD-01-I06（列 9）, 共通仕様の状況の判定 | 顧客 K の受注 2 件：納期遅延（出荷日 NULL・要求納期 `2000/01/01`）・出荷済（要求納期 `2199/12/31`・出荷日 `2001/01/01`） | K を選び検索 | 2 行。納期遅延の行は状況 = 「納期遅延」で class に `table-danger` を持ち、出荷済の行は状況 = 「出荷済」で `table-danger` を持たない |
| 検索_出荷状況の選択が一覧に効く | EV-ORD-102, SC-ORD-01-I05 | 上の 2 件 | K と「納期遅延」を選び検索 | 一覧が 1 行（状況 = 「納期遅延」）。`#totalCount` = 「全 1 件」 |
| 検索_0件の知らせ（エラーにしない） | EV-ORD-101 手順 7・EV-ORD-102 手順 4, DEC-ORD-02, 共通仕様 4 節（メッセージの区別） | 受注が 1 件も無い顧客 K | K を選び検索 | `#orderList tbody tr` が 0 行。`#messageArea` が「該当するデータがありません。」で、class に `text-info` を持ち `text-danger` を持たない。`#totalCount` = 「全 0 件」、`#pageInfo` = 「1 / 1 ページ」。ページ移動の 4 つのボタンがすべて非活性 |
| 検索_日付の形式違反は該当項目の直下だけ | VAL-DATE, ERR-VALIDATION, DEC-ORD-09, DEC-CMN-05 | — | `/OrdTf` を開き、`#OrderDateFrom` に `abc`、`#OrderDateTo` を空にして検索 | `#OrderDateFromError` が「受注日は yyyy/MM/dd の形式で指定してください。」、`#OrderDateToError` は空。`#messageArea` は空。`#OrderDateFrom` は `abc` のまま |
| 検索_終了の形式違反と両方の違反 | DEC-CMN-05, DEC-ORD-09 | — | (1) `#OrderDateTo` に `2198-01-05`、`#OrderDateFrom` は空で検索。(2) 続けて `#OrderDateFrom` に `2198/1/5` を入れ、検索 | (1) `#OrderDateToError` が同じ文言、`#OrderDateFromError` は空、`#messageArea` は空。(2) 両方の `…Error` が同じ文言で、`#messageArea` は空 |
| 検索_開始が終了より後は共通メッセージ領域 | VAL-DATE, EV-ORD-102 検証内容, DEC-CMN-04, 共通仕様 4 節（メッセージの区別） | — | `/OrdTf` を開き、開始 `2198/03/11`・終了 `2198/03/10` で検索 | `#messageArea` が「受注日の開始は終了以前を指定してください。」で、class に `text-danger` を持ち `text-info` を持たない。`#OrderDateFromError`・`#OrderDateToError` は空。入力欄は入力のまま |
| 検索_検証エラーでは直前の一覧を残す | DEC-ORD-03, ERR-VALIDATION, EV-ORD-102 手順 1 | 顧客 K の受注 2 件 | K で検索（2 行）→ 違反を入れて検索。違反 = TestCase：(1) `#OrderDateFrom` に `abc`　(2) 開始 `2198/03/11`・終了 `2198/03/10`（前後関係の違反） | どちらも：一覧が 2 行のまま（同じ受注 ID・同じ順）、`#totalCount` = 「全 2 件」、`#pageInfo` = 「1 / 1 ページ」のまま。`#CustomerID` は K、入力欄は入れた値のまま（(1) `abc`、(2) 開始・終了とも） |
| ページ切替_先頭・前・次・末尾 | EV-ORD-104, SC-ORD-01-B05, DEC-ORD-05, 共通仕様 4 節 | 顧客 K の受注 45 件（i 番目 = `2198/01/01` + i 日） | K で検索 → 次へ → 次へ → 前へ → 末尾へ → 先頭へ（各操作の後に確かめる） | 検索後：`#pageInfo` = 「1 / 3 ページ」、20 行（先頭行 = i = 45）、前・先頭は非活性、次・末尾は活性。次へ：「2 / 3 ページ」、20 行（先頭行 = i = 25）、4 つとも活性。さらに次へ：「3 / 3 ページ」、5 行（先頭行 = i = 5）、次・末尾は非活性、先頭・前は活性。前へ：「2 / 3 ページ」。末尾へ：「3 / 3 ページ」。先頭へ：「1 / 3 ページ」。どの移動でも `#totalCount` = 「全 45 件」のまま |
| ページ切替_検索するとページは1に戻る | EV-ORD-102 手順 3 | 同上 | K で検索 → 次へ（「2 / 3 ページ」）→ `#btnMain1`（検索） | `#pageInfo` = 「1 / 3 ページ」、先頭行 = i = 45 |
| ページ切替_入力欄だけ変えても最後に検索した条件で移る | DEC-ORD-04, EV-ORD-104 | 顧客 K の受注 45 件、別の顧客 L の受注 1 件 | K で検索 → `#CustomerID` を L に変えるだけ（検索しない）→ 次へ | `#pageInfo` = 「2 / 3 ページ」、一覧 20 行がすべて K の会社名（L の行を含まない）。`#totalCount` = 「全 45 件」 |
| ページ切替_検証エラーの後も直前の検索条件で移る | DEC-ORD-03, EV-ORD-104, EV-ORD-102 手順 1 | 顧客 K の受注 45 件 | K で検索 → 違反を入れて検索（エラー）→ 次へ。違反 = TestCase：(1) `#OrderDateFrom` に `abc`　(2) 開始 `2198/03/11`・終了 `2198/03/10` | どちらも：エラーの直後は一覧（20 行・先頭行 = i = 45）・`#pageInfo` = 「1 / 3 ページ」・`#totalCount` = 「全 45 件」のまま。次へ後：`#pageInfo` = 「2 / 3 ページ」、20 行（先頭行 = i = 25）、`#totalCount` = 「全 45 件」 |
| ページ切替_移動先が無くなっていたら最終ページ | DEC-ORD-06, EV-ORD-104 手順 1・3・4, DEC-ORD-02 | 顧客 K の受注 45 件 | K で検索 → 次へ（「2 / 3 ページ」）→ テストが DB から K の受注を 30 件消す（15 件残す）→ 次へ（画面では活性） | `#pageInfo` = 「1 / 1 ページ」、一覧 15 行、`#totalCount` = 「全 15 件」、4 つのページ移動ボタンがすべて非活性 |

## 5. 作らないケースと理由

ケース1 の 5 章と同じ（`ERR-DB`・`PRE-DB`・システム エラー、初期表示での 0 件、範囲外の `EV-ORD-103`・`105`〜`108` など、`OrdTfInit` の配送業者・商品の選択肢）。加えて、このケースで次を作らない。

| 対象 | 理由 |
| --- | --- |
| 未ログインで `/OrdTf` を開いたときのログイン画面への移動、`[Authorize]` の挙動 | 仕様（`SC-ORD-01`・`EV-ORD-*`）に無い。基盤・フレームワークの機能で、既存の画面と同じ |
| net10.0 だけの B層のケース | B層は 1 つで、ランタイム差は両方のランタイムで同じテストを流して確かめる（3 章） |

## 6. 仕様の決定

**なし。**ケース1 の決定（`DEC-ORD-10`：1753/01/01 より前の日付の検索条件は定めない。決定のレポートと `NorthwindDbSampleSpec@72e156e`）がそのまま当てはまる（ケースは作らない）。このケース固有の問い（P層が Core であることによる振る舞いの違い）は、仕様が決める範囲に無く、実装の決定（1 章）として扱った。

## 7. 段 2 の確かめ方（予定）

- **Red**：P層が無いので、E2E のテストは `Expect` で落ちる（`/OrdTf` が 404 になり要素が無い）。アプリの起動・ログインで落ちるのは Red ではない（先に直す）。段 2 では画面に要素を足さない。
- **B層（net10.0）**：最初から通る（3 章）。B層の実装を 1 か所ずつ壊して落ちることを確かめる。方針は 3 章の「壊す確かめの方針」のとおり（壊し方の一覧とケースの対応表、壊し方ごとの落ち方の例を、段 2 の報告に書く）。
- テストのファイルは、`Shared\WSServer_sample.Tests\` を除いて新規（`MVC_Sample_Core\MVC_Sample.Tests\`・`MVC_Sample_Core\MVC_Sample.E2ETests\`）。
