# テスト仕様：受注帳票プレビュー OrdTfReport（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

起こした仕様の版：NorthwindDbSampleSpec@b60c760（ブランチ proxy/ord-management。**起こしたのは 2962688**。2962688 → b60c760 の差分は決定記録だけ〔`LLD/Decisions.md` の索引の件数と、`LLD/Decisions/Orders.md` に DEC-ORD-64〜72 を足した。本文は変わっておらず、期待も変わらない〕で、この決定記録を足した版が b60c760。本文 `HLD/Common.md`・`HLD/UI_FlowList.md`・`LLD/EventSpec/Orders.md`・`LLD/UI_ElementsAndEventList/Orders.md`・`LLD/TableSchema/Orders.md`・`LLD/TableSchema/Employees.md` と、決定記録 `LLD/Decisions/Common.md`〔DEC-CMN-01〜07〕・`LLD/Decisions/Orders.md`〔DEC-ORD-01〜72〕を合わせて読んだ。`SC-ORD-05`・`EV-ORD-501`〜`504` を直接決めたのは DEC-ORD-64〜72）
規約：`docs/spec/cmn-framework.md` の「画面共通」「MVC」、`docs/spec/cmn-testfirst.md`、`docs/spec/test3-testfirst-OrdTfReport.md`、`docs/plan/cmn-testfirst.md` の「B層を共用する構成」
前の回：[受注一覧](./test3-testfirst-OrdTf-mvc-core-tests.md)・[受注詳細（照会・削除）](./test3-testfirst-OrdTfDetail-mvc-core-tests.md)・[受注詳細（登録・編集）](./test3-testfirst-OrdTfEdit-mvc-core-tests.md)・[出荷ステータス更新ダイアログ](./test3-testfirst-OrdTfShip-mvc-core-tests.md)（承認・実装済み。このケースはその上に足す）。決定のレポート：`test3-testfirst-OrdTfReport-mvc-core-decisions.md`（司令役が書く）

対象：`SC-ORD-05`（`I01`〜`I14`、`B01`・`B02`）、`SC-ORD-02` の `B04`・`B05`、`EV-ORD-501`〜`504`、`EV-ORD-204`。
範囲外：ほかのモジュールの帳票（`SC-SUP-05` など）、ブラウザの印刷のダイアログそのものの操作（印刷を呼んだことまでを確かめる）。

> **仕様の決定は 6 章（決定 1〜10。すべて答え済み。決めた者：代行）。** 本文にも決定記録にも決まりが無く、期待が変わるものを上げた。答えはどれも推奨のとおりだった。
> 根拠の列は、決定記録に記録された答えを `DEC-ORD-*` で書く（決定 2〜10 → DEC-ORD-64〜72）。**決定 1 は規約の解釈で、決定記録に入らないので「決定 1」のまま**。
> 決定 10（DEC-ORD-72）は答えが「定めない」なので、`帳票_合計の強調` は作らない（5 章）。

## 0. 進め方の前提

- B層は `Shared\`（`OrdTfBusiness`・`OrdTfParameterValue`・`OrdTfReturnValue`）を共用する。**この回の B層（`OrdTfReport`）とそのテスト（`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs` に足す）はこのケースで作り**、net10.0（`MVC_Sample_Core\MVC_Sample.Tests`）→ net48（`MVC_Sample\MVC_Sample.Tests`）の順に、同時にでなく全件流す。
- テストのプロジェクト、アプリの起動、ログイン、ブラウザ、日付の範囲（B層のテスト 2199 年・E2E 2198 年。**支払期限の境界のケースだけ、日付を直接渡すので 2199 年〜2204 年**）、並行実行の決まりは、受注一覧・受注詳細・受注詳細（登録・編集）・出荷ダイアログのとおり（新しいプロジェクトは作らない）。E2E は新しいクラス `OrdTfReportScreenTests`。
- 表示のメッセージ・文言は仕様の文言そのまま（`DEC-CMN-01`）。
- **この回の画面は DB を書き換えない**（`EV-ORD-501` 手順 8）。テストが期待を決めるための受注・明細・顧客・担当者・配送業者・商品は、**テストが自分で作り、後片付けで消す**（既存の `TestData`。足さない）。Northwind2 の既存の行は書き換えない。
- **終わりの確かめは、計画の手順に入れる**（範囲外。この回の始めの値と比べる）：受注 830 件・明細 2155 件・顧客 91 件・商品 77 件、既存の商品（ProductID 1〜77）の `UnitsInStock` の合計が 3119。
- 既存のテスト（受注一覧・受注詳細・受注詳細（登録・編集）・出荷ダイアログ）は変えない。詳細に `#btnDelivery`・`#btnInvoice` を足しても、既存の `OrdTfDetailScreenTests`（`#contents` の入力欄が 0、`#btnMain1`〜`#btnMain5` など）が通ることを、段 4 で全件流して確かめる。期待値の変更は無い。

## 1. テストが固定する契約（実装の決定。振る舞いは変えない。段 4 の実装はこれに従う）

### 1.1 B層

分離レベルは `ReadCommitted`（テストの呼び出しの値）。`MethodName` = `OrdTfReport`（`EV-ORD-501`）。読むだけで DB を更新しない（手順 8）。

**引数クラス `OrdTfParameterValue` に足すメンバ**（既存は変えない。`OrderID` は既存の意味のまま再利用する）

| メンバ | 型 | 意味 |
| --- | --- | --- |
| `IssueDate` | `DateTime?` | 発行日（`SC-ORD-05-I03`）。**null は当日（`DateTime.Today`）**。画面（P層）は渡さない。**テストが支払期限（発行日の翌月末日）の月末・閏年・年またぎを確かめるための入力**（仕様に無い入力だが、既定が仕様どおり＝当日なので振る舞いは変わらない。実装の決定） |

**戻り値クラス `OrdTfReturnValue` に足すメンバ**（既存は変えない。`OrderID`・`EmployeeName`・`ShipperName`・`OrderDate`・`ShippedDate`・`ShipName`〜`ShipCountry`・`Lines`〔`OrdTfDetailLine`〕・`Subtotal`・`Freight`〔`decimal?`。NULL は null〕・`Total` は、受注詳細と同じ意味のまま再利用する）

| メンバ | 型 | 意味 |
| --- | --- | --- |
| `ShipToText` | `string` | 宛先（I05）。出荷先情報（`ShipName`・`ShipAddress`・`ShipCity`・`ShipRegion`・`ShipPostalCode`・`ShipCountry`）をこの順に連結した文字列。顧客マスタの住所は使わない。区切りと空の要素の扱いはDEC-ORD-65 |
| `IssueDate` | `DateTime` | 発行日（I03）。引数の `IssueDate`（null なら当日）。時刻は 00:00 |
| `DueDate` | `DateTime` | 支払期限（I14）。`IssueDate` の翌月の末日。**帳票種別によらず常に返す**（納品書で表示しないのは P層の表示の規則＝手順 7） |

- `Lines`（`OrdTfDetailLine`）の `Price`（`単価 × (1 − 割引率)`。丸めない）・`Amount`（`Price × 数量` を小数第 3 位で四捨五入）は受注詳細と同じ。`UnitsInStock` は帳票に出さないので期待にしない。並びは商品名の昇順。
- 業務エラー：`ERR-NOTFOUND` は `ErrorFlag = true`、`ErrorMessage` =「対象のデータは存在しません。」（共通仕様 7 節の文言）。
- 金額は 10 進数（`decimal`）で計算する（共通仕様 3 節）。**列名・値を文字列で連結して SQL を組み立てない**（パラメタ化）。
- 帳票種別（納品書・請求書）は B層の入力にしない（取得するデータは同じ。種別による表示の切替は P層。手順 7）。

### 1.2 P層（MVC）の URL と動き

| 対象 | 指定 |
| --- | --- |
| 帳票プレビュー（`SC-ORD-05`） | GET `/OrdTf/Report?id=<受注 ID>&type=<delivery｜invoice>`（`delivery` = 納品書、`invoice` = 請求書。遷移パラメータ＝`EV-ORD-204` の受注 ID・帳票種別）。`ERR-NOTFOUND` は、一覧（`/OrdTf/Back` と同じ表示）へ移り、`#messageArea` にエラーを出す（`BackWithMessage`。一覧状態の扱いはDEC-ORD-70） |
| 詳細の納品書・請求書（`SC-ORD-02-B04`・`B05`） | `<a>` か `<button>`（`#btnDelivery`＝キャプション「納品書」、`#btnInvoice`＝キャプション「請求書」。画面上部。`#contents` の中で `#footer-buttons` の外。常時活性）。選ぶと GET `/OrdTf/Report?id=<その受注>&type=delivery｜invoice` へ遷移する（`EV-ORD-204`） |
| 戻る（`SC-ORD-05-B02`） | `#btnBack`（キャプション「戻る」）。GET `/OrdTf/Detail?id=<受注 ID>` へ遷移する（`EV-ORD-504`）。保持された一覧状態は変えない |
| 種別の切替（`EV-ORD-502`） | `#reportType` の選択の変更で、**クライアント側だけ**（JavaScript）で、帳票タイトル・出荷日・配送業者・支払期限の表示を切り替える。**サーバへ要求を出さない**（`EV-ORD-502` 手順 3：DB を読み直さない） |
| 印刷（`EV-ORD-503`） | `#btnPrint` の選択で `window.print()` を呼ぶだけ（サーバへ要求を出さない。手順 3）。**印刷の対象から除く**（手順 1）は、CSS の `@media print`（印刷メディアで `display: none`）で行う。除く対象：`#reportType`・`#btnPrint`・`#btnBack`・画面共通のヘッダ（`.page-header`）・メニュー（`.nav-side-menu`）。**共通のレイアウト（`_Layout.cshtml`）・共通の CSS を変えず**、この画面のビューの中（`HeaderScriptsSection` など）に置く。変えるなら報告の冒頭に書く（`QUALITY.md` 1 節） |
| 送信 | 帳票は GET だけ（状態を変える要求が無いので CSRF のトークンは要らない） |

- 一覧状態（Session の `OrdTfListState`）は、この回の画面は書き換えない。
- 受注 ID が無い・数値でない、帳票種別が `delivery`・`invoice` でないときはDEC-ORD-71。

## 2. 画面の id・class（段 4 の実装が付ける）

### 2.1 受注帳票プレビュー（`SC-ORD-05`。`/OrdTf/Report`）

| 対象 | 指定 |
| --- | --- |
| 帳票種別（I01） | `<select id="reportType">`。`option` は 2 つ（テキスト「納品書」`value="delivery"`、「請求書」`value="invoice"`。この順）。遷移パラメータの種別が選択されている。空の選択肢は無い |
| 帳票タイトル（I02）・発行日（I03）・受注 ID（I04）・宛先（I05）・受注日（I06） | `#reportTitle`・`#issueDate`・`#orderId`・`#shipTo`・`#orderDate`（テキスト＝値。NULL は空） |
| 出荷日（I07） | 値 `#shippedDate`。**キャプションと値を含む行 `#rowShippedDate`**（納品書のとき可視、請求書のとき不可視。**値が空でも行は可視**＝Playwright は空の要素を不可視と見なすので、表示の確かめは行で行う） |
| 配送業者（I08） | 値 `#shipperName`、行 `#rowShipper`（出荷日と同じ） |
| 担当者名（I09） | `#employeeName`（「姓 名」。`EMP-T1`。担当者が無いとき空） |
| 明細一覧（I10） | `<table id="reportLines">`（`thead th` が 6 列：商品名・単価・割引率・価格・数量・明細金額。`tbody tr` が明細 1 行。セルは左から 商品名・単価・割引率・価格・数量・明細金額）。商品名の昇順 |
| 小計・送料・合計（I11〜I13） | `#subtotal`・`#freight`・`#total`。金額は通貨記号なしの千区切り＋小数 2 桁（`1,186.50`）。送料 NULL は空 |
| 支払期限（I14） | 値 `#dueDate`、行 `#rowDueDate`（請求書のとき可視、納品書のとき不可視） |
| 日付 | `yyyy/MM/dd`（時刻は出さない。共通仕様 4 節） |
| 印刷（B01） | `#btnPrint`（キャプション「印刷」。`#contents` の中。常時活性。置き場所は決定 1） |
| 戻る（B02） | `#btnBack`（キャプション「戻る」。`#contents` の中で `#footer-buttons` の外〔画面上部。対象の仕様が画面定義書の配置と決めている。決定 1 の対象外〕。常時活性） |
| 共通メッセージ領域 | `#messageArea`（正常な表示では空） |

### 2.2 受注詳細（`SC-ORD-02`）への追加（既存の指定は変えない）

| 対象 | 指定 |
| --- | --- |
| 納品書（B04）・請求書（B05） | `#btnDelivery`・`#btnInvoice`（1.2 のとおり。画面上部の `#btnEdit`・`#btnShip` と同じ並び） |

## 3. B層のルート メソッドのテスト（`OrdTfBusinessTests`。net10.0 → net48 の順に流す）

`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs` に足す（既存の呼び出しの部品 `Call`・`D` と同じ形。`Call("OrdTfReport", pv => pv.OrderID = …)`）。**既存のケースは変えない。**
共通の前提：受注日などの日付は 2199 年（時刻 00:00 を既定）。テストが入れた顧客・担当者・配送業者・商品・受注だけを使う（`TestData`）。

### 3.1 `OrdTfReport`（`EV-ORD-501`）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfReport_受注情報 | EV-ORD-501 手順 1・2, SC-ORD-05-I04・I06〜I09, EMP-T1 | 顧客 K・担当者 E（姓・名）・配送業者 S。受注 1 件：K・E・S、受注日 `2199/03/05 15:30`、出荷日 `2199/03/10`、送料 0 | OrderID = その受注 | ErrorFlag = false。OrderID = その受注、OrderDate = `2199/03/05 15:30`、ShippedDate = `2199/03/10`、EmployeeName = 「姓 名」（姓 + 半角空白 + 名）、ShipperName = S の会社名 |
| OrdTfReport_宛先は出荷先情報で顧客の住所ではない | EV-ORD-501 手順 6, SC-ORD-05-I05, ORD-T6（出荷先は受注ごとに独立） | 顧客 K：Address `CustStreet`・City `CustCity`・Region `CustRegion`・PostalCode `000-0000`・Country `CustLand`。受注 1 件：**CustomerID = K**（顧客の住所が受注から引ける状態にする）、ShipName `ShipTo Co`・ShipAddress `4-5-6 Ginza`・ShipCity `Chuo`・ShipRegion `Tokyo`・ShipPostalCode `104-0061`・ShipCountry `Japan` | OrderID = その受注 | ShipToText が、`ShipTo Co`・`4-5-6 Ginza`・`Chuo`・`Tokyo`・`104-0061`・`Japan` をこの順に含む（正規表現 `^ShipTo Co([\s\S]*)4-5-6 Ginza([\s\S]*)Chuo([\s\S]*)Tokyo([\s\S]*)104-0061([\s\S]*)Japan$`。**区切りは期待にしない**）。`CustStreet`・`CustCity`・`CustRegion`・`000-0000`・`CustLand` のどれも含まない |
| OrdTfReport_担当者・配送業者・日付が無いと空 | EV-ORD-501 手順 2, 共通仕様 4 節（空値の表示）, ORD-T1 | 受注 1 件：EmployeeID・OrderDate・ShippedDate・ShipVia が NULL、送料 0 | OrderID = その受注 | ErrorFlag = false。EmployeeName・ShipperName が空文字。OrderDate・ShippedDate が null |
| OrdTfReport_明細の項目と並びと金額 | EV-ORD-501 手順 3・4, SC-ORD-05-I10〜I13, 共通仕様 3 節, OD-T4 | 受注 1 件（送料 `34.50`）。商品 A・M・X（名前の目印 `A`・`M`・`X`。名前の昇順 A < M < X）を **M・X・A の順に**明細へ入れる：M = 単価 `50.00`・割引 `0.25`・数量 2、X = `20.00`・`0.05`・3、A = `100.00`・`0.15`・12 | OrderID = その受注 | Lines が 3 行で A・M・X の順。A：UnitPrice = 100.00・Discount = 0.15・Price = 85.00・Quantity = 12・Amount = 1020.00。M：50.00・0.25・37.50・2・75.00。X：20.00・0.05・19.00・3・57.00。Subtotal = 1152.00、Freight = 34.50、Total = 1186.50 |
| OrdTfReport_明細金額は四捨五入して合算する | 共通仕様 3 節（明細金額は小数第 3 位を四捨五入し、その値を合算）, EV-ORD-501 手順 4 | 受注 1 件（送料 10.00）に、単価 `0.50`・割引 `0.15`・数量 1 の明細を商品 2 つで 2 行 | OrderID = その受注 | 各行の Price = 0.425、Amount = 0.43（偶数丸めなら 0.42）。Subtotal = 0.86、Total = 10.86 |
| OrdTfReport_明細金額の丸めは数量を掛けた後 | 共通仕様 3 節, EV-ORD-501 手順 4 | 受注 1 件（送料 0）に、単価 `0.50`・割引 `0.15`・数量 3 の明細 1 行 | OrderID = その受注 | Price = 0.425、Amount = 1.28（`0.425 × 3 = 1.275` を丸める。価格を先に丸めると 1.29） |
| OrdTfReport_明細が無い受注 | DEC-CMN-02, EV-ORD-501 手順 3・4 | 明細が 0 件の受注（送料 `7.25`） | OrderID = その受注 | ErrorFlag = false。Lines が 0 行、Subtotal = 0、Freight = 7.25、Total = 7.25 |
| OrdTfReport_送料がNULLなら0として足す | DEC-CMN-03, 共通仕様 4 節（空値の表示） | 受注 1 件（送料 NULL）に、単価 `12.50`・割引 0・数量 1 の明細 1 行 | OrderID = その受注 | Freight = null、Subtotal = 12.50、Total = 12.50 |
| OrdTfReport_ほかの受注の明細は含めない | EV-ORD-501 手順 3（OrderID で抽出） | 受注 o1（明細 2 行）と受注 o2（明細 1 行） | OrderID = o1 | Lines が 2 行で、o2 の明細の商品を含まない。Subtotal は o1 の明細だけの合計 |
| OrdTfReport_存在しない受注はERR-NOTFOUND | EV-ORD-501 手順 1, ERR-NOTFOUND | テストが受注を入れて `DeleteOrder` で消した ID | OrderID = その ID | ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」 |
| OrdTfReport_発行日は当日 | EV-ORD-501 手順 5, SC-ORD-05-I03（発行日＝当日）, SC-ORD-05-I14 | 受注 1 件 | OrderID = その受注（IssueDate は指定しない）。呼び出しの前後で `DateTime.Today` を取る | ErrorFlag = false。IssueDate = その日（前後の `DateTime.Today` のどちらか。日付をまたいだときだけ両方を許す）。DueDate = IssueDate の翌月の末日（その日の月の翌々月の 1 日の前日。例：テストの中で `new DateTime(y, m, 1).AddMonths(2).AddDays(-1)`） |
| OrdTfReport_支払期限は発行日の翌月末日（TestCase：下の 7 行） | SC-ORD-05-I14（発行日の翌月末日）, EV-ORD-501 手順 5・7 | 受注 1 件 | OrderID = その受注、IssueDate = 下表の発行日 | IssueDate = 発行日（時刻 00:00）、DueDate が下表のとおり |
| OrdTfReport_読むだけで受注・明細を更新しない | EV-ORD-501 手順 8（DB への保存・ファイル出力・非同期処理は行わない）, 共通仕様 9.1 節 | 受注 1 件（明細 2 行・送料 5.00。登録直後＝RowVersion 1） | OrderID = その受注で 2 回続けて呼ぶ | 1 回目：ErrorFlag = false、OrderID = その受注、Total = 明細の合計 + 5.00。2 回目も同じ。呼んだ後も `GetRowVersion` = 1、`CountOrders` = 1、`CountDetails` = 2、`GetOrder` の送料・出荷日・Ship* と `GetDetails` の単価・数量・割引・RowVersion が呼ぶ前と同じ |
| OrdTfReport_宛先は空の要素を詰めて連結する | SC-ORD-05-I05, DEC-ORD-65 | 受注 4 件：o0 = ShipName `S1`・ShipAddress `A1`・ShipCity `C1`・ShipRegion NULL・ShipPostalCode NULL・ShipCountry `Jp`、o1 = 同じで ShipRegion NULL・ShipPostalCode 空文字、o2 = ShipName `S1`・ShipAddress `A1`・ShipCity `C1`・ShipRegion `R1`・ShipPostalCode `P1`・ShipCountry `Jp`、o3 = Ship* がすべて NULL | それぞれの OrderID | ShipToText が、o0 = 正規表現 `^S1([\s\S]*)A1\1C1\1Jp$`、o1 = 同じ正規表現で o0 と同じ文字列、o2 = `^S1([\s\S]*)A1\1C1\1R1\1P1\1Jp$`（区切りの文字は期待にしない。空の要素の分の区切りが残らない）。o3：ShipToText が空文字 |
| OrdTfReport_商品マスタに無い明細は含めない | EV-ORD-501 手順 3（Products を内部結合）, DEC-ORD-66 | 受注 1 件（送料 `5.00`）に、存在する商品の明細 1 行（単価 `10.00`・割引 0・数量 2）と、テストが入れて `DeleteProduct` で消した ProductID の明細 1 行（単価 `100.00`・割引 0・数量 1） | OrderID = その受注 | ErrorFlag = false、Lines が 1 行。Subtotal = 20.00、Total = 25.00（商品マスタに無い明細の金額 100.00 を含めない） |
| OrdTfReport_廃番商品の商品名に付記しない | SC-ORD-05-I10（列 1「商品名」）, DEC-ORD-67 | 受注 1 件に商品 P（廃番でない）・Q（廃番）を入れる | OrderID = その受注 | P・Q とも ProductName が、それぞれの商品の名前と同じ（「（廃番）」を含まない） |

**支払期限の 7 行**（日付は 00:00。月末・閏年・年またぎ）：

| 発行日 | 支払期限（DueDate） | 見ること |
| --- | --- | --- |
| `2199/01/31` | `2199/02/28` | 31 日 → 28 日の月へ（平年） |
| `2199/12/31` | `2200/01/31` | 年またぎ |
| `2200/01/31` | `2200/02/28` | 2200 年は 100 の倍数で 400 の倍数でない＝平年（4 で割れても閏年にしない） |
| `2204/01/31` | `2204/02/29` | 閏年 |
| `2199/03/31` | `2199/04/30` | 31 日 → 30 日の月へ |
| `2199/11/30` | `2199/12/31` | 30 日の月 → 31 日の月 |
| `2199/06/01` | `2199/07/31` | 月初 |

- 追加：**16 メソッド・22 テストケース**（支払期限が 7 ケース。DEC-ORD-65〜67 のケース 3 メソッドを含む）。
- 「受注詳細」などの既存の B層のテストは変えない。net10.0 でも net48 でも同じ結果になること。

## 4. 画面の E2E（`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfReportScreenTests`。新しいクラス）

B層で確かめることは繰り返さない（金額の丸め・支払期限の月末・読むだけで更新しないことは B層）。画面の表示・種別による表示の切替・印刷・遷移を確かめる。
共通の前提：`[SetUp]` でログイン（既存の E2E と同じ部品）、テストが入れたデータ（2198 年）を使う。**要素が無いときに操作が待ち続けないよう、操作の前に `Expect` で要素を待つ**（Red を `Expect` の失敗にする）。**表示されていることの確かめは `ToBeVisibleAsync`、表示されないことの確かめは `ToBeHiddenAsync`**（`ToBeEnabledAsync` は不可視の要素でも通る）。
帳票は URL を直接開くか、詳細のボタンから開く（下の各ケース）。日付・金額の書式は 2.1 のとおり。「今日」は `DateTime.Today`（テストの実行時）、「翌月末」は `new DateTime(今日.Year, 今日.Month, 1).AddMonths(2).AddDays(-1)` の `yyyy/MM/dd`。

共通のデータ（「データ R」）：顧客 K（Address `CustStreet`・City `CustCity`・Country `CustLand`）・担当者 E（姓・名）・配送業者 S。受注 1 件：K・E・S、受注日 `2198/03/05 15:30`、要求納期 `2198/04/01`、出荷日 `2198/03/10`、送料 `34.50`、ShipName `ShipTo Co`・ShipAddress `4-5-6 Ginza`・ShipCity `Chuo`・ShipRegion `Tokyo`・ShipPostalCode `104-0061`・ShipCountry `Japan`。明細 3 行（商品 A・M・X〔名前の目印 `A`・`M`・`X`。名前の昇順〕を M・X・A の順に登録）：A = 単価 `100.00`・割引 `0.15`・数量 12、M = `50.00`・`0.25`・2、X = `20.00`・`0.05`・3（3.1 の「明細の項目と並びと金額」と同じ）。

### 4.1 ケース

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 帳票_納品書の表示 | EV-ORD-501 手順 1〜7（納品書）, SC-ORD-05-I01〜I13, EMP-T1, 共通仕様 3・4 節（日付・金額の表示） | データ R | `/OrdTf/Report?id=<その受注>&type=delivery` を開く | `#reportType` の選択が「納品書」（value `delivery`）で、`option` が「納品書」「請求書」の 2 つ。`#reportTitle` = 「納品書」。`#issueDate` = 今日（`yyyy/MM/dd`）。`#orderId` = 受注 ID。`#shipTo` が `ShipTo Co`・`4-5-6 Ginza`・`Chuo`・`Tokyo`・`104-0061`・`Japan` をこの順に含み（3.1 と同じ正規表現。区切りは期待にしない）、`CustStreet`・`CustCity`・`CustLand` を含まない。`#orderDate` = `2198/03/05`（時刻は出ない）。`#rowShippedDate` が可視で `#shippedDate` = `2198/03/10`、`#rowShipper` が可視で `#shipperName` = S の会社名。`#rowDueDate` が不可視。`#employeeName` = 「姓 名」。`#reportLines thead th` が「商品名」「単価」「割引率」「価格」「数量」「明細金額」。`#reportLines tbody tr` が 3 行で A・M・X の順、各行のセル（DEC-ORD-68：割引率は百分率）が A = [名前, `100.00`, `15%`, `85.00`, `12`, `1,020.00`]、M = [名前, `50.00`, `25%`, `37.50`, `2`, `75.00`]、X = [名前, `20.00`, `5%`, `19.00`, `3`, `57.00`]。`#subtotal` = `1,152.00`、`#freight` = `34.50`、`#total` = `1,186.50`（強調の class は期待にしない。DEC-ORD-72）。`#messageArea` は空 |
| 帳票_請求書の表示 | EV-ORD-501 手順 5・7（請求書）, SC-ORD-05-I01〜I03・I14 | データ R | `/OrdTf/Report?id=<その受注>&type=invoice` を開く | `#reportType` の選択が「請求書」（value `invoice`）。`#reportTitle` = 「請求書」。`#issueDate` = 今日。`#rowDueDate` が可視で `#dueDate` = 翌月末。`#rowShippedDate` と `#rowShipper` が不可視。`#orderId`・`#shipTo`・`#orderDate`・`#employeeName`・明細（A・M・X の順）・`#subtotal`・`#freight`・`#total` は納品書のときと同じ値。`#messageArea` は空 |
| 帳票_NULLは空欄で金額は0 | EV-ORD-501 手順 2・4・7, 共通仕様 4 節（空値の表示）, DEC-CMN-02・03, ORD-T1 | 受注 1 件：CustomerID・EmployeeID・OrderDate・RequiredDate・ShippedDate・ShipVia・送料・Ship* がすべて NULL、明細なし | `/OrdTf/Report?id=<その受注>&type=delivery` を開く | `#reportTitle` = 「納品書」（`Expect` で待つ）。`#orderDate`・`#employeeName`・`#freight` が空。`#rowShippedDate` と `#rowShipper` が可視で、`#shippedDate`・`#shipperName` が空（納品書は項目を表示し、値が空）。`#reportLines tbody tr` が 0 行。`#subtotal` = `0.00`、`#total` = `0.00` |
| 帳票_存在しない受注は一覧へ戻りエラーを表示する | EV-ORD-501 手順 1, ERR-NOTFOUND, 画面遷移 F-4 | テストが受注を入れて `DeleteOrder` で消した ID | `/OrdTf/Report?id=<その ID>&type=delivery` を直接開く | 一覧が表示される（`#orderList` が可視、`#reportTitle` は DOM に無い）。`#messageArea` が「対象のデータは存在しません。」で、class に `text-danger` を持ち `text-info` を持たない（戻った一覧の状態はDEC-ORD-70） |
| 帳票_種別を切り替えるとDBを読み直さずに表示が変わる | EV-ORD-502 手順 1〜3, SC-ORD-05-I01・I02・I07・I08・I14 | データ R | `type=delivery` で開く → テストが `BumpOrder`（送料 `99.00`）→ ネットワークが落ち着いてから要求の記録を始める → `#reportType` で「請求書」を選ぶ → 落ち着いてから確かめる → 「納品書」を選ぶ → 落ち着いてから確かめる | 請求書に切り替えた後：`#reportTitle` = 「請求書」。`#rowDueDate` が可視で `#dueDate` = 翌月末。`#rowShippedDate` と `#rowShipper` が不可視。**`#freight` = `34.50` のまま（`99.00` でない＝DB を読み直していない）**。納品書に戻した後：`#reportTitle` = 「納品書」。`#rowShippedDate`・`#rowShipper` が可視で、値が元のとおり（`2198/03/10`・S の会社名）。`#rowDueDate` が不可視。`#freight` = `34.50` のまま。**切り替えの間に、サーバへの要求が 0 件**（`page.Request` の記録。ページの移動もしない） |
| 詳細_帳票ボタンは常時活性（TestCase：出荷日 NULL／`2198/03/10`） | EV-ORD-204 入力, SC-ORD-02-B04・B05（活性＝常時） | 受注 1 件（明細 1 行。出荷日は TestCase） | 詳細（`/OrdTf/Detail?id=<その受注>`）を開く | `#btnDelivery` がキャプション「納品書」、`#btnInvoice` がキャプション「請求書」で、どちらも可視・活性。`#contents` の中で `#footer-buttons` の外にある。`#contents` の中の入力欄（`input`〔`hidden`・ボタンを除く〕・`select`・`textarea`）は 0 個のまま |
| 詳細_帳票ボタンで帳票へ遷移する（TestCase：納品書 `delivery`／請求書 `invoice`） | EV-ORD-204 手順 1・2, 画面遷移（`SC-ORD-02` → `SC-ORD-05`：受注 ID、帳票種別） | データ R | 詳細を開く → `#btnDelivery`（または `#btnInvoice`）を選ぶ | URL のパスが `/OrdTf/Report`、クエリ `id` = その受注 ID・`type` = TestCase の値。`#reportTitle` = 「納品書」（または「請求書」）、`#reportType` の選択がそれに一致、`#orderId` = その受注 ID |
| 印刷_ボタンはブラウザの印刷機能を呼ぶ | EV-ORD-503 手順 2・3, SC-ORD-05-B01 | データ R | ページが読み込まれる前に `window.print` を、呼び出し回数を数える関数に差し替える（`page.AddInitScriptAsync`）→ `/OrdTf/Report?id=<その受注>&type=delivery` を開く → ネットワークが落ち着いてから要求の記録を始める → `#btnPrint` を選ぶ | `#btnPrint` がキャプション「印刷」で可視・活性（選ぶ前）。選んだ後：`window.print` が 1 回呼ばれた。**サーバへの要求が 0 件**。画面は帳票のまま（`#reportTitle` = 「納品書」） |
| 印刷_印刷メディアでは操作用の要素を除く（TestCase：納品書 `delivery`／請求書 `invoice`） | EV-ORD-503 手順 1, SC-ORD-05-I01・B01・B02（印刷時は非表示）, 共通のヘッダ・メニュー | データ R | `type=…` で開く → （1）画面のメディアで確かめる → 印刷メディアに切り替える（`page.EmulateMediaAsync` の `Media = Print`）→ （2）確かめる → 画面のメディアに戻す → （3）確かめる | （1）`#reportType`・`#btnPrint`・`#btnBack`・`.page-header`・`.nav-side-menu` が可視。（2）これらが**すべて不可視**で、帳票の本体（`#reportTitle`・`#orderId`・`#reportLines`・`#total`）は**可視のまま**。（3）（1）と同じ（画面のメディアに戻せば再び見える＝除くのは印刷のときだけ） |
| 戻る_受注詳細へ復帰する（TestCase：納品書 `delivery`／請求書 `invoice`） | EV-ORD-504 手順 1, SC-ORD-05-I04・B02, 画面遷移（`SC-ORD-05` → `SC-ORD-02`：受注 ID） | データ R | `/OrdTf/Report?id=<その受注>&type=…` を開く → `#btnBack`（キャプション「戻る」。可視・活性）を選ぶ | 選ぶ前：`#btnBack` が `#contents` の中で `#footer-buttons` の外にある。選んだ後：URL のパスが `/OrdTf/Detail`、クエリ `id` = その受注 ID。詳細が表示される（`#orderId` = その受注 ID、`#detailList` が可視、`#reportTitle` は DOM に無い）。`#messageArea` は空 |
| 戻る_帳票から戻った詳細から一覧へ戻ると一覧状態が復元される | EV-ORD-504, EV-ORD-206, 画面遷移 F-2, EventSpec「一覧状態の保持」（画面遷移をまたいで保持し、復帰時に復元する） | K の受注 25 件（i 番目 = `2198/01/01` + i 日。明細の無い受注でよい） | `/OrdTf` を開き、K で検索 → 次へ（「2 / 2 ページ」。5 行、先頭行 = i = 5）→ 先頭行の受注 ID を選ぶ（詳細）→ `#btnDelivery`（帳票）→ 帳票の `#btnBack`（キャプション「戻る」）→ **詳細が表示されたこと（URL のパスが `/OrdTf/Detail`、`#detailList` が可視）を `Expect` で待ってから**、詳細の `#btnBack`（キャプション「一覧へ戻る」。帳票と詳細で id が同じ） | 一覧が表示される：`#CustomerID` = K、`#pageInfo` = 「2 / 2 ページ」、`#totalCount` = 「全 25 件」、一覧 5 行（先頭行 = i = 5 から i = 1。復元された一覧状態が、帳票を挟んでも保持されている） |
| 帳票_メインボタン | cmn-framework.md の画面共通, 決定 1 | データ R | `type=delivery` で開く | `#contents` の中に `#btnMain1`〜`#btnMain5` が 5 つあり、すべて非活性（キャプションなし）。`#btnPrint` は `#footer-buttons` の外（画面上部）にある |
| 印刷_フッタのボタンも除く | EV-ORD-503（操作用の要素を除く）, DEC-ORD-64 | データ R | `type=delivery` で開く → 印刷メディアに切り替える | `#footer-buttons` が不可視（画面のメディアでは可視） |
| 帳票_割引率と価格の表示 | SC-ORD-05-I10（列 3・4）, DEC-ORD-68・69 | 受注 1 件に、商品 A（単価 `0.50`・割引 `0.15`・数量 3）、M（`100.00`・`0.125`・1）、X（`20.00`・0・1）を A・M・X の順に（名前の昇順）入れる | `type=delivery` で開く | `#reportLines tbody tr` が 3 行。A：割引率 `15%`、価格 **`0.43`**（`0.425` を四捨五入。切り捨て・偶数丸めなら `0.42`）、明細金額 `1.28`。M：割引率 **`12.5%`**、価格 `87.50`。X：割引率 **`0%`**、価格 `20.00` |
| 帳票_廃番商品の商品名 | SC-ORD-05-I10（列 1）, DEC-ORD-67 | 受注 1 件に商品 P（廃番でない）・Q（廃番）。P の名前 < Q の名前 | `type=delivery` で開く | 1 行目・2 行目の商品名のセルが、P・Q の名前と同じ（「（廃番）」を含まない） |
| 帳票_存在しない受注で保持された一覧状態で一覧へ戻る | EV-ORD-501 手順 1, ERR-NOTFOUND, 画面遷移 F-2・F-4, DEC-ORD-70 | K の受注 25 件（i 番目 = `2198/01/01` + i 日） | K で検索 → 次へ（「2 / 2 ページ」）→ 先頭行（i = 5）の受注 ID を選ぶ（詳細）→ テストがその受注を `DeleteOrder` で消す → `#btnDelivery` | 一覧が表示される（`#orderList` が可視、`#reportTitle` は DOM に無い）。`#messageArea` が「対象のデータは存在しません。」（`text-danger`）。`#CustomerID` = K、`#pageInfo` = 「2 / 2 ページ」、`#totalCount` = 「全 24 件」、一覧 4 行（i = 4 から i = 1） |

- 追加：**16 メソッド・20 テストケース**（決定 1・DEC-ORD-64・67〜70 のケースを足した：`帳票_メインボタン`・`印刷_フッタのボタンも除く`・`帳票_割引率と価格の表示`・`帳票_廃番商品の商品名`・`帳票_存在しない受注で保持された一覧状態で一覧へ戻る` の 5 メソッド・5 ケース。足す前の内訳：「帳票_…」5 メソッド・5 ケース、「詳細_…」2 メソッド・4 ケース〔活性 2・遷移 2〕、「印刷_…」2 メソッド・3 ケース〔印刷メディアが 2〕、「戻る_…」2 メソッド・3 ケース〔復帰 2・一覧状態 1〕）。


## 5. 作らないケースと理由

受注一覧の 5 章と同じ（`ERR-DB`〔`DEC-CMN-07`〕・`PRE-DB`・システム エラー、未ログインの挙動など）。加えて次を作らない。

| 対象 | 理由 |
| --- | --- |
| 受注 ID が無い・数値でない・帳票種別が不正のとき（`PRE-PARAM` の違反） | DEC-ORD-71：定めない（実装に任せ、期待にしない。DEC-ORD-21 と同じ読み）。画面の操作からは起こらない |
| 帳票の合計（`#total`）の強調（`帳票_合計の強調`） | DEC-ORD-72：定めない（受注詳細の `SC-ORD-02-I20` の強調を帳票の `SC-ORD-05-I13` に当てるかは、実装に任せ、期待にしない）。`#total` の class（`fw-bold` の有無）は、4.1 のどのケースも期待にしない |
| ブラウザの印刷のダイアログそのものの操作、実際の印刷の結果（用紙・改ページ） | 範囲外（対象の仕様）。`window.print` を呼んだことと、印刷メディアで操作用の要素が除かれることまでを確かめる |
| 印刷の除外が `_Layout.cshtml`・共通の CSS で行われているか | 実装の決定（計画への条件として 1.2 に書いた。境界から見えない）。段 4 のレビューで見る |
| `#reportType` の選択を B層で確かめること | 帳票種別は B層の入力でない（1.1）。表示の切替は E2E で確かめる |
| 同じ商品名の明細の並び | 仕様が「商品名の昇順」だけで、同名の並びを決めていない。テストは名前の異なる商品を使う |
| 受注日・出荷日に時刻があるときの表示 | 日付は `yyyy/MM/dd`（共通仕様 4 節）。受注日の `15:30` が出ないことを「帳票_納品書の表示」で確かめる。出荷日は同じ書式 |
| 発行日の当日の判定（日付をまたぐ瞬間） | 実行時刻に依存する。B層は前後の `DateTime.Today` で許し、境界の月末・閏年は `IssueDate` を渡す支払期限の 7 行で確かめる |
| 帳票のサーバ側の生成・保存・非同期処理が無いこと | B層は「読むだけで受注・明細を更新しない」で DB の状態を見る。ファイル出力・非同期は境界から観測できない |
| 詳細の B04・B05 を押したときの確認ダイアログ | 仕様に無い（`EV-ORD-204` は確認なしで遷移） |

## 6. 仕様の決定（答え済み。決めた者：代行。答えは決定のレポートの「判断」のとおり）

1 件ごとに、問い・選択肢・推奨と理由・影響するケース。決定記録に入るものは、答えの後に代行役が案を書く。

**答え**（すべて推奨のとおり）：決定 1：GO（1。印刷も画面上部。フッタの 5 つはすべて非活性）／決定 2：GO（1。フッタのメインボタンも印刷の対象から除く）／決定 3：GO（1。区切りは定めない。空の要素は詰める）／決定 4：GO（1。商品マスタに無い明細は含めない）／決定 5：GO（1。付記しない）／決定 6：GO（1。受注詳細と同じ百分率）／決定 7：GO（1。四捨五入）／決定 8：GO（1。保持された一覧状態で戻る）／決定 9：GO（1。定めない。ケースは作らない）／決定 10：GO（3。定めない。ケースは作らない）。

**決定記録との対応**（NorthwindDbSampleSpec@b60c760 の `LLD/Decisions/Orders.md`。各 DEC の「決定」の欄を読み、上の答えとこのテスト仕様の期待が合うことを確かめた）：決定 2 = DEC-ORD-64、決定 3 = DEC-ORD-65、決定 4 = DEC-ORD-66、決定 5 = DEC-ORD-67、決定 6 = DEC-ORD-68、決定 7 = DEC-ORD-69、決定 8 = DEC-ORD-70、決定 9 = DEC-ORD-71、決定 10 = DEC-ORD-72。**決定 1 は規約の解釈で決定記録に入らない**（根拠は「決定 1」のまま）。

| # | 仕様の ID | 問い | 選択肢（間引かない） | 推奨と理由 | 影響するケース |
| --- | --- | --- | --- | --- | --- |
| 1 | cmn-framework.md「画面共通」, SC-ORD-05-B01, EV-ORD-503 | 印刷（B01）をメインボタン（フッタの 5 つ）に含めるか（戻る B02 と詳細の B04・B05 は、対象の仕様 `test3-testfirst-OrdTfReport.md` が画面定義書の配置〔遷移だけ〕と決めているので問わない）。規約は、メインボタンを「機能トランザクションを起動するボタン（検索・登録・更新・削除など）」とし、画面定義書は B01・B02 を「画面上部（印刷時は非表示）」に置く。**規約の解釈（決定記録には書かない）** | 1. 含めない：印刷も画面定義書の配置（画面上部）。フッタの 5 つは、すべて非活性（キャプションなし） ／ 2. 印刷を `#btnMain1` に置く（フッタ）。戻るは画面上部 | 1。印刷は `window.print()` を呼ぶだけで、サーバの機能トランザクションを起動しない（`EV-ORD-503` 手順 3）。画面定義書の配置に合う。一方、機能トランザクションの無い画面でフッタの 5 つがすべて空になるのは不自然に見える（人が目で見る確認の対象） | 4.1 `帳票_メインボタン`。2 なら 4 章の印刷のケースの「置き場所」と、印刷メディアのケース（フッタ）。ボタンの id（`#btnPrint`・`#btnBack`）は変えない |
| 2 | EV-ORD-503 手順 1・イベント概要（「操作用の要素を除いた状態で印刷する」）, cmn-framework.md「画面共通」 | 手順 1 が除く対象は、I01・B01・B02・「画面共通のヘッダ・メニュー」で、**コンテンツのフッタ部のメインボタン（5 つ。規約で全画面に置く）は書いていない**。これらも印刷の対象から除くか | 1. 除く（操作用の要素なので） ／ 2. 除かない（手順 1 に書いていない。印刷に空のボタンが出る） ／ 3. 定めない（実装に任せ、テストの期待にしない） | 1。イベント概要が「操作用の要素を除いた状態」と言い、フッタのボタンは操作用の要素。手順 1 の列挙は、規約のフッタが画面定義書に書かれていないための漏れと読む | 4.1 `印刷_フッタのボタンも除く`（1 以外なら変える・作らない）。決定 1 が 2 のとき、印刷ボタンの扱いが変わる |
| 3 | SC-ORD-05-I05, EV-ORD-501 手順 6 | 宛先（出荷先情報 6 項目の連結）の区切りと、空の要素（NULL・空文字）の扱い。本文は「連結して表示する」だけで、区切りも空の要素の扱いも書いていない（受注詳細の顧客住所 `SC-ORD-02-I05` は「空の要素を詰めて連結する」と書き、区切りは DEC-ORD-18 が「定めない」とした） | 1. 区切りは定めない（実装に任せ、期待にしない）。空の要素は詰める（DEC-ORD-18・45 と同じ） ／ 2. 区切りを半角空白に定める。空の要素は詰める ／ 3. 区切りを定めない。空の要素の扱いも定めない（要素が順に並ぶことだけ） | 1。同じ「住所の連結」で、画面ごとに別の規則にする理由が本文に無い（DEC-ORD-45 と同じ考え）。区切りの文字は DEC-ORD-18 が定めないとした | 3.1 `OrdTfReport_宛先は空の要素を詰めて連結する`（3.1 の `宛先は出荷先情報で…` は、どの答えでも通る強さで書いた） |
| 4 | EV-ORD-501 手順 3・4, DEC-ORD-24（受注詳細）, 共通仕様 6 節 | 商品マスタに無い商品の明細（DB は外部キーを持たないので起こりうる）を、帳票の明細・小計・合計に含めるか。手順 3 は受注詳細（`EV-ORD-201`）と同じく `Products` と内部結合して取る。DEC-ORD-24 は受注詳細の読みを決めた（要レビュー付き。DEC-CMN-02 の原則と食い違うとの指摘あり） | 1. 含めない（DEC-ORD-24 と同じ。明細一覧と小計を揃える） ／ 2. 含める（受注一覧の合計金額と揃える） ／ 3. 定めない（実装に任せ、期待にしない） | 1。手順 3・4 の文が受注詳細と同じ並び（内部結合 → 明細ごとに算出 → 小計・合計）で、同じ画面の読みを変える理由が無い。DEC-ORD-24 を見直すときは、この決定も一緒に見直す | 3.1 `OrdTfReport_商品マスタに無い明細は含めない` |
| 5 | SC-ORD-05-I10（列 1「商品名」）, SC-ORD-02-I17（列 1「廃番商品は『（廃番）』を付記する」） | 帳票の明細の商品名に、廃番商品の「（廃番）」を付記するか。`SC-ORD-05-I10` の列は「商品名」とだけ書き、付記の注記が無い | 1. 付記しない（本文は商品名と書く。付記は受注詳細の列の備考） ／ 2. 付記する（受注詳細と同じ） ／ 3. 定めない（実装に任せ、期待にしない） | 1。付記は `SC-ORD-02-I17` の列 1 の備考にあり、`SC-ORD-05-I10` には無い。本文の書き分け（付記は `SC-ORD-02-I17` の列の備考にだけある）が理由。本文に書いていないことの読みなので、要レビュー | 3.1 `OrdTfReport_廃番商品の商品名に付記しない`、4.1 `帳票_廃番商品の商品名`。2 なら B層の `Lines` の ProductName が付記を含める |
| 6 | SC-ORD-05-I10（列 3「割引率」）, DEC-ORD-25（受注詳細） | 割引率の表記。`SC-ORD-05-I10` は列名を書くだけで、書式を書いていない（受注詳細は百分率。端数があれば小数で `12.5%`、整数なら `15%`・`0%`） | 1. 受注詳細と同じ（百分率。DEC-ORD-25） ／ 2. 小数のまま（`0.15`） ／ 3. 定めない（実装に任せ、期待にしない） | 1。同じ明細の同じ列で、割引率の情報を落とさない。共通仕様 3 節は 0〜1 の実数としか言わないので、表示は画面ごとの決まり。受注詳細と揃える | 4.1 `帳票_割引率と価格の表示`（割引率）。`帳票_納品書の表示` の割引率の列もこの答えに合わせた（`15%`・`25%`・`5%`） |
| 7 | SC-ORD-05-I10（列 4「価格」）, DEC-ORD-26（受注詳細）, 共通仕様 3 節（画面表示は小数 2 桁） | 価格（`単価 × (1 − 割引率)`。3 桁以上になりうる。例 `0.425`）の小数 2 桁の表示の丸め | 1. 四捨五入（DEC-ORD-26 と同じ） ／ 2. 切り捨て ／ 3. 偶数丸め ／ 4. 定めない（実装に任せ、期待にしない） | 1。共通仕様 3 節が決めている丸めは四捨五入（明細金額）で、受注詳細・編集（DEC-ORD-26・46）と同じに見えるようにする。帳票だけ別の丸めにする理由が無い | 4.1 `帳票_割引率と価格の表示`（価格）。4.1 の表示のケースは、丸めの要らない値（`85.00` など）だけ |
| 8 | EV-ORD-501 手順 1（`ERR-NOTFOUND`）, 共通仕様 7 節, 画面遷移 F-2・F-4, DEC-ORD-28（受注詳細・削除の ERR-NOTFOUND） | 帳票の読み込みで `ERR-NOTFOUND` になって一覧へ戻るとき、**保持された一覧状態**で戻るか、既定の一覧か。DEC-ORD-28 は `EV-ORD-201`・`EV-ORD-203` について決めた（`EV-ORD-501` は含まない） | 1. 保持された一覧状態で戻る（DEC-ORD-28 と同じ） ／ 2. 既定の一覧 ／ 3. 定めない（実装に任せ、期待にしない） | 1。一覧 → 詳細 → 帳票と来た一覧状態を、帳票が消えた受注でも保持された状態で戻すのが、共通仕様 4 節「検索条件の保持」と F-2 の読み。詳細・削除と揃える | 4.1 `帳票_存在しない受注で保持された一覧状態で…`。4.1 の `帳票_存在しない受注は…` は一覧の表示とメッセージだけ（一覧状態を期待にしない） |
| 9 | EV-ORD-501 事前条件 `PRE-PARAM`（受注 ID、帳票種別）, DEC-ORD-21（受注詳細） | 受注 ID が無い・数値でない、または帳票種別が `delivery`・`invoice` でない（遷移パラメータが要件に違反する）URL を開いたとき、どうするか。DEC-ORD-21 は `EV-ORD-201` の受注 ID だけを決めた | 1. 定めない（実装に任せ、期待にしない） ／ 2. `ERR-NOTFOUND` と同じ扱い ／ 3. 種別が不正なら納品書として表示する | 1。画面の操作からは起こらない（詳細のボタンが正しい値を渡す）。DEC-ORD-21・10・11 と同じく、通常の操作で起こらないものは定めない | なし（作らない） |
| 10 | SC-ORD-05-I13（合計）, SC-ORD-02-I20（合計の強調）, 共通仕様 3 節 | 帳票の合計（`#total`）を強調表示するか。受注詳細は `SC-ORD-02-I20` の備考で強調と決めているが、`SC-ORD-05-I13` の備考は空 | 1. 当てる（受注詳細と同じ強調。class `fw-bold`） ／ 2. 当てない ／ 3. 定めない（実装に任せ、期待にしない） | 3。強調は `SC-ORD-02-I20` だけに書かれ、帳票の本文は何も言わない。決めるまで期待にしない（見た目の判断で、人が目で見る確認の対象でもある） | なし（作らない。5 章）。4.1 は強調を期待にしない |

### 6.1 決定を待たずに決めたこと（本文・決定記録から読めるので、期待にした）

- 担当者名（I09）は「姓 + 半角空白 + 名」（`EMP-T1`）。担当者が無いとき・配送業者が無いとき・出荷日が NULL のときは空（共通仕様 4 節）。
- 明細の並びは商品名の昇順。金額は 10 進数で、明細金額を小数第 3 位で四捨五入して合算、送料 NULL は 0 として足す、明細が無い受注は小計 0（`DEC-CMN-02`・`DEC-CMN-03`）。
- 納品書は出荷日・配送業者を表示し、支払期限を表示しない。請求書は支払期限（発行日の翌月末日）を表示し、出荷日・配送業者を表示しない（手順 7）。出荷日が NULL のときも納品書は出荷日の項目を表示し、値が空（`SC-ORD-05-I07`「NULL のときは空」）。
- 切替（`EV-ORD-502`）は DB を読み直さない：境界から観測できる形（サーバへの要求が 0 件、DB を変えても送料が変わらない）で期待にした。
- 印刷（`EV-ORD-503`）はブラウザの印刷機能を呼ぶだけ（サーバへ要求を出さない）。除く対象（I01・B01・B02・ヘッダ・メニュー）は、印刷メディアで不可視。
- 戻る（`EV-ORD-504`）は受注 ID を渡して詳細へ。保持された一覧状態は変えない（「一覧状態の保持」は画面遷移をまたぐ）。
- 詳細の B04・B05 は、メインボタンに含めず画面上部（前の回の決定 4 と同じ読み。遷移だけのボタン）。

### 6.2 実装の決定（人に上げない。計画に書く）

- 帳票の URL の `type` の値（`delivery`・`invoice`）と、`IssueDate`（引数。null は当日）を B層の入力にすること（1.1・1.2）。
- 印刷の除外を CSS の `@media print` で、この画面のビューの中に置くこと（共通のレイアウトを変えない。1.2）。
- 切替をクライアント側（JavaScript）で行うこと（1.2。サーバへ要求を出さない）。
- 支払期限（`DueDate`）の算出を B層で行い、納品書・請求書のどちらでも返すこと（1.1）。

## 7. 段 2 の確かめ方（予定）

- **Red（B層）**：引数クラスに `IssueDate`、戻り値クラスに `ShipToText`・`IssueDate`・`DueDate` を足し、`OrdTfBusiness` に `OrdTfReport` の**戻り値を設定するだけの空の `UOC_`** を置く。テストは `Assert` で落ちる（空の実装は ErrorFlag = false・空の値を返すため、`ErrorFlag = true` を期待する `存在しない受注は…` も `Assert` で落ちる）。`読むだけで…` は 1 つ目の `OrderID`・`Total` の確かめで落ちる。ビルドや呼び出しで落ちるのは Red ではない。`MVC_Sample_Core\MVC_Sample.Tests`（net10.0）で流す。net48 は段 4 の後で全件。
- **Red（P層）**：ビルドが通り、`/OrdTf/Report` を開ける最小の土台（`Report.cshtml`・`Report` のアクション。要素なし）を先に置く。E2E のテストは `Expect` で落ちる。**ケースが最初から通ってしまったら、段 2 の報告に書き、壊す確かめで実装を見ていることを確かめる**（`TESTING.md` 4）。
- **最初から通りうるケース**：なし（予定）。`帳票_存在しない受注は…` は、土台の `Report` が一覧へ移らなければ、最初の `Expect`（`#orderList`）で落ちる。
- 決定 1〜10 は答え済み。決定に依存していたケースは 3.1・4.1 に移した（決定 10 のケースは作らない。5 章）。根拠は決定記録の `DEC-ORD-64〜72` に置き換えた（決定 1 だけ「決定 1」のまま）。
- テストのファイル：`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`（足す。`TestData.cs` は足さない）、`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfReportScreenTests.cs`（新規）。既存のテストのファイルは変えない。
