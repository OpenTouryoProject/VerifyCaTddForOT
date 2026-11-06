# テスト仕様：受注一覧の検索（MVC_Sample_Core / net10.0）

段 1（`TESTING.md`）の成果物。

- 起こした仕様の版：**NorthwindDbSampleSpec@fe574ce**（初版は @6545f92。@fe574ce への追随は「3.2」）
- レビュー反映済み（2026-11-06）。人に返した事項への回答は「3.1」に記録した。

- 対象：`OrdTfBusiness` の `MethodName` = `OrdTfSearch`（`UOC_OrdTfSearch`）。`DoBusinessLogic` の境界でテストする
- 仕様の出どころ（NorthwindDbSampleSpec）：
  - `LLD/EventSpec/Orders.md`：`EV-ORD-101`・`EV-ORD-102`、「共通の処理」（受注検索クエリ・状況の判定）
  - `LLD/UI_ElementsAndEventList/Orders.md`：`SC-ORD-01`（`I01`〜`I08`、`I06` の列）
  - `LLD/TableSchema/Orders.md`：`ORD-T2`・`ORD-T7`
  - `HLD/Common.md`：`VAL-DATE`・`ERR-VALIDATION`・「金額の算出式」（3 節）・7 節（メッセージは文言で定め、ID は実装で決める）
- テストのプロジェクト・クラス：`MVC_Sample.Tests`・`OrdTfBusinessTests`（`[Basis]` に下表の根拠を付ける）

## 0. 前提と入出力（B層の境界）

テストが見る入力と出力。**B層の引数・戻り値の項目は、ここで決めて承認を受ける**（仕様書は画面項目で書かれているため、B層の項目への写しは本書が決める）。

| 引数クラス `OrdTfParameterValue` | 対応する画面項目 | 型（案） |
| --- | --- | --- |
| `CustomerID` | `SC-ORD-01-I01` | `string`（空 = 未指定） |
| `EmployeeID` | `SC-ORD-01-I02` | `int?`（null = 未指定） |
| `OrderDateFrom` / `OrderDateTo` | `I03` / `I04` | `DateTime?`（null = 未指定） |
| `ShipStatus` | `I05` | `string`：`すべて`／`未出荷`／`出荷済`／`納期遅延` |
| `PageNo` | （`EV-ORD-101`・`102` は 1 固定） | `int`（1） |

| 戻り値クラス `OrdTfReturnValue` | 対応する画面項目 | 備考 |
| --- | --- | --- |
| `Rows`（`DataTable`） | `SC-ORD-01-I06` | 列：OrderID・CustomerName・EmployeeName・OrderDate・RequiredDate・ShippedDate・ShipperName・TotalAmount・Status |
| `TotalCount` | `I07` | 総件数（ページ分ではなく全体） |
| `ErrorFlag` / `ErrorMessageID` | — | 業務エラー |

- 並べ替えの指定は `EV-ORD-105` の範囲外なので、引数に持たない（固定：受注日の降順、同日は受注 ID の降順）。
- `actionType` = `SQL`、`MyUserInfo("test", "127.0.0.1")`、分離レベル = `User`（`opentouryo-test-layer-b`）。

## 1. テストデータ（テストが入れて消す）

- **テスト用の顧客を 2 つ作る**（A・B）。`CustomerID` は `nchar(5)` で**英大文字 5 桁**（`CUS-T1`・`VAL-CODE`）なので、`Z` ＋ ランダムな英大文字 4 桁。
  `[SetUp]` で INSERT、`[TearDown]` で、その顧客の `Order Details` → `Orders` → `Customers` を DELETE する。
- 受注は、テスト用の顧客に対して作る。担当者・配送業者は Northwind の既存行を**参照するだけ**（書き換えない）。
- 日付は `DateTime.Today` からの相対で作る（「当日」を使う判定のため）。
- 件数は必ず「テスト用の顧客」で絞って数える。

## 2. テストケース

### 2.1 抽出条件（EV-ORD-102 手順 2）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfSearch_顧客で絞る | EV-ORD-102 | 顧客 A に 2 件、顧客 B に 1 件 | 顧客 = A | ErrorFlag = false、TotalCount = 2、Rows は A の 2 件だけ |
| OrdTfSearch_担当者で絞る | EV-ORD-102 | 顧客 A に、担当者 1 が 2 件・担当者 2 が 1 件 | 顧客 = A、担当者 = 1 | TotalCount = 2、すべて担当者 1 |
| OrdTfSearch_受注日開始を含む | EV-ORD-102 | 顧客 A に受注日 D-1・D・D+1 の 3 件 | 顧客 = A、開始 = D | TotalCount = 2（D と D+1。当該日を含む） |
| OrdTfSearch_受注日終了を含む | EV-ORD-102 | 同上 | 顧客 = A、終了 = D | TotalCount = 2（D-1 と D。当該日を含む） |
| OrdTfSearch_受注日の範囲 | EV-ORD-102 | 同上 | 顧客 = A、開始 = D、終了 = D | TotalCount = 1（開始 = 終了は有効） |
| OrdTfSearch_条件はANDで組み合わさる | EV-ORD-102 | 顧客 A に、(担当者 1・D)・(担当者 1・D+5)・(担当者 2・D) | 顧客 = A、担当者 = 1、開始 = D、終了 = D | TotalCount = 1 |
| OrdTfSearch_未指定の条件は加えない | EV-ORD-102 | 顧客 A に 2 件。受注日を既存のどの受注より新しい 2199-01-01 にし、既定の並び（受注日の降順）で 1 ページ目（20 件）に入る | 全項目未指定（出荷状況 = すべて） | ErrorFlag = false、Rows にテスト用の 2 件が含まれる、TotalCount >= 2 |
| OrdTfSearch_未出荷 | EV-ORD-102, ORD-T2 | 顧客 A に、ShippedDate が NULL の 2 件と、値のある 1 件 | 顧客 = A、状況 = 未出荷 | TotalCount = 2（NULL の行だけ） |
| OrdTfSearch_出荷済 | EV-ORD-102, ORD-T2 | 同上 | 顧客 = A、状況 = 出荷済 | TotalCount = 1（NULL でない行だけ） |
| OrdTfSearch_納期遅延_未出荷で期限切れ | EV-ORD-102, ORD-T7 | 未出荷・RequiredDate = 昨日 の 1 件 | 顧客 = A、状況 = 納期遅延 | 該当する |
| OrdTfSearch_納期遅延_出荷が期限後 | EV-ORD-102, ORD-T7 | 出荷済・ShippedDate > RequiredDate の 1 件 | 同上 | 該当する |
| OrdTfSearch_納期遅延でない行は除く | EV-ORD-102, ORD-T7 | (a) 未出荷・RequiredDate = 今日（`< 当日` でない）(b) 未出荷・RequiredDate = 明日 (c) 出荷済・ShippedDate = RequiredDate (d) 出荷済・ShippedDate < RequiredDate | 顧客 = A、状況 = 納期遅延 | 0 件 |
| OrdTfSearch_納期遅延_RequiredDateがNULLは対象外 | EV-ORD-102, ORD-T7 | (a) 未出荷・RequiredDate = NULL (b) 出荷済・RequiredDate = NULL | 顧客 = A、状況 = 納期遅延 | 0 件 |

### 2.2 一覧の内容（EV-ORD-101 手順 4〜7、EV-ORD-102 手順 3〜4）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfSearch_既定の並びは受注日の降順_同日はID降順 | EV-ORD-101, SC-ORD-01-I06 | 顧客 A に、受注日 D の 2 件（ID が小・大）と D-1 の 1 件 | 顧客 = A | Rows の順：(D, ID 大)、(D, ID 小)、(D-1) |
| OrdTfSearch_1ページは20件_総件数は全体 | EV-ORD-101, EV-ORD-102 | 顧客 A に 25 件 | 顧客 = A | Rows.Count = 20、TotalCount = 25 |
| OrdTfSearch_ちょうど20件 | EV-ORD-101 | 顧客 A に 20 件 | 顧客 = A | Rows.Count = 20、TotalCount = 20 |
| OrdTfSearch_該当なしはエラーにしない | EV-ORD-101 手順 7 | 顧客 A に受注なし | 顧客 = A | ErrorFlag = false、Rows.Count = 0、TotalCount = 0 |
| OrdTfSearch_状況_未出荷 | 状況の判定, ORD-T7 | 未出荷・RequiredDate = 明日 | 顧客 = A | Status = 「未出荷」 |
| OrdTfSearch_状況_納期遅延_未出荷 | 状況の判定, ORD-T7 | 未出荷・RequiredDate = 昨日 | 顧客 = A | Status = 「納期遅延」 |
| OrdTfSearch_状況_納期遅延_出荷済 | 状況の判定, ORD-T7 | 出荷済・ShippedDate > RequiredDate | 顧客 = A | Status = 「納期遅延」 |
| OrdTfSearch_状況_出荷済 | 状況の判定, ORD-T7 | 出荷済・ShippedDate <= RequiredDate | 顧客 = A | Status = 「出荷済」 |
| OrdTfSearch_状況_RequiredDateがNULLは遅延にしない | 状況の判定, ORD-T7 | (a) 未出荷・NULL (b) 出荷済・NULL | 顧客 = A（出荷状況では絞らない） | 2 件が出る。OrderID で見分けて (a) 「未出荷」(b) 「出荷済」 |
| OrdTfSearch_合計金額 | 受注検索クエリ, Common §3 金額の算出式 | 明細 2 行（単価 10.00・割引 0.15・数量 3／単価 7.50・割引 0・数量 2）、送料 5.00 | 顧客 = A | TotalAmount = 25.50 + 15.00 + 5.00 = 45.50（10×0.85×3 = 25.50、7.5×1×2 = 15.00） |
| OrdTfSearch_合計金額_割引率realの誤差を出さない | Common §3（10 進数で計算する） | 明細 1 行（単価 19.99・割引 0.1・数量 3）、送料 0 | 顧客 = A | TotalAmount = 53.97（19.99×0.9×3 = 53.973 → 小数第 3 位を四捨五入） |
| OrdTfSearch_合計金額_明細が無い受注は送料だけ | Common §3 金額の算出式（明細が無い受注の小計は 0） | 明細 0 行、送料 5.00 | 顧客 = A | 行が出る、TotalAmount = 5.00（小計 0 + 送料） |
| OrdTfSearch_合計金額_送料がNULLは0として足す | Common §3 金額の算出式（Freight が NULL のときは 0） | 明細 1 行（単価 10.00・割引 0・数量 3）、Freight = NULL（INSERT で NULL を明示） | 顧客 = A | TotalAmount = 30.00 |
| OrdTfSearch_合計金額_明細も送料も無い | Common §3 金額の算出式 | 明細 0 行、Freight = NULL（INSERT で NULL を明示） | 顧客 = A | 行が出る、TotalAmount = 0.00 |
| OrdTfSearch_外部結合_配送業者がNULL | 受注検索クエリ, SC-ORD-01-I06 列 7 | ShipVia = NULL の 1 件 | 顧客 = A | その行が出る、ShipperName は空 |
| OrdTfSearch_外部結合_担当者がNULL | 受注検索クエリ, SC-ORD-01-I06 列 3 | EmployeeID = NULL の 1 件 | 顧客 = A | その行が出る、EmployeeName は空 |
| OrdTfSearch_顧客名と担当者名を返す | 受注検索クエリ, SC-ORD-01-I06 列 2・3 | 顧客 A（会社名 X）、担当者 1 | 顧客 = A | CustomerName = X、EmployeeName = 担当者 1 の「姓 名」 |

### 2.3 入力の検証（EV-ORD-102 手順 1、`VAL-DATE`）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfSearch_開始が終了より後はエラー | VAL-DATE, ERR-VALIDATION | — | 開始 = D+1、終了 = D | ErrorFlag = true、ErrorMessage = 「受注日の開始は終了以前を指定してください。」（仕様の文言と一致。`BaseLogic` が業務例外の Message をそのまま入れる）、ErrorMessageID は確かめない（ID は仕様で定めず実装で決める）、Rows が空、TotalCount = 0 |
| OrdTfSearch_開始のみ指定は検証を通る | VAL-DATE | 顧客 A に受注日 D の 1 件 | 開始 = D | ErrorFlag = false、TotalCount = 1（検索まで進む） |
| OrdTfSearch_終了のみ指定は検証を通る | VAL-DATE | 顧客 A に受注日 D の 1 件 | 終了 = D | ErrorFlag = false、TotalCount = 1（検索まで進む） |

## 3. 人に返す事項（仕様の漏れ・曖昧）

承認の前に決めてほしい。**推測では埋めていない。** 決まるまで、関係するケースは「要確認」のまま。

1. **`VAL-DATE` 違反の `ErrorMessageID`。** 仕様にあるのはメッセージ文（「受注日の開始は終了以前を指定してください。」）だけで、**メッセージ ID が無い**。
   `MSGDefinition.xml` に採番する ID（と、このプロジェクトの親クラス2 が `%1` 置換などをするか）を決めてほしい。
   → **決着（3.2）**：ID は実装で決める。ケースは ErrorMessage の文言を確かめる。
2. **検証を B層と画面のどちらで行うか**（`test6-testfirst.md` の決まり）。
   **提案：B層で行う**（テストできる。画面は B層の `ErrorFlag` を表示するだけにして、検証を二重に書かない）。2.3 は B層で行う前提で書いてある。
3. **日付として解釈できない入力**（`VAL-DATE` の前半）。B層の引数を `DateTime?` にすると、解釈できない文字列は画面（モデルバインド）で弾かれ、B層には届かない。
   B層を `string` で受けて検証するか、画面で弾くかを決めてほしい（案：画面。B層のケースは作らない）。
4. **明細が 0 件の受注の合計金額。** 「合計は `Order Details` の集計に `Freight` を加える」とあるが、明細が無い場合（`ORD-T5` により本システムでは発生しないが、既存データにはあり得る）に
   送料だけを返すか 0 を返すかが書かれていない。→ **決着（3.2）**：小計 0。ケースを足した。
5. **`Freight` が NULL の受注**（`Orders.Freight` は NULL 可）の合計金額。仕様に無い。→ **決着（3.2）**：0 として足す。ケースを足した。
6. **`ERR-DB`（接続・実行の失敗）。** 実 DB のテストで確実に起こす手段が無い（接続文字列を壊すと、親クラス2 の挙動に依存する）。**ケースを作っていない。**
   起こすなら、`BusinessSystemException` を捕まえるケースを別に決める。
7. **「選択肢の構築」（`EV-ORD-101` 手順 1・2：顧客・担当者・出荷状況のドロップダウン）を B層に置くか。** 仕様は B層を指定していない。
   **提案：`OrdTfSearch` とは別の `MethodName`（例 `OrdTfInit`）にする。** その場合、本書の範囲外（`EV-ORD-101` 手順 1・2 のテストは別途）。この指定があれば、ケースを足す。
8. **`EV-ORD-101`（ページ読み込み）の既定条件の検索は、`OrdTfSearch` を条件なしで呼ぶ**ものとして扱った（2.1 の「未指定の条件は加えない」・2.2 の並び）。よいか。
9. **テスト用の担当者。** `Employees` の既存行（ID 1・2）を参照する前提にしてある。Northwind の元の行が揃っていない環境では前提が崩れる。
   テストが担当者を自分で入れる方式にするか決めてほしい（案：入れる。ID は `IDENTITY` なので `SCOPE_IDENTITY()` で受ける）。
10. **`TotalAmount` の型・丸め。** 明細ごとに小数第 3 位を四捨五入してから合算する（Common §3）。DB の `real`（`Discount`）の誤差を避けるため、
    SQL 側で `decimal` へキャストして計算する前提で期待値を書いた。
11. **`opentouryo-test-layer-b` の例の `"T-" + Guid…` は顧客 ID の書式に合わない**（`nchar(5)`・英大文字 5 桁）。本書は `Z` ＋英大文字 4 桁にした。スキル側の例の修正候補。
12. **出荷状況の「すべて」を表す値。** 引数の `ShipStatus` を文字列にしたが、列挙体にする案もある。仕様は固定値の表示名しか定めていない。

## 3.1 回答（2026-11-06、人）

1. `ErrorFlag = true` だけを確かめる（ID は後で決める）。2. B層。3. 画面（B層のケースは作らない）。4・5. ケースを作らない（**仕様の漏れとして残す**。→ 3.2 で決まった）。
6. 作らない。7. `OrdTfInit` を別にし、今回は範囲外。8. よい。9. 既存の担当者を参照し、`[SetUp]` で存在を確かめる。10. よい。
11. スキル側で直す。12. 文字列。

## 3.2 仕様の更新への追随（@6545f92 → @fe574ce、2026-11-06、人が承認）

`NorthwindDbSampleSpec#2` で決まった内容（テスト仕様が参照する文書の差分は `HLD/Common.md` だけ。`SC-ORD-03`・`04` の送料欄の変更は範囲外）。

- 質問 1：メッセージは文言で定め、ID は実装で決める（Common 7 節）。→ 2.3 の最初のケースに **ErrorMessage の文言の一致を足した**（期待の追加。トレーラー不要）。ErrorMessageID は確かめない。
- 質問 4：明細が無い受注の小計は 0。質問 5：Freight が NULL のときは 0 として足す。→ 2.2 に 3 件を足した（ケースの追加。トレーラー不要）。

### 段 2 の確認記録（足したケースが実装を見ていること）

足したケースは、実装が決定と同じ扱いを先にしているため、**最初から通る**。そこで、実装の該当箇所を一時的に壊し、失敗することを確かめてから戻した。
手順：実装を 1 か所ずつ壊す → `dotnet test MVC_Sample.Tests --filter "Name~OrdTfSearch"` → 元に戻す（毎回、`git diff` で差分が無いこと、全 33 件が合格することを確認）。

足したケースの実行結果（壊す前）：足した 3 件と文言の確認を含む 6 件が合格（失敗 0）。

| 壊した箇所（`OrdTfBusiness.cs`） | 失敗を期待するケース | 結果 |
| --- | --- | --- |
| `ISNULL(o.Freight, 0)` → `o.Freight` | 送料がNULLは0として足す／明細も送料も無い | 失敗 2 件（送料がNULLは0として足す／明細も送料も無い）。他 31 件は合格。期待どおり |
| 明細の小計の `ISNULL(SUM(...), 0)` → `SUM(...)`（括弧を保つ。最初に括弧を崩して SQL エラーにし 29 件が失敗したため、やり直した） | 明細が無い受注は送料だけ／明細も送料も無い | 失敗 2 件（明細が無い受注は送料だけ／明細も送料も無い）。他 31 件は合格。期待どおり |
| 文言を「受注日の開始は終了以前です。」に変える | 開始が終了より後はエラー | 失敗 1 件（開始が終了より後はエラー）。他 32 件は合格。期待どおり |

## 4. 範囲外（作らない）

顧客が NULL の受注（`SC-ORD-01-I06` 列 2）は、テストのデータを顧客で区切れないため作らない。

`EV-ORD-103`（条件クリア）〜 `EV-ORD-108`（ページ切替・並べ替え・各遷移・出荷更新）。ページ 2 以降の取得、並べ替え列の切替は含めない（`test6-testfirst.md`）。
