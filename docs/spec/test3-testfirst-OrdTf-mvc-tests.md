# テスト仕様：受注一覧 OrdTf（ケース1：MVC_Sample ＝ net48 ASP.NET MVC）

起こした仕様の版：NorthwindDbSampleSpec@72e156e（main。本文 `HLD/Common.md`・`LLD/EventSpec/Orders.md`・`LLD/UI_ElementsAndEventList/Orders.md`・`LLD/TableSchema/Orders.md` と、決定記録 `LLD/Decisions/Common.md`・`LLD/Decisions/Orders.md` を合わせて読んだ）
規約：`docs/spec/cmn-framework.md` の「画面共通」「MVC」（VerifyCaTddForOT@aac8bc1）、`docs/spec/cmn-testfirst.md`、`docs/spec/test3-testfirst-OrdTf.md`

対象：`SC-ORD-01`（`I01`〜`I08`、`B01`、`B05`）、`EV-ORD-101`・`EV-ORD-102`・`EV-ORD-104`。範囲外は対象の仕様のとおり（`EV-ORD-103`・`105`〜`108`、新規登録、出荷更新、詳細への遷移、一覧状態の復元）。

## 0. 進め方の前提

- このケースが最初に回るので、**B層（`OrdTfBusiness`・`OrdTfParameterValue`・`OrdTfReturnValue`）とそのテスト（`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`）もここで作る**。2 つ目以降のケースは B層を作らず、このテストを流す。
- テストのプロジェクト
  - B層：`MVC_Sample\MVC_Sample.Tests\`（net48・SDK 形式。`Shared\WSServer_sample.Tests\**\*.cs` をリンクで取り込む。`opentouryo-test-layer-b`）
  - 画面：`MVC_Sample\MVC_Sample.E2ETests\`（net48・SDK 形式。`OrdTfScreenTests`。`opentouryo-test-e2e-web`。IIS Express・ポート 8090）
- 画面は MVC_Sample の既存のログイン（`/Home/Login`。利用者名 `#UserName` を入れて `input[name=normal]` を押す）の後に、`/OrdTf`（コントローラ `OrdTfController`・アクション `Index`）で開く。
- 表示のメッセージ・文言は仕様の文言そのまま（`DEC-CMN-01`：メッセージ ID は実装で決め、テストは文言で確かめる）。

## 1. テストが固定する契約（実装の決定。段 4 の実装はこれに従う）

仕様が決めていない「B層とテストの境界の形」を、テストを先に書くためにここで決める。振る舞いは変えない（作り方だけ）。

### 1.1 B層の呼び出し

| 項目 | 値 |
| --- | --- |
| クラス | `WSServer_sample.Business.OrdTfBusiness`（`MyFcBaseLogic`）。D層は `CmnDao`（SQL は定義ファイルか SQL 文） |
| 引数クラス・戻り値クラス | `WSIFType_sample.OrdTfParameterValue`・`OrdTfReturnValue`（`Shared\IfType\`。`[Serializable]`） |
| 引数クラスの生成 | `new OrdTfParameterValue("OrdTf", "-", <MethodName>, "SQL", new MyUserInfo("test", "127.0.0.1"))` |
| `MethodName` | `OrdTfInit`（`EV-ORD-101` の選択肢）・`OrdTfSearch`（`EV-ORD-102`・`EV-ORD-104`。ページは `PageNo`） |
| 呼び方 | `new OrdTfBusiness().DoBusinessLogic(pv, DbEnum.IsolationLevelEnum.ReadCommitted)`（読み取りだけの処理。P層も同じ値で呼ぶ） |

### 1.2 引数クラス（`OrdTfParameterValue`。画面の入力と同じ形）

| メンバ | 型 | 意味 |
| --- | --- | --- |
| `CustomerID` | `string` | 顧客 ID。null・空文字は未指定 |
| `EmployeeID` | `int?` | 担当者 ID。null は未指定 |
| `OrderDateFrom` | `string` | 受注日（開始）。画面の入力のまま（`yyyy/MM/dd`）。null・空白だけは未指定（`DEC-ORD-09`） |
| `OrderDateTo` | `string` | 受注日（終了）。同上 |
| `ShippedStatus` | `string` | 「すべて」「未出荷」「出荷済」「納期遅延」のいずれか（画面の選択肢の値と同じ）。null・空文字は「すべて」 |
| `PageNo` | `int` | 表示するページ（1 始まり。`OrdTfSearch` のみ）。`EV-ORD-102` は 1、`EV-ORD-104` は移動先 |

### 1.3 戻り値クラス（`OrdTfReturnValue`。`ErrorFlag`・`ErrorMessage` は基底のもの）

| メンバ | 型 | 意味 |
| --- | --- | --- |
| `CustomerOptions`・`EmployeeOptions` | `List<OrdTfOption>`（`Value`・`Text` の文字列） | `OrdTfInit` の選択肢。**先頭に空白の選択肢（`Value`・`Text` とも空文字）を含む** |
| `Rows` | `List<OrdTfRow>` | `OrdTfSearch` の当該ページの行（最大 20 件） |
| `TotalCount` | `int` | 検索結果の総件数 |
| `TotalPages` | `int` | 総ページ数（最小 1。`DEC-ORD-02`） |
| `PageNo` | `int` | 実際に返したページ（移動先が総ページ数を超えるときは最終ページ。`DEC-ORD-06`） |
| `ErrorFields` | `List<string>` | 項目単位の検証の違反の項目（`"OrderDateFrom"`・`"OrderDateTo"`）。関連チェックの違反・正常は空 |

`OrdTfRow`：`OrderID`（`int`）・`CustomerName`・`EmployeeName`（`姓 + " " + 名`）・`ShipperName`（`string`。NULL は空文字か null）・`OrderDate`・`RequiredDate`・`ShippedDate`（`DateTime?`）・`TotalAmount`（`decimal`）・`Status`（`"未出荷"`・`"出荷済"`・`"納期遅延"`）。

### 1.4 その他の決め

- **「当日」は、アプリの実行環境の日付（`DateTime.Today`）**。DB の時計（`GETDATE()`）は使わない（コンテナの時刻が UTC のことがあり、ずれる）。`状況の判定`・`出荷状況「納期遅延」` の「`RequiredDate < 当日`」は、日付の単位（当日 00:00 より前）で比べる。
- **金額の計算は 10 進数（`decimal`）で行う**（仕様が決めていること：共通仕様 3 節・`LLD/TableSchema.md` の `Discount` の扱い。**実装の決定ではない**）。明細金額（`価格 × 数量`）だけを小数第 3 位で四捨五入（0 から離れる向き）し、`価格 = 単価 × (1 - 割引率)` は丸めない。小計 = Σ 丸めた明細金額。
- 画面（P層）の id・class（E2E のテストが決める。段 4 の実装がこれを付ける）は「4. 画面の E2E」の冒頭。

## 2. テストデータの決まり（B層・E2E 共通）

- テストが自分で入れて消す（親子は子から：`Order Details` → `Orders` → 顧客・担当者・配送業者）。Northwind の元の行は書き換えない。
- 顧客：`CustomerID` は英大文字 5 桁をランダムに作る。`CompanyName` は `"Tf" + <A〜Z 1 文字の目印> + <GUID 8 桁>`（目印で並びを決める）。担当者：`LastName` は `"Tf" + <目印> + <GUID 8 桁>`、`FirstName` は `<目印> + <GUID 4 桁>`（nvarchar(10) に収める）。配送業者：`CompanyName = "TfSh" + <GUID 8 桁>`。
- 受注の `OrderID` は IDENTITY（`INSERT …; SELECT CAST(SCOPE_IDENTITY() AS int);` で受ける）。`Order Details` の `ProductID` は 1・2 など任意（外部キー制約は無い）。
- **日付の範囲：B層のテストは 2199 年、E2E は 2198 年**（並行して動いても、条件なしの先頭ページを押し出し合わない）。顧客・担当者で絞るケースの受注日は任意だが、B層は 2199 年に揃える。**B層の「条件なし」のケース以外の受注日は `2199/12/29` 以前**（「条件なし」の T は `2199/12/30` ～ で最も新しくする）。「状況のデータ」の受注日は固定の `2199/03/01`（8 件とも）。時刻の細かい値は各ケースに書く。
- 状況のケースの `RequiredDate`・`ShippedDate` は、各ケースに書いた固定の日付（過去 `2000/01/01`・未来 `2199/12/31`）と、当日（`DateTime.Today`）で作る。

### 状況のデータ（顧客 A に 8 件。`状況` と抽出条件のケースで共通）

| 記号 | `RequiredDate` | `ShippedDate` | `状況` | 「未出荷」 | 「出荷済」 | 「納期遅延」 |
| --- | --- | --- | --- | --- | --- | --- |
| a | 2000/01/01（過去） | NULL | 納期遅延 | ○ | － | ○ |
| b | 当日 | NULL | 未出荷 | ○ | － | － |
| c | 2199/12/31（未来） | NULL | 未出荷 | ○ | － | － |
| d | NULL | NULL | 未出荷 | ○ | － | － |
| e | 2001/01/01 | 2001/01/05（遅れて出荷） | 納期遅延 | － | ○ | ○ |
| f | 2001/01/01 | 2001/01/01（納期当日に出荷） | 出荷済 | － | ○ | － |
| g | 2001/01/05 | 2001/01/01（早く出荷） | 出荷済 | － | ○ | － |
| h | NULL | 2001/01/01 | 出荷済 | － | ○ | － |

（「未出荷」「出荷済」は `EV-ORD-102` 手順 2 の文字どおり、`ShippedDate` の NULL か否かだけで絞る。納期遅延の行も、出荷の有無の側の絞り込みに含まれる。）

## 3. B層のルート メソッドのテスト（`OrdTfBusinessTests`。`Shared\WSServer_sample.Tests\`）

### 3.1 選択肢の構築（`OrdTfInit`。`EV-ORD-101` 手順 1）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfInit_顧客の先頭は空白の選択肢 | EV-ORD-101, SC-ORD-01-I01 | テスト用の顧客が 2 件 | `OrdTfInit` | ErrorFlag = false。CustomerOptions の先頭（0 番目）が Value = ""・Text = ""。Value = "" の選択肢はこの 1 件だけ |
| OrdTfInit_顧客の値と表示と並び | EV-ORD-101, SC-ORD-01-I01 | テスト用の顧客 K1・K2。`CustomerID` の昇順と `CompanyName` の昇順が**逆**（K1 の ID が小さく、会社名は K1 が大きい） | `OrdTfInit` | CustomerOptions に K1・K2 が含まれ、それぞれ Value = CustomerID・Text = CompanyName。K2 の選択肢が K1 より前にある（会社名の昇順） |
| OrdTfInit_担当者の先頭は空白の選択肢 | EV-ORD-101, SC-ORD-01-I02 | テスト用の担当者が 3 件 | `OrdTfInit` | EmployeeOptions の先頭が Value = ""・Text = ""。Value = "" の選択肢はこの 1 件だけ |
| OrdTfInit_担当者の値と表示と並び | EV-ORD-101, SC-ORD-01-I02 | テスト用の担当者 E1・E2・E3 を、この順に登録（ID は E1 < E2 < E3）。**E1 と E2 の LastName は同じ文字列（GUID も共有）、E3 の LastName はそれより大きい**（目印を `X`／`Y` にする）。**FirstName は E3 < E2 < E1 になる固定の先頭文字**（E1 = `C`＋GUID 4 桁、E2 = `B`＋…、E3 = `A`＋…） | `OrdTfInit` | EmployeeOptions に 3 件が含まれ、Value = EmployeeID の 10 進文字列、Text = `姓 + " " + 名`（C# で組んだ文字列）。並びは E2、E1、E3（姓の昇順、同じ姓の E1・E2 は名の昇順。名だけで並べると E3 が先頭になり、ID 順では E1 が先頭になる） |

### 3.2 受注の検索：抽出条件（`OrdTfSearch`。`EV-ORD-102` 手順 2）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfSearch_顧客で絞る | EV-ORD-102, SC-ORD-01-I01 | 顧客 A の受注 2 件、顧客 B の受注 1 件 | CustomerID = A、PageNo = 1 | ErrorFlag = false。Rows の OrderID が A の 2 件だけ。TotalCount = 2 |
| OrdTfSearch_担当者で絞る | EV-ORD-102, SC-ORD-01-I02 | 担当者 E1 の受注 2 件（顧客 A・B 各 1）、E2 の受注 1 件 | EmployeeID = E1 | Rows が E1 の 2 件だけ。TotalCount = 2 |
| OrdTfSearch_顧客と担当者はAND | EV-ORD-102 | 受注：(A, E1)・(A, E2)・(B, E1) | CustomerID = A、EmployeeID = E1 | Rows が (A, E1) の 1 件だけ |
| OrdTfSearch_受注日の開始を含む | EV-ORD-102, SC-ORD-01-I03 | 顧客 A の受注：`d-1 23:59`・`d 00:00`・`d+1 00:00`（`d` = 2199/03/10） | CustomerID = A、OrderDateFrom = "2199/03/10" | Rows が `d 00:00`・`d+1` の 2 件（`d-1 23:59` は含まない） |
| OrdTfSearch_受注日の終了は当日の時刻付きも含む | EV-ORD-102, SC-ORD-01-I04, DEC-ORD-07 | 顧客 A の受注：`d-1 00:00`・`d 00:00`・`d 15:30`・`d+1 00:00` | CustomerID = A、OrderDateTo = "2199/03/10" | Rows が `d-1`・`d 00:00`・`d 15:30` の 3 件（`d+1` は含まない） |
| OrdTfSearch_受注日の開始と終了が同じ日 | EV-ORD-102, SC-ORD-01-I03, I04, DEC-ORD-07 | 上と同じ 4 件 | CustomerID = A、OrderDateFrom = OrderDateTo = "2199/03/10" | ErrorFlag = false。Rows が `d 00:00`・`d 15:30` の 2 件 |
| OrdTfSearch_出荷状況で絞る（4 通り） | EV-ORD-102, SC-ORD-01-I05 | 「状況のデータ」の 8 件（顧客 A） | CustomerID = A、ShippedStatus = TestCase：「すべて」／「未出荷」／「出荷済」／「納期遅延」 | Rows の集合が、表の列のとおり：すべて = a〜h、未出荷 = a・b・c・d、出荷済 = e・f・g・h、納期遅延 = a・e。TotalCount も同じ件数 |
| OrdTfSearch_出荷状況が未指定ならすべて | EV-ORD-102, SC-ORD-01-I05 | 同上 | CustomerID = A、ShippedStatus = null と ""（TestCase） | Rows が a〜h の 8 件 |
| OrdTfSearch_空白だけの日付は未指定 | DEC-ORD-09, EV-ORD-102 | 顧客 A の受注 3 件（`2199/03/09`・`10`・`11`） | CustomerID = A、OrderDateFrom = "   "、OrderDateTo = " "（TestCase：両方空白／null／""） | ErrorFlag = false。Rows が 3 件（日付で絞らない） |
| OrdTfSearch_受注日がNULLの受注は日付の範囲を指定すると含まない | EV-ORD-102 手順 2（`OrderDate >= 開始日`・`<= 終了日`）, ORD-T1 | 顧客 A の受注 2 件：受注日 `2199/03/10` の 1 件と、受注日 NULL の 1 件 | (1) CustomerID = A、OrderDateFrom = "2199/03/01" のみ　(2) CustomerID = A、OrderDateTo = "2199/03/31" のみ（TestCase） | どちらも ErrorFlag = false。Rows が受注日 `2199/03/10` の 1 件だけ（NULL の受注は含まない）。TotalCount = 1 |
| OrdTfSearch_条件なしは全件の先頭ページ | EV-ORD-102 手順 2（未指定の条件は加えない）, SC-ORD-01-I06, 共通仕様 4 節（1 ページ 20 件） | **テストが受注を 21 件入れる**（既存データに頼らない）：T = 受注日 `2199/12/30`（全受注で最も新しい）、ほか 20 件 = 受注日 `2199/12/01`〜`12/20`（顧客はテスト用の 1 つ） | すべて未指定、PageNo = 1 | ErrorFlag = false。Rows[0].OrderID = T。Rows の件数 = 20（21 件入れたので、1 ページ目は 20 件で、残りの 1 件以上は次ページ）。TotalPages ≥ 2。PageNo = 1 |

### 3.3 受注の検索：並び・行の内容（`SC-ORD-01-I06`）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfSearch_受注日の降順で同日は受注IDの降順（時刻は見ない） | SC-ORD-01-I06, DEC-ORD-07 | 顧客 A の受注を次の順に登録（ID は昇順）：X = `2199/03/10 15:30`、Y = `2199/03/10 09:00`、Z = `2199/03/11 00:00`、W = `2199/03/09 00:00` | CustomerID = A | Rows の並びが Z、Y、X、W（同日の X・Y は、時刻の新しい X ではなく ID の大きい Y が先） |
| OrdTfSearch_受注日がNULLの受注は末尾 | DEC-ORD-08, ORD-T1 | 顧客 A の受注：受注日 `2199/03/10`（先に登録）と受注日 NULL（後に登録＝ID が大きい） | CustomerID = A | Rows の並びが（受注日あり、受注日 NULL）。NULL の行が末尾 |
| OrdTfSearch_行の内容 | SC-ORD-01-I06, EV-ORD-102 | 顧客 A（会社名 `CA`）・担当者 E1（姓 `LN`・名 `FN`）・配送業者 S（会社名 `CS`）に、受注日 `2199/03/10 15:30`・要求納期 `2199/04/01`・出荷日 `2199/03/20` の受注 1 件 | CustomerID = A | Rows の 1 件が OrderID = 登録した ID、CustomerName = "CA"、EmployeeName = "LN FN"、ShipperName = "CS"、OrderDate = 2199/03/10 15:30、RequiredDate = 2199/04/01、ShippedDate = 2199/03/20 |
| OrdTfSearch_顧客・担当者・配送業者がNULLでも行は残り名称は空 | SC-ORD-01-I06（列 2・3・7）, EV-ORD-102（外部結合） | 受注：`O1` = 顧客 NULL・E1・配送業者 S、`O2` = 顧客 A・担当者 NULL・S、`O3` = 顧客 A・E1・配送業者 NULL（いずれも明細なし） | (1) EmployeeID = E1　(2) CustomerID = A | (1) Rows が `O1`・`O3`（2 件）で、`O1` の CustomerName が空（null か ""）、`O3` の ShipperName が空。(2) Rows が `O2`・`O3` で、`O2` の EmployeeName が空 |
| OrdTfSearch_日付がNULLの列は null | SC-ORD-01-I06（列 4〜6）, ORD-T1 | 顧客 A の受注 1 件（受注日・要求納期・出荷日が NULL） | CustomerID = A | Rows の 1 件の OrderDate・RequiredDate・ShippedDate がいずれも null |
| OrdTfSearch_状況の判定 | EV-ORD-101 手順 5 の「状況の判定」, ORD-T7, ORD-T2 | 「状況のデータ」の 8 件 | CustomerID = A | 行ごとの Status が表の「状況」の列のとおり（a = 納期遅延、b = 未出荷、c = 未出荷、d = 未出荷、e = 納期遅延、f = 出荷済、g = 出荷済、h = 出荷済） |

### 3.4 受注の検索：合計金額（`受注検索クエリ`・共通仕様 3 節「金額の算出式」・`DEC-CMN-02`・`DEC-CMN-03`）

根拠の「共通仕様 3 節」は `HLD/Common.md` の「金額の算出式」（ID なし。`OD-T4` が従うと定める）。浮動小数点を避ける根拠は `LLD/TableSchema.md` の「`Discount`（`real`）の扱い」（比較・計算の前に 10 進数へ変換する。`CAST(Discount AS decimal(5,4))`）。いずれも顧客 A の受注 1 件を `CustomerID = A` で検索し、Rows[0].TotalAmount を比べる。

| ケース | 根拠 | 前提 | 期待 TotalAmount |
| --- | --- | --- | --- |
| OrdTfSearch_合計金額 = 小計 + 送料 | EV-ORD-102（受注検索クエリ）, 共通仕様 3 節 | 明細 1：単価 10.00・数量 3・割引 0。明細 2：単価 20.00・数量 2・割引 0.25。送料 5.50 | 65.50（30.00 + 15.00 × 2 + 5.50） |
| OrdTfSearch_明細金額ごとに四捨五入して合算する | OD-T4, 共通仕様 3 節（10 進数・小数第 3 位を四捨五入・明細金額を丸めて合算） | 明細 2 件とも：単価 0.50・数量 1・割引 0.15（0.425 → 0.43）。送料 10.00 | 10.86（0.43 + 0.43 + 10.00。浮動小数点なら 0.42、丸めてから足さないなら 0.85 になる） |
| OrdTfSearch_明細金額の丸めは数量を掛けた後 | OD-T4, 共通仕様 3 節 | 明細 1 件：単価 0.50・数量 3・割引 0.15（0.425 × 3 = 1.275 → 1.28）。送料 0 | 1.28（価格を丸めてから掛けると 1.29） |
| OrdTfSearch_明細が無い受注の合計は送料だけ | DEC-CMN-02 | 明細なし。送料 7.25 | 7.25 |
| OrdTfSearch_送料がNULLなら0として足す | DEC-CMN-03 | 明細 1 件：単価 12.50・数量 1・割引 0。送料 NULL | 12.50 |
| OrdTfSearch_明細も送料も無い受注の合計は0 | DEC-CMN-02, DEC-CMN-03 | 明細なし。送料 NULL | 0 |
| OrdTfSearch_明細が複数でも受注は1行 | EV-ORD-102（受注検索クエリ）, SC-ORD-01-I07 | 明細 3 件の受注 1 件 | Rows が 1 行、TotalCount = 1（TotalAmount は 3 件の合計） |

### 3.5 受注の検索：ページング（`EV-ORD-102` 手順 3〜4、`EV-ORD-104`、`DEC-ORD-02`・`DEC-ORD-06`）

受注日は、ID の昇順に 1 日ずつ新しくする（i 番目 = 2199/01/01 + i 日。i は 1 から）。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfSearch_総件数と総ページ数 | EV-ORD-102 手順 4, SC-ORD-01-I07, I08, 共通仕様 4 節（1 ページ 20 件） | 顧客 A の受注 N 件（TestCase：20／21／40／45） | CustomerID = A、PageNo = 1 | TotalCount = N、TotalPages = TestCase：1／2／2／3 |
| OrdTfSearch_ページごとの20件 | EV-ORD-104 手順 2, SC-ORD-01-I06 | 顧客 A の受注 45 件。さらに顧客 B の受注 3 件（A のどれよりも新しい日付） | CustomerID = A、PageNo = 1・2・3（TestCase） | PageNo = 1：Rows が新しい順の 20 件（i = 45〜26）。2：20 件（i = 25〜6）。3：5 件（i = 5〜1）。いずれも TotalCount = 45、TotalPages = 3、返した PageNo = 入力。B の受注は含まない（絞り込みを保ったまま移る） |
| OrdTfSearch_0件のとき | EV-ORD-101 手順 7, DEC-ORD-02 | 受注が 1 件も無い顧客 A | CustomerID = A、PageNo = 1 | ErrorFlag = false（エラーにしない）。Rows が 0 件、TotalCount = 0、TotalPages = 1、PageNo = 1 |
| OrdTfSearch_移動先が無くなっていたら最終ページ | DEC-ORD-06, EV-ORD-104 手順 3〜4 | 顧客 A の受注 25 件（2 ページ分） | CustomerID = A、PageNo = 3 | ErrorFlag = false。PageNo = 2、TotalPages = 2、Rows が最終ページの 5 件（i = 5〜1） |
| OrdTfSearch_0件で2ページ目を求めたら1ページ目 | DEC-ORD-06, DEC-ORD-02 | 受注が 1 件も無い顧客 A | CustomerID = A、PageNo = 2 | ErrorFlag = false。PageNo = 1、TotalPages = 1、Rows が 0 件 |

### 3.6 受注の検索：検証（`EV-ORD-102` 検証内容・`VAL-DATE`・`ERR-VALIDATION`・`DEC-ORD-09`・`DEC-CMN-04`・`DEC-CMN-05`）

文言：`F` = 「受注日は yyyy/MM/dd の形式で指定してください。」、`R` = 「受注日の開始は終了以前を指定してください。」（`DEC-CMN-01`：文言で確かめる）。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfSearch_開始の形式違反 | VAL-DATE, ERR-VALIDATION, DEC-ORD-09, DEC-CMN-05 | — | OrderDateFrom = TestCase：`abc`／`2199/1/5`／`2199-01-05`／`2199/02/30`、OrderDateTo = 未指定 | ErrorFlag = true、ErrorMessage = `F`、ErrorFields = ["OrderDateFrom"] |
| OrdTfSearch_終了の形式違反 | VAL-DATE, ERR-VALIDATION, DEC-ORD-09, DEC-CMN-05 | — | OrderDateTo = 同じ 4 通り、OrderDateFrom = 未指定 | ErrorFlag = true、ErrorMessage = `F`、ErrorFields = ["OrderDateTo"] |
| OrdTfSearch_開始も終了も形式違反 | DEC-CMN-05, DEC-ORD-09 | — | OrderDateFrom = `abc`、OrderDateTo = `2199-01-05` | ErrorFlag = true、ErrorMessage = `F`、ErrorFields が "OrderDateFrom"・"OrderDateTo" の両方 |
| OrdTfSearch_開始が終了より後 | VAL-DATE, EV-ORD-102 検証内容, DEC-CMN-04 | — | OrderDateFrom = "2199/03/11"、OrderDateTo = "2199/03/10" | ErrorFlag = true、ErrorMessage = `R`、ErrorFields が空（関連チェックは項目に紐付けない） |
| OrdTfSearch_検証に通る日付は受け付ける | VAL-DATE, DEC-ORD-09 | 顧客 A の受注 1 件（受注日 `2199/03/10`） | CustomerID = A、OrderDateFrom = "2199/01/05"、OrderDateTo = "2199/12/31" | ErrorFlag = false、ErrorFields が空、Rows が 1 件 |

## 4. 画面の E2E（`MVC_Sample.E2ETests\OrdTfScreenTests`）

B層で確かめることは繰り返さない（抽出条件・並び・金額の計算・状況の判定は 3 章）。画面の表示・活性・条件の保持・メッセージの区別を確かめる。

### 4.1 画面の id・class（テストが決める。段 4 の実装が付ける）

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

### 4.2 ケース

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

| 対象 | 理由 |
| --- | --- |
| `ERR-DB`（接続・実行の失敗の文言と「画面の状態を変えない」）、`PRE-DB`、システム エラー（`BusinessSystemException`） | テストから DB を落とせない・接続文字列を壊す方法が共用の設定を汚すため。確かめられない。`DEC-CMN-06`（情報の後にエラーを出すときの表示）は「定めない」なので、情報の直後にエラーを出す操作はテストにしない |
| `EV-ORD-101` 手順 7（0 件の知らせ）を**初期表示**で確かめる | 既存の受注が 830 件あり、条件なしの初期表示は 0 件にならない。同じ表示を「検索」の 0 件のケースで確かめる（B層は `OrdTfSearch_0件のとき`） |
| 範囲外の `EV-ORD-103`（条件クリア）・`105`（並べ替え）・`106`〜`108`、列見出しの並べ替え、受注 ID のリンク、「操作」列、他の画面からの復元（画面遷移 F-2） | 対象の仕様で範囲外 |
| `OrdTfInit` の配送業者・商品の選択肢（「選択肢の構築」の残り） | この画面（`I01`〜`I08`）が使わない |

## 6. 仕様の決定

| # | 仕様の ID | 問い | 決定 |
| --- | --- | --- | --- |
| 1 | `EV-ORD-102` 検証内容（`VAL-DATE`・`DEC-ORD-09`） | 1753/01/01 より前の日付（例 `1000/01/01`）は `DEC-ORD-09` の形式としては受け付けるが、`datetime` が持てない。どう扱うか | **決定済み：定めない（`DEC-ORD-10`）。テストの期待にしない**（ケースは作らない。決定のレポート `docs/spec/test3-testfirst-OrdTf-mvc-decisions.md`） |
