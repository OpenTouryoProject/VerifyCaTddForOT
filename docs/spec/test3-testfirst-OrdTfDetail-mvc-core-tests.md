# テスト仕様：受注詳細（照会・削除）と一覧からの遷移 OrdTfDetail（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

起こした仕様の版：NorthwindDbSampleSpec@ec66d6e（ブランチ proxy/ord-management。d65135e からの差分は決定記録 DEC-ORD-18〜28 の追加だけで、期待は変わらない。本文 `HLD/Common.md`・`HLD/UI_FlowList.md`・`LLD/EventSpec/Orders.md`・`LLD/UI_ElementsAndEventList/Orders.md`・`LLD/TableSchema/Orders.md` と、決定記録 `LLD/Decisions/Common.md`・`LLD/Decisions/Orders.md`〔DEC-CMN-01〜07・DEC-ORD-01〜28〕を合わせて読んだ）
規約：`docs/spec/cmn-framework.md` の「画面共通」「MVC」、`docs/spec/cmn-testfirst.md`、`docs/spec/test3-testfirst-OrdTfDetail.md`、`docs/plan/cmn-testfirst.md` の「B層を共用する構成」
受注一覧のテスト仕様：[test3-testfirst-OrdTf-mvc-core-tests.md](./test3-testfirst-OrdTf-mvc-core-tests.md)（以下「受注一覧」。承認・実装済み。このケースはその上に足す）。決定のレポート：[test3-testfirst-OrdTfDetail-mvc-core-decisions.md](./test3-testfirst-OrdTfDetail-mvc-core-decisions.md)

対象：`SC-ORD-02`（`I01`〜`I20`、`B01`、`B02`、`B06`）、`SC-ORD-01` の `B03` と `I06` の受注 ID 列の選択、`EV-ORD-106`・`EV-ORD-107`・`EV-ORD-201`・`EV-ORD-203`・`EV-ORD-206`、受注一覧に戻ったときの一覧状態の復元（画面遷移 F-2、`EV-ORD-101` 手順 3、「一覧状態の保持」）。
範囲外は対象の仕様のとおり（`EV-ORD-108`・`202`・`204`・`205`、`SC-ORD-03`〜`05`、`SC-ORD-02` の `B03`〜`B05`、他のモジュールから `SC-ORD-02` への遷移）。

## 0. 進め方の前提

- B層は `Shared\`（`OrdTfBusiness`・`OrdTfParameterValue`・`OrdTfReturnValue`）を共用する。**受注詳細の B層（`OrdTfDetail`・`OrdTfDelete`）とそのテスト（`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs` に足す）はこのケースで作り**、net10.0（`MVC_Sample_Core\MVC_Sample.Tests`）→ net48（`MVC_Sample\MVC_Sample.Tests`）の順に、同時にでなく全件流す（`docs/plan/cmn-testfirst.md`「B層を共用する構成」）。
- テストのプロジェクト（`MVC_Sample_Core\MVC_Sample.Tests`・`MVC_Sample_Core\MVC_Sample.E2ETests`）、アプリの起動、ログイン、ブラウザ、日付の範囲（B層のテスト 2199 年・E2E 2198 年）、並行実行の決まりは、受注一覧の 1 章のとおり（新しいプロジェクトは作らない）。
- 画面は受注一覧のログインの後に、`/OrdTf`（一覧）と `/OrdTf/Detail?id=<受注 ID>`（詳細）で開く。
- 表示のメッセージ・文言は仕様の文言そのまま（`DEC-CMN-01`：メッセージ ID は実装で決め、テストは文言で確かめる）。
- **データを書き換えるテスト**（削除）は、テストが自分で作った受注と明細だけを消す。Northwind2 の既存の 830 件は消さない・書き換えない。後片付けは落ちても行う（`TearDown` の `CleanUp`）。**終わりに受注の件数が 830 件かの確かめは、計画の手順に入れる**（テスト仕様の範囲外）。
- 受注一覧の既存のテスト（B層・E2E）は変えない（決定 4：遷移だけのボタンはメインボタンに含めないので、既存の `初期表示_ボタン` は変わらない。期待値の変更は無い）。

## 1. テストが固定する契約（実装の決定。振る舞いは変えない。段 4 の実装はこれに従う）

仕様が決めていない「B層とテストの境界の形」と、画面の URL・id。

### 1.1 B層

**引数クラス `OrdTfParameterValue`（`Shared\IfType\`）に足すメンバ**（既存のメンバは変えない）

| メンバ | 型 | 意味 |
| --- | --- | --- |
| `OrderID` | `int` | 対象の受注 ID（`OrdTfDetail`・`OrdTfDelete`） |
| `RowVersion` | `int` | 画面が保持していた `Orders.RowVersion`（`OrdTfDelete`） |

**戻り値クラス `OrdTfReturnValue` に足すメンバ**（既存のメンバは変えない）

| メンバ | 型 | 意味 |
| --- | --- | --- |
| `OrderID` | `int` | 受注 ID（I01） |
| `Status` | `string` | 状況（I02）。「未出荷」「出荷済」「納期遅延」 |
| `CustomerName`・`CustomerPhone`・`CustomerAddress`・`EmployeeName`・`ShipperName` | `string` | I03・I04・I05・I06・I10。無いとき（NULL・参照先なし）は空文字 |
| `OrderDate`・`RequiredDate`・`ShippedDate` | `DateTime?` | I07・I08・I09。NULL は null（時刻は表示側が出さない） |
| `ShipName`・`ShipAddress`・`ShipCity`・`ShipRegion`・`ShipPostalCode`・`ShipCountry` | `string` | I11〜I16。NULL は空文字 |
| `Lines` | `List<OrdTfDetailLine>` | 明細（I17）。商品名の昇順 |
| `Subtotal`・`Total` | `decimal` | 小計（I18）・合計（I20） |
| `Freight` | `decimal?` | 送料（I19）。NULL は null |
| `RowVersion` | `int` | `Orders.RowVersion`（`EV-ORD-201` 手順 7） |
| `CanEdit` | `bool` | 編集ボタンの活性（`EV-ORD-201` 手順 8。`ShippedDate` が NULL のとき true） |

**`OrdTfDetailLine`（新しい型）**：`ProductName`（string。廃番の付記を含めた表示名）・`UnitPrice`（decimal）・`Discount`（decimal。0〜1 の割引率。`real` を 10 進数へ変換した値）・`Price`（decimal。`単価 × (1 − 割引率)`。丸めない）・`Quantity`（int）・`UnitsInStock`（int?）・`Amount`（decimal。明細金額＝`Price × 数量` を小数第 3 位で四捨五入）。

- **`MethodName`**：`OrdTfDetail`（受注 1 件と明細。引数は `OrderID`）・`OrdTfDelete`（削除。引数は `OrderID`・`RowVersion`）。分離レベルは `ReadCommitted`（テストの呼び出しの値）。「当日」は `DateTime.Today`（受注一覧と同じ）。
- 業務エラー：`ERR-NOTFOUND` は `ErrorFlag = true`、`ErrorMessage` =「対象のデータは存在しません。」。`ERR-CONFLICT` は `ErrorFlag = true`、`ErrorMessage` =「他の利用者によって更新されています。最新の内容を読み込み直してください。」（共通仕様 7 節の文言。メッセージ ID は実装で決める＝`DEC-CMN-01`）。
- 削除は B層の 1 トランザクションで、`Order Details` → `Orders`（`RowVersion` 条件付き）の順（`ORD-T8`・`EV-ORD-203` 手順 2〜5）。`Orders` の削除が 0 件ならロールバックして、`Orders` に当該 `OrderID` が存在するかで `ERR-CONFLICT` と `ERR-NOTFOUND` を分ける。**列名・値を文字列で連結しない**（パラメタ化）。

### 1.2 P層（MVC）の URL

| 対象 | 指定 |
| --- | --- |
| 受注一覧 | `/OrdTf`（既存。直接開く＝メニューからと同じ） |
| 受注詳細（`SC-ORD-02`） | GET `/OrdTf/Detail?id=<受注 ID>` |
| 一覧への復帰 | GET `/OrdTf/Back`。保持された一覧状態があれば復元して再検索した一覧を表示し、無ければ既定の一覧を表示する（`EV-ORD-101` 手順 3）。詳細の「一覧へ戻る」・削除の完了（`EV-ORD-203` 手順 6）・`ERR-NOTFOUND` の後は、一覧を表示する（`ERR-NOTFOUND` の後に保持された一覧状態を使うかはDEC-ORD-28。テストは URL でなく `#orderList` で確かめる） |
| 受注登録（`SC-ORD-03`）の URL（`EV-ORD-107`。**この回は画面が無い**。テストは URL だけを確かめる） | `/OrdTf/Edit?mode=new`（受注 ID は付けない） |
| 一覧の受注 ID（`EV-ORD-106`） | 各行の 1 列目のセルに `<a>`（テキスト＝受注 ID）。選ぶと `/OrdTf/Detail?id=<受注 ID>` へ遷移する |

- 一覧状態（最後に実行した検索の条件・ページ・並べ替え列・方向）をどこに保持するか（Session など）は計画で決める。**テストは画面の振る舞いだけを見る。**
- 削除は POST（`[ValidateAntiForgeryToken]`）で、削除の確認は `window.confirm`（`cmn-framework.md` の MVC）。

### 1.3 テストのデータ（`TestData`。B層用 `Shared\WSServer_sample.Tests\TestData.cs`・E2E 用 `MVC_Sample_Core\MVC_Sample.E2ETests\TestData.cs` の両方に足す。既存の呼び出しは変えない）

| 足すもの | 内容 |
| --- | --- |
| `NewCustomer` の引数 | `phone`・`address`・`city`・`region`・`postalCode`・`country`（既定は NULL） |
| `NewOrder` の引数 | `shipName`・`shipAddress`・`shipCity`・`shipRegion`・`shipPostalCode`・`shipCountry`（既定は NULL） |
| `NewProduct(name = null, discontinued = false, unitsInStock = 0)`（`unitsInStock` は `int?`。null で NULL） | 商品を入れて `ProductID` を返す。名前の既定は `"Tf" + GUID 8 桁`、目印を指定するときは `"Tf" + <目印> + GUID 8 桁`。後始末は明細・受注の後に消す |
| `DeleteProduct(productId)` | 商品を消す（明細は消さない。商品マスタに無い明細を作る。後始末は、無い行の削除でも落ちない） |
| `NewOrdersSequence` の引数 | `employeeId`（既定は NULL。全受注に設定する） |
| `CountOrders(orderId)`・`CountDetails(orderId)`・`GetRowVersion(orderId)` | DB の状態を直接読む |
| `BumpOrder(orderId, freight)` | 他の利用者の更新を模す：`UPDATE Orders SET Freight = @f, RowVersion = RowVersion + 1 WHERE OrderID = @o` |
| `DeleteOrder(orderId)` | 他の利用者の削除を模す（明細 → 受注。後始末は、無い行の削除でも落ちない） |

## 2. 画面の id・class（段 4 の実装が付ける）

### 2.1 受注詳細（`SC-ORD-02`。`/OrdTf/Detail`）

| 対象 | 指定 |
| --- | --- |
| 受注情報（I01〜I10） | `#orderId`・`#status`・`#customerName`・`#customerPhone`・`#customerAddress`・`#employeeName`・`#orderDate`・`#requiredDate`・`#shippedDate`・`#shipperName`（テキスト＝値。NULL は空） |
| 出荷先情報（I11〜I16） | `#shipName`・`#shipAddress`・`#shipCity`・`#shipRegion`・`#shipPostalCode`・`#shipCountry` |
| 明細一覧（I17） | `<table id="detailList">`（`thead th` が 7 列：商品名・単価・割引率・価格・数量・在庫・明細金額。`tbody tr` が明細 1 行）。1 行のセルは左から 商品名・単価・割引率・価格・数量・在庫・明細金額 |
| 小計・送料・合計（I18〜I20） | `#subtotal`・`#freight`・`#total`。**`#total` は class に `fw-bold` を持つ**（強調表示。Bootstrap 5.1）。金額は通貨記号なしの千区切り＋小数 2 桁（`1,186.50`）、割引率は百分率（`0.15` → `15%`）、日付は `yyyy/MM/dd`、数量・在庫は整数 |
| 共通メッセージ領域 | `#messageArea`（受注一覧と同じ。エラーは `text-danger`、情報は `text-info`） |
| メインボタン（フッタに 5 つ。`#contents` の中。決定 4） | `#btnMain1` = 「削除」（B02。常時活性）。`#btnMain2`〜`#btnMain5` は非活性（キャプションなし）。遷移だけのボタン（編集 B01・一覧へ戻る B06）はメインボタンに含めず、画面定義書の配置に従う |
| 編集（B01。画面上部） | `#btnEdit`（キャプション「編集」。`#contents` の中でフッタの外の上部。`ShippedDate` が NULL でないとき非活性）。押したときの振る舞いは作らない（`EV-ORD-202` は範囲外） |
| 一覧へ戻る（B06。画面下部） | `#btnBack`（キャプション「一覧へ戻る」。`#contents` の中でフッタの外の下部。`/OrdTf/Back` へ移る） |
| 照会モード | 画面に入力欄（`input`〔`hidden`・ボタンを除く〕・`select`・`textarea`）を置かない（I01〜I20 はすべて「ラベル」「表」で「出」） |

### 2.2 受注一覧（`SC-ORD-01`）への追加（既存の指定は変えない）

| 対象 | 指定 |
| --- | --- |
| 新規登録（B03。一覧の上部。決定 4） | `#btnNew`（キャプション「新規登録」。常時活性。`#contents` の中でフッタの外の、一覧の上部）。メインボタンは変えない（`#btnMain1` が「検索」で活性、`#btnMain2`〜`#btnMain5` は非活性のまま）。選ぶと、現在の一覧状態を保持して `/OrdTf/Edit?mode=new` へ遷移する（`EV-ORD-107`） |
| 受注 ID（I06 の 1 列目） | 1.2 のとおり `<a>` |

## 3. B層のルート メソッドのテスト（`OrdTfBusinessTests`。net10.0 → net48 の順に流す）

`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs` に足す（既存の呼び出しの部品 `Call`・`D` と同じ形。`Call("OrdTfDetail", pv => pv.OrderID = …)`）。**既存のケースは変えない。**
共通の前提：受注日などの日付は 2199 年（時刻 00:00 を既定）。テストが入れた顧客・担当者・配送業者・商品・受注だけを使う（`TestData`）。

### 3.1 受注詳細（`OrdTfDetail`。`EV-ORD-201`）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfDetail_受注情報 | EV-ORD-201 手順 1・2・5, SC-ORD-02-I01〜I10, EMP-T1, SC-ORD-02-I06 | 顧客 K（電話 `03-1234-5678`）・担当者 E（姓・名）・配送業者 S。受注 1 件：K・E・S、受注日 `2199/03/05`、要求納期 `2199/04/01`、出荷日 `2199/03/10`、送料 0 | OrderID = その受注 | ErrorFlag = false。OrderID = その受注、CustomerName = K の会社名、CustomerPhone = `03-1234-5678`、EmployeeName = 「姓 名」（姓 + 半角空白 + 名）、ShipperName = S の会社名、OrderDate = `2199/03/05`、RequiredDate = `2199/04/01`、ShippedDate = `2199/03/10`、Status = 「出荷済」 |
| OrdTfDetail_出荷先情報 | EV-ORD-201 手順 1, SC-ORD-02-I11〜I16, ORD-T6（出荷先は受注ごとに独立） | 受注 1 件：ShipName = `ShipTo Co`・ShipAddress = `4-5-6 Ginza`・ShipCity = `Chuo`・ShipRegion = `Tokyo`・ShipPostalCode = `104-0061`・ShipCountry = `Japan`。顧客 K の住所は別の値 | OrderID = その受注 | ShipName 以下の 6 項目が受注の値のとおり（顧客の住所ではない） |
| OrdTfDetail_顧客住所は空の要素を詰めて連結する | SC-ORD-02-I05（空の要素を詰めて連結）, DEC-ORD-18（区切り文字は定めない） | 顧客 3 件（受注は各 1 件）：K0 = Address `A1`・City `C1`・Region NULL・PostalCode NULL・Country `Jp`、K1 = 同じで Region NULL・PostalCode 空文字、K2 = Address `A1`・City `C1`・Region `R1`・PostalCode `P1`・Country `Jp` | それぞれの受注で OrderID を指定 | **区切り文字は期待にしない**。CustomerAddress が、K0 = 正規表現 `^A1([\s\S]*)C1\1Jp$`、K1 = 同じ正規表現で K0 と同じ文字列、K2 = `^A1([\s\S]*)C1\1R1\1P1\1Jp$`（要素が順に並び、区切りが同じ文字列で、空の要素の分の区切りが残らない） |
| OrdTfDetail_顧客・担当者・配送業者が無いと空 | EV-ORD-201 手順 2, 共通仕様 4 節（空値の表示）, ORD-T1 | 受注 1 件：CustomerID・EmployeeID・OrderDate・RequiredDate・ShippedDate・ShipVia・Ship* がすべて NULL、送料 0 | OrderID = その受注 | ErrorFlag = false。CustomerName・CustomerPhone・CustomerAddress・EmployeeName・ShipperName・ShipName〜ShipCountry がすべて空文字。OrderDate・RequiredDate・ShippedDate が null |
| OrdTfDetail_状況の判定（TestCase：下の 9 行） | EV-ORD-201 手順 5, ORD-T7, ORD-T2, EventSpec「状況の判定」 | 受注 1 件（要求納期・出荷日は下表。今日＝`DateTime.Today`） | OrderID = その受注 | Status が下表のとおり |
| OrdTfDetail_出荷済は編集不可（TestCase：出荷日 NULL／`2001/01/01`） | EV-ORD-201 手順 8, ORD-T4, SC-ORD-02-B01 | 受注 1 件 | OrderID = その受注 | CanEdit = true（出荷日 NULL）／false（出荷日あり） |
| OrdTfDetail_明細の項目と並びと金額 | EV-ORD-201 手順 3・4, SC-ORD-02-I17〜I20, 共通仕様 3 節, OD-T4 | 受注 1 件（送料 `34.50`）。商品 A・M・X（名前の目印 `A`・`M`・`X`。名前の昇順 A < M < X）を**M・X・A の順に**明細へ入れる：M = 単価 `50.00`・割引 `0.25`・数量 2・在庫 17、X = `20.00`・`0.05`・3・在庫 0、A = `100.00`・`0.15`・12・在庫 5 | OrderID = その受注 | Lines が 3 行で A・M・X の順。A：UnitPrice = 100.00・Discount = 0.15・Price = 85.00・Quantity = 12・UnitsInStock = 5・Amount = 1020.00。M：50.00・0.25・37.50・2・17・75.00。X：20.00・0.05・19.00・3・0・57.00。Subtotal = 1152.00、Freight = 34.50、Total = 1186.50 |
| OrdTfDetail_廃番商品は商品名に付記する | EV-ORD-201 手順 6, SC-ORD-02-I17（列 1） | 受注 1 件に商品 P（廃番でない）・Q（廃番）を入れる | OrderID = その受注 | P の ProductName が P の名前と同じ（「（廃番）」を含まない）。Q の ProductName が Q の名前で始まり「（廃番）」で終わる |
| OrdTfDetail_明細金額は四捨五入して合算する | 共通仕様 3 節（明細金額は小数第 3 位を四捨五入し、その値を合算）, EV-ORD-201 手順 4 | 受注 1 件（送料 10.00）に、単価 `0.50`・割引 `0.15`・数量 1 の明細を商品 2 つで 2 行 | OrderID = その受注 | 各行の Price = 0.425、Amount = 0.43（`0.425` の四捨五入。偶数丸めなら 0.42）。Subtotal = 0.86、Total = 10.86 |
| OrdTfDetail_明細金額の丸めは数量を掛けた後 | 共通仕様 3 節, EV-ORD-201 手順 4 | 受注 1 件（送料 0）に、単価 `0.50`・割引 `0.15`・数量 3 の明細 1 行 | OrderID = その受注 | Price = 0.425、Amount = 1.28（`0.425 × 3 = 1.275` を丸める。価格を先に丸めると 1.29） |
| OrdTfDetail_明細が無い受注 | DEC-CMN-02, EV-ORD-201 手順 3・4 | 明細が 0 件の受注（送料 `7.25`） | OrderID = その受注 | ErrorFlag = false。Lines が 0 行、Subtotal = 0、Freight = 7.25、Total = 7.25 |
| OrdTfDetail_送料がNULLなら0として足す | DEC-CMN-03, 共通仕様 4 節（空値の表示） | 受注 1 件（送料 NULL）に、単価 `12.50`・割引 0・数量 1 の明細 1 行 | OrderID = その受注 | Freight = null、Subtotal = 12.50、Total = 12.50 |
| OrdTfDetail_行バージョン | EV-ORD-201 手順 7, 共通仕様 9.1 節（既定 1・更新で +1） | 受注 1 件（登録直後） | (1) OrderID = その受注。(2) `BumpOrder` の後、同じ OrderID | (1) RowVersion = 1。(2) RowVersion = 2 |
| OrdTfDetail_ほかの受注の明細は含めない | EV-ORD-201 手順 3（OrderID で抽出） | 受注 o1（明細 2 行）と受注 o2（明細 1 行） | OrderID = o1 | Lines が 2 行で、o2 の明細の商品を含まない。Subtotal は o1 の明細だけの合計 |
| OrdTfDetail_在庫がNULLの商品 | 共通仕様 4 節（空値の表示）, SC-ORD-02-I17（列 6） | 受注 1 件に、在庫 NULL の商品 1 行（`NewProduct(unitsInStock: null)`） | OrderID = その受注 | Lines が 1 行で UnitsInStock = null |
| OrdTfDetail_商品マスタに無い明細は含めない | EV-ORD-201 手順 3（Products を ProductID で内部結合）, DEC-ORD-24, 共通仕様 6 節（DB は妥当性を保証しない） | 受注 1 件（送料 `5.00`）に、存在する商品の明細 1 行（単価 `10.00`・割引 0・数量 2）と、テストが入れて `DeleteProduct` で消した ProductID の明細 1 行（単価 `100.00`・割引 0・数量 1） | OrderID = その受注 | ErrorFlag = false、Lines が 1 行（存在する商品の行）。Subtotal = 20.00、Total = 25.00（商品マスタに無い明細の金額 100.00 を含めない） |
| OrdTfDetail_割引率は端数も0も10進数で返る | DEC-ORD-25, SC-ORD-02-I17（列 3）, 共通仕様 3 節（割引率は 0〜1 の実数・10 進数で計算） | 受注 1 件に、単価 `100.00`・割引 `0.125`・数量 1 の明細（商品 A）と、単価 `20.00`・割引 `0`・数量 1 の明細（商品 M） | OrderID = その受注 | A：Discount = 0.125、Price = 87.50、Amount = 87.50。M：Discount = 0、Price = 20.00、Amount = 20.00 |
| OrdTfDetail_存在しない受注はERR-NOTFOUND | EV-ORD-201 手順 1, ERR-NOTFOUND | テストが受注を入れて `DeleteOrder` で消した ID | OrderID = その ID | ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」 |

**状況の判定の 9 行**（DEC-ORD-27：同じ日で時刻が後のときは定めない＝時刻の差のケースは作らず、日付は 00:00 の値だけにする。今日 = `DateTime.Today`。日付は 00:00）：

| 要求納期 | 出荷日 | Status |
| --- | --- | --- |
| `2000/01/01` | NULL | 納期遅延 |
| `2199/12/31` | NULL | 未出荷 |
| 今日 | NULL | 未出荷（当日は遅延でない） |
| 今日 − 1 日 | NULL | 納期遅延 |
| NULL | NULL | 未出荷（要求納期が NULL は遅延としない） |
| `2199/12/31` | `2001/01/01` | 出荷済 |
| `2001/01/01` | `2001/01/01` | 出荷済（`出荷日 > 要求納期` でない） |
| `2001/01/01` | `2001/01/02` | 納期遅延 |
| NULL | `2001/01/01` | 出荷済 |

### 3.2 受注の削除（`OrdTfDelete`。`EV-ORD-203`）

「行バージョン」は `GetRowVersion`（登録直後は 1）。件数は `CountOrders`・`CountDetails`（DB を直接読む）。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfDelete_受注と明細が消える | EV-ORD-203 手順 2〜5, ORD-T8 | 受注 1 件（明細 2 行） | OrderID = その受注、RowVersion = 1 | ErrorFlag = false。`CountOrders` = 0、`CountDetails` = 0 |
| OrdTfDelete_ほかの受注と明細は消えない | EV-ORD-203 手順 3・4（OrderID の行だけ） | 受注 o1（明細 2 行）と o2（明細 1 行・送料あり） | OrderID = o1、RowVersion = 1 | o1 の受注・明細が 0 件。o2 の受注が 1 件・明細が 1 件のまま、`OrdTfDetail(o2)` の内容（送料・Lines）が削除の前と同じ |
| OrdTfDelete_明細が無い受注も消える | EV-ORD-203 手順 3・4, DEC-CMN-02（DB は妥当性を保証しない） | 明細が 0 件の受注 | OrderID = その受注、RowVersion = 1 | ErrorFlag = false。`CountOrders` = 0 |
| OrdTfDelete_行バージョンが違えばERR-CONFLICTでロールバック | EV-ORD-203 手順 2〜4, ERR-CONFLICT, 共通仕様 9.1 節 | 受注 1 件（明細 2 行・送料 5.00）。`BumpOrder`（送料 99.00。行バージョン = 2）の後 | OrderID = その受注、RowVersion = 1（古い値） | ErrorFlag = true、ErrorMessage =「他の利用者によって更新されています。最新の内容を読み込み直してください。」。`CountOrders` = 1、**`CountDetails` = 2（明細も消えない＝ロールバック）**、`GetRowVersion` = 2、`OrdTfDetail` の Freight = 99.00 |
| OrdTfDelete_存在しない受注はERR-NOTFOUND | EV-ORD-203 手順 4, ERR-NOTFOUND（他の利用者による削除を含む）, 共通仕様 9.1 節（CONFLICT と区別） | テストが受注を入れて `DeleteOrder` で消した ID | OrderID = その ID、RowVersion = 1 | ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」（ERR-CONFLICT の文言ではない） |
| OrdTfDelete_消した受注をもう一度消すとERR-NOTFOUND | EV-ORD-203 手順 4, ERR-NOTFOUND | 受注 1 件 | OrderID = その受注、RowVersion = 1 で 2 回続けて呼ぶ | 1 回目：ErrorFlag = false。2 回目：ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」 |

- 追加：**24 メソッド・33 テストケース**（3.1 が 18 メソッド・27 ケース〔状況の判定 9・編集の活性 2〕、3.2 が 6 メソッド・6 ケース）。合計は、受注一覧の時点の件数（51 メソッド・82 テストケース）と合わせて 75 メソッド・115 テストケース（共用の `OrdTfBusinessTests.cs` の件数）。
- 「受注一覧」の B層のテストは変えない。net10.0 でも net48 でも同じ結果（`decimal`・日付・文字列が同じ）になること。

## 4. 画面の E2E（`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfDetailScreenTests`。新しいクラス）

B層で確かめることは繰り返さない（状況の判定・金額の丸め・削除の範囲とロールバックは B層）。画面の表示・活性・遷移・一覧状態の復元・確認ダイアログ・メッセージの区別を確かめる。
共通の前提：`[SetUp]` でログイン（受注一覧の E2E と同じ部品）、テストが入れたデータ（2198 年）を使う。**要素が無いときに操作が待ち続けないよう、操作の前に `Expect` で要素を待つ**（Red を `Expect` の失敗にする）。一覧の並びは行の受注 ID で見る。
詳細は URL を直接開くか、一覧の受注 ID を選んで開く（下の各ケース）。確認ダイアログは `page.Dialog` で受ける（メッセージを記録して、承認か取り消し）。

### 4.1 ケース

「K」はテストが入れた顧客、「E」は担当者、「S」は配送業者。`i 番目` は `NewOrdersSequence`（受注日 = `2198/01/01` + i 日）の i 番目（1 から）。

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 詳細_受注情報と明細と金額の表示 | EV-ORD-201 手順 1〜5, SC-ORD-02-I01〜I04・I06〜I20, EMP-T1, 共通仕様 3・4 節（日付・空値・金額・割引率の表示） | K（電話 `03-1234-5678`）・E・S。受注 1 件：K・E・S・受注日 `2198/03/05 15:30`・要求納期 `2198/04/01`・出荷日 NULL・送料 `34.50`・Ship* の値。明細 3 行（商品 A・M・X を M・X・A の順に登録。3.1 の「明細の項目と並びと金額」と同じ値：A = `100.00`・0.15・12・在庫 5、M = `50.00`・0.25・2・在庫 17、X = `20.00`・0.05・3・在庫 0） | `/OrdTf/Detail?id=<その受注>` を開く | `#orderId` = 受注 ID、`#status` = 「未出荷」、`#customerName` = K の会社名、`#customerPhone` = `03-1234-5678`、`#employeeName` = 「姓 名」、`#orderDate` = `2198/03/05`（時刻は出ない）、`#requiredDate` = `2198/04/01`、`#shippedDate` = 空、`#shipperName` = S の会社名、`#shipName`〜`#shipCountry` = Ship* の値。`#detailList thead th` が「商品名」「単価」「割引率」「価格」「数量」「在庫」「明細金額」。`#detailList tbody tr` が 3 行で A・M・X の順、セルは A = [名前, `100.00`, `15%`, `85.00`, `12`, `5`, `1,020.00`]、M = [名前, `50.00`, `25%`, `37.50`, `2`, `17`, `75.00`]、X = [名前, `20.00`, `5%`, `19.00`, `3`, `0`, `57.00`]。`#subtotal` = `1,152.00`、`#freight` = `34.50`、`#total` = `1,186.50` で class に `fw-bold` を持つ。`#contents` の中に、入力欄（`input`〔`hidden`・ボタンを除く〕・`select`・`textarea`）が 0 個 |
| 詳細_顧客住所は空の要素を詰めて連結する | SC-ORD-02-I05, DEC-ORD-18（区切り文字は定めない） | 顧客 K：Address = `A1`・City = `C1`・Region = NULL・PostalCode = 空文字・Country = `Jp`。K の受注 1 件 | 詳細を開く | `#customerAddress` が正規表現 `^A1([\s\S]*)C1\1Jp$` に一致する（区切りは期待にしない。要素が順に並び、空の要素の分の区切りが残らない） |
| 詳細_メインボタンと編集の活性（TestCase：出荷日 NULL／`2198/03/10`） | EV-ORD-201 手順 8, ORD-T4, SC-ORD-02-B01・B02・B06, cmn-framework.md の画面共通, 決定 4 | 受注 1 件（要求納期 `2198/04/01`。出荷日は TestCase） | 詳細を開く | `#contents` の中に `#btnMain1`〜`#btnMain5` が 5 つ。`#btnMain1` はキャプション「削除」で活性。`#btnMain2`〜`#btnMain5` は非活性。`#btnEdit` がキャプション「編集」で、出荷日 NULL のとき活性・出荷日ありのとき非活性。`#btnBack` がキャプション「一覧へ戻る」で活性。`#btnEdit`・`#btnBack` はフッタ（`#footer-buttons`）の外にある |
| 詳細_廃番商品は商品名に付記し、在庫がNULLなら空 | EV-ORD-201 手順 6, SC-ORD-02-I17（列 1・列 6）, 共通仕様 4 節（空値の表示） | 受注 1 件に商品 P（廃番でない・在庫 5）・Q（廃番・**在庫 NULL**）。P の名前 < Q の名前 | 詳細を開く | `#detailList tbody tr` が 2 行。1 行目の商品名のセルが P の名前（「（廃番）」を含まない）、在庫のセルが `5`。2 行目の商品名のセルが Q の名前を含み「（廃番）」を含み、**在庫のセルが空** |
| 詳細_割引率と価格の表示 | DEC-ORD-25, DEC-ORD-26, SC-ORD-02-I17（列 3・4）, 共通仕様 3 節（画面表示は小数 2 桁） | 受注 1 件に、商品 A（単価 `0.50`・割引 `0.15`・数量 3）、M（`100.00`・`0.125`・1）、X（`20.00`・0・1）を A・M・X の順に（名前の昇順）入れる | 詳細を開く | `#detailList tbody tr` が 3 行。A：割引率 `15%`、価格 **`0.43`**（価格 `0.425` を小数第 3 位で四捨五入。切り捨て・偶数丸めなら `0.42`）、明細金額 `1.28`。M：割引率 **`12.5%`**、価格 `87.50`。X：割引率 **`0%`**、価格 `20.00` |
| 詳細_NULLは空欄で金額は0 | EV-ORD-201 手順 2, 共通仕様 4 節（空値の表示）, DEC-CMN-02・03 | 受注 1 件：CustomerID・EmployeeID・OrderDate・RequiredDate・ShippedDate・ShipVia・Ship*・送料がすべて NULL、明細なし | 詳細を開く | `#customerName`〜`#shipperName`（`#status` を除く）・`#shipName`〜`#shipCountry`・`#freight` がすべて空。`#status` = 「未出荷」。`#detailList tbody tr` が 0 行。`#subtotal` = `0.00`、`#total` = `0.00` |
| 詳細_存在しない受注は一覧へ戻りエラーを表示する | EV-ORD-201 手順 1, ERR-NOTFOUND, 画面遷移 F-2・F-4, DEC-ORD-28 | K の受注 25 件（i 番目 = `2198/01/01` + i 日） | K で検索 → 次へ（「2 / 2 ページ」。5 行）→ 先頭行（i = 5）の受注 ID を選ぶ（詳細）→ テストがその受注を `DeleteOrder` で消す → ページを再読み込みする | 一覧が表示される（`#orderList` が見える、`#orderId` は無い）。`#messageArea` が「対象のデータは存在しません。」で、class に `text-danger` を持ち `text-info` を持たない。保持された一覧状態で戻る：`#CustomerID` = K、`#pageInfo` = 「2 / 2 ページ」、`#totalCount` = 「全 24 件」、一覧 4 行（i = 4 から i = 1） |
| 一覧_受注IDを選ぶと詳細へ遷移する | EV-ORD-106, SC-ORD-01-I06（列 1） | K の受注 1 件 | `/OrdTf` を開き、K を選んで検索 → 1 行目の受注 ID（セルの `<a>`）を選ぶ | URL が `/OrdTf/Detail`（クエリ `id` = その受注 ID）。`#orderId` = その受注 ID |
| 一覧_詳細から戻ると検索条件・ページ・並べ替えが復元される | EV-ORD-106 手順 1, EV-ORD-206, EV-ORD-101 手順 3, 画面遷移 F-2, EventSpec「一覧状態の保持」, 共通仕様 4 節（検索条件の保持） | K の受注 45 件（i 番目 = 受注日 `2198/01/01` + i 日。**すべて E・出荷日 NULL（未出荷）**）に加えて、**受注日が範囲（i = 5〜44）の中の K の受注 2 件**：(x) 担当者 E2（E と別）・未出荷・受注日 `2198/01/20`、(y) 担当者 E・出荷済（出荷日 `2198/02/11`）・受注日 `2198/02/10`（x・y は 5 つの条件のうち担当者・出荷状況で除かれる） | `/OrdTf` を開き、K・E・開始 `2198/01/06`（i = 5）・終了 `2198/02/14`（i = 44）・「未出荷」を選ぶ（入れる）→ 検索（40 件）→ `#sortOrderDate`（昇順）→ 次へ（「2 / 2 ページ」。先頭行 = i = 25）→ 先頭行の受注 ID を選ぶ（詳細）→ `#btnBack`（一覧へ戻る）→ 先頭へ | 詳細の後、`#btnBack` の直後：`#CustomerID` = K、`#EmployeeID` = E、`#OrderDateFrom` = `2198/01/06`、`#OrderDateTo` = `2198/02/14`、`#ShippedStatus` の選択 = 「未出荷」。`#pageInfo` = 「2 / 2 ページ」、`#totalCount` = 「全 40 件」、一覧 20 行で先頭行 = i = 25 から昇順に i = 44 まで（x・y を含まない。含むと「全 42 件」になる＝復元した担当者・出荷状況が再検索に効いていない）。先頭へ：「1 / 2 ページ」、20 行で i = 5 から昇順に i = 24 まで（x・y を含まない。並べ替えも保持）。どの時点でも `#messageArea` は空 |
| 一覧_詳細から戻ると再検索される | EV-ORD-206 手順 2, EV-ORD-101 手順 4 | K の受注 25 件（i 番目 = `2198/01/01` + i 日） | K で検索 → 次へ（「2 / 2 ページ」。5 行、先頭行 = i = 5）→ 先頭行を選ぶ（詳細）→ テストが i = 1 の受注を `DeleteOrder` で消す → `#btnBack` | `#totalCount` = 「全 24 件」、`#pageInfo` = 「2 / 2 ページ」、一覧 4 行（i = 5 から i = 2。消えた受注を含まない） |
| 一覧_入力欄だけ変えて詳細へ行っても最後に検索した条件で復元される | EV-ORD-106 手順 1, EV-ORD-102 手順 5, DEC-ORD-04, DEC-ORD-19 | K の受注 3 件、別の顧客 L の受注 1 件 | K で検索 → `#CustomerID` を L に変えるだけ（検索しない）→ K の行の受注 ID を選ぶ（詳細）→ `#btnBack` | `#CustomerID` = K（L ではない）、一覧が K の 3 行、`#totalCount` = 「全 3 件」 |
| 一覧_詳細を直接開いて戻ると既定の一覧 | EV-ORD-101 手順 3（保持された一覧状態が無い場合は既定）, EV-ORD-206 | 受注 1 件（このブラウザでは一覧を開いていない＝保持された一覧状態が無い） | `/OrdTf/Detail?id=<その受注>` を直接開く → `#btnBack` | 一覧が表示される。`#CustomerID` = 空、`#EmployeeID` = 空、`#OrderDateFrom`・`#OrderDateTo` = 空、`#ShippedStatus` の選択 = 「すべて」、`#pageInfo` = `1 / \d+ ページ` |
| 一覧_復帰以外で直接開くと保持した状態は使わない | EV-ORD-101 入力（保持された一覧状態は他画面からの復帰時のみ）, DEC-ORD-20 | K の受注 3 件 | K で検索 → 1 行目の受注 ID を選ぶ（詳細）→ `/OrdTf` を直接開く（メニューからと同じ。`#btnBack` を使わない） | `#CustomerID` = 空、`#pageInfo` = `1 / \d+ ページ`（保持した条件を使わない） |
| 新規登録_一覧状態を保持して登録の URL へ遷移する | EV-ORD-107 手順 1・2, SC-ORD-01-B03, 決定 4 | K の受注 45 件（i 番目 = `2198/01/01` + i 日） | K で検索 → 次へ（「2 / 3 ページ」）→ `#btnNew`（キャプション「新規登録」）→ 遷移後に `/OrdTf/Back` を開く | `#btnNew` は活性（検索後も）で、フッタ（`#footer-buttons`）の外にある。押した後：URL のパスが `/OrdTf/Edit`、クエリに `mode=new` を持ち `id` を持たない。`/OrdTf/Back` の後：`#CustomerID` = K、`#pageInfo` = 「2 / 3 ページ」、先頭行 = i = 25（既定の並びの 2 ページ目） |
| 削除_確認を取り消すと何も起きない | EV-ORD-203 手順 1, 共通仕様 4 節（削除の確認） | 受注 1 件（明細 1 行） | 詳細を開く → `#btnMain1`（削除）→ ダイアログを取り消す | ダイアログのメッセージが「受注 ID nnn を削除します。よろしいですか？」（nnn = 受注 ID）。取り消した後も詳細のまま（`#orderId` = 受注 ID）、`#messageArea` は空。ネットワークが落ち着いてから確かめて、`CountOrders` = 1、`CountDetails` = 1 |
| 削除_承認すると削除して保持された一覧状態で一覧へ戻る | EV-ORD-203 手順 1〜6, EV-ORD-206, 共通仕様 4 節（削除の確認・メッセージの区別）, DEC-ORD-22 | K の受注 45 件（i 番目 = `2198/01/01` + i 日）。**25 番目の受注に明細 1 行** | K で検索 → 次へ（「2 / 3 ページ」。先頭行 = i = 25）→ 先頭行の受注 ID を選ぶ（詳細）→ `#btnMain1` → ダイアログを承認 | ダイアログのメッセージが「受注 ID nnn を削除します。よろしいですか？」。承認の後、一覧が表示される：`#messageArea` が「受注 ID nnn を削除しました。」で、class に `text-info` を持ち `text-danger` を持たない。`#CustomerID` = K、`#pageInfo` = 「2 / 3 ページ」、`#totalCount` = 「全 44 件」、一覧 20 行（i = 24 から i = 5。削除した受注を含まない）。`CountOrders` = 0、`CountDetails` = 0 |
| 削除_最後のページの最後の1件を消すと最終ページを表示する | EV-ORD-203 手順 6, DEC-ORD-06, DEC-ORD-02, DEC-ORD-23 | K の受注 41 件（i 番目 = `2198/01/01` + i 日） | K で検索 → 末尾へ（「3 / 3 ページ」。1 行 = i = 1）→ その行の受注 ID を選ぶ → `#btnMain1` → 承認 | 一覧が表示される：`#pageInfo` = 「2 / 2 ページ」、`#totalCount` = 「全 40 件」、一覧 20 行（最終ページ。i = 21 から i = 2。削除した i = 1 は含まない）。`#messageArea` が「受注 ID nnn を削除しました。」（情報） |
| 削除_他の利用者が更新していたらERR-CONFLICTで最新を再表示する | EV-ORD-203 手順 4, ERR-CONFLICT, 共通仕様 9.1 節, EV-ORD-201 手順 7（保持する RowVersion も更新） | 受注 1 件（送料 `5.00`、明細 1 行） | 詳細を開く → テストが `BumpOrder`（送料 `99.00`）→ `#btnMain1` → 承認 → 続けて `#btnMain1` → 承認 | 1 回目の後：詳細のまま（`#orderId` = 受注 ID）、`#messageArea` が「他の利用者によって更新されています。最新の内容を読み込み直してください。」で class に `text-danger` を持ち `text-info` を持たない、`#freight` = `99.00`（最新の再表示）、`CountOrders` = 1・`CountDetails` = 1。2 回目の後：一覧が表示され、`#messageArea` が「受注 ID nnn を削除しました。」（情報）、`CountOrders` = 0 |
| 削除_既に消されていたらERR-NOTFOUNDで一覧へ戻る | EV-ORD-203 手順 4, ERR-NOTFOUND（他の利用者による削除を含む）, 画面遷移 F-2・F-4, DEC-ORD-28 | K の受注 25 件（i 番目 = `2198/01/01` + i 日） | K で検索 → 次へ（「2 / 2 ページ」）→ 先頭行（i = 5）の受注 ID を選ぶ（詳細）→ テストがその受注を `DeleteOrder` で消す → `#btnMain1`（削除）→ 承認 | 一覧が表示される（`#orderList` が見える、`#orderId` は無い）。`#messageArea` が「対象のデータは存在しません。」で、class に `text-danger` を持ち `text-info` を持たない。保持された一覧状態で戻る：`#CustomerID` = K、`#pageInfo` = 「2 / 2 ページ」、`#totalCount` = 「全 24 件」、一覧 4 行（i = 4 から i = 1） |

### 4.2 既存の E2E の変更

なし。決定 4（遷移だけのボタンはメインボタンに含めない）により、受注一覧の `OrdTfScreenTests`（`初期表示_ボタン` を含む）は変えない。新規登録は `#btnNew`（一覧の上部）で、メインボタンの `#btnMain2`〜`#btnMain5` は非活性のまま。

- 追加：**19 メソッド・20 テストケース**（「詳細_メインボタンと編集の活性」が 2 ケース）。受注一覧の E2E（26 メソッド・36 テストケース）と合わせて 45 メソッド・56 テストケース（`OrdTfScreenTests` 26・`OrdTfDetailScreenTests` 19）。

## 5. 作らないケースと理由

受注一覧の 5 章と同じ（`ERR-DB`〔`DEC-CMN-07`〕・`PRE-DB`・システム エラー、未ログインの挙動など）。加えて次を作らない。

| 対象 | 理由 |
| --- | --- |
| `EV-ORD-202`（編集への遷移）、編集ボタンを押したときの振る舞い | 範囲外（次の回）。この回は活性（`EV-ORD-201` 手順 8）だけを確かめる |
| `EV-ORD-108`・`204`・`205`、`SC-ORD-02` の B03〜B05 | 範囲外（3 回目以降）。置かない |
| `SC-ORD-03` の画面そのもの | 次の回。`EV-ORD-107` は URL への遷移と一覧状態の保持まで |
| 遷移パラメータの受注 ID が無い・数値でないとき（`PRE-PARAM` の違反） | DEC-ORD-21：定めない（実装に任せ、テストの期待にしない）。画面の操作からは起こらない |
| 削除の CSRF 対策（`[ValidateAntiForgeryToken]`） | 仕様（本文・決定記録）に無い。`QUALITY.md` 4 節のレビューの観点（Web のセキュリティ）で見る |
| 詳細・削除のトランザクションの分離レベル | 実装の決定（`ReadCommitted`）で、境界から観測できない |
| 状況の判定で、同じ日で出荷日の時刻が要求納期より後のとき | DEC-ORD-27：定めない（実装に任せ、時刻の差のケースを作らない）。ケースは時刻 00:00 の 9 行 |
| 詳細の ShippedDate が NULL でないときの `#status` の画面表示（納期遅延の強調など） | 仕様は `SC-ORD-02-I02` に強調を求めていない（B層で状況の判定を確かめる） |
| 検証エラー・復元の組み合わせ（ページ・並べ替えの全パターン） | 一覧状態の保持は受注一覧のページ切替・並べ替えのケースで確かめ済み。ここは遷移をまたぐ復元の代表（5 条件・ページ・並べ替え）に絞る |

## 6. 仕様の決定（決定済み。決定のレポートの判断のとおり）

決定のレポート：[test3-testfirst-OrdTfDetail-mvc-core-decisions.md](./test3-testfirst-OrdTfDetail-mvc-core-decisions.md)。決定 1〜3・5〜12 は仕様の決定記録（NorthwindDbSampleSpec@ec66d6e の `LLD/Decisions/Orders.md`）に記録済みで、根拠は `DEC-ORD-*` と書く。**決定 4 は、このプロジェクトの規約（`cmn-framework.md`）の解釈で、仕様の決定記録には無い**。記録は決定のレポート（上のファイル）にあり、根拠は「決定 4」と書く。

| # | 記録 | 仕様の ID | 決めたこと | 影響するケース |
| --- | --- | --- | --- | --- |
| 1 | DEC-ORD-18 | SC-ORD-02-I05（顧客住所の連結） | **定めない**（区切り文字は実装に任せ、期待にしない）。テストは、各要素が順に並ぶことと、空の要素（NULL・空文字）が詰められること（区切りの残りが無いこと）を、区切りの文字を使わない正規表現（`^A1([\s\S]*)C1\1Jp$`）で確かめる | B層 `OrdTfDetail_顧客住所は…`、E2E `詳細_顧客住所は…` |
| 2 | DEC-ORD-19 | EV-ORD-106 手順 1, EV-ORD-102 手順 5, DEC-ORD-04 | 詳細へ遷移するときに保持する検索条件は、**最後に実行した検索の条件**（入力欄の今の値ではない） | E2E `一覧_入力欄だけ変えて…` |
| 3 | DEC-ORD-20 | EV-ORD-101 入力（保持された一覧状態は復帰時のみ） | **復帰（`/OrdTf/Back`）のときだけ復元**し、直接開いたときは既定の一覧 | E2E `一覧_復帰以外で直接開くと…` |
| 4 | 決定 4（決定のレポート） | cmn-framework.md の画面共通、SC-ORD-01-B03、SC-ORD-02-B01・B02・B06 | 遷移だけのボタン（B03・B01・B06）は**メインボタンに含めず、画面定義書の配置に従う**。一覧の B03 は一覧の上部（`#btnNew`）、詳細の B01 は画面上部（`#btnEdit`）・B06 は画面下部（`#btnBack`）。フッタのメインボタンは、一覧が `#btnMain1`（検索）だけ、詳細が `#btnMain1`（削除）だけで、残りは非活性。**既存の `初期表示_ボタン` は変えない**。規約（`cmn-framework.md`）の文は変えない | E2E `詳細_メインボタンと編集の活性`・`新規登録_…`・削除の各ケース |
| 5 | DEC-ORD-21 | PRE-PARAM（EV-ORD-201） | **定めない**（ケースを作らない） | なし |
| 6 | DEC-ORD-22 | EV-ORD-203 手順 6 | 削除の完了メッセージは**情報**（`text-info`） | E2E の削除の 3 ケース |
| 7 | DEC-ORD-23 | EV-ORD-203 手順 6, EV-ORD-206, DEC-ORD-06 | 復元するページが無くなっていたら**最終ページ**を表示する | E2E `削除_最後のページの最後の1件を…` |
| 8 | DEC-ORD-24 | EV-ORD-201 手順 3・4 | 商品マスタに無い商品の明細は小計・合計に**含めない**（明細一覧と揃える。一覧の合計金額とは食い違いうる） | B層 `OrdTfDetail_商品マスタに無い明細は含めない` |
| 9 | DEC-ORD-25 | SC-ORD-02-I17（列 3） | 割引率の百分率は、**端数があれば小数で表示**（`12.5%`。整数なら `15%`・`0%`） | B層 `OrdTfDetail_割引率は端数も0も10進数で返る`、E2E `詳細_割引率と価格の表示` |
| 10 | DEC-ORD-26 | SC-ORD-02-I17（列 4） | 価格の 2 桁の表示は**四捨五入**（`0.425` → `0.43`） | E2E `詳細_割引率と価格の表示` |
| 11 | DEC-ORD-27 | EventSpec「状況の判定」 | 同じ日で出荷日の時刻が要求納期より後のときは**定めない**（時刻の差のケースを作らない。9 行のまま） | なし |
| 12 | DEC-ORD-28 | 共通仕様 7 節 ERR-NOTFOUND, F-2・F-4 | `ERR-NOTFOUND` で戻った一覧は**保持された一覧状態**（削除の完了と同じ） | E2E `詳細_存在しない受注は…`・`削除_既に消されていたら…`（戻った一覧の `#CustomerID`・`#pageInfo`・`#totalCount` を期待に含む） |

## 7. 段 2 の確かめ方（予定）

- **Red（B層）**：引数クラス・戻り値クラスに 1.1 のメンバを足し、`OrdTfBusiness` に `OrdTfDetail`・`OrdTfDelete` の**戻り値を設定するだけの空の `UOC_`** を置く（`OrdTfDetailLine` の型も）。テストは `Assert` で落ちる（空の実装は ErrorFlag = false・空の値を返すため、`ErrorFlag = true` を期待する `存在しない…`・`行バージョンが違えば…`・`消した受注をもう一度…` も `Assert` で落ちる）。ビルドや呼び出しで落ちるのは Red ではない。`MVC_Sample_Core\MVC_Sample.Tests`（net10.0）で流す。net48 は段 4 の後で全件。
- **Red（P層）**：ビルドが通り、`/OrdTf/Detail` を開ける最小の土台（`Detail.cshtml`・`Detail`・`Back` のアクション。要素なし）を先に置く。E2E のテストは `Expect` で落ちる。**ケースが最初から通ってしまったら、段 2 の報告に書き、壊す確かめで実装を見ていることを確かめる**（`TESTING.md` 4）。
- **最初から通りうるケース**：なし（予定）。`削除_確認を取り消すと何も起きない` は、土台の詳細に要素が無いので、最初の `Expect` で落ちる。
- 決定 1〜12 は決まっている（根拠は `DEC-ORD-18`〜`28`。決定 4 だけは「決定 4」）。DEC-ORD-21・27 のケースは作らない。
- テストのファイル：`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`・`TestData.cs`（足す）、`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfDetailScreenTests.cs`（新規）・`TestData.cs`（足す）。`OrdTfScreenTests.cs` は変えない。
