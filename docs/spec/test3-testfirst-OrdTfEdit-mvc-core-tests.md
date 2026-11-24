# テスト仕様：受注詳細（登録・編集）OrdTfEdit（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

起こした仕様の版：NorthwindDbSampleSpec@a724ca5（ブランチ proxy/ord-management。da98653 からの差分は決定記録 DEC-ORD-29〜49 の追加と索引の件数だけ〔693e663 → cb427e9 は DEC-ORD-48、cb427e9 → a724ca5 は DEC-ORD-49 の 1 節と件数だけ〕で、期待は変わらない。本文 `HLD/Common.md`・`HLD/UI_FlowList.md`・`LLD/EventSpec/Orders.md`・`LLD/UI_ElementsAndEventList/Orders.md`・`LLD/TableSchema/Orders.md` と、決定記録 `LLD/Decisions/Common.md`〔DEC-CMN-01〜07〕・`LLD/Decisions/Orders.md`〔DEC-ORD-01〜49〕を合わせて読んだ）
規約：`docs/spec/cmn-framework.md` の「画面共通」「MVC」、`docs/spec/cmn-testfirst.md`、`docs/spec/test3-testfirst-OrdTfEdit.md`、`docs/plan/cmn-testfirst.md` の「B層を共用する構成」
前の回：[受注一覧](./test3-testfirst-OrdTf-mvc-core-tests.md)・[受注詳細（照会・削除）](./test3-testfirst-OrdTfDetail-mvc-core-tests.md)（承認・実装済み。このケースはその上に足す）。決定のレポート：[test3-testfirst-OrdTfEdit-mvc-core-decisions.md](./test3-testfirst-OrdTfEdit-mvc-core-decisions.md)

対象：`SC-ORD-03`（`I01`〜`I18`、`B01`〜`B04`）、`EV-ORD-301`〜`309`、`EV-ORD-202`（詳細の「編集」から編集モードへ）。
範囲外：`SC-ORD-04`・`05` とそこへの遷移（`EV-ORD-108`・`204`・`205`）、F-3 のうちキャンセル以外の離脱（ヘッダのメニューなど）。

> **仕様の決定は 6 章（決定 1〜21。記録は NorthwindDbSampleSpec@a724ca5 の DEC-ORD-29〜49、決定 N → DEC-ORD-(28+N)）。** 答えは決定のレポートの「判断」（代行が決めた）。根拠は DEC-ORD-29〜49 で書く。推奨と違う答えは DEC-ORD-36（NULL）だけ。決定の記録が無い項目は、期待にしない。

## 0. 進め方の前提

- B層は `Shared\`（`OrdTfBusiness`・`OrdTfParameterValue`・`OrdTfReturnValue`）を共用する。**この回の B層（`OrdTfEditInit`・`OrdTfCustomer`・`OrdTfProduct`・`OrdTfSave`）とそのテスト（`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs` に足す）はこのケースで作り**、net10.0（`MVC_Sample_Core\MVC_Sample.Tests`）→ net48（`MVC_Sample\MVC_Sample.Tests`）の順に、同時にでなく全件流す。
- テストのプロジェクト、アプリの起動、ログイン、ブラウザ、日付の範囲（B層 2199 年・E2E 2198 年）、並行実行の決まりは受注一覧・受注詳細のとおり（新しいプロジェクトは作らない）。E2E は新しいクラス `OrdTfEditScreenTests`。
- 表示のメッセージは仕様の文言そのまま（`DEC-CMN-01`）。**仕様に文言が無いものは、文言を期待にしない**（DEC-ORD-30・31・33 の答え。「空でない」「エラーの class」だけ確かめる）。
- **データを書き換えるテスト**は、テストが作った受注・明細・顧客・担当者・配送業者・商品だけを書き換える・消す。Northwind2 の 830 件と既存の顧客・商品は触らない。後片付けは落ちても行う（`CleanUp`。**保存で作られた受注は、テストが作った顧客の受注として消す**。1.3）。**終わりに受注の件数が 830 件、`Customers`・`Products` の件数が始めと同じかの確かめは、計画の手順に入れる**（範囲外）。
- 受注一覧・受注詳細の既存のテストは変えない（期待値の変更は無い）。1 回目の `新規登録_一覧状態を保持して登録の URL へ遷移する` が確かめる URL `/OrdTf/Edit?mode=new` も変えない。

## 1. テストが固定する契約（実装の決定。振る舞いは変えない。段 4 の実装はこれに従う）

### 1.1 B層

分離レベルは `ReadCommitted`（保存も。テストの呼び出しの値）。「当日」は `DateTime.Today`。`MethodName`：`OrdTfEditInit`・`OrdTfCustomer`・`OrdTfProduct`・`OrdTfSave`。

**引数クラス `OrdTfParameterValue` に足すメンバ**（既存は変えない。`OrderID`・`RowVersion` は詳細の回で足し済み）

| メンバ | 型 | 意味 |
| --- | --- | --- |
| `Mode` | `string` | `"new"`（登録）・`"edit"`（編集）。`OrdTfEditInit`・`OrdTfSave` |
| `CustomerID` | `string` | 既存。`OrdTfCustomer` は選択された顧客 ID、`OrdTfSave` は入力値（空・null は未選択） |
| `EmployeeIDText`・`ShipViaText` | `string` | `OrdTfSave` の担当者・配送業者の入力値（選択肢の value。空・null は未選択。数値でない文字列は存在しないものとして `VAL-EXISTS`） |
| `OrderDateText`・`RequiredDateText` | `string` | `OrdTfSave` の受注日・要求納期（画面の入力のまま） |
| `FreightText` | `string` | 送料（画面の入力のまま。空は 0。`DEC-ORD-01`） |
| `ShipName`・`ShipAddress`・`ShipCity`・`ShipRegion`・`ShipPostalCode`・`ShipCountry` | `string` | 出荷先（`OrdTfSave`） |
| `ProductID` | `int` | `OrdTfProduct` の商品 ID |
| `SaveLines` | `List<OrdTfSaveLine>` | 明細入力表の行（画面の並びのまま） |
| `DeletedProductIDs` | `List<int>` | 編集で、削除対象として印を付けた**読み込み時の**明細行の商品 ID |

`OrdTfSaveLine`（新しい型。入力の文字列は画面の入力のまま）：`ProductIDText`（選択された商品 ID。空は未選択）・`UnitPriceText`・`DiscountText`・`QuantityText`、`OriginalProductID`（`int?`。読み込み時に存在した行はその商品 ID、画面で足した行は null）・`OriginalUnitPrice`（`decimal?`）・`OriginalDiscount`（`decimal?`）・`OriginalQuantity`（`int?`）（読み込み時の値。`EV-ORD-308` 手順 4-3「値が変更された行」の判定に使う）。

**戻り値クラス `OrdTfReturnValue` に足すメンバ**（既存は変えない。`CustomerOptions`・`EmployeeOptions`〔先頭に空白の選択肢を含む〕・`OrderID`・`OrderDate`・`RequiredDate`・`ShippedDate`・`Ship*`・`Freight`・`RowVersion`・`CustomerPhone`・`CustomerAddress`・`CustomerName`・`Subtotal`・`Total` は既存の意味のまま再利用する）

| メンバ | 型 | 意味 |
| --- | --- | --- |
| `ShipperOptions`・`ProductOptions` | `List<OrdTfOption>` | 配送業者・商品の選択肢。**先頭に空白の選択肢（`Value`・`Text` とも空文字）**。商品は廃番でないものだけ（`OD-T3`）、商品名の昇順。配送業者は会社名の昇順 |
| `EditCustomerID` | `string` | `OrdTfEditInit`（編集）の顧客 ID（NULL は null） |
| `EditEmployeeID`・`EditShipVia` | `int?` | 同、担当者 ID・配送業者 ID（NULL は null） |
| `EditLines` | `List<OrdTfEditLine>` | `OrdTfEditInit`（編集）の明細。商品名の昇順 |
| `ProductName` | `string` | `OrdTfProduct` の商品名 |
| `UnitPrice` | `decimal?` | `OrdTfProduct` の `Products.UnitPrice` |
| `UnitsInStock` | `int?` | `OrdTfProduct` の `Products.UnitsInStock` |
| `ErrorMessages` | `List<string>` | `OrdTfSave` の検証で、**共通メッセージ領域に出すもの**（関連チェック・個別の検証。文言） |
| `ErrorFields`（既存） | `List<string>` | `OrdTfSave` の項目単位の違反の項目。キー：`CustomerID`・`EmployeeID`・`OrderDate`・`RequiredDate`・`ShipVia`・`Freight`・`ShipName`・`ShipAddress`・`ShipCity`・`ShipRegion`・`ShipPostalCode`・`ShipCountry`、明細は `Lines[i].ProductID`・`Lines[i].UnitPrice`・`Lines[i].Discount`・`Lines[i].Quantity`（`i` は入力の行の 0 始まりの添字） |

`OrdTfEditLine`（新しい型）：`ProductID`（int）・`ProductName`（string。付記なしの商品名）・`Discontinued`（bool）・`UnitPrice`（decimal）・`Discount`（decimal。0〜1）・`Quantity`（int）・`UnitsInStock`（int?）・`RowVersion`（int。明細の行バージョン）。

- **検証の返し方**：`OrdTfSave` は、検証をすべて行って違反を集める。違反が 1 件でもあれば `ErrorFlag = true`、DB は更新しない。項目単位の違反は `ErrorFields`、関連チェック・個別の違反は `ErrorMessages`（文言）。`ErrorMessage`（基底）は項目単位の文言を期待にしない（DEC-ORD-30）。
- **業務エラー**：`ERR-NOTFOUND` は `ErrorFlag = true`・`ErrorMessage` =「対象のデータは存在しません。」、`ERR-CONFLICT` は同「他の利用者によって更新されています。最新の内容を読み込み直してください。」。`OrdTfEditInit` の出荷済は `ErrorFlag = true`・`ErrorMessage` =「出荷済の受注は編集できません。」。
- **保存の戻り値**：成功は `ErrorFlag = false`・`OrderID`（登録は採番値、編集は受注 ID）・`RowVersion`（保存後の `Orders.RowVersion`）。**`ErrorFlag = true`（検証の違反・`ERR-*`）のときの `OrderID` は 0**。
- **保存は B層の 1 トランザクション**（`EV-ORD-308` 手順 2〜5）。編集は `Orders` を `OrderID` と `RowVersion` の条件で更新（`RowVersion = RowVersion + 1`）→ 0 件ならロールバックして `Orders` の有無で `ERR-CONFLICT`/`ERR-NOTFOUND` → 明細を行ごとに適用（削除→更新→挿入。**全削除・再登録にしない**）。**パラメタ化する。文字列で連結しない。**

### 1.2 P層（MVC）の URL と動き

| 対象 | 指定 |
| --- | --- |
| 登録モード | GET `/OrdTf/Edit?mode=new`（既存の約束。受注 ID は付けない） |
| 編集モード | GET `/OrdTf/Edit?mode=edit&id=<受注 ID>`（**テストが固定する。** 受注詳細の「編集」〔`#btnEdit`〕はここへ遷移する。遷移の前の出荷済の判定は 2.2） |
| 保存 | POST（`[ValidateAntiForgeryToken]`）。成功は受注詳細 `/OrdTf/Detail?id=<受注 ID>` へリダイレクトし、`#messageArea` に完了メッセージ（情報）を出す。**成功後の再読み込みで二重登録しない** |
| 確認ダイアログ | `window.confirm`（出荷先の上書き〔`EV-ORD-302`〕・キャンセル〔`EV-ORD-309`〕）。メッセージは、キャンセルは「入力内容は保存されません。よろしいですか？」、上書きは文言を期待にしない（DEC-ORD-33） |
| 金額の再計算・行の追加削除・商品の反映 | 実装は自由（画面内の script でも往復でもよい）。**テストは操作の後に `Expect` で待って確かめる**（画面の振る舞いだけを見る） |
| `ERR-NOTFOUND`（読み込み・顧客選択・商品選択・保存） | 受注一覧へ戻り（保持された一覧状態。直接開いて状態が無ければ既定の一覧）、`#messageArea` に「対象のデータは存在しません。」（エラー）（DEC-ORD-40） |
| 編集の読み込みで出荷済 | 受注詳細 `/OrdTf/Detail?id=…` へ戻り、`#messageArea` に「出荷済の受注は編集できません。」（エラー） |
| キャンセル | 編集：`/OrdTf/Detail?id=<受注 ID>`、登録：保持された一覧状態の一覧（`/OrdTf/Back` と同じ） |

### 1.3 テストのデータ（`TestData`。B層用・E2E 用の両方に足す。既存の呼び出しは変えない）

| 足すもの | 内容 |
| --- | --- |
| `NewProduct` の引数 | `unitPrice`（`decimal?`、既定は null＝列の既定値 0。指定したら入れる。NULL を入れる指定は作らない） |
| `NewDetail`（既存） | 割引は `float`（real）。0.125 などの 2 進で正確な値をテストに使う |
| `GetOrder(orderId)` | `Orders` の 1 行を読む（`CustomerID`・`EmployeeID`・`OrderDate`・`RequiredDate`・`ShippedDate`・`ShipVia`・`Freight`・`Ship*`・`RowVersion`） |
| `GetDetails(orderId)` | `Order Details` を ProductID 昇順で読む（`ProductID`・`UnitPrice`・`Quantity`・`Discount`・`RowVersion`） |
| `BumpDetail(orderId, productId)` | 他の利用者が明細だけを更新したことを模す：`UPDATE [Order Details] SET RowVersion = RowVersion + 1 WHERE …`（`Orders.RowVersion` は変えない） |
| `DeleteCustomer(customerId)` | 他の利用者の顧客の削除を模す（後始末は、無い行の削除でも落ちない。E2E 用） |
| `ShipOrder(orderId)` | 他の利用者の出荷確定を模す：`ShippedDate` を設定し `RowVersion + 1` |
| `CleanUp`（既存の拡張） | 記録した受注に加え、**記録した顧客を `CustomerID` に持つ受注とその明細**も消す。さらに、**テストが `OrdTfSave` の戻り値の OrderID（> 0）を `Track(orderId)` で記録**し、顧客が K でない受注（検証をすり抜けた誤保存）も消す。**テストが入れた目印の `ShipName` を持つ受注と明細も消す**（落ちても） |
| `CountOrdersOfCustomer(customerId)` | 顧客で受注を数える（DB を直接読む） |
| `CountOrdersOfShipName(shipName)` | 出荷先名称（`ShipName`）が一致する受注を数える（DB を直接読む。他のテストと並行しても干渉しない目印として使う） |

## 2. 画面の id・class（E2E が決める。段 4 の実装が付ける）

### 2.1 受注詳細（登録・編集。`/OrdTf/Edit`）

| 対象 | 指定 |
| --- | --- |
| 受注 ID（I01） | `#orderId`（テキスト。登録は「（登録時に採番）」、編集は受注 ID） |
| 受注情報の入力（I02・I05〜I09） | `<select id="CustomerID">`・`<select id="EmployeeID">`・`<input id="OrderDate">`・`<input id="RequiredDate">`・`<select id="ShipVia">`・`<input id="Freight">`（日付は `yyyy/MM/dd` のテキスト。顧客・担当者・配送業者の先頭の `<option value="">` が空白）。必須の表示名の末尾は「＊」 |
| 顧客情報（I03・I04） | `#customerPhone`・`#customerAddress`（テキスト。顧客が空なら空） |
| 出荷先（I10〜I15） | `<input id="ShipName">`・`ShipAddress`・`ShipCity`・`ShipRegion`・`ShipPostalCode`・`ShipCountry` |
| 項目の直下のエラー（項目単位。`DEC-CMN-05`） | `#<入力の id>Error`（例 `#OrderDateError`・`#FreightError`・`#ShipNameError`）。違反が無いときは空。class に `text-danger` |
| 明細入力表（I16） | `<table id="lineList">`。`tbody tr` が 1 行。行の中：`.line-product`（`<select>`。先頭に空白の option）・`.line-unitprice`・`.line-discount`・`.line-quantity`（`<input>`）・`.line-price`・`.line-stock`・`.line-amount`（テキスト）・`.line-remove`（「行削除」ボタン。B02）・行内の項目単位のエラー `.line-product-error`・`.line-unitprice-error`・`.line-discount-error`・`.line-quantity-error`（空か `text-danger`） |
| 小計・合計（I17・I18） | `#subtotal`・`#total`。金額は通貨記号なしの千区切り＋小数 2 桁（`1,186.50`）、価格・明細金額も同じ |
| 共通メッセージ領域 | `#messageArea`（エラーは `text-danger`、情報は `text-info`、警告は `text-warning`。在庫超過の警告の表示先はDEC-ORD-44 の答え＝共通メッセージ領域） |
| 明細行追加（B01。明細表の上部） | `#btnAddLine`（キャプション「明細行追加」。常時活性。`#contents` の中でフッタの外） |
| メインボタン（フッタ 5 つ） | `#btnMain1` =「保存」（B03。活性）。`#btnMain2`〜`#btnMain5` は非活性（遷移だけ・入力支援のボタンはメインボタンに含めない。1 回目の決定 4） |
| キャンセル（B04。画面下部） | `#btnCancel`（キャプション「キャンセル」。常時活性。フッタ `#footer-buttons` の外） |

### 2.2 受注詳細（照会）への追加

- 詳細への遷移後の `#messageArea` は詳細の既存の指定（`text-info`/`text-danger`）。**`#btnEdit` を押すと `/OrdTf/Edit?mode=edit&id=…` へ遷移する**（既存の指定に動きを足す）。
- **`#btnEdit` は、遷移の前に `EV-ORD-202` 手順 1 の判定をする**：既存の B層のメソッド `OrdTfDetail`（`CanEdit`＝`ShippedDate` が NULL か）で出荷日を読み直し、`CanEdit = false` なら遷移せず、詳細のまま `#messageArea` に「出荷済の受注は編集できません。出荷を取り消してから編集してください。」（エラー）を出す（`OrdTfEditInit` の文言〔`EV-ORD-301` 手順 3-2〕とは別の文言）。`CanEdit = true` なら編集モードの URL へ遷移する。

## 3. B層のルート メソッドのテスト（`OrdTfBusinessTests`。net10.0 → net48）

`Call("OrdTfEditInit", pv => …)` の形で、既存の部品を使う。**既存のケースは変えない。** 受注日などの日付は 2199 年。テストが入れた顧客 K・担当者 E・配送業者 S・商品 P を使う。「K」「E」「S」「P」はテストが入れたもの。名前の目印の決まりは受注一覧のとおり。

### 3.1 `OrdTfEditInit`（`EV-ORD-301`）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfEditInit_顧客の選択肢 | EV-ORD-301 手順 1, 選択肢の構築, SC-ORD-03-I02 | 顧客 K1・K2（ID の昇順と会社名の昇順が逆） | Mode = "new" | ErrorFlag = false。CustomerOptions の先頭が Value = ""・Text = ""（空白は 1 件だけ）。K1・K2 を含み、Value = CustomerID・Text = 会社名、K2 が K1 より前 |
| OrdTfEditInit_担当者の選択肢 | EV-ORD-301 手順 1, SC-ORD-03-I05, 選択肢の構築（姓・名の昇順） | 担当者 E1・E2・E3（`OrdTfInit` の担当者のケースと同じ構成：E1・E2 は姓が同じで名は E2 < E1、E3 は姓が大きい） | Mode = "new" | EmployeeOptions の先頭が空白。3 件を含み、Value = EmployeeID、Text = `姓 + " " + 名`。並びは E2・E1・E3 |
| OrdTfEditInit_配送業者の選択肢 | EV-ORD-301 手順 1, SC-ORD-03-I08（先頭に空白の選択肢・会社名の昇順） | 配送業者 S1・S2（ID の昇順と会社名の昇順が逆） | Mode = "new" | ShipperOptions の先頭が Value = ""・Text = ""（空白は 1 件だけ）。S1・S2 を含み、Value = ShipperID・Text = 会社名、S2 が S1 より前 |
| OrdTfEditInit_商品の選択肢は廃番を除く | EV-ORD-301 手順 1, OD-T3, 選択肢の構築 | 商品 P1・P2（ID の昇順と商品名の昇順が逆）、廃番の商品 Q | Mode = "new" | ProductOptions の先頭が空白（1 件だけ）。P1・P2 を含み、Value = ProductID・Text = 商品名、P2 が P1 より前。**Q を含まない** |
| OrdTfEditInit_編集の受注情報 | EV-ORD-301 手順 3-1・3-3・4, SC-ORD-03-I01〜I15, ORD-T6 | K（電話 `03-1234-5678`・住所の要素あり）・E・S。受注 1 件：K・E・S、受注日 `2199/03/05`、要求納期 `2199/04/01`、出荷日 NULL、送料 `34.50`、Ship* は顧客と別の値 | Mode = "edit"、OrderID = その受注 | ErrorFlag = false。OrderID、EditCustomerID = K、EditEmployeeID = E、EditShipVia = S、OrderDate・RequiredDate、Freight = 34.50、Ship* が受注の値（顧客の住所ではない）、CustomerPhone = `03-1234-5678`、RowVersion = 1。CustomerAddress が正規表現（K の要素が順に並び、区切りは期待にしない。DEC-ORD-45） |
| OrdTfEditInit_編集の行バージョン | EV-ORD-301 手順 3-3, 共通仕様 9.1 | 受注 1 件。`BumpOrder` の後 | Mode = "edit" | RowVersion = 2 |
| OrdTfEditInit_編集で顧客・担当者・配送業者が無い | EV-ORD-301 手順 4, ORD-T1（表示側は NULL を許容） | CustomerID・EmployeeID・ShipVia・日付・Ship* が NULL の受注（明細 1 行） | Mode = "edit" | ErrorFlag = false。EditCustomerID = null、EditEmployeeID = null、EditShipVia = null、CustomerPhone・CustomerAddress が空、OrderDate・RequiredDate が null |
| OrdTfEditInit_編集の明細の項目と並びと金額 | EV-ORD-301 手順 3-4・5, SC-ORD-03-I16・I17・I18, OD-T4, 共通仕様 3 節 | 受注 1 件（送料 `34.50`）に、商品 A・M・X（名前の昇順 A < M < X）を **M・X・A の順に**入れる：M = `50.00`・`0.25`・2・在庫 17、X = `20.00`・`0.05`・3・在庫 NULL、A = `100.00`・`0.15`・12・在庫 5。ほかの受注 o2 にも明細 1 行 | Mode = "edit" | EditLines が 3 行で A・M・X の順。各行の ProductID・ProductName（付記なし）・UnitPrice・Discount・Quantity・UnitsInStock（X は null）。RowVersion = 1（各行）。Subtotal = 1152.00、Total = 1186.50。o2 の明細を含まない |
| OrdTfEditInit_編集の明細金額は四捨五入して合算する | 共通仕様 3 節（明細金額は小数第 3 位を四捨五入し、その値を合算）, OD-T4, EV-ORD-301 手順 5 | 受注 1 件（送料 10.00）に、単価 `0.50`・割引 `0.15`・数量 1 の明細を商品 2 つで 2 行 | Mode = "edit" | 各行の UnitPrice = 0.50・Discount = 0.15・Quantity = 1。Subtotal = 0.86（各行 `0.425` を四捨五入して 0.43 ずつ。偶数丸めなら 0.84）、Total = 10.86 |
| OrdTfEditInit_編集の明細金額の丸めは数量を掛けた後 | 共通仕様 3 節, OD-T4, EV-ORD-301 手順 5 | 受注 1 件（送料 0）に、単価 `0.50`・割引 `0.15`・数量 3 の明細 1 行 | Mode = "edit" | Subtotal = 1.28（`0.425 × 3 = 1.275` を丸める。価格を先に丸めると 1.29）、Total = 1.28。Discount = 0.15（real を 10 進数へ変換した値） |
| OrdTfEditInit_編集の明細の行バージョン | EV-ORD-301 手順 3-4（各行の RowVersion も保持） | 明細 2 行。うち 1 行を `BumpDetail` | Mode = "edit" | 該当行の RowVersion = 2、他方 = 1 |
| OrdTfEditInit_廃番の既存明細は残し選択肢には無い | EV-ORD-301 手順 3-5, OD-T3 | 受注 1 件に、廃番でない商品 P・廃番の商品 Q の明細 | Mode = "edit" | EditLines に P・Q の 2 行、Q の Discontinued = true・P は false。ProductOptions に Q を含まない（P を含む） |
| OrdTfEditInit_編集で存在しない受注 | EV-ORD-301 手順 3-1, ERR-NOTFOUND | `DeleteOrder` で消した ID | Mode = "edit" | ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」 |
| OrdTfEditInit_編集で出荷済は編集させない（TestCase：出荷日 `2001/01/01`／当日） | EV-ORD-301 手順 3-2, ORD-T4, ERR-BIZ | 出荷日が NULL でない受注 1 件 | Mode = "edit" | ErrorFlag = true、ErrorMessage =「出荷済の受注は編集できません。」 |

### 3.2 `OrdTfCustomer`（`EV-ORD-302` 手順 2〜4）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfCustomer_電話と住所 | EV-ORD-302 手順 3, SC-ORD-03-I03・I04 | 顧客 K（電話 `03-1234-5678`、住所・市・地域・郵便番号・国あり） | CustomerID = K | ErrorFlag = false。CustomerPhone = `03-1234-5678`。CustomerAddress が正規表現で、K の 5 要素が順に並ぶ（区切りは期待にしない。DEC-ORD-45） |
| OrdTfCustomer_出荷先の既定値 | EV-ORD-302 手順 4, ORD-T6 | 顧客 K（上と同じ） | CustomerID = K | ShipName = K の会社名、ShipAddress = K の `Address`、ShipCity = `City`、ShipRegion = `Region`、ShipPostalCode = `PostalCode`、ShipCountry = `Country`（住所の連結ではなく各項目） |
| OrdTfCustomer_空の要素は空 | EV-ORD-302 手順 3・4, 共通仕様 4 節（空値の表示）, DEC-ORD-45 | 顧客 K0：Address `A1`・City `C1`・Region NULL・PostalCode 空文字・Country `Jp`・電話 NULL | CustomerID = K0 | CustomerPhone が空（null か空文字）。ShipRegion・ShipPostalCode が空。CustomerAddress が `^A1([\s\S]*)C1\1Jp$` に一致 |
| OrdTfCustomer_存在しない顧客 | EV-ORD-302 手順 2, ERR-NOTFOUND | 存在しない顧客 ID | CustomerID = その ID | ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」 |

### 3.3 `OrdTfProduct`（`EV-ORD-306` 手順 3・4）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfProduct_単価と在庫 | EV-ORD-306 手順 4, OD-T2 | 商品 P：単価 `12.50`・在庫 7 | ProductID = P | ErrorFlag = false。ProductName = P の名前、UnitPrice = 12.50、UnitsInStock = 7 |
| OrdTfProduct_在庫がNULL | EV-ORD-306 手順 4, 共通仕様 4 節 | 商品 P（在庫 NULL） | ProductID = P | UnitsInStock = null |
| OrdTfProduct_存在しない商品 | EV-ORD-306 手順 3, ERR-NOTFOUND, VAL-EXISTS | `DeleteProduct` で消した ID | ProductID = その ID | ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」 |

### 3.4 `OrdTfSave` の登録（`EV-ORD-308` 手順 3）

共通の前提：顧客 K・担当者 E・配送業者 S・商品 P1・P2（廃番でない）。有効な入力 `V` ＝ Mode = "new"、CustomerID = K、EmployeeIDText = E、OrderDateText = `2199/03/05`、RequiredDateText = `2199/04/01`、ShipViaText = S、FreightText = `12.50`、Ship* = 6 項目に値、SaveLines = [P1：`10.00`・`0.15`・`2`（Original* は null）]。DB の確認は `GetOrder`・`GetDetails`。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfSave_登録で受注と明細が入る | EV-ORD-308 手順 3-1〜3-3 | 上の共通 | `V`（明細 2 行：P1 `10.00`・`0.15`・2 と P2 `20.00`・`0`・1） | ErrorFlag = false。OrderID > 0（`GetOrder` で読める）。`GetOrder`：CustomerID = K、EmployeeID = E、OrderDate = `2199/03/05`、RequiredDate = `2199/04/01`、ShipVia = S、Freight = 12.50、Ship* が入力のとおり。`GetDetails`：2 行（P1：UnitPrice 10.00・Quantity 2・Discount 0.15、P2：20.00・1・0）。RowVersion = 1（受注・各明細。戻り値の RowVersion = 1） |
| OrdTfSave_登録の出荷日はNULL | EV-ORD-308 手順 3-1（`ShippedDate` は NULL） | 上の共通 | `V` | `GetOrder` の ShippedDate が NULL（付随の期待。**ShippedDate を NULL で SET する実装は検出しない**＝登録では元々 NULL のため） |
| OrdTfSave_登録で送料が空なら0 | DEC-ORD-01 | 上の共通 | `V`（FreightText = ""） | ErrorFlag = false。`GetOrder` の Freight = 0（NULL でない） |
| OrdTfSave_登録で要求納期・配送業者を省略できる | EV-ORD-308 検証内容（要求納期は指定されている場合のみ）, SC-ORD-03-I07・I08（必須でない） | 上の共通 | `V`（RequiredDateText = ""、ShipViaText = ""、Ship* = すべて ""） | ErrorFlag = false。`GetOrder` の RequiredDate・ShipVia が NULL。**Ship*（6 項目）も NULL**（空文字でない。DEC-ORD-36） |
| OrdTfSave_登録の明細の割引は端数も入る | OD-T4, 共通仕様 3 節（割引率は 0〜1 の実数） | 上の共通 | `V`（明細：`100.00`・`0.125`・1） | `GetDetails` の Discount = 0.125（real で正確に格納される値）、UnitPrice = 100.00 |
| OrdTfSave_在庫を超える数量でも保存できる | SC-ORD-03-I16（列 6：警告は保存を妨げない）, EV-ORD-306 手順 6, EV-ORD-307 手順 3 | 上の共通。商品 P（在庫 5） | `V`（明細：P・単価 `10.00`・割引 0・数量 `6`） | ErrorFlag = false。`GetDetails` の P：Quantity = 6 |
| OrdTfSave_登録でほかの受注に触れない | EV-ORD-308 手順 3 | ほかの受注 o2（K・明細 1 行）。`BumpOrder` 済み（RowVersion 2） | `V` | o2 の RowVersion = 2・明細 1 行のまま・Freight が登録前と同じ |

### 3.5 `OrdTfSave` の検証（`EV-ORD-308` 検証内容・手順 1）

どのケースも、**違反があれば DB を更新しない**。確かめ方：**戻り値の OrderID が 0（成功時だけ > 0）** に加え、登録は (a) `CountOrdersOfCustomer(K)` が 0、(b) **有効な入力 `V` の `ShipName` に、ケースごとに一意の目印（`"Tf" + GUID 8 桁`。40 桁以内）を入れ、`CountOrdersOfShipName(目印)` が 0** であること（顧客を空・存在しない顧客にした違反でも誤った保存を見つける。保存の前後の IDENT_CURRENT は、並行して動く E2E の登録と干渉するので使わない）。`ShipName` の桁数のケースは、目印の代わりに桁数の値で入れ、違反のとき `CountOrdersOfShipName`（その値）が 0 であることを同じ関数で数える。編集は受注・明細が保存の前と同じ（`GetOrder`・`GetDetails`）。有効な入力 `V` から**指定した項目だけ**を変える。`ErrorFields` は期待の項目と**一致**（余計な項目を含まない）、`ErrorMessages` は空（指定が無いとき）。

| ケース | 根拠 | 入力（`V` からの変更） | 期待 |
| --- | --- | --- | --- |
| OrdTfSave_必須の欠け（TestCase：顧客／担当者／受注日。値は空・null） | VAL-REQUIRED, ORD-T1, DEC-CMN-05 | 該当項目を空 | ErrorFlag = true。ErrorFields = [`CustomerID`／`EmployeeID`／`OrderDate`]（1 件）。ErrorMessages が空 |
| OrdTfSave_明細の必須の欠け（TestCase：商品／単価／割引率／数量） | VAL-REQUIRED, EV-ORD-308 検証内容 | 明細 1 行（商品が空の行も 1 行として数える＝DEC-ORD-43）の該当項目を空 | ErrorFlag = true。ErrorFields = [`Lines[0].ProductID`／`.UnitPrice`／`.Discount`／`.Quantity`]。**ErrorMessages が空**（`ORD-T5` に当たらない。DEC-ORD-43） |
| OrdTfSave_受注日の空白だけは必須の欠け（DEC-ORD-29） | VAL-REQUIRED, DEC-ORD-09（空白だけは未指定とみなす）を当てたDEC-ORD-29 | OrderDateText = `   `（空白だけ） | ErrorFlag = true。ErrorFields = [`OrderDate`] |
| OrdTfSave_受注日の形式（TestCase：`abc`／`2199/1/5`／`2199-01-05`／`2199/02/30`）（DEC-ORD-29） | VAL-DATE, DEC-ORD-09 を当てたDEC-ORD-29、DEC-CMN-05 | OrderDateText = 各値 | ErrorFlag = true。ErrorFields = [`OrderDate`]。ErrorMessages が空 |
| OrdTfSave_要求納期の形式（同 4 通り）（DEC-ORD-29） | VAL-DATE | RequiredDateText = 各値 | ErrorFields = [`RequiredDate`] |
| OrdTfSave_要求納期の空白だけは未指定（DEC-ORD-29） | VAL-DATE（要求納期は指定されている場合のみ）, DEC-ORD-09 を当てたDEC-ORD-29 | RequiredDateText = `   ` | ErrorFlag = false。`GetOrder` の RequiredDate が NULL |
| OrdTfSave_日付の範囲の端は通る（TestCase：2 通り） | VAL-DATE, TableSchema Orders（datetime。`1753/01/01`〜`9999/12/31` が入る）。範囲外の扱いはDEC-ORD-47（答え：定めない）のためケースにしない | OrderDateText = `1753/01/01`（RequiredDateText は空）／OrderDateText = `2199/03/05`、RequiredDateText = `9999/12/31` | いずれも ErrorFlag = false。`GetOrder` の日付が入力のとおり |
| OrdTfSave_受注日が要求納期より後は関連チェック | VAL-DATE, ORD-T3, DEC-CMN-04 | OrderDateText = `2199/04/02`、RequiredDateText = `2199/04/01` | ErrorFlag = true。**ErrorFields が空**、ErrorMessages が 1 件（空でない。文言は期待にしない。DEC-ORD-31） |
| OrdTfSave_受注日と要求納期が同じ日は通る | ORD-T3（`受注日 <= 要求納期`） | 両方 `2199/04/01` | ErrorFlag = false。戻り値の OrderID > 0、`GetOrder` で入力の値が入っている（OrderDate = RequiredDate = `2199/04/01`） |
| OrdTfSave_送料の違反（TestCase：`-0.01`／`abc`） | VAL-NUMERIC, EV-ORD-303 | FreightText = 各値 | ErrorFields = [`Freight`] |
| OrdTfSave_送料0は通る | VAL-NUMERIC（0 以上） | FreightText = `0` | ErrorFlag = false。`GetOrder` の Freight = 0 |
| OrdTfSave_単価の違反（TestCase：`-0.01`／`abc`）と0 | VAL-NUMERIC, EV-ORD-308 検証内容（明細の単価は 0 以上） | UnitPriceText = 各値（`0` は通る） | ErrorFields = [`Lines[0].UnitPrice`]（`0` は ErrorFlag = false。戻り値の OrderID > 0、`GetOrder` で入力の値が入っている・`GetDetails` の UnitPrice = 0） |
| OrdTfSave_割引率の違反（TestCase：`-0.01`／`1.01`／`abc`）と境界0・1 | VAL-NUMERIC | DiscountText = 各値（`0`・`1` は通る） | ErrorFields = [`Lines[0].Discount`]（`0`・`1` は ErrorFlag = false。戻り値の OrderID > 0、`GetOrder` で入力の値が入っている・`GetDetails` の Discount = 入力の値） |
| OrdTfSave_数量の違反（TestCase：`0`／`-1`／`1.5`／`abc`）と境界1 | VAL-NUMERIC | QuantityText = 各値（`1` は通る） | ErrorFields = [`Lines[0].Quantity`]（`1` は ErrorFlag = false。戻り値の OrderID > 0、`GetOrder` で入力の値が入っている・`GetDetails` の Quantity = 1） |
| OrdTfSave_出荷先の桁数（TestCase：`ShipName` 40／`ShipAddress` 60／`ShipCity` 15／`ShipRegion` 15／`ShipPostalCode` 10／`ShipCountry` 15） | VAL-LENGTH, TableSchema Orders の桁数 | 該当項目を **桁数ちょうど**（通る）／**桁数 + 1**（違反）。日本語の文字を含む。**`ShipName` の値は、`"Tf" + GUID 8 桁 + 日本語の文字（`あ`）`で 40 桁・41 桁ちょうどに作る**（例 `"Tf" + guid8 + new string('あ', 30)` が 40 桁、1 文字足したものが 41 桁。同じ値を使う別のケース・前の実行の残りと件数が混ざらない）。ほかの 5 項目は決まった値のまま | ちょうど：ErrorFlag = false、保存された値が同じ。+1：ErrorFields = [該当項目のキー] |
| OrdTfSave_存在しない顧客・担当者・配送業者・商品（TestCase：4 通り） | VAL-EXISTS, DEC-CMN-05 | CustomerID／EmployeeIDText／ShipViaText／明細の ProductIDText を、存在しない値（または数値でない文字列）に | ErrorFlag = true。ErrorFields = [`CustomerID`／`EmployeeID`／`ShipVia`／`Lines[0].ProductID`] |
| OrdTfSave_商品の重複 | VAL-DUP, OD-T1, DEC-CMN-04 | 明細 2 行に同じ商品 P1 | ErrorFlag = true。ErrorMessages に「同一商品の明細が重複しています。」を含む。ErrorFields が空 |
| OrdTfSave_明細が0行 | ORD-T5, ERR-BIZ | SaveLines が空 | ErrorFlag = true。ErrorMessages に「明細を 1 行以上入力してください。」を含む |
| OrdTfSave_新規の廃番商品は指定できない | OD-T3, ERR-BIZ | 明細の商品が廃番の商品 Q | ErrorFlag = true。ErrorMessages に「廃番商品は指定できません。」を含む |
| OrdTfSave_登録でOriginalProductIDを付けても新規の廃番商品は指定できない | OD-T3, EV-ORD-308 検証内容（個別：廃番。編集モードで読み込み時から存在する行は対象外）, ERR-BIZ | 明細の商品を廃番の商品 Q にし、**`OriginalProductID` = Q（`OriginalUnitPrice`・`OriginalDiscount`・`OriginalQuantity` も付ける）**。Mode = "new" | ErrorFlag = true。ErrorMessages に「廃番商品は指定できません。」を含む。受注が入らない（`AssertNothingSaved`） |
| OrdTfSave_違反は全部まとめて返す | EV-ORD-308 手順 1（全エラーを表示）, VAL-*, DEC-CMN-04・05 | 登録で：顧客を空、受注日 `2199/04/02` かつ要求納期 `2199/04/01`、送料 `-1`、明細 2 行（同じ商品 P1・数量 `0`） | ErrorFlag = true。ErrorFields が `CustomerID`・`Freight`・`Lines[0].Quantity`・`Lines[1].Quantity` を含む。ErrorMessages が「同一商品の明細が重複しています。」と関連チェックの 2 件。受注は入らない |

### 3.6 `OrdTfSave` の編集（`EV-ORD-308` 手順 4）

前提：編集用の受注 o（K・E・S、受注日 `2199/03/05`、送料 `5.00`、出荷日 NULL）に明細 2〜3 行（商品 A・M・X。A = `100.00`・`0.15`・12、M = `50.00`・`0.25`・2、X = `20.00`・`0.05`・3）。編集の入力は、`OrdTfEditInit` で読んだ状態を模す：Mode = "edit"、OrderID = o、RowVersion = 1、各行の Original* に読み込み時の値。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfSave_編集で受注の項目が更新される | EV-ORD-308 手順 4-1, 共通仕様 9.1 | o（明細 A・M） | 受注日・要求納期・担当者・配送業者・送料・Ship* をすべて別の値にして、明細は変えない | ErrorFlag = false、OrderID = o、RowVersion = 2。`GetOrder` の各項目が入力のとおり、RowVersion = 2、ShippedDate が NULL のまま、CustomerID が入力の顧客 |
| OrdTfSave_編集で空にした出荷先・要求納期・配送業者はNULLになる | DEC-ORD-36（空で保存した出荷先・要求納期・配送業者は NULL）, EV-ORD-308 手順 4-1 | o（要求納期・配送業者・Ship* に値がある。明細 A） | 要求納期・配送業者・Ship* の 6 項目をすべて空にして保存。明細は変えない | ErrorFlag = false。`GetOrder` の RequiredDate・ShipVia・Ship*（6 項目）がすべて NULL（空文字でない） |
| OrdTfSave_編集でほかの受注に触れない | EV-ORD-308 手順 4-1（OrderID の条件） | o とほかの受注 o2（明細 1 行） | `OrdTfSave_編集で受注の項目が更新される` と同じ（受注日・要求納期・担当者・配送業者・送料・Ship* をすべて別の値にする。明細は変えない） | o2 の RowVersion = 1、送料・明細が保存の前と同じ |
| OrdTfSave_編集で明細を行ごとに適用する | EV-ORD-308 手順 4-3, OD-T6 | o（明細 A・M・X）。**A の行に `BumpDetail`**（行バージョン 2。受注は変えない）。画面を開いた後の状態を模す | A：変更なし。M：数量 2 → 5、割引 `0.25` → `0.5`。X：削除（`DeletedProductIDs` に X）。N（新商品）：`30.00`・`0`・4 を足す | ErrorFlag = false。`GetDetails` が A・M・N の 3 行。**A の RowVersion = 2 のまま**（変更なしの行に触れない＝全削除・再登録でない）。M：Quantity = 5・Discount = 0.5・UnitPrice 50.00、RowVersion = 2。N：RowVersion = 1。X が無い |
| OrdTfSave_編集で単価だけ変えた行も更新される | EV-ORD-308 手順 4-3（値が変更された行） | o（明細 A） | A の単価 `100.00` → `99.99` | `GetDetails` の A：UnitPrice = 99.99、RowVersion = 2 |
| OrdTfSave_編集で商品を変えた行は削除して挿入する | EV-ORD-308 手順 4-3（商品が変更された既存行） | o（明細 A・M） | A の行の商品を N に変える（単価・数量は別の値） | `GetDetails` が M・N の 2 行（A が無い）。N：入力の値、RowVersion = 1。M は変わらない |
| OrdTfSave_編集で2行の商品を入れ替える | EV-ORD-308 手順 4-3（主キーの一部は更新しない）, VAL-DUP（入力に重複は無い） | o（明細 A・M） | 1 行目の商品を M に、2 行目の商品を A に変える | ErrorFlag = false。`GetDetails` が A・M の 2 行で、A に 2 行目の入力値、M に 1 行目の入力値（主キーの衝突で落ちない） |
| OrdTfSave_編集で削除した行の商品を別の行に付け替える | EV-ORD-308 手順 4-3（削除→挿入） | o（明細 A・M） | A の行を削除（`DeletedProductIDs` に A）、M の行の商品を A に変える | ErrorFlag = false。`GetDetails` が A の 1 行（M の行の入力値）。M が無い |
| OrdTfSave_編集で既存の廃番商品の行は残せる | OD-T3（読み込み時から存在する行は対象外） | o（明細：廃番の商品 Q、廃番でない P） | Q の行は変えず、P の数量だけ変える | ErrorFlag = false。Q の行が残る（Q の行は RowVersion = 1 のまま） |
| OrdTfSave_編集で既存の廃番商品の行は値を変えても保存できる | OD-T3（読み込み時から存在する行は対象外。値の変更の有無で区別しない） | o（明細：廃番の商品 Q・数量 2） | Q の行の数量を 2 → 3（商品は変えない） | ErrorFlag = false。`GetDetails` の Q：Quantity = 3、RowVersion = 2 |
| OrdTfSave_編集で新規に廃番商品は足せない | OD-T3, ERR-BIZ | o（明細 A） | 廃番の商品 Q の行を足す | ErrorFlag = true。ErrorMessages に「廃番商品は指定できません。」。DB は保存の前と同じ（RowVersion = 1、明細 A のみ） |
| OrdTfSave_編集で削除の印と同じ商品の行を残しても明細は1行以上残る（TestCase：残す行は読み込み時のまま／残す行の数量を 13 に変える）（DEC-ORD-49 の条件） | ORD-T5（受注は必ず 1 件以上の明細を持つ）, EV-ORD-308 検証内容（個別：明細が 1 行以上）, DEC-ORD-49 | o（明細 A のみ） | SaveLines = [A の行（`Same(A)`＝読み込み時のまま。または数量だけ 12 → 13 に変えた行）]、`DeletedProductIDs` = [A]（同じ商品を、残す行と削除の印の両方に指定する細工した入力） | **ErrorFlag の真偽は期待にしない**（どの扱いでもよい：エラーにして DB を変えない、または削除の印を無視して行を残す）。保存の後、`GetDetails(o).Count` ≧ 1（明細が 0 行にならない）。ErrorFlag = true のときは、`GetOrder` の RowVersion = 1 で `GetDetails` が保存の前と同じ |
| OrdTfSave_編集で読み込み時に無かった行にOriginalProductIDを付けても廃番商品は足せない | OD-T3, EV-ORD-308 検証内容（個別：廃番。編集モードで読み込み時から存在する行は対象外）, ERR-BIZ | o（明細 A。Q の明細は無い） | 廃番の商品 Q の行を足し、**`OriginalProductID` = Q（`OriginalUnitPrice`・`OriginalDiscount`・`OriginalQuantity` も付ける）** | ErrorFlag = true。ErrorMessages に「廃番商品は指定できません。」。DB は保存の前と同じ（RowVersion = 1、明細 A のみ。Q の行は入らない） |
| OrdTfSave_編集で全明細を削除はできない | ORD-T5, ERR-BIZ | o（明細 A） | A を削除（SaveLines が空、`DeletedProductIDs` = [A]） | ErrorFlag = true。ErrorMessages に「明細を 1 行以上入力してください。」。明細 A が残る・RowVersion = 1 |
| OrdTfSave_編集で行バージョンが違えばERR-CONFLICTでロールバック | EV-ORD-308 手順 4-2, ERR-CONFLICT | o（明細 A・M）。`BumpOrder`（送料 99.00。RowVersion = 2） | RowVersion = 1（古い値）。M の数量を変え、X を足し、A を削除 | ErrorFlag = true、ErrorMessage =「他の利用者によって更新されています。最新の内容を読み込み直してください。」。**`GetDetails` が A・M のまま（M の数量も元のまま。X は無い＝ロールバック）**、`GetOrder` の Freight = 99.00・RowVersion = 2 |
| OrdTfSave_編集で存在しない受注はERR-NOTFOUND | EV-ORD-308 手順 4-2, ERR-NOTFOUND, 共通仕様 9.1（CONFLICT と区別） | `DeleteOrder` で消した ID | OrderID = その ID、RowVersion = 1 | ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」（CONFLICT の文言ではない）。明細・受注は入っていない |
| OrdTfSave_編集で消された受注でも検証の違反が先に返る | EV-ORD-308 手順 1・4-2（検証が先。存在の確認は更新の手順）, ERR-VALIDATION, ERR-NOTFOUND | `DeleteOrder` で消した受注 o（明細 A を持っていた） | Mode = "edit"、OrderID = o、RowVersion = 1、送料 `-1`（ほかは有効な値） | ErrorFlag = true。ErrorFields = [`Freight`]。ErrorMessage は「対象のデータは存在しません。」ではない（受注が無くても、検証の違反が先に返る） |
| OrdTfSave_編集の検証エラーでは何も更新しない | EV-ORD-308 手順 1, ERR-VALIDATION | o（明細 A・M） | 送料 `-1`、M の数量を `0` | ErrorFlag = true。ErrorFields = [`Freight`, `Lines[1].Quantity`]（M の行が 2 行目）。`GetOrder` の RowVersion = 1・送料 5.00、`GetDetails` が保存の前と同じ |
| OrdTfSave_編集でも必須の欠けは違反（TestCase：顧客／担当者／受注日。値は空）（DEC-ORD-48） | VAL-REQUIRED, ORD-T1, EV-ORD-308 検証内容（モードを区別しない）, DEC-CMN-05, DEC-ORD-48 | o（明細 A。顧客・担当者・受注日に値がある） | Mode = "edit"、RowVersion = 1。該当項目（`CustomerID`／`EmployeeIDText`／`OrderDateText`）だけを空にして、ほかは読み込んだ状態のまま | ErrorFlag = true。ErrorFields = [`CustomerID`／`EmployeeID`／`OrderDate`]（1 件）。ErrorMessages が空。戻り値の OrderID は 0。`GetOrder` の RowVersion = 1・受注の各項目、`GetDetails` が保存の前と同じ |
| OrdTfSave_編集で保存の後は古い行バージョンで保存できない | 共通仕様 9.1（`RowVersion = RowVersion + 1`）, ERR-CONFLICT | o | (1) RowVersion = 1 で保存。(2) もう一度 RowVersion = 1 で保存。(3) (1) の戻り値の RowVersion で保存 | (1) ErrorFlag = false、RowVersion = 2。(2) ErrorFlag = true、ErrorMessage が CONFLICT の文言。(3) ErrorFlag = false、RowVersion = 3 |
| OrdTfSave_編集で他の受注の同じ商品の明細に触れない | EV-ORD-308 手順 4-3（OrderID と ProductID の一致） | o（明細 A）と o2（明細 A） | o の A の数量を変え、M を足す | o2 の明細 A の数量・RowVersion が保存の前と同じ |

- 追加：**3.1 が 14 メソッド・15 ケース、3.2 が 4、3.3 が 3、3.4 が 7、3.5 が 21 メソッド・約 58 ケース（TestCase を数える）、3.6 が 21 メソッド・約 24 ケース**。段 2 の報告で実数を書く。net10.0 でも net48 でも同じ結果になること。

## 4. 画面の E2E（`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfEditScreenTests`。新しいクラス）

B層で確かめることは繰り返さない（検証の全項目・金額の丸め・明細の行ごとの適用とロールバックは B層）。画面の表示・活性・遷移・確認ダイアログ・メッセージの区別・再計算の画面への反映を確かめる。
共通の前提：`[SetUp]` でログイン、2198 年のデータ。**操作の前に `Expect` で要素を待つ**（Red を `Expect` の失敗にする）。入力は `FillAsync`、選択は `SelectOptionAsync`（change が発火する）、日付・数値の確定は change（`BlurAsync` を足してよい）。確認ダイアログは `page.Dialog` で受ける（記録して承認か取り消し）。「K」「E」「S」「P」はテストが入れたもの。`行 i` は `#lineList tbody tr` の i 番目（1 始まり）。

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 登録_初期表示 | EV-ORD-301 手順 2・4・5, SC-ORD-03-I01〜I18, 1 回目の決定 4（メインボタン） | K・E・S・P（単価 `12.50`）がある | `/OrdTf/Edit?mode=new` を開く | `#orderId` =「（登録時に採番）」。`#OrderDate` = 今日（`yyyy/MM/dd`）。`#Freight` が数値 0（値を数値に読んで 0 と比べる。`0`・`0.00` どちらでもよい）。`#CustomerID`・`#EmployeeID`・`#ShipVia` の選択が空白。`#RequiredDate` と `#ShipName`〜`#ShipCountry` が空。`#customerPhone`・`#customerAddress` が空。`#lineList tbody tr` が 1 行（商品の選択が空、`.line-discount` = 0、`.line-quantity` = 1、`.line-remove` は非活性）。`#subtotal` = `0.00`・`#total` = `0.00`。`#btnAddLine` が活性。`#btnMain1`（保存）が活性で `#btnMain2`〜`#btnMain5` が非活性。`#btnCancel` が活性で `#footer-buttons` の外。`#messageArea` が空。**`#contents` の中の入力（`input`〔`type` が `hidden`・`button`・`submit` のものを除く〕・`select`・`textarea`）が、受注情報の 6 個（`#CustomerID`・`#EmployeeID`・`#OrderDate`・`#RequiredDate`・`#ShipVia`・`#Freight`）・出荷先の 6 個と、明細の各行の 4 個（`.line-product`・`.line-unitprice`・`.line-discount`・`.line-quantity`）だけ**（SC-ORD-03-I01〜I16。ほかに入力欄を置かない） |
| 登録_選択肢 | EV-ORD-301 手順 1, OD-T3, SC-ORD-03-I02・I05・I08・I16 列 1 | K1・K2、E1・E2、S、P1・P2（廃番でない）、Q（廃番） | 登録を開く | `#CustomerID` の最初の option が空白で、K1・K2 の会社名がある。`#EmployeeID`・`#ShipVia` も同様（先頭が空白）。`.line-product` の選択肢に P1・P2 があり Q が無い |
| 顧客選択_顧客情報と出荷先の既定値 | EV-ORD-302 手順 3・4, ORD-T6, DEC-ORD-45 | 顧客 K（電話・住所・市・地域・郵便番号・国あり） | 登録を開き、`#CustomerID` で K を選ぶ | `#customerPhone` = 電話。`#customerAddress` が K の要素を順に含む（区切りは期待にしない）。`#ShipName` = K の会社名、`#ShipAddress`・`#ShipCity`・`#ShipRegion`・`#ShipPostalCode`・`#ShipCountry` = K の各項目。確認ダイアログは出ない |
| 顧客選択_自動設定のままなら確認なしで上書き（DEC-ORD-32） | EV-ORD-302 手順 4（既に手入力している場合のみ確認）, DEC-ORD-32 | 顧客 K1・K2（出荷先が別の値） | 登録を開く → K1 を選ぶ → K2 を選ぶ | 2 回目も確認ダイアログが出ない。`#ShipName` = K2 の会社名、他も K2 の値 |
| 顧客選択_手入力があれば確認して承認なら上書き（DEC-ORD-32・33） | EV-ORD-302 手順 4, DEC-ORD-32 | K1・K2 | 登録を開く → `#ShipName` に `手入力` と入れる → K1 を選ぶ → 確認を承認 | 確認ダイアログが 1 回出る（文言は期待にしない）。`#ShipName` = K1 の会社名（上書き）。`#customerPhone` = K1 の電話 |
| 顧客選択_手入力があれば確認して取り消しなら上書きしない（DEC-ORD-32・33） | EV-ORD-302 手順 4（取り消された場合は上書きしない） | K1 | 同上で確認を取り消す | `#ShipName` = `手入力` のまま。`#ShipAddress` などは空のまま。**顧客情報（`#customerPhone`）は K1 の電話に変わる**（手順 3 は上書きと別） |
| 顧客選択_空に戻すと顧客情報だけ消える | EV-ORD-302 手順 1 | K | 登録を開く → K を選ぶ（出荷先が入る）→ 空白を選ぶ | `#customerPhone`・`#customerAddress` が空。`#ShipName` は K の会社名のまま。確認ダイアログは出ない |
| 顧客選択_編集では出荷先を変えない | EV-ORD-302 手順 5 | 受注 1 件（K1、Ship* は別の値）、顧客 K2 | 編集を開く → `#CustomerID` で K2 を選ぶ | `#customerPhone` が K2 の電話に変わる。`#ShipName`〜`#ShipCountry` が受注の値のまま。確認ダイアログは出ない |
| 顧客選択_他の利用者が消した顧客はERR-NOTFOUNDで一覧へ（DEC-ORD-40） | EV-ORD-302 手順 2, ERR-NOTFOUND, 画面遷移 F-4 | 顧客 K2（受注 25 件、i 番目 = `2198/01/01` + i 日）と、受注の無い顧客 K1（テストが `DeleteCustomer` で消す。**一覧状態を作る顧客と消す顧客は分ける**。消した顧客は一覧の `#CustomerID` の選択肢から無くなる）。`/OrdTf` で K2 を検索 → 次へ（「2 / 2 ページ」）→ `#btnNew` | 登録を開く（K1 が選択肢にある）→ テストが K1 を消す → `#CustomerID` で K1 を選ぶ | 一覧が表示される（`#orderList` が見え `#orderId` は無い）。`#messageArea` が「対象のデータは存在しません。」で class に `text-danger`。保持された一覧状態で戻る：`#CustomerID` = K2、`#pageInfo` = 「2 / 2 ページ」、`#totalCount` = 「全 25 件」、一覧 5 行 |
| 送料_変更で合計が変わる | EV-ORD-303 手順 2, 共通仕様 3 節 | 登録。明細 1 行（P：`10.00`・割引 0・数量 2。小計 `20.00`） | `#Freight` に `5.5` を入れて確定 | `#total` = `25.50`。`#subtotal` = `20.00`。`#Freight`の項目エラー `#FreightError` が空 |
| 送料_不正値はエラーで合計を変えない（TestCase：`-1`／`abc`） | EV-ORD-303 手順 1, VAL-NUMERIC, DEC-CMN-05 | 上と同じ（合計 `25.50` にした後） | `#Freight` に各値を入れて確定 | `#FreightError` が空でなく `text-danger`。`#total` = `25.50` のまま。`#messageArea` は空（項目単位は共通メッセージ領域に出さない） |
| 明細行追加_空行が足され既存の入力は残る | EV-ORD-304 手順 1〜3 | 登録。行 1 に P1・数量 3 を入れた | `#btnAddLine` | `#lineList tbody tr` が 2 行。行 2：商品の選択が空、`.line-discount` = 0、`.line-quantity` = 1、単価が空。行 1 の入力（商品・数量 3）が残る。**行 1・行 2 の `.line-remove` が活性** |
| 明細行削除_行が減り金額が再計算される | EV-ORD-305 手順 2〜4 | 登録。行 1：P1（`10.00`・0・数量 2）、行 2：P2（`20.00`・0・数量 1）。小計 `40.00` | 行 1 の `.line-remove` | `#lineList tbody tr` が 1 行（P2）。`#subtotal` = `20.00`。`.line-remove` が非活性（残り 1 行） |
| 明細行削除_1行のときは削除できない | EV-ORD-305 手順 1, SC-ORD-03-B02（2 行以上のとき活性） | 登録（1 行） | 行 1 の `.line-remove` の活性を見る | 非活性 |
| 商品選択_単価と在庫と金額が反映される | EV-ORD-306 手順 4・5, OD-T2, 共通仕様 3 節 | 商品 P（単価 `12.50`・在庫 20） | 登録 → 行 1 で P を選ぶ（数量は既定 1） | `.line-unitprice` = `12.50`、`.line-stock` = `20`、`.line-price` = `12.50`、`.line-amount` = `12.50`。`#subtotal` = `12.50`、`#total` = `12.50`。`#messageArea` に在庫の警告が出ない（`text-warning` を持たない） |
| 商品選択_在庫が NULL の商品は在庫を空にする | EV-ORD-306 手順 4, 共通仕様 4 節 | 商品 P（在庫 NULL） | 行 1 で P を選ぶ | `.line-stock` が空。`#messageArea` に在庫の警告が出ない（`text-warning` を持たない） |
| 商品選択_重複は元に戻してエラーを出す | EV-ORD-306 手順 2, OD-T1, ERR-BIZ | 行 1：P1 を選択済み、行 2 を追加 | 行 2 で P1 を選ぶ | 行 2 の商品の選択が元（空）に戻る。`#messageArea` が「この商品は既に明細に存在します。数量を変更してください。」で `text-danger`。`#subtotal` は行 1 だけの値 |
| 商品選択_空に戻すと行の値が消える | EV-ORD-306 手順 1 | 行 1：P（`12.50`・数量 2）を選択済み | 行 1 の商品を空白に戻す | `.line-unitprice`・`.line-stock`・`.line-price`・`.line-amount` が空。`#subtotal` = `0.00`・`#total` = 送料のみ |
| 商品選択_他の利用者が消した商品はERR-NOTFOUNDで一覧へ（DEC-ORD-40） | EV-ORD-306 手順 3, ERR-NOTFOUND | 商品 P（`DeleteProduct` で後から消す） | 登録を開く → テストが P を消す → 行 1 で P を選ぶ | 一覧が表示される。`#messageArea` が「対象のデータは存在しません。」（`text-danger`） |
| 在庫超過_警告が出て入力は妨げない（DEC-ORD-44） | EV-ORD-306 手順 6, EV-ORD-307 手順 3, 共通仕様 4 節（警告の区別）, DEC-ORD-44 | 商品 P（在庫 5） | 行 1 で P を選び、`.line-quantity` を `6` にする | `#messageArea` が「在庫数を超えています。」で、class に `text-warning` を持ち `text-danger`・`text-info` を持たない。`.line-quantity` = 6 のまま。**数量を `5` に直した後の `#messageArea` は期待にしない**（本文が警告を消す時機を決めていないので期待にしない。`DEC-CMN-06` と同じ考え方）。保存を妨げないことは B層 `在庫を超える数量でも保存できる` で確かめる |
| 明細の再計算_単価・割引率・数量の変更 | EV-ORD-307 手順 2, 共通仕様 3 節（明細金額は小数第 3 位を四捨五入） | 商品 P（単価 `0.50`） | 行 1 で P を選ぶ → `.line-unitprice` = `0.50`、`.line-discount` = `0.15`、`.line-quantity` = `3` | `.line-price` = `0.43`（`0.425` を四捨五入。DEC-ORD-46。DEC-ORD-26 は SC-ORD-02 だけ。切り捨て・偶数丸めなら `0.42`）、`.line-amount` = `1.28`（`0.425 × 3 = 1.275`）、`#subtotal` = `1.28`。数量を `1`・割引 0・単価 `100` にすると `.line-amount` = `100.00` |
| 明細の再計算_不正値は項目にエラーを出して再計算しない（TestCase：単価 `-1`／割引率 `1.5`／数量 `0`） | EV-ORD-307 手順 1, VAL-NUMERIC, DEC-CMN-05 | 行 1：P（`10.00`・0・数量 2。`.line-amount` = `20.00`） | 該当項目に各値を入れて確定 | 該当の `.line-*-error` が空でなく `text-danger`。`.line-amount`・`#subtotal` が `20.00` のまま。`#messageArea` が空 |
| 編集_読み込み | EV-ORD-301 手順 3, SC-ORD-03-I01〜I18 | 受注 1 件（K・E・S、受注日 `2198/03/05`、要求納期 `2198/04/01`、送料 `34.50`、Ship* の値）。明細は B層の「明細の項目と並びと金額」と同じ（M・X・A の順に入れる） | `/OrdTf/Edit?mode=edit&id=<受注>` を開く | `#orderId` = 受注 ID。`#CustomerID`・`#EmployeeID`・`#ShipVia` の選択が K・E・S。`#OrderDate` = `2198/03/05`、`#RequiredDate` = `2198/04/01`、`#Freight` = 34.5（`34.50`）、`#ShipName`〜`#ShipCountry` が受注の値。`#customerPhone` = K の電話。`#lineList tbody tr` が 3 行で A・M・X の順、各行の商品の選択・単価・割引（`0.15`・`0.25`・`0.05`）・数量・価格（`85.00`・`37.50`・`19.00`）・明細金額（`1,020.00`・`75.00`・`57.00`）。`#subtotal` = `1,152.00`、`#total` = `1,186.50`。`#btnMain1` が活性。`.line-remove` が活性（3 行） |
| 編集_廃番の既存明細は選択状態で残り、他の行の選択肢には無い | EV-ORD-301 手順 3-5, OD-T3 | 受注 1 件に、廃番でない P と廃番の Q の明細 | 編集を開く | 2 行。Q の行の `.line-product` の選択が Q（選択肢に Q を含む）。P の行の `.line-product` の選択肢に Q が無い |
| 編集_存在しない受注はERR-NOTFOUNDで一覧へ | EV-ORD-301 手順 3-1, ERR-NOTFOUND, 画面遷移 F-4 | `DeleteOrder` で消した ID | 編集を開く | 一覧が表示される（`#orderList` が見える、`#orderId` は無い）。`#messageArea` が「対象のデータは存在しません。」で `text-danger`・`text-info` を持たない |
| 編集_出荷済は編集させず詳細へ戻す | EV-ORD-301 手順 3-2, ORD-T4, ERR-BIZ | 出荷日ありの受注 1 件 | 編集を開く | 詳細が表示される（`#orderId` = 受注 ID、`#detailList` が見える）。`#messageArea` が「出荷済の受注は編集できません。」で `text-danger` |
| 詳細の編集_編集画面へ遷移する | EV-ORD-202 手順 2, SC-ORD-02-B01 | 未出荷の受注 1 件 | 詳細を開き `#btnEdit` | URL が `/OrdTf/Edit` でクエリに `mode=edit` と `id`＝受注 ID。`#orderId` = 受注 ID。`#CustomerID` が受注の顧客 |
| 詳細の編集_表示後に出荷確定されたら遷移せずエラー | EV-ORD-202 手順 1（判定は 2.2 のとおり `OrdTfDetail` の `CanEdit`）, ORD-T4, ERR-BIZ | 未出荷の受注 1 件 | 詳細を開く（`#btnEdit` が活性）→ テストが `ShipOrder` → `#btnEdit` | 詳細のまま（`#orderId` = 受注 ID。編集の `#CustomerID` が無い）。`#messageArea` が「出荷済の受注は編集できません。出荷を取り消してから編集してください。」で `text-danger` |
| 保存_登録に成功すると詳細へ遷移する | EV-ORD-308 手順 3・5・6, SC-ORD-02, DEC-ORD-35 | K・E・S・P1・P2 | 登録を開く → K・E・S を選ぶ、`#OrderDate` に `2198/03/05`、`#RequiredDate` に `2198/04/01`、`#Freight` に `10`、行 1：P1（単価 `10.00`・割引 `0.15`・数量 2）、`#btnAddLine`、行 2：P2（数量 1）→ `#btnMain1` | 詳細（`/OrdTf/Detail?id=<採番>`）が表示される。`#messageArea` が「受注 ID nnn を保存しました。」（nnn = `#orderId`）で `text-info`（`text-danger` を持たない）。`#customerName` = K、`#orderDate` = `2198/03/05`、`#shipName` が自動設定した K の会社名、`#detailList tbody tr` が 2 行、`#freight` = `10.00`。DB：K の受注が 1 件、明細 2 行 |
| 保存_登録の後に詳細を再読み込みしても二重登録しない | EV-ORD-308 手順 6, EV-ORD-201（再読み込みはページ読み込み）, 引き継ぎ（POST の再送） | 上の保存の後 | 詳細で `page.ReloadAsync` | K の受注が 1 件のまま。詳細の表示が変わらない |
| 保存_検証エラーは項目の直下と共通メッセージ領域に出て画面に留まる | EV-ORD-308 手順 1, ERR-VALIDATION, DEC-CMN-04・05 | 登録。K・E を選び、受注日 `2198/04/02`、要求納期 `2198/04/01`（関連チェックの違反）、`#Freight` に `-1`、行 1 の商品を空、`#ShipName` に `手入力` | `#btnMain1` | 編集画面に留まる（`#orderId` =「（登録時に採番）」）。`#FreightError`・`.line-product-error` が空でなく `text-danger`。`#OrderDateError`・`#RequiredDateError` は空。`#messageArea` が空でなく `text-danger`（関連チェックの 1 件。項目単位の文言は含めない）。入力値（`#ShipName` = `手入力`、`#OrderDate` など）が保持される。DB：K の受注が 0 件 |
| 保存_編集に成功すると詳細へ遷移し最新が出る | EV-ORD-308 手順 4・6, ORD-T4 | 受注 1 件（明細 A・M、送料 `5.00`、Ship* の値） | 編集を開く → `#Freight` を `9`、`#ShipCity` を `Osaka`、M の数量を `5`、A の行を削除（`.line-remove`）、`#btnAddLine` で X を足す（`10.00`・0・数量 1）→ `#btnMain1` | 詳細が表示され、`#messageArea` が「受注 ID nnn を保存しました。」（`text-info`）。`#freight` = `9.00`、`#shipCity` = `Osaka`、`#detailList tbody tr` が M・X の 2 行（M の数量 5）。`#shippedDate` が空（付随。ShippedDate を NULL で SET する実装は検出しない）。DB：`GetOrder` の RowVersion = 2 |
| 保存_他の利用者が更新していたらERR-CONFLICTで最新を再表示する | EV-ORD-308 手順 4-2, ERR-CONFLICT, 共通仕様 7 節（最新データを再表示）, DEC-ORD-34, 引き継ぎ（再表示と再送） | 受注 1 件（送料 `5.00`、明細 1 行） | 編集を開く → `#ShipCity` に `Osaka` と入れる → テストが `BumpOrder`（送料 `99.00`）→ `#btnMain1` →（再表示の後）`#ShipCity` に `Kyoto` と入れて `#btnMain1` | 1 回目の後：編集画面に留まり（`#orderId` = 受注 ID）、`#messageArea` が「他の利用者によって更新されています。最新の内容を読み込み直してください。」で `text-danger`、`#Freight` = 99（最新）、`#ShipCity` が受注の元の値（`Osaka` を捨てる）。DB の `ShipCity` は元のまま。2 回目の後：詳細が表示され `#messageArea` が「受注 ID nnn を保存しました。」（`text-info`）、`#shipCity` = `Kyoto`、`#freight` = `99.00` |
| 保存_編集で消されていたらERR-NOTFOUNDで一覧へ | EV-ORD-308 手順 1・4-2（検証が先。担当者が必須＝VAL-REQUIRED）, ERR-NOTFOUND, 画面遷移 F-4 | K の受注 25 件（**担当者 E**。i 番目 = `2198/01/01` + i 日。担当者が NULL の受注は、編集の保存で `VAL-REQUIRED` が先に返るので使わない）。一覧状態を保持するため `/OrdTf` で K を検索 → 次へ → 先頭行を選ぶ（詳細）→ `#btnEdit` | 編集画面で入力を変える → テストがその受注を `DeleteOrder` → `#btnMain1` | 一覧が表示される。`#messageArea` が「対象のデータは存在しません。」で `text-danger`。保持された一覧状態：`#CustomerID` = K、`#pageInfo` = 「2 / 2 ページ」、`#totalCount` = 「全 24 件」、一覧 4 行 |
| キャンセル_編集で変更が無ければ確認なしで詳細へ | EV-ORD-309 手順 1・2, F-3（未保存の変更がある場合のみ） | 受注 1 件 | 編集を開く → `#btnCancel` | 確認ダイアログが出ない。詳細（`#orderId` = 受注 ID、`#detailList`）が表示される。`#messageArea` が空 |
| キャンセル_編集で変更があれば確認し、取り消すと何もしない | EV-ORD-309 手順 1, F-3, DEC-ORD-37 | 受注 1 件 | 編集を開く → `#ShipCity` を変える → `#btnCancel` → 取り消す | ダイアログのメッセージが「入力内容は保存されません。よろしいですか？」。取り消した後も編集画面のまま（`#ShipCity` が変えた値）。DB の受注が変わらない |
| キャンセル_編集で変更があり承認すると詳細へ戻る | EV-ORD-309 手順 1・2・3 | 受注 1 件 | 上のあと、もう一度 `#btnCancel` → 承認 | 詳細が表示される（`#shipCity` が元の値）。DB の受注・RowVersion が変わらない |
| キャンセル_登録で変更があり承認すると保持した一覧状態の一覧へ戻る | EV-ORD-309 手順 2, F-2・F-3 | K の受注 45 件（i 番目 = `2198/01/01` + i 日） | `/OrdTf` で K を検索 → 次へ（「2 / 3 ページ」）→ `#btnNew` → `#Freight` に `5` を入れる → `#btnCancel` → 承認 | ダイアログのメッセージが「入力内容は保存されません。よろしいですか？」。一覧が表示され、`#CustomerID` = K、`#pageInfo` = 「2 / 3 ページ」、先頭行 = i = 25。DB に受注が増えていない（K の受注 45 件） |
| キャンセル_登録で変更が無ければ確認なしで一覧へ | EV-ORD-309 手順 1・2 | 受注 1 件 | `/OrdTf` を開き `#btnNew` → `#btnCancel` | 確認ダイアログが出ない。一覧が表示される |

- 追加：**E2E 39 メソッド**（不正値の 2 ケースは TestCase で 5 ケース増える。ケース数は段 2 の報告で実数を書く）。

## 5. 作らないケースと理由

| 対象 | 理由 |
| --- | --- |
| `ERR-DB`・`PRE-DB`・システム エラー | `DEC-CMN-07`：定めない（受注一覧と同じ） |
| 項目単位の検証の文言・関連チェックの文言・上書き確認の文言 | DEC-ORD-30・31・33 の答え：定めない（文言を期待にしない） |
| 数値の上限・桁（数量が smallint を超える、金額が money を超える、小数の桁） | DEC-ORD-38 の答え：定めない |
| 登録で商品マスタの単価が NULL のときの初期単価 | DEC-ORD-42 の答え：定めない |
| 編集の読み込み時に商品マスタに無い明細 | 読み込みは `Products` との内部結合（`EV-ORD-301` 手順 3-4）。表示しない行は保存の入力に出ない（`DEC-ORD-24` と同じ読み）。通常の操作で起こらない |
| 編集で、読み込み時の廃番の行の商品を別の廃番の商品に変える | UI の選択肢からは起こらず、「読み込み時から存在する行は対象外」の範囲が仕様で決まらない（行の商品が変わると削除＋挿入）。直接の呼び出しでしか起こらない |
| 編集で、廃番でない商品の行に、読み込み時に無かったのに `OriginalProductID` を付けて送る（`OriginalProductID` が付いているのに、その (OrderID, ProductID) の明細が DB に無い） | 細工した POST・B層を直接呼んだときだけ起こる（他の利用者が明細を変える操作 ── 受注の保存・受注の削除・出荷の確定 ── は、どれも親の `Orders.RowVersion` を進めるので `ERR-CONFLICT` か `ERR-NOTFOUND` になる）。DEC-ORD-49（決定 21。答え：4＝定めない）のとおり、行の扱いはケースにしない（検証を `Original*` を信じて外さない、という条件は 3.5・3.6 の廃番のケースと 3.6 の ORD-T5 のケースで確かめる）。実装は、廃番の検査は画面の主張を信じず、既存の行かは DB で確かめる |
| 保存時に受注が出荷済になっていて行バージョンが同じ | 出荷確定は `RowVersion + 1`（`EV-ORD-402`）なので `ERR-CONFLICT` で守られる。出荷確定の画面は範囲外 |
| 他の利用者が明細だけを更新・削除したときの `ERR-CONFLICT` | `OD-T6`：楽観排他は親の `Orders.RowVersion`。明細の変更を検出しない（`BumpDetail` は「触れない行を触れない」の確認だけに使う） |
| 画面の複数タブでの一覧状態の混線 | 一覧状態の保持の方法は計画（段 3）で決める。画面の振る舞いとしては、キャンセルで保持した一覧状態に戻ることだけを確かめる |
| 保存の CSRF 対策、保存・顧客選択・商品選択のトランザクションの分離レベル | 仕様に無い（`QUALITY.md` 4 節のレビューの観点）／境界から観測できない |
| 遷移パラメータの `mode`・`id` が無い・不正 | `PRE-PARAM` の違反の扱いは本文に無い。`DEC-ORD-21` と同じ（定めない） |
| ヘッダのメニューなどのキャンセル以外の離脱の確認 | 対象の仕様の範囲外 |
| `EV-ORD-305` 手順 1（1 行のとき削除せず「明細は 1 行以上必要です。」を表示） | 1 行のときは `.line-remove` が非活性で、画面の操作からは起こらない。E2E `明細行削除_1行のときは削除できない` で非活性だけを確かめる。この表示はテストで確かめられないので、期待を計画に書かない |
| E2E「明細 1 行・商品が空で保存すると共通メッセージ領域に `ORD-T5` が出る」 | DEC-ORD-43（答え：数える）により、商品が空の行は `VAL-REQUIRED` の項目単位の違反で、`ORD-T5` に当たらない。画面の操作では明細 0 行にできない（0 行は B層 `OrdTfSave_明細が0行` で確かめる） |

## 6. 仕様の決定（答え済み。決定のレポートの判断のとおり。記録は DEC-ORD-29〜47）

答えは「答え」の列（GO（n）は選択肢の番号。決めた者はすべて代行。依頼 004）。テスト仕様の期待は答えのとおり書いた（決定 8＝DEC-ORD-36 だけ推奨と違う）。表の番号は決定のレポートの「決定 N」で、本文の根拠は DEC-ORD-(28+N)。

| # | 仕様の ID | 問い | 選択肢（間引かない） | 推奨と理由 | 影響するケース | 答え |
| --- | --- | --- | --- | --- | --- | --- |
| 1（DEC-ORD-29） | `SC-ORD-03-I06`・`I07`、`VAL-DATE`、共通仕様 4 節、`DEC-ORD-09` | 受注日・要求納期の入力の形式。`DEC-ORD-09` は検索条件の日付だけを決めている | 日付の形式：1. 同じ規則（`yyyy/MM/dd` の形で存在する日付だけ。`2199/1/5`・`2199-01-05`・`2199/02/30` は違反） ／ 2. 日付として読めれば形は問わない（読めないものだけ違反） ／ 3. 定めない。**空白だけの入力**：a. 未指定とみなす（`DEC-ORD-09` と同じ。受注日は必須なので `VAL-REQUIRED`、要求納期は未指定） ／ b. 形式の違反にする ／ c. 定めない | 形式は 1、空白だけは a。共通仕様 4 節が日付の入力を `yyyy/MM/dd` と定めていて、検索条件と揃う | B 3.5 `受注日の形式`・`要求納期の形式`・`受注日の空白だけ`・`要求納期の空白だけ` | GO（形式 1・空白だけ a）。代行 |
| 2（DEC-ORD-30） | `VAL-REQUIRED`・`VAL-LENGTH`・`VAL-NUMERIC`・`VAL-DATE`（形式）・`VAL-EXISTS`、`DEC-CMN-01`・`05` | 項目単位の検証の違反の文言。本文はどの項目にも文言を決めていない（各 `VAL-*` は「エラー内容を表示」とだけ） | 1. 項目・規則ごとに文言を決める ／ 2. 定めない（項目の直下に空でない文言が出ること・`text-danger` だけを期待にし、文言は期待にしない） | 2。文言を決めるのは決定の数が多く、振る舞い（どこに出るか）は `DEC-CMN-05` で決まっている | B 3.5 の項目単位の全ケース、E2E の項目エラー | GO（2。定めない）。代行。要レビュー |
| 3（DEC-ORD-31） | `EV-ORD-308` の `VAL-DATE`（`ORD-T3`）、`DEC-CMN-04` | 受注日 > 要求納期（関連チェック）の文言。本文に文言が無い（`DEC-CMN-04` は表示先を共通メッセージ領域とだけ決めている） | 1. 文言を決める（例「要求納期は受注日以降の日付を指定してください。」） ／ 2. 定めない（共通メッセージ領域に空でないエラーが出ることだけ） | 2。期待にする文言が仕様にない | B `受注日が要求納期より後は関連チェック`、E2E `保存_検証エラー…` | GO（2。定めない）。代行。要レビュー |
| 4（DEC-ORD-32） | `EV-ORD-302` 手順 4 | 「利用者が既に出荷先情報を手入力している場合」の定義。自動で設定した値は手入力に当たるか | 1. 出荷先の 6 項目のうち 1 つでも空でなければ確認（自動設定の値も含む） ／ 2. 利用者が入力した（自動設定の値から変えた）項目があるときだけ確認。自動設定のままなら確認しない ／ 3. 定めない | 2。1 は顧客を 2 回選ぶだけで確認が出て、「手入力」の語に合わない | E2E `顧客選択_自動設定のままなら…`・`手入力があれば…` | GO（2）。代行 |
| 5（DEC-ORD-33） | `EV-ORD-302` 手順 4 | 上書きの確認の文言（本文に無い） | 1. 文言を決める ／ 2. 定めない（確認ダイアログが出ること、承認で上書き・取り消しで保持だけを期待にする） | 2 | E2E 顧客選択の確認の 2 ケース | GO（2。定めない）。代行。要レビュー |
| 6（DEC-ORD-34） | 共通仕様 7 節 `ERR-CONFLICT`、`EV-ORD-308` 手順 4-2 | 「更新を取り消して最新データを再表示する」とき、利用者が入力・編集した内容（行の削除・追加を含む）を捨てて DB の最新を出すか | 1. 捨てて DB の最新を表示（保持する `RowVersion` も最新にする。もう一度保存できる） ／ 2. 利用者の入力を残し、`RowVersion` だけ最新にする ／ 3. 定めない | 1。「最新データを再表示」の文字どおり。2 は他の利用者の更新を見ないまま上書きできる | E2E `保存_他の利用者が更新していたら…` | GO（1）。代行 |
| 7（DEC-ORD-35） | `EV-ORD-308` 手順 6、共通仕様 4 節（メッセージ表示） | 保存の完了メッセージの種類（`DEC-ORD-22` は削除だけを決めている） | 1. 情報 ／ 2. 定めない | 1。`DEC-ORD-22` と同じ理由 | E2E `保存_登録に成功すると…`・`保存_編集に成功すると…` | GO（1。情報）。代行 |
| 8（DEC-ORD-36） | `EV-ORD-308` 手順 3-1、`SC-ORD-03-I10`〜`I15` | 空のまま保存した出荷先（`Ship*`）・要求納期・配送業者を DB に何で入れるか | 1. NULL ／ 2. 空文字（出荷先） ／ 3. 定めない（読み戻して空であることだけ） | 3（出荷先は NULL か空文字。要求納期・配送業者は NULL）。共通仕様 4 節の空値の表示は NULL も空文字も空表示で、どちらでも画面は同じ | B `登録で要求納期・配送業者を省略できる` | GO（1。NULL）。代行。**推奨（3）と違う** |
| 9（DEC-ORD-37） | `EV-ORD-309` 手順 1、画面遷移 F-3 | 「未保存の変更がある」の定義 | 1. 読み込み直後（または直前に表示した）の入力値から 1 つでも変わっていれば変更あり（元に戻しても、値が同じなら変更なし） ／ 2. 入力の操作をしたら変更あり（値が同じでも） ／ 3. 定めない | 1。「保存されない内容」があるかを問う確認の趣旨に合う | E2E キャンセルの 4 ケース | GO（1）。代行 |
| 10（DEC-ORD-38） | `VAL-NUMERIC` | 数値の上限・小数の桁（数量の上限 `smallint`、金額 `money`、割引率の小数の桁など）。本文は下限・割引の範囲だけ | 1. 上限を決める ／ 2. 定めない（テストにしない） | 2。サンプルの操作で起こらない | なし | GO（2。定めない）。代行 |
| 11（DEC-ORD-39） | `EV-ORD-301` 手順 2-3、`SC-ORD-03-I16`（列の既定値） | 登録の初期の空行・追加した空行の既定値は割引率 0・数量 1 でよいか（`EV-ORD-304` が明記し、`301` は「空行」とだけ書く） | 1. 割引率 0・数量 1（列の既定値） ／ 2. すべて空 | 1。列の既定値と `304` に合う | E2E `登録_初期表示` | GO（1）。代行 |
| 12（DEC-ORD-40） | 共通仕様 7 節 `ERR-NOTFOUND`、`EV-ORD-302` 手順 2・`EV-ORD-306` 手順 3、画面遷移 F-2・F-4、`DEC-ORD-28` | 入力画面の中のイベント（顧客選択・商品選択）で `ERR-NOTFOUND` になったときの扱い（7 節は一覧へ戻ると書き、入力中の内容の扱いは書かない） | 1. 7 節のとおり一覧へ戻る（入力中の内容は失う。保持された一覧状態で戻る＝`DEC-ORD-28` と同じ） ／ 2. 画面に留まり、該当の項目にエラーを出す ／ 3. 定めない | 1。7 節の文のまま。`ERR-VALIDATION` の「画面に留まる」と区別されている | E2E `顧客選択_他の利用者が消した…`・`商品選択_他の利用者が消した…` | GO（1。保持した一覧状態で）。代行 |
| 13（DEC-ORD-41） | `EV-ORD-301` 手順 3-5、`SC-ORD-03-I16` 列 1 | 編集で廃番商品の行の選択肢の表示名に「（廃番）」を付けるか（`SC-ORD-02` の一覧は付記する。`SC-ORD-03` は書かない） | 1. 付けない（商品名のみ） ／ 2. 付ける ／ 3. 定めない（商品名を含むことだけ） | 3 | E2E `編集_廃番の既存明細は…`（選択の確かめを「選択された option の value」で行い、表示名は期待にしない） | GO（3。定めない）。代行 |
| 14（DEC-ORD-42） | `EV-ORD-306` 手順 4（`OD-T2`） | 商品マスタの `UnitPrice` が NULL のとき、明細の単価の初期値 | 1. 空（利用者に入力させる） ／ 2. 0 ／ 3. 定めない | 3。テストの対象にしない | なし | GO（3。定めない）。代行 |
| 15（DEC-ORD-43） | `EV-ORD-308` 個別（`ORD-T5`）、`EV-ORD-305` 手順 1・4、`VAL-REQUIRED` | 商品が空の明細行（行はあるが商品が未選択）を、`ORD-T5`（明細 1 行以上）の行数に数えるか。本文にも決定記録にも「商品が空の行は数えない」は無い | 1. 数える（行があれば `ORD-T5` に当たらない。商品の未選択は `VAL-REQUIRED` の項目単位の違反） ／ 2. 数えない（商品が空の行は無いものとして、全行が空なら「明細を 1 行以上入力してください。」） ／ 3. 定めない | 1。`EV-ORD-305` 手順 1・4 は行数で `ORD-T5` を扱い、`VAL-REQUIRED` は「明細の各行で商品…が入力されていること」と行を前提にしている | B 3.5 `明細の必須の欠け`・`明細が0行`、5 章の E2E（作らない） | GO（1。数える）。代行 |
| 16（DEC-ORD-44） | 共通仕様 4 節（メッセージ表示）、`SC-ORD-03-I16` 列 6、`EV-ORD-306` 手順 6・`EV-ORD-307` 手順 3 | 在庫超過の警告「在庫数を超えています。」をどこに出すか | 1. 共通メッセージ領域に警告（`text-warning`）として出す ／ 2. 当該行に出す ／ 3. 両方 ／ 4. 定めない | 1。4 節が「情報／警告／エラーを区別して共通メッセージ領域に表示」と定めている。複数行が超過したときの重なりは定めない | E2E `在庫超過_警告が出て入力は妨げない` | GO（1）。代行 |
| 17（DEC-ORD-45） | `SC-ORD-03-I04`、`DEC-ORD-18`（対象は `SC-ORD-02-I05` だけ） | 編集画面の顧客住所の区切りと、空の要素の詰め方は `SC-ORD-02-I05` と同じにするか | 1. 同じ（区切りは定めない。空の要素を詰める） ／ 2. 区切りを決める（半角空白など） ／ 3. 定めない（要素が順に並ぶことだけ。空の要素の扱いも期待にしない） | 1。同じ住所の連結で、別の規則を作る理由が無い。ただし本文の根拠は無いので決定にする | B `OrdTfEditInit_編集の受注情報`・`OrdTfCustomer_電話と住所`・`空の要素は空`、E2E `顧客選択_顧客情報と出荷先の既定値` | GO（1。区切りは定めない・空の要素は詰める。DEC-ORD-18 の決定は「定めない」）。代行 |
| 18（DEC-ORD-46） | `SC-ORD-03-I16` 列 4、`DEC-ORD-26`（対象は `SC-ORD-02-I17` 列 4） | 編集画面の明細の価格（`単価 × (1 - 割引率)`）の小数 2 桁表示の丸め | 1. 四捨五入（`0.425` → `0.43`） ／ 2. 切り捨て ／ 3. 偶数丸め ／ 4. 定めない | 1。`SC-ORD-02` と揃う。画面表示は 2 桁（共通仕様 3 節） | E2E `明細の再計算_単価・割引率・数量の変更`（`.line-price`） | GO（1。四捨五入）。代行。要レビュー（DEC-ORD-26 と一緒に） |
| 19（DEC-ORD-47） | `VAL-DATE`、`DEC-ORD-09`・`10`（対象は検索条件）、TableSchema Orders（`datetime`） | 保存する受注日・要求納期が `1753/01/01` より前のときの扱い（`datetime` に入らない） | 1. 定めない（実装に任せ、テストの期待にしない） ／ 2. `VAL-DATE` の違反にする（項目の直下） ／ 3. 専用の文言を決める | 1。`DEC-ORD-10` と同じ扱い。決まるまで、範囲の端の通るケース（`1753/01/01`・`9999/12/31`）だけを作る | B 3.5 `日付の範囲の端は通る` | GO（1。定めない）。代行 |
| 20（DEC-ORD-48） | `ORD-T1`、`EV-ORD-308` 検証内容（`VAL-REQUIRED`）、`EV-ORD-301` 手順 3 | 編集モードで保存するとき、読み込み時に NULL だった顧客・担当者・受注日も必須にするか（`ORD-T1` は新規登録で必須とし編集を書かず、`EV-ORD-308` の検証内容はモードを区別せず `VAL-REQUIRED` とする） | 1. 編集でも必須 ／ 2. 編集では、読み込み時に NULL だった項目は空のままでよい（それ以外は必須） ／ 3. 定めない | 1。`EV-ORD-308` の文がモードを区別せずに必須としている。`ORD-T1` の「表示側は NULL を許容」は表示の決まりで、保存の検証を緩める根拠にならない | B 3.6 `編集でも必須の欠けは違反` | GO（1。編集でも必須）。代行（依頼 006）。Northwind2 の 830 件に顧客・担当者・受注日が NULL の行は 0 件 |
| 21（DEC-ORD-49） | `EV-ORD-308` 手順 4-3、`OD-T6` | 編集の保存で、`OriginalProductID` が付いているのに、その (OrderID, ProductID) の明細が DB に無い行（廃番でない商品）を受け取ったとき、どうするか（画面の操作では起こらない。細工した POST・B層を直接呼んだときだけ） | 1. 読み込み時に無かった行として挿入する ／ 2. 何もしない（行を捨てる） ／ 3. `ERR-CONFLICT` にする ／ 4. 定めない（実装に任せ、テストの期待にしない） | 4。画面の操作では起こらない（他の利用者が明細を変える操作は、どれも `Orders.RowVersion` を進めるので `ERR-CONFLICT`・`ERR-NOTFOUND` になる）。`DEC-ORD-10`・`11` と同じ扱い | なし（5 章に作らないケースとして書いた）。ただし下の条件から、3.6 `OrdTfSave_編集で削除の印と同じ商品の行を残しても明細は1行以上残る` を足した | GO（4。定めない）。代行。**条件**：定めないのは「行をどう扱うか」だけ。どの扱いでも、保存の検証（OD-T1 の重複・OD-T3 の廃番・VAL-EXISTS・VAL-REQUIRED・VAL-NUMERIC）を、画面が送る読み込み時の値（`OriginalProductID` など `Original*`）を信じて外さない（期待は EV-ORD-308 の検証内容・OD-T3 で決まる）。外れる経路があれば、テスト仕様に足す。実装が選んだ扱いは計画に理由と一緒に書く。記録は NorthwindDbSampleSpec@a724ca5 の DEC-ORD-49 |

> 決定 11（DEC-ORD-39）は本文から読めるが、`301` の「空行」と `304` の既定値の関係を本文が明記していないので、念のため上げた。

## 7. 段 2 の確かめ方（予定）

- **Red（B層）**：引数・戻り値クラスに 1.1 のメンバを足し、`OrdTfBusiness` に `OrdTfEditInit`・`OrdTfCustomer`・`OrdTfProduct`・`OrdTfSave` の**戻り値を設定するだけの空の `UOC_`** を置く（`OrdTfSaveLine`・`OrdTfEditLine` の型も）。テストは `Assert` で落ちる。net10.0 の `MVC_Sample_Core\MVC_Sample.Tests` で流す。ビルドや呼び出しで落ちるのは Red ではない。net48 は段 4 の後で全件。
- **Red（P層）**：ビルドが通り、`/OrdTf/Edit?mode=new`・`mode=edit&id=` を開ける最小の土台（要素なし。既存の 404 と本文 1 行の土台を、`Edit.cshtml` の空の土台に置き換える）を先に置く。E2E は `Expect` で落ちる。
- **最初から通りうるケース**：予定では、**B層の `OrdTfSave_…検証` の一部**（空の実装の戻り値 `ErrorFlag = false` で `ErrorFlag = false` を期待するケース：`送料0は通る`・`受注日と要求納期が同じ日は通る`・`桁数ちょうど`・境界値の「通る」側）は最初から通りうる。空の実装は ErrorFlag = false を返すので、**「通る」側は `GetOrder`・`GetDetails` の値（受注が入っていること）も確かめる**ようにして、Red にする（表の期待に既に書いた）。E2E で最初から通るものは無い。
- テストのファイル：`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`・`TestData.cs`（足す）、`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfEditScreenTests.cs`（新規）・`TestData.cs`（足す）。

## 8. 段 2 の追加の確かめ（テスト仕様の差分を足したとき。TESTING.md 4）

段 4 の実装が作業ツリーにある状態で、足した・直したテストを流した（net10.0。net48 も同じ結果）。B層 220 ケース（既存 213 ＋ 新しい 7）：失敗 4・合格 216。

| ケース | 今の実装の結果 | 落ち方の例（実際の値） |
| --- | --- | --- |
| `OrdTfSave_登録でOriginalProductIDを付けても新規の廃番商品は指定できない` | 失敗（Red） | `Assert.That(rv.ErrorFlag, Is.True)`：`Expected: True / But was: False`（廃番の検査が Original* を信じて外れ、受注が入る） |
| `OrdTfSave_編集で読み込み時に無かった行にOriginalProductIDを付けても廃番商品は足せない` | 失敗（Red） | 同上（`Expected: True / But was: False`） |
| `OrdTfSave_編集で削除の印と同じ商品の行を残しても明細は1行以上残る(False／True)` | 失敗（Red）×2 | `Assert.That(TestData.GetDetails(o).Count, Is.GreaterThanOrEqualTo(1))`：`Expected: greater than or equal to 1 / But was: 0`（どちらの TestCase も 0 行になる） |
| `OrdTfSave_編集でも必須の欠けは違反("CustomerID"／"EmployeeID"／"OrderDate")` | **最初から通る**（DEC-ORD-48 の答えは実装が既にそうしている） | 下の「壊して確かめた」 |
| E2E `保存_編集で消されていたらERR_NOTFOUNDで一覧へ`（前提を `employeeId: c.E` に直した） | **最初から通る**（実装が既に検証 → 存在確認の順で、担当者があれば NOTFOUND へ進む） | 下の「壊して確かめた」 |

**壊して確かめた**（実装の該当箇所を 1 か所ずつ一時的に壊し、流し、戻した。戻したファイルは更新時刻を今にして、戻した後に全件を流した）

| 壊した箇所 | 落ちたケース | 落ち方の例 |
| --- | --- | --- |
| `OrdTfBusiness.ValidateSave` の顧客の必須の検査を、編集のとき違反にせず `v.CustomerID = ""` にする | `OrdTfSave_編集でも必須の欠けは違反("CustomerID")`（ほかの 2 つは通る） | `Assert.That(rv.ErrorFlag, Is.True)`：`Expected: True / But was: False` |
| 同、担当者の必須の検査を、編集のとき違反にせず `v.EmployeeID = 0` にする | `…("EmployeeID")` | 同上 |
| 同、受注日の必須の検査を、編集のとき違反にせず `orderDate = DateTime.Today` にする | `…("OrderDate")` | 同上 |
| `OrdTfController.Save` の `rv.ErrorMessageID == OrdTfBusiness.MsgIdNotFound` を、別の値との比較に変える（ERR-NOTFOUND が検証エラーの再描画に落ちる） | E2E `保存_編集で消されていたらERR_NOTFOUNDで一覧へ` | `PlaywrightException : Locator expected to be visible`（`#orderList`。編集画面に留まる） |

戻した後の全件（作業ツリーの実装）：B層 net10.0 220 ケース中、失敗 4（上の Red の 4 ケース）・合格 216。net48 も同じ。E2E net10.0 は 98 ケース全部合格（2 分 14 秒）。後始末：Orders 830・Order Details 2155・Customers 91・Products 77、Tf で始まるテストのデータ 0 件。

### 8.1 検証が先（`OrdTfSave_編集で消された受注でも検証の違反が先に返る`）の確かめ

- 段 2 の時点（段 4 の実装が作業ツリーにある状態。版：Red のコミット 8266d54 の後、決定 21 の追随の前）で流した：**最初から通る**（実装が検証 → 存在確認の順のため）。
- 壊して確かめた：`OrdTfBusiness.UOC_OrdTfSave` の `ValidateSave` の前に、編集のとき `OrdTfOrderCount.sql` で受注の有無を確かめ、無ければ ERR-NOTFOUND を投げる処理を一時的に足した（存在確認 → 検証 → 更新の順）。結果：このケースだけ失敗。落ち方の例：`Assert.That(rv.ErrorFields, Is.EquivalentTo(new[] { "Freight" }))` が `Expected: equivalent to < "Freight" > / But was: <empty>`（ErrorFlag は true だが、ErrorFields が空で ERR-NOTFOUND になる）。**ほかのケースは、この壊し方で新しく落ちなかった**（落ちたのは、実装を直す前から Red の 4 ケースと、このケースだけ。`OrdTfSave_編集で存在しない受注はERR_NOTFOUND`・`OrdTfSave_編集の検証エラーでは何も更新しない` は通った。順の入れ替えを落とすのは、このケースだけ）。戻した後：B層 221 ケース、net10.0・net48 とも失敗 4（Red）・合格 217。