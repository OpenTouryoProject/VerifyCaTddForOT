# テスト仕様：受注一覧 OrdTf（ケース3：WebForms_Sample ＝ net48 ASP.NET Web Forms）

起こした仕様の版：NorthwindDbSampleSpec@2240ab3（main の先頭。DEC-ORD-11・DEC-CMN-07 までを含む。本文 `HLD/Common.md`・`LLD/EventSpec/Orders.md`・`LLD/UI_ElementsAndEventList/Orders.md`・`LLD/TableSchema/Orders.md` と、決定記録 `LLD/Decisions/Common.md`・`LLD/Decisions/Orders.md` を合わせて読んだ。ケース1・2 の版 `72e156e` からの差分は `LLD/Decisions/Orders.md` の `DEC-ORD-11` の追加（と索引の件数）だけで、既存のケースの期待は変わらない。0b7e58a から 2240ab3 への差分は `LLD/Decisions/Common.md` の `DEC-CMN-07` の追加（と索引の件数）だけで、ケースの期待は変わらない。6 章）
規約：`docs/spec/cmn-framework.md` の「画面共通」「WebForms」（VerifyCaTddForOT@bc1b648。`cmn-framework.md` はコミット済みで未変更）、`docs/spec/cmn-testfirst.md`、`docs/spec/test3-testfirst-OrdTf.md`、`docs/plan/cmn-testfirst.md` の「B層を共用する構成」
ケース1 のテスト仕様：[test3-testfirst-OrdTf-mvc-tests.md](./test3-testfirst-OrdTf-mvc-tests.md)（以下「ケース1」）。ケース2：[test3-testfirst-OrdTf-mvc-core-tests.md](./test3-testfirst-OrdTf-mvc-core-tests.md)。決定のレポート：[ケース1](./test3-testfirst-OrdTf-mvc-decisions.md)・[ケース2](./test3-testfirst-OrdTf-mvc-core-decisions.md)

対象：`SC-ORD-01`（`I01`〜`I08`、`B01`、`B05`）、`EV-ORD-101`・`EV-ORD-102`・`EV-ORD-104`。範囲外は対象の仕様のとおり（`EV-ORD-103`・`105`〜`108`、新規登録、出荷更新、詳細への遷移、一覧状態の復元）。

## 0. 進め方の前提

- **B層（`OrdTfBusiness`・`OrdTfParameterValue`・`OrdTfReturnValue`・SQL 定義）とそのテスト（`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`）は、ケース1 で作成済み。このケースでは B層を作らず、B層のテストのコードも足さない。**
  B層の契約（引数クラス・戻り値クラス・`MethodName`・分離レベル `ReadCommitted`・「当日」は `DateTime.Today`）はケース1 の 1 章のとおり。
  `WebForms_Sample.csproj` は `WSIFType_sample`・`WSServer_sample`（B層・D層・引数クラス・戻り値クラスを持つ共用のプロジェクト）を既に `ProjectReference` している。段 4 は、それを経由して `OrdTfBusiness` を呼ぶ。
- このケースで作るのは次の 2 つだけ。
  1. **P層**（画面 `.aspx`・コードビハインド・マスタページへのフッタ ボタンの差し込み・メニューの導線。段 4）。
  2. **net48 の `WebForms_Sample.E2ETests`**（`OrdTfScreenTests`。`opentouryo-test-e2e-web`）。
- **`WebForms_Sample.Tests`（B層のテストのプロジェクト）は作らない**（実装の決定）。理由：ランタイムが net48 で、ケース1 の `MVC_Sample\MVC_Sample.Tests`（net48。`Shared\WSServer_sample.Tests\` をリンク）が同じ B層のアセンブリを既に流している。B層は 1 つなので、同じテストをもう一度別のプロジェクトで流しても、確かめることが増えない。
  B層の不足が P層の作業中に見つかったら、ケース1 のテスト仕様と `Shared\WSServer_sample.Tests\` に足し（レビューを通し）、**net48（`MVC_Sample\MVC_Sample.Tests`）と net10.0（`MVC_Sample_Core\MVC_Sample.Tests`）の両方で B層のテストを流す**。
- 画面は WebForms_Sample の既存のログイン（`/Aspx/start/login.aspx`。利用者名 `txtUserID` を入れて `btnButton1`〈ログイン〉を押す。パスワードは要らない）の後に、`/Aspx/OrdTf/OrdTf.aspx`（画面コード `OrdTf`。初期表示はページ ロード）で開く。ログインは Forms 認証のチケットを付け、`defaultUrl` の `menu.aspx` へ移る（Session に利用者情報を入れるので、ASP.NET 状態サービスが動いている必要がある。9 章の「人の手が要る操作」）。
- 表示のメッセージ・文言は仕様の文言そのまま（`DEC-CMN-01`：メッセージ ID は実装で決め、テストは文言で確かめる）。

## 1. テストのプロジェクト（実装の決定。テストの境界の形）

| 項目 | 値 |
| --- | --- |
| 置き場所 | Web のプロジェクト（`WebForms_Sample\WebForms_Sample\`）と**同じ階層**：`WebForms_Sample\WebForms_Sample.E2ETests\`（ケース1 の `MVC_Sample\MVC_Sample.E2ETests` と同じ並び）。`WebForms_Sample\WebForms_Sample.sln` に `dotnet sln add` する |
| `WebForms_Sample.E2ETests` | SDK 形式・`net48`。`Microsoft.NET.Test.Sdk` 18.10.1・`NUnit` 4.6.1・`NUnit3TestAdapter` 6.3.0・`Microsoft.Playwright` 1.63.0・`Microsoft.Data.SqlClient` 7.0.0・`System.Security.Cryptography.Pkcs` 10.0.13（Web の `packages.config` と同じ版）。Web・OpenTouryo は参照しない（外から操作する）。名前空間 `WebForms_Sample.E2ETests`。ファイルは `AppHost.cs`・`TestData.cs`・`QualityAttributes.cs`（`Basis`・`Observation`）・`OrdTfScreenTests.cs` |
| アプリの起動 | `AppHost`（`[SetUpFixture]`）が IIS Express（`iisexpress.exe /path:<WebForms_Sample\WebForms_Sample> /port:<空きポート> /clr:v4.0`。`OT_RESOURCE_ROOT` は引き継ぐ）で起動し、ログイン画面（`/Aspx/start/login.aspx`・認証不要・200）を待つ。**固定のポートは使わない**（ケース1 の E2E の 8090 と衝突させず、並行して流せるようにする。使われているポートを避けて空きポートを取る）。終了時はプロセス ツリーごと止める。Web は先に MSBuild でビルドしておく（テストはビルドしない）。Web のフォルダへの道は、テストの出力（`bin\Debug\net48`）から `..\..\..\..\WebForms_Sample` |
| ブラウザ | Edge のチャネル・ヘッドレス（ケース1 と同じ） |
| 待ち時間 | `Expect` は 5 秒（`SetDefaultTimeout(5000)`）。**ナビゲーション（`GotoAsync`・押下後のポストバックの読み込み）は 60 秒**（`SetDefaultNavigationTimeout(60000)`）。Web Forms は初回のアクセスで `.aspx`・マスタページを動的にコンパイルするため、最初の 1 回が数秒かかる。待ち合わせの固定の時間は使わない |
| ポストバックの完了待ち | 押す操作（`#btnMain1`・`#btnPage*`）の部品は、押した**後**にポストバックの読み込みの完了（`WaitForLoadState`）を待ってから返す。待たないと、押す前の画面に対する `Expect`（特に「変わらない」を確かめるケース）が、古い画面で通ってしまう |
| ログイン（`[SetUp]`） | 新しいブラウザ コンテキストで `/Aspx/start/login.aspx` を開き、`[id$='txtUserID']` に `e2e` を入れ、`[id$='btnButton1']` を押し、`menu.aspx` への移動（URL の大文字小文字を区別しない。`defaultUrl` は `Aspx/Start/menu.aspx`）を待つ。ログイン画面の id はサーバ コントロールの ID に親の接頭辞が付くので、末尾一致の指定にする |
| テストのデータ | `TestData`（E2E 側）はケース1 と同じ中身。接続文字列だけ、Web の `Web.config` の `connectionStrings` の `ConnectionString_SQL` から読む（ケース1 と同じ） |
| `OrdTfScreenTests` | ケース1 の E2E と**同じケース**（画面の仕様が同じため）。ログイン・画面の開き方・アプリの起動だけが違う。テストのコードはコピーして別ファイルとする（共用にしない：フレームワークごとに id・操作が変わりうる） |
| 日付の範囲 | E2E は 2198 年（ケース1・2 と同じ。顧客で絞った結果だけに頼るので、E2E どうしを並行して流してよい） |
| 並行実行 | B層のテスト（net48・net10.0）を同時に流さない（ケース2 の決まり）。E2E どうし・B層と E2E は並行してよい（日付の範囲・顧客が違う） |

## 2. 画面の id・class（ケース1 の 4.1 と同じ「ブラウザに出る id」。段 4 の実装が付ける）

テストが見るのは、**ブラウザに出る要素の `id`**（ケース1・2 と同じ表）。Web Forms のサーバ コントロールの `id` は、そのままでは親の接頭辞が付く（`ctl00_ContentPlaceHolder_A_…`）ので、表の `id` が出るようにするのが段 4 の実装の決定（計画に書く。例：`ClientIDMode="Static"`、または HTML コントロール）。

| 対象 | 指定 |
| --- | --- |
| 顧客（I01）・担当者（I02） | `<select id="CustomerID">`・`<select id="EmployeeID">`。先頭の `<option value="">` が空白（すべて）。表示は顧客 = 会社名、担当者 = `姓 + " " + 名` |
| 受注日（I03）・（I04） | `<input id="OrderDateFrom">`・`<input id="OrderDateTo">`（テキスト。`yyyy/MM/dd`）。直下の項目単位のエラー：`#OrderDateFromError`・`#OrderDateToError`（違反が無いときは空） |
| 出荷状況（I05） | `<select id="ShippedStatus">`。option は「すべて」「未出荷」「出荷済」「納期遅延」の順で、value は表示と同じ。既定は「すべて」 |
| 受注一覧（I06） | `<table id="orderList">`（**`thead` に見出しの行、`tbody` にデータの行**）。列は左から 受注 ID・顧客名・担当者名・受注日・要求納期・出荷日・配送業者・合計金額・状況（9 列。範囲外の「操作」列は置かない）。**納期遅延の行（`tr`）は class に `table-danger` を持ち、それ以外の行は持たない**。`GridView` で出す（`cmn-framework.md` の WebForms）。下の「GridView の出力の決め」に従う |
| 総件数（I07）・ページ情報（I08） | `#totalCount`（「全 nnn 件」）・`#pageInfo`（「n / m ページ」） |
| 共通メッセージ領域 | `#messageArea`。メッセージが無いときは空。エラーは自分自身に class `text-danger`、情報は `text-info`（`cmn-framework.md` の WebForms） |
| メインボタン（フッタに 5 つ） | `#btnMain1`〜`#btnMain5`（コンテンツのフッタ部＝`#contents` の中。マスタページにレイアウトを置き、キャプション・活性は画面のコードビハインドの初期処理で決める。`cmn-framework.md`）。`#btnMain1` は「検索」で活性、`#btnMain2`〜`#btnMain5` は非活性（**非表示にしない**。機能トランザクションの範囲外のため）。キャプションは**アクセシブル名**で確かめる（`<input value>` でも `<button>` の文字でも同じに読める） |
| ページ移動（B05） | `#btnPageFirst`・`#btnPagePrev`・`#btnPageNext`・`#btnPageLast`（先頭・前・次・末尾の 4 つ。ページ番号の選択は作らない。`DEC-ORD-05`。画面定義書の配置どおり一覧の下部。メインボタンに含めない）。活性は「該当方向にページが存在するとき」 |

**GridView の出力の決め**（テストの境界。実装の決定として計画に書く）：
- 見出しの行を `thead` に、データの行を `tbody` に出す（`GridView` の既定は `thead` を出さず、見出しも `tr` として並べる。`UseAccessibleHeader` と見出し行の `TableSection` の指定が要る）。`#orderList thead th` が 9 個、`#orderList tbody tr` がデータの行だけ。
- **0 件のとき、`tbody` に行を出さない**（`EmptyDataText`・`EmptyDataTemplate` の行を出さない。出すと `#orderList tbody tr` が 1 になる。0 件の知らせは `#messageArea` に出す。`#orderList` 自体を出さなくてもよい）。
- `GridView` の組み込みのページング（`AllowPaging`）は使わない（ページは B層が返す 20 件。移動は 4 つのボタン）。
- 空値のセルは、空文字か `&nbsp;` のどちらでもよい（`Expect` の `ToHaveText` は空白を正規化して比べる）。

## 3. B層のルート メソッドのテスト（ケース1 で作成済み。このケースでは足さない）

B層のテストはケース1 の 3 章のとおりで、**新しいケースは足さない**。`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`（`3.1` 選択肢・`3.2` 抽出条件・`3.3` 並びと行・`3.4` 合計金額・`3.5` ページング・`3.6` 検証。ケース2 の記載で 39 メソッド・57 テストケース）が、この画面の B層の契約。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| （ケース1 の 3.1〜3.6 の全ケース。`OrdTfBusinessTests` の全メソッド。**作成済み**。場所：`Shared\WSServer_sample.Tests\`） | ケース1 の各ケースの根拠のとおり | ケース1 のとおり | ケース1 のとおり | ケース1 のとおり |

- このケースで B層のテストを流すのは、B層に変更が入ったときだけ（上の 0 章）。段 2 で B層の壊す確かめはしない（B層はこのケースで触らない）。

## 4. 画面の E2E（`WebForms_Sample\WebForms_Sample.E2ETests\OrdTfScreenTests`）

B層で確かめることは繰り返さない（抽出条件・並び・金額の計算・状況の判定は B層のテスト）。画面の表示・活性・条件の保持・メッセージの区別を確かめる。ケースはケース1 の 4.2 と同じ（表の 17 行。ケース1 の E2E のテストは 17 メソッド・19 テストケース）。表の「画面を開く」は `/Aspx/OrdTf/OrdTf.aspx` を開くこと。

### 4.1 ケース

共通の前提：`[SetUp]` でログインし、テストが入れたデータを使う。一覧の件数・並びに頼るのはテストが入れた顧客で絞った結果だけ（初期表示のケースを除く）。

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 初期表示_選択肢と既定の条件 | EV-ORD-101 手順 1〜3, SC-ORD-01-I01〜I05 | テスト用の顧客 K・担当者 E | 画面を開く | `#CustomerID` の先頭 option が value = ""・text = ""、K の会社名の option を含む。`#EmployeeID` の先頭が空白で、E の「姓 名」の option を含む。`#OrderDateFrom`・`#OrderDateTo` が空。`#ShippedStatus` の option が「すべて」「未出荷」「出荷済」「納期遅延」の順で、選択は「すべて」 |
| 初期表示_一覧とページ情報 | EV-ORD-101 手順 4〜6, SC-ORD-01-I06〜I08, 共通仕様 4 節 | **テストが受注を 21 件入れる**（受注日は `2198/01/01` 以降。既存データの件数には頼らない） | 画面を開く | `#orderList tbody tr` が 20 行（21 件以上あるので）。`#totalCount` が `全 \d+ 件`（21 以上）、`#pageInfo` が `1 / \d+ ページ`。見出し（`thead th`）が 9 列で、表示名が「受注 ID」「顧客名」「担当者名」「受注日」「要求納期」「出荷日」「配送業者」「合計金額」「状況」 |
| 初期表示_ボタン | cmn-framework.md の画面共通, SC-ORD-01-B01, B05 | **テストが受注を 21 件入れる**（2198 年。既存データに頼らない。21 件以上あるので次ページが存在する） | 画面を開く | `#contents` の中に `#btnMain1`〜`#btnMain5` が 5 つ（表示されている）。`#btnMain1` は活性でキャプション「検索」、`#btnMain2`〜`#btnMain5` は非活性。1 ページ目なので `#btnPageFirst`・`#btnPagePrev` は非活性、`#btnPageNext`・`#btnPageLast` は活性 |
| 検索_顧客で絞った一覧の表示 | EV-ORD-102 手順 4, SC-ORD-01-I06〜I08, 共通仕様 3・4 節（日付・空値・金額の表示） | 顧客 K の受注 2 件：新しい方 = 受注日 `2198/03/05 15:30`・要求納期 `2198/04/01`・出荷日 NULL・担当者 E・配送業者 S・明細（単価 100.00・数量 12・割引 0）・送料 34.50、古い方 = 受注日 `2198/03/01`・担当者・配送業者 NULL・明細なし・送料 0 | `#CustomerID` で K を選び、`#btnMain1` を押す | `#orderList tbody tr` が 2 行で、新しい方が先頭。先頭行：受注 ID = 登録した ID、顧客名 = K の会社名、担当者名 = 「姓 名」、受注日 = `2198/03/05`（時刻は出ない）、要求納期 = `2198/04/01`、出荷日 = 空、配送業者 = S の会社名、合計金額 = `1,234.50`、状況 = 「未出荷」。2 行目：担当者名・配送業者・要求納期・出荷日 = 空、合計金額 = `0.00`。`#totalCount` = 「全 2 件」、`#pageInfo` = 「1 / 1 ページ」。`#CustomerID` は K の選択のまま。`#messageArea` は空。ページ移動の 4 つのボタンが非活性（ページが 1 つ） |
| 検索_すべての条件の指定 | EV-ORD-102 手順 2, SC-ORD-01-I01〜I05 | 顧客 K の受注：(E1, `2198/05/10`, 未出荷)・(E1, `2198/05/20`, 未出荷)・(E1, `2198/05/20`, 出荷済 = 出荷日あり)・(E2, `2198/05/20`, 未出荷)・(E1, `2198/06/01`, 未出荷) | K・E1・開始 `2198/05/15`・終了 `2198/05/20`・「未出荷」を選び（入力し）、`#btnMain1` を押す | 一覧が 1 行（(E1, `2198/05/20`, 未出荷)）。`#totalCount` = 「全 1 件」。5 つの入力欄が操作後も入力のまま |
| 検索_納期遅延の行は強調 | EV-ORD-101 手順 5, SC-ORD-01-I06（列 9）, 共通仕様の状況の判定 | 顧客 K の受注 2 件：納期遅延（出荷日 NULL・要求納期 `2000/01/01`）・出荷済（要求納期 `2199/12/31`・出荷日 `2001/01/01`） | K を選び検索 | 2 行。納期遅延の行は状況 = 「納期遅延」で class に `table-danger` を持ち、出荷済の行は状況 = 「出荷済」で `table-danger` を持たない |
| 検索_出荷状況の選択が一覧に効く | EV-ORD-102, SC-ORD-01-I05 | 上の 2 件 | K と「納期遅延」を選び検索 | 一覧が 1 行（状況 = 「納期遅延」）。`#totalCount` = 「全 1 件」 |
| 検索_0件の知らせ（エラーにしない） | EV-ORD-101 手順 7・EV-ORD-102 手順 4, DEC-ORD-02, 共通仕様 4 節（メッセージの区別） | 受注が 1 件も無い顧客 K | K を選び検索 | `#orderList tbody tr` が 0 行。`#messageArea` が「該当するデータがありません。」で、class に `text-info` を持ち `text-danger` を持たない。`#totalCount` = 「全 0 件」、`#pageInfo` = 「1 / 1 ページ」。ページ移動の 4 つのボタンがすべて非活性 |
| 検索_日付の形式違反は該当項目の直下だけ | VAL-DATE, ERR-VALIDATION, DEC-ORD-09, DEC-CMN-05 | — | 画面を開き、`#OrderDateFrom` に `abc`、`#OrderDateTo` を空にして検索 | `#OrderDateFromError` が「受注日は yyyy/MM/dd の形式で指定してください。」、`#OrderDateToError` は空。`#messageArea` は空。`#OrderDateFrom` は `abc` のまま |
| 検索_終了の形式違反と両方の違反 | DEC-CMN-05, DEC-ORD-09 | — | (1) `#OrderDateTo` に `2198-01-05`、`#OrderDateFrom` は空で検索。(2) 続けて `#OrderDateFrom` に `2198/1/5` を入れ、検索 | (1) `#OrderDateToError` が同じ文言、`#OrderDateFromError` は空、`#messageArea` は空。(2) 両方の `…Error` が同じ文言で、`#messageArea` は空 |
| 検索_開始が終了より後は共通メッセージ領域 | VAL-DATE, EV-ORD-102 検証内容, DEC-CMN-04, 共通仕様 4 節（メッセージの区別） | — | 画面を開き、開始 `2198/03/11`・終了 `2198/03/10` で検索 | `#messageArea` が「受注日の開始は終了以前を指定してください。」で、class に `text-danger` を持ち `text-info` を持たない。`#OrderDateFromError`・`#OrderDateToError` は空。入力欄は入力のまま |
| 検索_検証エラーでは直前の一覧を残す | DEC-ORD-03, ERR-VALIDATION, EV-ORD-102 手順 1 | 顧客 K の受注 2 件 | K で検索（2 行）→ 違反を入れて検索。違反 = TestCase：(1) `#OrderDateFrom` に `abc`　(2) 開始 `2198/03/11`・終了 `2198/03/10`（前後関係の違反） | どちらも：一覧が 2 行のまま（同じ受注 ID・同じ順）、`#totalCount` = 「全 2 件」、`#pageInfo` = 「1 / 1 ページ」のまま。`#CustomerID` は K、入力欄は入れた値のまま（(1) `abc`、(2) 開始・終了とも） |
| ページ切替_先頭・前・次・末尾 | EV-ORD-104, SC-ORD-01-B05, DEC-ORD-05, 共通仕様 4 節 | 顧客 K の受注 45 件（i 番目 = `2198/01/01` + i 日） | K で検索 → 次へ → 次へ → 前へ → 末尾へ → 先頭へ（各操作の後に確かめる） | 検索後：`#pageInfo` = 「1 / 3 ページ」、20 行（先頭行 = i = 45）、前・先頭は非活性、次・末尾は活性。次へ：「2 / 3 ページ」、20 行（先頭行 = i = 25）、4 つとも活性。さらに次へ：「3 / 3 ページ」、5 行（先頭行 = i = 5）、次・末尾は非活性、先頭・前は活性。前へ：「2 / 3 ページ」。末尾へ：「3 / 3 ページ」。先頭へ：「1 / 3 ページ」。どの移動でも `#totalCount` = 「全 45 件」のまま |
| ページ切替_検索するとページは1に戻る | EV-ORD-102 手順 3 | 同上 | K で検索 → 次へ（「2 / 3 ページ」）→ `#btnMain1`（検索） | `#pageInfo` = 「1 / 3 ページ」、先頭行 = i = 45 |
| ページ切替_入力欄だけ変えても最後に検索した条件で移る | DEC-ORD-04, EV-ORD-104 | 顧客 K の受注 45 件、別の顧客 L の受注 1 件 | K で検索 → `#CustomerID` を L に変えるだけ（検索しない）→ 次へ | `#pageInfo` = 「2 / 3 ページ」、一覧 20 行がすべて K の会社名（L の行を含まない）。`#totalCount` = 「全 45 件」 |
| ページ切替_検証エラーの後も直前の検索条件で移る | DEC-ORD-03, EV-ORD-104, EV-ORD-102 手順 1 | 顧客 K の受注 45 件 | K で検索 → 違反を入れて検索（エラー）→ 次へ。違反 = TestCase：(1) `#OrderDateFrom` に `abc`　(2) 開始 `2198/03/11`・終了 `2198/03/10` | どちらも：エラーの直後は一覧（20 行・先頭行 = i = 45）・`#pageInfo` = 「1 / 3 ページ」・`#totalCount` = 「全 45 件」のまま。次へ後：`#pageInfo` = 「2 / 3 ページ」、20 行（先頭行 = i = 25）、`#totalCount` = 「全 45 件」 |
| ページ切替_移動先が無くなっていたら最終ページ | DEC-ORD-06, EV-ORD-104 手順 1・3・4, DEC-ORD-02 | 顧客 K の受注 45 件 | K で検索 → 次へ（「2 / 3 ページ」）→ テストが DB から K の受注を 30 件消す（15 件残す）→ 次へ（画面では活性） | `#pageInfo` = 「1 / 1 ページ」、一覧 15 行、`#totalCount` = 「全 15 件」、4 つのページ移動ボタンがすべて非活性 |

- 「検索_検証エラーでは直前の一覧を残す」「ページ切替_検証エラーの後も直前の検索条件で移る」のデータは、テストが入れた顧客だけで、テストの間に他の利用者が変更しない。`DEC-ORD-11`（他の利用者によるデータの変更があるときの「直前の表示のまま」は定めない）は、このデータの前提のもとでは効かない（ケースは作らない。6 章）。
- Web Forms 固有の確かめ（ViewState による入力欄の保持・一覧状態の保持）は、上のケースが画面の観測で既に含む：検索条件が入力のまま残る（「検索_すべての条件の指定」「検索_検証エラーでは直前の一覧を残す」）、保持した条件でページが移る（「ページ切替_入力欄だけ変えても…」「ページ切替_検証エラーの後も…」）。保持の仕組み（ViewState・hidden・Session）は実装の決定で、テストは境界から見えることだけを確かめる。

## 5. 作らないケースと理由

ケース1 の 5 章と同じ（`ERR-DB`〔`DEC-CMN-07`：DB の失敗時の画面での振る舞いは定めない。実装に任せ、テストの期待にしない〕・`PRE-DB`・システム エラー、初期表示での 0 件、範囲外の `EV-ORD-103`・`105`〜`108` など、`OrdTfInit` の配送業者・商品の選択肢）。加えて、このケースで次を作らない。

| 対象 | 理由 |
| --- | --- |
| 未ログインで `/Aspx/OrdTf/OrdTf.aspx` を開いたときのログイン画面への移動（Forms 認証）、画面遷移の定義（`SCDefinition.xml` の `directLink`）の挙動 | 仕様（`SC-ORD-01`・`EV-ORD-*`）に無い。基盤・フレームワークの機能で、既存の画面と同じ。`FxScreenTransitionMode` が `off`（`app.config`）なので、画面遷移のチェックは走らない。`SCDefinition.xml` への登録はテストの前提にしない（登録するかは段 4 の実装の決定。主スイッチを有効にするなら `directLink="allow"` で登録する） |
| ダイアログ（`ShowOKMessageDialog`・`ShowYesNoMessageDialog`・`ShowModalScreen`） | この範囲に、確認・OK・子画面の仕様が無い（検索・ページ移動のメッセージは共通メッセージ領域）。`cmn-framework.md` の WebForms のダイアログの決まりは、範囲が増えたときに起こす |
| 入力欄での Enter による暗黙の送信 | 仕様に無い（ケース1・2 でも作っていない）。段 4 は、Enter が検索になるようにしてよい（`DefaultButton` など。実装の決定） |
| マスタページに既存のサンプルのボタン・ラベル（`sampleScreen.master` の共通イベント エリアなど）を持ち込まないこと | 仕様に無い。画面の id の衝突を避けるのは実装の決定（新しいマスタページか、このページが使うレイアウトに、メインボタン 5 つだけを置く） |
| net10.0 の B層のケース | ケース2 で流している。B層は 1 つで、ランタイム差は両方のランタイムで同じテストを流して確かめる |

## 6. 仕様の決定

**段 1 で上げる仕様の決定はなし。**次の決定が当てはまる（`DEC-ORD-10`・`DEC-ORD-11` はケース1・2 で、`DEC-CMN-07` はこのケースの段 4 のレビューで決まった。新しいケースは作らない）。
- `DEC-ORD-10`：1753/01/01 より前の日付の検索条件は定めない（ケースは作らない）。
- `DEC-ORD-11`：検証エラーの直後の一覧が、他の利用者によるデータの変更をどう反映するかは定めない（実装に任せ、テストの期待にしない。決定のレポートには再び載せない）。
- `DEC-CMN-07`：DB への接続・実行の失敗（`ERR-DB`）の画面での振る舞いは定めない（実装に任せ、テストの期待にしない。ケースは作らない）。

このケース固有の問い（Web Forms の GridView・ViewState・マスタページ・ClientID・ログイン画面の id）は、仕様が決める範囲に無く、実装の決定（1・2 章）として扱った。

## 7. 段 2 の確かめ方（予定）

- **Red**：P層が無いので、E2E のテストは `Expect` で落ちる（`/Aspx/OrdTf/OrdTf.aspx` が 404 になり要素が無い）。アプリの起動・ログインで落ちるのは Red ではない（先に直す）。段 2 では画面に要素を足さない。
  操作の部品は、押す前に `Expect` で対象の活性・表示を待つ（要素が無いときに 30〜60 秒待って `TimeoutException` になるのではなく、`Expect` で落ちるようにする。ケース1 と同じ）。
  ナビゲーションの待ち時間を 60 秒にするので、Red の確認では、画面を開いた後の `Expect`（5 秒）で落ちることを確かめる。
- **B層**：このケースでは流さず、壊す確かめもしない（ケース1・2 で済んでいる）。
- テストのファイルは新規（`WebForms_Sample\WebForms_Sample.E2ETests\`）。

## 8. テストの数

| 種類 | 数 | 場所 |
| --- | --- | --- |
| B層のルート メソッドの単体テスト | **作成済み**（ケース1。このケースでは増やさない） | `Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs` |
| 画面の E2E | 17 メソッド・19 テストケース（ケース1 と同じ） | `WebForms_Sample\WebForms_Sample.E2ETests\OrdTfScreenTests.cs` |

## 9. 人の手が要る操作

- ASP.NET 状態サービス（`aspnet_state`）が動いていること（`Web.config` が `sessionState mode="StateServer"`。ログインが `Session` に利用者情報を入れる）。**今の環境では Running を確認済み**。止まっていたら、管理者の権限で開始してもらう。
