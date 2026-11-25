# テスト仕様：出荷ステータス更新ダイアログ OrdTfShip（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

起こした仕様の版：NorthwindDbSampleSpec@2962688（ブランチ proxy/ord-management。**起こしたのは a724ca5**。a724ca5 → 2962688 の差分は決定記録だけ〔`LLD/Decisions/Orders.md` に DEC-ORD-50〜63 を足し、索引の件数を 63 にした。本文は変わっていない〕で、この決定記録を足した版が 2962688。本文 `HLD/Common.md`・`HLD/UI_FlowList.md`・`LLD/EventSpec/Orders.md`・`LLD/UI_ElementsAndEventList/Orders.md`・`LLD/TableSchema/Orders.md`・`LLD/TableSchema/Products.md` と、決定記録 `LLD/Decisions/Common.md`〔DEC-CMN-01〜07〕・`LLD/Decisions/Orders.md`〔DEC-ORD-01〜63〕を合わせて読んだ。`SC-ORD-04`・`EV-ORD-401`〜`404` を直接決めた決定記録は無い）
規約：`docs/spec/cmn-framework.md` の「画面共通」「MVC」、`docs/spec/cmn-testfirst.md`、`docs/spec/test3-testfirst-OrdTfShip.md`、`docs/plan/cmn-testfirst.md` の「B層を共用する構成」
前の回：[受注一覧](./test3-testfirst-OrdTf-mvc-core-tests.md)・[受注詳細（照会・削除）](./test3-testfirst-OrdTfDetail-mvc-core-tests.md)・[受注詳細（登録・編集）](./test3-testfirst-OrdTfEdit-mvc-core-tests.md)（承認・実装済み。このケースはその上に足す）。決定のレポート：[test3-testfirst-OrdTfShip-mvc-core-decisions.md](./test3-testfirst-OrdTfShip-mvc-core-decisions.md)

対象：`SC-ORD-04`（`I01`〜`I08`、`B01`〜`B03`）、`EV-ORD-401`〜`404`、`EV-ORD-108`（一覧の `SC-ORD-01-B04` から）、`EV-ORD-205`（詳細の `SC-ORD-02-B03` から）。
範囲外：`SC-ORD-05`（帳票）とそこへの遷移（`EV-ORD-204`。詳細の `B04`・`B05` は置かない）。配送管理の出荷追跡画面（`SC-SHP-04`）からの表示。

> **仕様の決定は 6 章（決定 1〜15。すべて答え済み。決めた者：代行）。** 本文にも決定記録にも決まりが無く、期待が変わるものを上げた。答えは決定のレポートの「判断」のとおり。
> テスト仕様の期待は答えのとおり書いた（**決定 1 だけ推奨と違う**）。根拠の列の「決定 N」は、決定 2〜15 を決定記録の DEC-ORD-50〜63 に置き換えた（決定 N → DEC-ORD-(48+N)）。決定 1 は規約の解釈で決定記録に入らないので「決定 1」のまま。

## 0. 進め方の前提

- B層は `Shared\`（`OrdTfBusiness`・`OrdTfParameterValue`・`OrdTfReturnValue`）を共用する。**この回の B層（`OrdTfShipInit`・`OrdTfShipSave`・`OrdTfShipCancel`）とそのテスト（`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs` に足す）はこのケースで作り**、net10.0（`MVC_Sample_Core\MVC_Sample.Tests`）→ net48（`MVC_Sample\MVC_Sample.Tests`）の順に、同時にでなく全件流す。
- テストのプロジェクト、アプリの起動、ログイン、ブラウザ、日付の範囲（B層 2199 年・E2E 2198 年）、並行実行の決まりは受注一覧・受注詳細・受注詳細（登録・編集）のとおり（新しいプロジェクトは作らない）。E2E は新しいクラス `OrdTfShipScreenTests`。
- 表示のメッセージは仕様の文言そのまま（`DEC-CMN-01`）。**本文に文言があるもの**（`ERR-NOTFOUND`・`ERR-CONFLICT`・在庫不足・未出荷の取消・完了メッセージ・取消の確認・`ORD-T3` の違反）だけを期待にする。**文言が無いもの**（項目単位の検証の文言）は「空でない」「エラーの class」だけを確かめる（DEC-ORD-56）。
- **データを書き換えるテスト**（出荷の確定・取消。`Orders` と `Products.UnitsInStock` を書き換える）は、**テストが作った受注・明細・顧客・配送業者・商品だけ**を書き換える・消す。Northwind2 の受注 830 件・商品 77 件は書き換えない。在庫の不足・ちょうど 0 になる・明細が複数の商品にまたがる、も、テストが作った商品で作る。後片付けは落ちても行う（`CleanUp`）。
- **終わりの確かめは、計画の手順に入れる**（範囲外。この回の始めの値と比べる）：受注 830 件・明細 2155 件・顧客 91 件・商品 77 件、**既存の商品（ProductID 1〜77）の `UnitsInStock` の合計が 3119**、既存の商品の `Products.RowVersion` の合計が始めと同じ。
- 受注一覧・受注詳細・受注詳細（登録・編集）の既存のテストは変えない。**ただし 1 件だけ変わる**（2.3）：受注一覧の `初期表示_一覧とページ情報` の列見出し（`SC-ORD-01` に 10 列目「操作」が足される）。期待値の変更に当たり、コミットのトレーラー `Expectation-Change:` が要る（`TESTING.md` 3・4。範囲は 2.3）。

## 1. テストが固定する契約（実装の決定。振る舞いは変えない。段 4 の実装はこれに従う）

### 1.1 B層

分離レベルは `ReadCommitted`（確定・取消も。テストの呼び出しの値）。「当日」は `DateTime.Today`。`MethodName`：`OrdTfShipInit`・`OrdTfShipSave`・`OrdTfShipCancel`。

**引数クラス `OrdTfParameterValue` に足すメンバ**（既存は変えない。`OrderID`・`RowVersion`・`ShipViaText`・`FreightText` は既存の意味のまま再利用する）

| メンバ | 型 | 意味 |
| --- | --- | --- |
| `OrderID`（既存） | `int` | 対象の受注 ID。3 つとも |
| `RowVersion`（既存） | `int` | ダイアログが保持している `Orders.RowVersion`。`OrdTfShipSave`・`OrdTfShipCancel` |
| `ShippedDateText` | `string` | `OrdTfShipSave` の出荷日（画面の入力のまま。`yyyy/MM/dd`） |
| `ShipViaText` | `string` | 同、配送業者（選択肢の value＝`ShipperID`。空・null は未選択。数値でない文字列は存在しないものとして `VAL-EXISTS`） |
| `FreightText` | `string` | 同、送料（画面の入力のまま。空は 0。`DEC-ORD-01`） |
| `DisplayedShipped` | `bool` | `OrdTfShipSave` の「表示時点の出荷済（true）／未出荷（false）の別」（`EV-ORD-402` の入力）。DEC-ORD-60 の答えが、DB の状態と食い違うときの扱いを決める |

**戻り値クラス `OrdTfReturnValue` に足すメンバ**（既存は変えない。`OrderID`・`CustomerName`・`Status`・`RequiredDate`・`Freight`〔`decimal?`。NULL は null〕・`RowVersion`・`ShipperOptions`〔先頭に空白の選択肢〕・`ErrorFields`・`ErrorFieldMessages`・`ErrorMessages` は既存の意味のまま再利用する）

| メンバ | 型 | 意味 |
| --- | --- | --- |
| `ShipIsShipped` | `bool` | `OrdTfShipInit` の「出荷済か」（`ShippedDate` が NULL でない。ボタンの表示名・活性・内訳の表示を決める） |
| `ShipDate` | `DateTime?` | `OrdTfShipInit` の `SC-ORD-04-I05` の値（未出荷は当日、出荷済は `ShippedDate`） |
| `ShipViaID` | `int?` | 同、`SC-ORD-04-I06` の選択（`Orders.ShipVia`。NULL は null＝未選択） |
| `StockLines` | `List<OrdTfShipStockLine>` | 同、`SC-ORD-04-I08` の在庫引落の内訳（未出荷だけ。出荷済は空）。**並びは期待にしない**（仕様が決めていない） |

`OrdTfShipStockLine`（新しい型）：`ProductName`（string）・`Quantity`（int）・`UnitsInStock`（int?。NULL は null）・`Short`（bool。`Quantity > UnitsInStock`＝強調する行）。

- **検証の返し方**：`OrdTfShipSave` は検証（`EV-ORD-402` 検証内容）をすべて行い、違反を集める。違反が 1 件でもあれば `ErrorFlag = true`、DB は更新しない。項目単位の違反は `ErrorFields`（キー：`ShippedDate`・`ShipVia`・`Freight`）と `ErrorFieldMessages`（同じ順）、関連チェックは `ErrorMessages`。`ErrorMessage`（基底）は項目単位の文言を期待にしない（DEC-ORD-56）。**検証は、`Orders` の行を探す（`ERR-NOTFOUND`・`ERR-CONFLICT`）より先**（手順 1 → 3）。
- **業務エラー**：`ERR-NOTFOUND` は `ErrorFlag = true`・`ErrorMessage` =「対象のデータは存在しません。」、`ERR-CONFLICT` は同「他の利用者によって更新されています。最新の内容を読み込み直してください。」、在庫不足は同「在庫が不足しています：」＋商品名（`EV-ORD-402` 手順 4-2）、未出荷の取消は同「この受注は出荷されていません。」（`EV-ORD-403` 手順 4）。
- **成功**：`ErrorFlag = false`・`OrderID` = 引数の受注 ID。完了メッセージ（「受注 ID nnn の出荷情報を更新しました。」「受注 ID nnn の出荷を取り消しました。」）は P層が組み立てる（B層は文言を持たない）。
- **確定・取消は B層の 1 トランザクション**（`EV-ORD-402` 手順 2〜7、`EV-ORD-403` 手順 2〜7）。`Orders` を `OrderID` と `RowVersion` の条件で更新（`RowVersion = RowVersion + 1`）。**計画への条件（テストは固定しない。境界から見えないので、計画の D層の設計と段 4 のレビューで見る）：在庫は `UnitsInStock = UnitsInStock - Quantity`（取消は `+`）の加減算で更新し、読んだ値で書き戻さない。** **パラメタ化する。文字列で連結しない。** どの業務エラーでも、**何も更新しない**（ロールバック）。

### 1.2 P層（MVC）の動き

| 対象 | 指定 |
| --- | --- |
| 一覧の出荷更新（`SC-ORD-01-B04`） | 各行の「操作」列（10 列目。`td` の 10 番目）の `<button type="button" class="btn-ship">`。表示名は「出荷更新」（DEC-ORD-50 の答えが「出荷変更」の行を決める）。常時活性。押すと、一覧の上にダイアログを重ねて表示する（`EV-ORD-108`） |
| 詳細の出荷更新（`SC-ORD-02-B03`） | `<button type="button" id="btnShip">`（キャプション「出荷更新」。画面上部。`#contents` の中で `#footer-buttons` の外。常時活性）。押すと、詳細の上にダイアログを重ねて表示する（`EV-ORD-205`） |
| ダイアログ（`SC-ORD-04`） | `#shipDialog`。**元画面の上に重ねて表示する**（F-5）：開いている間も元画面の URL は変わらず、元画面の要素（一覧は `#orderList`、詳細は `#orderId`）はそのまま残る。重ねる作り（部分ビューを取って差し込む・`<dialog>`・iframe など）は計画で決める（実装の決定。`Views\OrdTf\Ship.cshtml`）。**ダイアログの取得・確定・取消・閉じるのアクション名・URL は固定しない**（画面の操作だけで確かめる） |
| 確認ダイアログ | `window.confirm`。取消だけ（「出荷を取り消し、在庫を戻します。よろしいですか？」）。**出荷の確定・情報更新には確認を出さない**（`EV-ORD-402` に確認の手順が無い） |
| 確定・取消の成功 | ダイアログを閉じ、元画面を再読み込みする（一覧はDEC-ORD-51 の答え、詳細は `EV-ORD-201`）。元画面の `#messageArea` に完了メッセージ（DEC-ORD-55 の答え＝情報 `text-info`）。**成功の後の再読み込みで二重に確定しない**（POST の結果を返さず、再読み込みは GET） |
| 閉じる（`EV-ORD-404`） | ダイアログを閉じ、元画面を、変更がなくても再読み込みする。DB は更新しない。`#messageArea` は空 |
| 検証の違反（`ERR-VALIDATION`） | ダイアログに留まり、入力値を保持し、項目の直下に違反を出す（`DEC-CMN-05`。DEC-ORD-57 の答えが「出荷日の受注日以前」の表示先を決める）。**項目単位は `#shipMessage` に出さない** |
| 業務エラー（`ERR-BIZ`）・競合（`ERR-CONFLICT`）・消された（`ERR-NOTFOUND`） | DEC-ORD-54・53・52 の答えのとおり |
| 送信 | 確定・取消は POST（`[ValidateAntiForgeryToken]`）。**「表示時点の出荷済／未出荷の別」と `RowVersion` をダイアログが保持する方法（hidden か Session か）は計画で決める**（実装の決定。どちらでも、B層は DB の `Orders` の行で業務の検証をする。DEC-ORD-60） |

### 1.3 テストのデータ（`TestData`。B層用・E2E 用の両方に足す。既存の呼び出しは変えない）

| 足すもの | 内容 |
| --- | --- |
| `GetUnitsInStock(productId)` | `Products.UnitsInStock` を DB から読む（`int?`） |
| `SetUnitsInStock(productId, value)` | 他の利用者が在庫を変えたことを模す：`UnitsInStock` だけを更新する（`RowVersion` は変えない） |
| `GetProductRowVersion(productId)` | `Products.RowVersion` を読む |
| 既存の部品 | `NewOrder`（出荷日・配送業者・送料を指定できる）・`NewDetail`・`NewProduct(unitsInStock:)`・`GetOrder`・`GetDetails`・`GetRowVersion`・`BumpOrder`・`DeleteOrder`・`ShipOrder`・`DeleteProduct`・`CleanUp`（商品は明細・受注より後に消す）を使う。`NewProduct` の在庫は `short` の範囲 |

## 2. 画面の id・class（E2E が決める。段 4 の実装が付ける）

### 2.1 ダイアログ（`SC-ORD-04`）

| 対象 | 指定 |
| --- | --- |
| ダイアログ | `#shipDialog`（開いているとき可視。閉じたら不可視か DOM に無い）。元画面の id と重ならない（ダイアログの要素はすべて `ship` で始める。**ただし `#shipName`・`#shipAddress`・`#shipCity`・`#shippedDate`・`#shipperName` などは詳細画面が既に使っているので、ダイアログの id には使わない**。このテスト仕様のダイアログの id はすべて `#ship` ＋ 大文字始まりの語、または `#btnShip…`）。**ダイアログの入力欄（`#shipShippedDate`・`#shipShipVia`・`#shipFreight`）は、開くまでは詳細・一覧の `#contents` の DOM に置かない**（開いたときに差し込むか、`#contents` の外に置く）。既存の `OrdTfDetailScreenTests` の照会モードの確かめ（`#contents` の入力欄〔hidden を除く `input`・`select`・`textarea`〕が 0）は変えない。この作り方の幅の狭めは、計画への条件 |
| 表示項目（I01〜I04） | `#shipOrderId`・`#shipCustomerName`・`#shipStatus`・`#shipRequiredDate`（テキスト。日付は `yyyy/MM/dd`、NULL は空） |
| 入力（I05〜I07） | `<input id="shipShippedDate">`（日付は `yyyy/MM/dd` のテキスト）・`<select id="shipShipVia">`（先頭に `<option value="">` の空白。会社名の昇順。option の value は `ShipperID`）・`<input id="shipFreight">`。必須の表示名の末尾は「＊」 |
| 項目の直下のエラー | `#shipShippedDateError`・`#shipShipViaError`・`#shipFreightError`（違反が無いときは空。class に `text-danger`） |
| 在庫引落の内訳（I08） | `<table id="shipStockList">`。`tbody tr` が 1 行。行の中：`.ship-product`・`.ship-qty`・`.ship-stock`（テキスト。在庫が NULL は空）。**在庫が足りない行（`Quantity > UnitsInStock`）の `tr` は class に `table-danger`**（一覧の納期遅延と同じ強調）。出荷済のときは表示しない（`ToBeHiddenAsync`） |
| 共通メッセージ領域 | `#shipMessage`（エラーは `text-danger`、情報は `text-info`）。ダイアログに留まったときのエラーだけがここに出る（完了メッセージは元画面の `#messageArea`） |
| ボタン（決定 1 の答え＝3。ダイアログの下部に、ダイアログ専用の 5 つのメインボタンと、別に閉じる） | `#shipMain1`（B01。キャプション：未出荷は「出荷確定」、出荷済は「出荷情報を更新」。常に活性。読みは 6.1）・`#shipMain2`（B02。キャプション「出荷取消」。出荷済のとき活性、未出荷は非活性）・`#shipMain3`〜`#shipMain5`（常に非活性。キャプションなし）。**5 つとも常に見える**。B03 は別に `#btnShipClose`（キャプション「閉じる」。常時活性。メインボタンに含めない）。6 つとも `#shipDialog` の下部。元画面のフッタ（`#footer-buttons`）の `#btnMain1`〜`#btnMain5` は、ダイアログが開いている間も変えない |

### 2.2 一覧・詳細への追加

- 一覧：`#orderList thead th` の 10 番目に「操作」（並べ替えない。`button`・`a`・`input` を持たない）。各行の 10 番目の `td` に `button.btn-ship`。
- 詳細：`#contents` の中に `#btnShip`（`#footer-buttons` の外）。

### 2.3 既存のテストの変更（1 件）

受注一覧の E2E `初期表示_一覧とページ情報`（`OrdTfScreenTests`）は `#orderList thead th` が 9 個の見出しだけを期待している。`SC-ORD-01` の 10 列目「操作」（`SC-ORD-01-I06` 列 10、`B04`）が足されるので、**期待の配列の末尾に `"操作"` を足す**。これは今あるケースの期待を変える変更で、期待値の変更に当たる。変わったのは仕様ではなく**作る範囲**：前の回のテスト仕様（`test3-testfirst-OrdTf-mvc-core-tests.md`）は「範囲外の『操作』列は置かない」としていて、この回の範囲の文書 `docs/spec/test3-testfirst-OrdTfShip.md`「作る範囲」が `SC-ORD-01` に `B04` を足す。コミットのトレーラー：`Expectation-Change: SC-ORD-01 の 10 列目「操作」（SC-ORD-01-I06・B04。範囲の変更：test3-testfirst-OrdTfShip.md「作る範囲」。前の回の範囲外の記述を置き換える。NorthwindDbSampleSpec@2962688）`。**変えない既存のテスト**：`OrdTfDetailScreenTests` の照会モードの確かめ（`#contents` の入力欄が 0。1.2・2.1 のとおり、ダイアログの入力欄を開く前から置かない条件を計画に付ける）。同じクラスの `th` の構造の確かめ（`ths.Nth(7)`・`Nth(8)` が button・a・input を持たない）は変えない。

## 3. B層のルート メソッドのテスト（`OrdTfBusinessTests`。net10.0 → net48）

`Call("OrdTfShipInit", pv => …)` の形で、既存の部品を使う。**既存のケースは変えない。** 受注日などの日付は 2199 年。テストが入れた顧客 K・配送業者 S0・S1・S2・商品 P1・P2・P3・R を使う。名前の目印の決まりは受注一覧のとおり。

**共通の前提**
- `Sc`（未出荷）：顧客 K、配送業者 S0・S1、商品 P1（在庫 10）・P2（在庫 7）。受注 o：K・受注日 `2199/03/01`・要求納期 `2199/12/31`・出荷日 NULL・配送業者 S0・送料 `5.00`・Ship* に値。明細：P1 数量 3、P2 数量 2。`RowVersion` = 1。
- `Sh`（出荷済）：`Sc` の受注 o を出荷日 `2199/03/05` にしたもの。在庫は引落済のつもりで P1 = 7・P2 = 5 にする（明細は P1 数量 3、P2 数量 2 のまま）。`RowVersion` = 1。
- `V`（確定の有効な入力）：OrderID = o、RowVersion = 1、DisplayedShipped = false、ShippedDateText = `2199/03/10`、ShipViaText = S1、FreightText = `12.50`。
- `AssertUntouched`：受注 o の `ShippedDate`・`ShipVia`・`Freight`・`RowVersion` が呼ぶ前と同じ、かつ P1・P2 の在庫と `Products.RowVersion` が呼ぶ前と同じ（`GetOrder`・`GetUnitsInStock`・`GetProductRowVersion`）。

### 3.1 `OrdTfShipInit`（`EV-ORD-401`）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfShipInit_未出荷の受注情報 | EV-ORD-401 手順 1〜3・5, SC-ORD-04-I01〜I04 | `Sc` | OrderID = o | ErrorFlag = false。OrderID = o、CustomerName = K の会社名、Status =「未出荷」、RequiredDate = `2199/12/31`、ShipIsShipped = false |
| OrdTfShipInit_状況の判定（TestCase：要求納期／出荷日／期待。`2001/01/01`・NULL／`納期遅延`、`2199/12/31`・NULL／`未出荷`、NULL・NULL／`未出荷`、`2199/06/01`・`2199/06/02`／`納期遅延`、`2199/06/02`・`2199/06/01`／`出荷済`、`2199/06/01`・`2199/06/01`／`出荷済`、NULL・`2001/01/01`／`出荷済`、**当日・NULL／`未出荷`（`RequiredDate < 当日` の境界。当日は含まない）、当日 − 1 日・NULL／`納期遅延`**。当日は `DateTime.Today` からテストの中で作る） | EV-ORD-401 手順 3, 状況の判定, ORD-T7, SC-ORD-04-I03 | 受注 1 件（要求納期・出荷日を各値） | OrderID = その受注 | Status = 期待。ShipIsShipped = 出荷日が NULL でないか |
| OrdTfShipInit_顧客が無い受注も表示できる | EV-ORD-401 手順 2（`Customers` を外部結合）, ORD-T1（表示側は NULL を許容）, 共通仕様 4 節（空値） | CustomerID が NULL の受注（未出荷。要求納期 `2199/12/31`。明細 1 行） | OrderID = その受注 | ErrorFlag = false（`ERR-NOTFOUND` にならない）。OrderID = その受注、Status =「未出荷」。CustomerName が空（null か空文字） |
| OrdTfShipInit_未出荷の出荷日は当日 | EV-ORD-401 手順 5-1, SC-ORD-04-I05 | `Sc` | OrderID = o | ShipDate = `DateTime.Today`（時刻なし） |
| OrdTfShipInit_未出荷の配送業者と送料は受注の値 | EV-ORD-401 手順 5-2・5-3, SC-ORD-04-I06・I07 | `Sc` の送料を `12.50` にしたもの（配送業者 S0） | OrderID = o | ShipViaID = S0、Freight = 12.50 |
| OrdTfShipInit_未出荷で配送業者が未設定なら未選択 | EV-ORD-401 手順 5-2 | 受注（未出荷。ShipVia NULL。明細 1 行） | OrderID = その受注 | ErrorFlag = false。OrderID = その受注、ShipIsShipped = false、ShipViaID = null |
| OrdTfShipInit_送料がNULLなら空 | EV-ORD-401 手順 5-3, 共通仕様 4 節（NULL は空） | 受注（未出荷。送料 NULL。明細 1 行） | OrderID = その受注 | ErrorFlag = false。OrderID = その受注、ShipIsShipped = false、Freight = null |
| OrdTfShipInit_内訳の項目と強調 | EV-ORD-401 手順 5-4, SC-ORD-04-I08 | `Sc` の P2 を在庫 1 にしたもの（数量 2 のまま。P1 は在庫 10・数量 3） | OrderID = o | StockLines が 2 件（並びは見ない）。P1：Quantity = 3・UnitsInStock = 10・Short = false。P2：Quantity = 2・UnitsInStock = 1・Short = true。ProductName = それぞれの商品名 |
| OrdTfShipInit_内訳の強調の境界 | EV-ORD-401 手順 5-4（`Quantity > UnitsInStock` の行を強調） | 受注（未出荷）に、商品 A（在庫 3・数量 3）・B（在庫 3・数量 4）・C（在庫 3・数量 2） | OrderID = その受注 | A の Short = false（ちょうどは強調しない）、B の Short = true、C の Short = false |
| OrdTfShipInit_内訳は当該受注の明細だけ | EV-ORD-401 手順 5-4（`OrderID` で抽出） | `Sc` ＋ ほかの受注 o2（未出荷。商品 R の明細 1 行） | OrderID = o | StockLines が 2 件（P1・P2）。R を含まない |
| OrdTfShipInit_内訳の在庫がNULLなら空 | EV-ORD-401 手順 5-4, 共通仕様 4 節（NULL は空）。`Short` は期待にしない（DEC-ORD-62） | 受注（未出荷）に、在庫 NULL の商品 P の明細 | OrderID = その受注 | ErrorFlag = false。OrderID = その受注。StockLines が 1 件で、P の行の UnitsInStock = null |
| OrdTfShipInit_商品マスタに無い明細は内訳に含めない | EV-ORD-401 手順 5-4（`Products` を内部結合） | `Sc` ＋ `DeleteProduct` で商品を消した明細（商品 Z） | OrderID = o | StockLines が 2 件（P1・P2）。Z を含まない |
| OrdTfShipInit_出荷済の表示 | EV-ORD-401 手順 6-1・6-2, SC-ORD-04-I05〜I08 | `Sh` の送料を `7.25` にしたもの（出荷日 `2199/03/05`・配送業者 S0・要求納期 `2199/12/31`） | OrderID = o | ErrorFlag = false。ShipIsShipped = true、ShipDate = `2199/03/05`、ShipViaID = S0、Freight = 7.25、Status =「出荷済」、**StockLines が空** |
| OrdTfShipInit_出荷済で配送業者が未設定なら未選択 | EV-ORD-401 手順 6-1 | 受注（出荷済。ShipVia NULL。明細 1 行） | OrderID = その受注 | ErrorFlag = false。OrderID = その受注、ShipIsShipped = true、ShipViaID = null |
| OrdTfShipInit_配送業者の選択肢 | EV-ORD-401 手順 4, 選択肢の構築, SC-ORD-04-I06（会社名の昇順）, 手順 5-2・6-1（未選択を表す空白の選択肢） | 配送業者 S1・S2（ID の昇順と会社名の昇順が逆）、受注 1 件 | OrderID = その受注 | ShipperOptions の先頭が Value = ""・Text = ""（空白は 1 件だけ）。S1・S2 を含み、Value = ShipperID・Text = 会社名、S2 が S1 より前 |
| OrdTfShipInit_行バージョン | EV-ORD-401 手順 7, 共通仕様 9.1 | `Sc`。続けて `BumpOrder` | OrderID = o（`BumpOrder` の前と後） | RowVersion = 1、後は 2 |
| OrdTfShipInit_存在しない受注 | EV-ORD-401 手順 1, ERR-NOTFOUND | `DeleteOrder` で消した ID | OrderID = その ID | ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」 |

### 3.2 `OrdTfShipSave`（`EV-ORD-402`）

未出荷からの確定（在庫を引き落とす）。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfShipSave_確定で出荷情報が更新される | EV-ORD-402 手順 6, ORD-T2 | `Sc` | `V` | ErrorFlag = false、OrderID = o。`GetOrder`：ShippedDate = `2199/03/10`（日付だけ。時刻なし）、ShipVia = S1、Freight = 12.50、RowVersion = 2。CustomerID・EmployeeID・OrderDate・RequiredDate・Ship*（6 項目）は元のまま |
| OrdTfShipSave_確定で在庫が引き落とされる | EV-ORD-402 手順 4-3, OD-T5, PRD-T5 | `Sc` | `V` | P1 の在庫 = 7（10 − 3）、P2 = 5（7 − 2）。ErrorFlag = false |
| OrdTfShipSave_在庫がちょうど0になる | EV-ORD-402 手順 4-2（`Quantity > UnitsInStock` のときだけ不足）, PRD-T5（減算後に負にならない） | `Sc` と同じ受注日 `2199/03/01`・要求納期 `2199/12/31` の受注（未出荷）に、P1（在庫 3・数量 3）・P2（在庫 2・数量 2） | `V` | ErrorFlag = false。P1・P2 の在庫 = 0。ShippedDate = `2199/03/10` |
| OrdTfShipSave_在庫が1足りない | EV-ORD-402 手順 4-2, PRD-T5, ERR-BIZ | `Sc` と同じ受注日 `2199/03/01`・要求納期 `2199/12/31` の受注（未出荷）に、P1（在庫 2・数量 3） | `V` | ErrorFlag = true、ErrorMessage =「在庫が不足しています：」＋P1 の商品名。AssertUntouched（P1 の在庫 = 2 のまま、受注は未出荷・RowVersion = 1 のまま） |
| OrdTfShipSave_在庫不足は何も更新しない | EV-ORD-402 手順 4-2, 共通仕様 7 節（ロールバック） | `Sc` の P2 だけを不足にする（P1 在庫 10・数量 3 は足りる、P2 在庫 1・数量 2） | `V` | ErrorFlag = true、ErrorMessage =「在庫が不足しています：」＋P2 の商品名（P1 の商品名を含まない）。AssertUntouched（**P1 の在庫が 10 のまま**。足りる商品も引かない） |
| OrdTfShipSave_在庫が不足する商品が複数なら全商品名を列挙する | EV-ORD-402 手順 4-2（複数ある場合は全商品名を列挙）, DEC-ORD-61 | `Sc` と同じ受注日 `2199/03/01`・要求納期 `2199/12/31` の受注（未出荷）に、P1（在庫 1・数量 2）・P2（在庫 0・数量 1）・P3（在庫 9・数量 1） | `V` | ErrorFlag = true。ErrorMessage が「在庫が不足しています：」で始まり、P1・P2 の商品名を含み、P3 の商品名を含まない。AssertUntouched。**区切り・並びは期待にしない（DEC-ORD-61）** |
| OrdTfShipSave_確定でほかの受注と商品に触れない | EV-ORD-402 手順 4-3・6（`ProductID`・`OrderID` の一致で更新） | `Sc` ＋ ほかの受注 o2（K。未出荷。P1 数量 1。`BumpOrder` 済み〔RowVersion 2〕）、明細に無い商品 R（在庫 4） | `V` | o2：ShippedDate NULL・RowVersion = 2・Freight が元のまま・明細 1 行のまま。R の在庫 = 4。**P1 の在庫 = 7**（o2 の数量を引かない） |
| OrdTfShipSave_商品マスタに無い明細があっても確定できる | EV-ORD-402 手順 4-1（`Products` を内部結合。商品マスタに無い明細は在庫の検査・引き落としの対象外）, DEC-ORD-24 と同じ読み | `Sc` ＋ `DeleteProduct` で商品を消した明細（商品 Z。数量 5） | `V` | ErrorFlag = false。ShippedDate = `2199/03/10`。P1 = 7・P2 = 5（Z の明細は在庫を見ない） |
| OrdTfShipSave_確定で送料が空なら0 | DEC-ORD-01（SC-ORD-04-I07） | `Sc` | `V`（FreightText = ""） | ErrorFlag = false。`GetOrder` の Freight = 0（NULL でない） |
| OrdTfShipSave_送料0は通る | VAL-NUMERIC（0 以上）, EV-ORD-402 検証内容 | `Sc` | `V`（FreightText = `0`） | ErrorFlag = false。Freight = 0、ShippedDate = `2199/03/10`、P1 の在庫 = 7（「通る」側も DB の値で確かめる） |
| OrdTfShipSave_出荷日が受注日と同じ日は通る | VAL-DATE, ORD-T3（`受注日 <= 出荷日`） | `Sc`（受注日 `2199/03/01`） | `V`（ShippedDateText = `2199/03/01`） | ErrorFlag = false。ShippedDate = `2199/03/01`、P1 の在庫 = 7 |
| OrdTfShipSave_出荷日の上端は通る | VAL-DATE・ORD-T3（`受注日 <= 出荷日`）, DEC-ORD-58 | `Sc` | `V`（ShippedDateText = `9999/12/31`） | ErrorFlag = false。ShippedDate = `9999/12/31`、P1 の在庫 = 7（`datetime` の上端。答えがDEC-ORD-58 の 2 なら消す） |
| OrdTfShipSave_出荷日が受注日より前 | VAL-DATE, ORD-T3, DEC-ORD-57 | `Sc` | `V`（ShippedDateText = `2199/02/28`） | ErrorFlag = true。ErrorFields = [ShippedDate]、ErrorFieldMessages[0] =「出荷日は受注日以降を指定してください。」、ErrorMessages は空。AssertUntouched |
| OrdTfShipSave_出荷日が空（TestCase：空文字／null） | VAL-REQUIRED, EV-ORD-402 検証内容 | `Sc` | `V`（ShippedDateText を各値） | ErrorFlag = true。ErrorFields = [ShippedDate]。ErrorFieldMessages[0] が空でない（文言は期待にしない）。AssertUntouched |
| OrdTfShipSave_出荷日が空白だけ | VAL-REQUIRED, DEC-ORD-56（DEC-ORD-29 と同じ：空白だけは未指定とみなす） | `Sc` | `V`（ShippedDateText = 半角空白 2 つ） | ErrorFlag = true。ErrorFields = [ShippedDate]。AssertUntouched |
| OrdTfShipSave_出荷日が日付でない（TestCase：`abc`／`2199/13/01`／`2199/02/30`） | VAL-DATE（日付として解釈できる） | `Sc` | `V`（ShippedDateText を各値） | ErrorFlag = true。ErrorFields = [ShippedDate]。AssertUntouched |
| OrdTfShipSave_出荷日の形式（TestCase：`2199-03-10`／`2199/3/10`） | VAL-DATE, 共通仕様 4 節（`yyyy/MM/dd`）, DEC-ORD-56（DEC-ORD-29 と同じ規則） | `Sc` | `V`（ShippedDateText を各値） | ErrorFlag = true。ErrorFields = [ShippedDate]。AssertUntouched |
| OrdTfShipSave_配送業者の未選択（TestCase：空文字／null） | VAL-REQUIRED | `Sc` | `V`（ShipViaText を各値） | ErrorFlag = true。ErrorFields = [ShipVia]。AssertUntouched |
| OrdTfShipSave_存在しない配送業者（TestCase：`2147483647`／`abc`） | VAL-EXISTS | `Sc` | `V`（ShipViaText を各値） | ErrorFlag = true。ErrorFields = [ShipVia]。AssertUntouched |
| OrdTfShipSave_送料の不正（TestCase：`-0.01`／`abc`） | VAL-NUMERIC | `Sc` | `V`（FreightText を各値） | ErrorFlag = true。ErrorFields = [Freight]。AssertUntouched |
| OrdTfShipSave_違反はまとめて返す | EV-ORD-402 手順 1（全エラーを表示）, 共通仕様 6 節（全項目をまとめて検証） | `Sc` | `V`（ShippedDateText = `abc`、ShipViaText = ""、FreightText = `-1`） | ErrorFlag = true。ErrorFields が ShippedDate・ShipVia・Freight の 3 つ（順は見ない）。AssertUntouched |
| OrdTfShipSave_検証が先（TestCase：消された受注／行バージョンが違う受注） | EV-ORD-402 手順 1 → 3 | `Sc` を `DeleteOrder`、または `BumpOrder`（Freight 99） | `V`（ShippedDateText = "" 。消された受注は RowVersion = 1 のまま） | ErrorFlag = true。ErrorFields = [ShippedDate]。ErrorMessage が「対象のデータは存在しません。」でも競合の文言でもない。行バージョンが違う受注は、Freight = 99.00・RowVersion = 2・在庫 P1 = 10 のまま |
| OrdTfShipSave_行バージョンが違うと競合 | EV-ORD-402 手順 3, ERR-CONFLICT, 共通仕様 9.1 | `Sc`。`BumpOrder`（Freight 99 → RowVersion 2） | `V`（RowVersion = 1） | ErrorFlag = true、ErrorMessage =「他の利用者によって更新されています。最新の内容を読み込み直してください。」。`GetOrder`：Freight = 99.00・RowVersion = 2・ShippedDate NULL。P1 = 10・P2 = 7（引かない） |
| OrdTfShipSave_消された受注 | EV-ORD-402 手順 3, ERR-NOTFOUND | `Sc` を `DeleteOrder` | `V` | ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」。P1 = 10・P2 = 7（引かない） |
| OrdTfShipSave_同じ行バージョンの2回目は競合で在庫は1回しか引かれない | EV-ORD-402 手順 3・6, 共通仕様 9.1 | `Sc` | `V` を 2 回 | 1 回目 ErrorFlag = false。2 回目 ErrorFlag = true・競合の文言。P1 = 7（10 − 3。4 ではない）、P2 = 5。RowVersion = 2 |

出荷済の情報更新（在庫は変更しない）。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfShipSave_出荷済は情報だけ更新し在庫は変えない | EV-ORD-402 手順 5・6 | `Sh` | OrderID = o、RowVersion = 1、DisplayedShipped = true、ShippedDateText = `2199/03/12`、ShipViaText = S1、FreightText = `20.00` | ErrorFlag = false。ShippedDate = `2199/03/12`、ShipVia = S1、Freight = 20.00、RowVersion = 2。**P1 = 7・P2 = 5・`Products.RowVersion` = 1 のまま** |
| OrdTfShipSave_出荷済の更新は在庫が数量に足りなくても通る | EV-ORD-402 手順 5（在庫は変更しない。不足の検査は出荷確定の手順 4 だけ） | `Sh` の P1 を在庫 0（数量 3） | 上と同じ | ErrorFlag = false。P1 の在庫 = 0 のまま（負にならず、不足のエラーにもならない） |
| OrdTfShipSave_出荷済の更新でも検証する | EV-ORD-402 手順 1（確定・情報更新の共通）, VAL-DATE | `Sh` | DisplayedShipped = true、ShippedDateText = `2199/02/28` | ErrorFlag = true。ErrorFields = [ShippedDate]。受注の ShippedDate = `2199/03/05`・RowVersion = 1・在庫 7・5 のまま |
| OrdTfShipSave_出荷済の更新で行バージョンが違うと競合・消されたら NOTFOUND（TestCase：`BumpOrder`〔Freight 99〕／`DeleteOrder`） | EV-ORD-402 手順 3・6（出荷済の分岐も `RowVersion` の条件）, ERR-CONFLICT・ERR-NOTFOUND, 共通仕様 9.1 | `Sh` を `BumpOrder`（Freight 99 → RowVersion 2）、または `DeleteOrder` | OrderID = o、RowVersion = 1、DisplayedShipped = true、ShippedDateText = `2199/03/12`、S1、`20.00` | ErrorFlag = true。ErrorMessage が、競合は「他の利用者によって更新されています。最新の内容を読み込み直してください。」、消された受注は「対象のデータは存在しません。」。競合のとき：ShippedDate = `2199/03/05` のまま・ShipVia = S0・Freight = 99.00・RowVersion = 2。P1 = 7・P2 = 5 のまま |

DB の状態と「表示時点の別」が食い違うとき（画面の操作では起こらない。細工した要求か B層を直接呼んだときだけ）。DEC-ORD-60 の答え＝1（DB の `ShippedDate` を正とし、引数の別は使わない）。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfShipSave_表示時点が未出荷と主張されてもDBが出荷済なら在庫を引かない | DEC-ORD-60, EV-ORD-402 手順 4・5, OD-T5, DEC-ORD-49 の考え方（画面の値で業務の検証を外さない） | `Sh`（DB は出荷済。P1 = 7・P2 = 5） | RowVersion = 1、DisplayedShipped = false、ShippedDateText = `2199/03/12`、S1、`20.00` | ErrorFlag = false（出荷情報の更新として扱う）。ShippedDate = `2199/03/12`。**P1 = 7・P2 = 5 のまま**（二重に引かない） |
| OrdTfShipSave_表示時点が出荷済と主張されてもDBが未出荷なら在庫を引き落とす | DEC-ORD-60, EV-ORD-402 手順 4, OD-T5, PRD-T5 | `Sc`（DB は未出荷。P1 = 10・P2 = 7） | `V`（DisplayedShipped = true） | ErrorFlag = false。ShippedDate = `2199/03/10`。**P1 = 7・P2 = 5**（引き落としを省かせない） |
| OrdTfShipSave_出荷済と主張してもDBが未出荷で在庫が足りなければ不足 | DEC-ORD-60, EV-ORD-402 手順 4-2, PRD-T5 | `Sc` の P2 を在庫 1（数量 2） | `V`（DisplayedShipped = true） | ErrorFlag = true、ErrorMessage =「在庫が不足しています：」＋P2 の商品名。AssertUntouched（在庫の検査を外させない） |

在庫の更新の版数。DEC-ORD-59 の答え＝1（`Products.RowVersion` も +1）。

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfShipSave_確定で在庫を更新した商品の行バージョンが進む | DEC-ORD-59, 共通仕様 9.1（更新系イベントは `RowVersion = RowVersion + 1`） | `Sc` ＋ 明細に無い商品 R | `V` | P1・P2 の `Products.RowVersion` = 2（始め 1）。R = 1 のまま |

### 3.3 `OrdTfShipCancel`（`EV-ORD-403`）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfShipCancel_出荷日が未設定に戻る | EV-ORD-403 手順 6 | `Sh`（配送業者 S0・送料 `5.00`） | OrderID = o、RowVersion = 1 | ErrorFlag = false、OrderID = o。`GetOrder`：ShippedDate が NULL、RowVersion = 2、**ShipVia = S0・Freight = 5.00（変更しない）**。CustomerID・EmployeeID・OrderDate・RequiredDate・Ship*（6 項目）は元のまま |
| OrdTfShipCancel_在庫が戻る | EV-ORD-403 手順 5, OD-T5 | `Sh`（P1 = 7・P2 = 5。数量 3・2） | 同上 | P1 = 10、P2 = 7 |
| OrdTfShipCancel_在庫は数量を加算して戻す | EV-ORD-403 手順 5（`UnitsInStock + Quantity`）, OD-T5 | `Sh` の P1 を在庫 20（数量 3）、P2 を在庫 4（数量 2）にしたもの（始めの在庫を 7 以外にして、固定値へ書く実装を除く） | 同上 | P1 = 23（20 + 3）、P2 = 6（4 + 2） |
| OrdTfShipCancel_取消で在庫を戻した商品の行バージョンが進む | DEC-ORD-59, 共通仕様 9.1（更新系イベントは `RowVersion = RowVersion + 1`）, EV-ORD-403 手順 5 | `Sh` ＋ 明細に無い商品 R | OrderID = o、RowVersion = 1 | P1・P2 の `Products.RowVersion` = 2（始め 1）。R = 1 のまま |
| OrdTfShipCancel_ほかの受注と商品に触れない | EV-ORD-403 手順 5・6 | `Sh` ＋ ほかの受注 o2（K。未出荷。P1 数量 1。RowVersion 2〔`BumpOrder`〕）、明細に無い商品 R（在庫 4） | 同上 | o2：RowVersion = 2・ShippedDate NULL・明細 1 行のまま。R = 4。P1 = 10（o2 の数量を足さない） |
| OrdTfShipCancel_未出荷の受注はERR-BIZ | EV-ORD-403 手順 4, ERR-BIZ | `Sc`（未出荷。RowVersion = 1。P1 = 10・P2 = 7） | OrderID = o、RowVersion = 1 | ErrorFlag = true、ErrorMessage =「この受注は出荷されていません。」。ShippedDate が NULL・RowVersion = 1 のまま。**P1 = 10・P2 = 7 のまま**（誤って戻さない） |
| OrdTfShipCancel_行バージョンが違うと競合 | EV-ORD-403 手順 3, ERR-CONFLICT | `Sh`。`BumpOrder`（Freight 99 → RowVersion 2） | RowVersion = 1 | ErrorFlag = true、ErrorMessage =「他の利用者によって更新されています。最新の内容を読み込み直してください。」。ShippedDate = `2199/03/05` のまま・Freight = 99.00・RowVersion = 2。P1 = 7・P2 = 5（戻さない） |
| OrdTfShipCancel_消された受注 | EV-ORD-403 手順 3, ERR-NOTFOUND | `Sh` を `DeleteOrder` | 同上 | ErrorFlag = true、ErrorMessage =「対象のデータは存在しません。」。P1 = 7・P2 = 5 |
| OrdTfShipCancel_同じ行バージョンの2回目は競合で在庫は1回しか戻らない | EV-ORD-403 手順 3・6, 共通仕様 9.1 | `Sh` | 同上を 2 回 | 1 回目 ErrorFlag = false。2 回目 ErrorFlag = true・競合の文言。P1 = 10（13 ではない）、P2 = 7。RowVersion = 2 |
| OrdTfShipCancel_確定と取消の往復で在庫は元に戻る | EV-ORD-402・403, OD-T5, PRD-T5, 共通仕様 9.1 | `Sc` | `V` で確定 → 取消（RowVersion = 2）→ 確定（RowVersion = 3、`V` と同じ入力。DisplayedShipped = false） | 各時点の P1 の在庫：7 → 10 → 7。P2：5 → 7 → 5。ShippedDate：`2199/03/10` → NULL → `2199/03/10`。RowVersion：2 → 3 → 4。全部 ErrorFlag = false |

- 追加：**B層 60 メソッド**（`OrdTfShipInit` 17・`OrdTfShipSave` 33・`OrdTfShipCancel` 10。TestCase で実行数は増える）。**すべて作る**（DEC-ORD-58・59・60 の 6 ケースを含む。答えが決まったので、決定待ちで作らないものは無い）。DEC-ORD-56 の答え（1。④ａ・⑤ａ）のとおり、`出荷日が空白だけ`・`出荷日の形式` を作る。実数は段 2 の報告に書く。

## 4. 画面の E2E（`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfShipScreenTests`。新しいクラス）

B層で確かめることは繰り返さない（検証の全項目・在庫の加減算の値の細部・複数の不足の列挙・版数は B層）。画面の表示・活性・遷移（重ねて表示・閉じる・再読み込み）・確認ダイアログ・メッセージの区別・ダイアログに留まるときの入力の扱いを確かめる。DB の確認は、画面の操作の結果が DB に届いたか（届いていないか）を見る最小限にする。
共通の前提：`[SetUp]` でログイン、2198 年のデータ。**操作の前に `Expect` で要素を待つ**（Red を `Expect` の失敗にする）。入力は `FillAsync`、選択は `SelectOptionAsync`。確認ダイアログは `page.Dialog` で受ける（記録して承認か取り消し）。「K」「S0」「S1」「P1」「P2」はテストが入れたもの。

**`Es`（E2E の共通の受注）**：顧客 K、配送業者 S0・S1、商品 P1（在庫 10）・P2（在庫 5）。受注 o：K・受注日 `2198/03/01`・要求納期 `2198/12/31`・未出荷・配送業者 S0・送料 `12.50`・Ship* に値。明細：P1（単価 `10.00`・割引 0・数量 3）、P2（`10.00`・0・数量 2）。小計 `50.00`、合計 `62.50`。`Es 不足`：P2 の在庫が 1（数量 2 に足りない）。
「一覧を開く」＝`/OrdTf` を開き `#CustomerID` で K を選び `#btnMain1`（検索）。「詳細を開く」＝`/OrdTf/Detail?id=<o>`。「ダイアログを開く」＝一覧の o の行（受注 ID のリンクがある行）の `button.btn-ship`、または詳細の `#btnShip` を押し、`#shipDialog` が見えるのを待つ。「今日」は `DateTime.Today` の `yyyy/MM/dd`。

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 一覧_操作列と出荷更新ボタン | SC-ORD-01-I06（列 10）, SC-ORD-01-B04, EV-ORD-108 | K の受注 2 件（a：未出荷で要求納期 `2198/12/31`＝状況「未出荷」、b：未出荷で要求納期 `2001/01/01`＝「納期遅延」） | 一覧を開く | `#orderList thead th` の 10 番目が「操作」で、`button`・`a`・`input` を持たない。a・b の行の `td` 10 番目の `button.btn-ship` が 1 つずつ、表示名「出荷更新」、活性 |
| 一覧_出荷済の行は出荷変更（DEC-ORD-50） | SC-ORD-01-B04（出荷済の行では表示名を「出荷変更」）, ORD-T2, DEC-ORD-50 | K の受注 2 件（c：出荷日 `2198/03/05`・要求納期 `2198/12/31`＝「出荷済」、d：出荷日 `2198/03/05`・要求納期 `2198/03/01`＝出荷が遅れた「納期遅延」） | 一覧を開く | c・d の行の `button.btn-ship` の表示名が「出荷変更」（d は状況が「納期遅延」でも `ShippedDate` が NULL でないので）。活性 |
| 一覧_出荷更新は行ごとに対象が違う | EV-ORD-108 入力（選択された行の `OrderID`） | K の受注 2 件 a・b（`Es` の形） | 一覧を開く → b の行の `.btn-ship` | `#shipOrderId` = b の受注 ID（a ではない） |
| 一覧_出荷更新で元画面の上に重ねて表示される | EV-ORD-108 手順 1, F-5（ダイアログは元画面の上に重ねて表示）, 決定 1 | `Es` | 一覧を開く → ダイアログを開く | `#shipDialog` が見える。URL のパスが `/OrdTf` のまま。`#orderList` が 1 つあり、K の行が見える（元画面が DOM に残る）。元画面のフッタは変わらない：`#btnMain1` の名前が「検索」で活性、`#btnMain2`〜`#btnMain5` が非活性。`#footer-buttons` の中に `#shipMain1`〜`#shipMain5`・`#btnShipClose` が無い。この 6 つは `#shipDialog` の中に 1 つずつあり、`#shipMain3`〜`#shipMain5` は見えて非活性 |
| 詳細_出荷更新ボタン（TestCase：未出荷／出荷済） | SC-ORD-02-B03（常時活性）, EV-ORD-205 | 受注 1 件（未出荷、または出荷日 `2198/04/05`） | 詳細を開く | `#contents #btnShip` の名前が「出荷更新」で活性。`#footer-buttons #btnShip` が無い。既存の `#btnEdit`（編集）の活性は出荷日の有無のまま |
| 詳細_出荷更新で元画面の上に重ねて表示される | EV-ORD-205 手順 1, F-5 | `Es` | 詳細を開く → ダイアログを開く | `#shipDialog` が見える。URL のパスが `/OrdTf/Detail` のまま。`#orderId` = o と `#detailList` が詳細のまま残る（`#shipOrderId` とは別の要素）。`#btnMain1` の名前が「削除」のまま |
| 表示_未出荷 | EV-ORD-401 手順 1〜5, SC-ORD-04-I01〜I08・B01〜B03 | `Es 不足` | 一覧を開く → ダイアログを開く | `#shipOrderId` = o、`#shipCustomerName` = K の会社名、`#shipStatus` =「未出荷」、`#shipRequiredDate` = `2198/12/31`、`#shipShippedDate` = 今日、`#shipShipVia` の選択が S0、`#shipFreight` が数値 12.5（`12.50`・`12.5` どちらでもよい）。`#shipStockList tbody tr` が 2 行：P1（`.ship-qty` = 3・`.ship-stock` = 10・`table-danger` を持たない）、P2（2・1・**`table-danger` を持つ**）。`#shipMain1` の名前が「出荷確定」で活性、`#shipMain2` が非活性、`#shipMain3`〜`#shipMain5` が見えて非活性、`#btnShipClose` が活性。`#shipMessage`・3 つの `…Error` が空 |
| 表示_未出荷で配送業者が未設定なら未選択 | EV-ORD-401 手順 5-2 | 未出荷の受注（ShipVia NULL。明細 1 行） | ダイアログを開く | `#shipShipVia` の選択の value が空（先頭の空白） |
| 表示_配送業者の選択肢 | EV-ORD-401 手順 4, 選択肢の構築, SC-ORD-04-I06, 手順 5-2・6-1（未選択を表す空白の選択肢） | 配送業者 S1・S2（ID の昇順と会社名の昇順が逆）、受注 1 件 | ダイアログを開く | `#shipShipVia` の最初の option が空白（value が空）で、S1・S2 の会社名があり、S2 が S1 より前 |
| 表示_出荷済 | EV-ORD-401 手順 6, SC-ORD-04-I05〜I08・B01・B02 | 受注（出荷日 `2198/04/05`・要求納期 `2198/04/01`＝「納期遅延」・配送業者 S0・送料 `7.25`。明細 1 行） | ダイアログを開く | `#shipStatus` =「納期遅延」、`#shipShippedDate` = `2198/04/05`、`#shipShipVia` の選択が S0、`#shipFreight` が数値 7.25。**`#shipStockList` が見えない**。`#shipMain1` の名前が「出荷情報を更新」で活性、`#shipMain2` が活性（名前「出荷取消」）、`#shipMain3`〜`#shipMain5` が非活性 |
| 表示_出荷済で納期内は出荷済 | EV-ORD-401 手順 3, 状況の判定 | 受注（出荷日 `2198/03/05`・要求納期 `2198/12/31`） | ダイアログを開く | `#shipStatus` =「出荷済」 |
| 表示_詳細から開いても同じ内容 | EV-ORD-205 手順 1, EV-ORD-401 | `Es 不足` | 詳細を開く → ダイアログを開く | 「表示_未出荷」と同じ：`#shipOrderId`・`#shipStatus`・`#shipShippedDate`（今日）・`#shipStockList` が 2 行で P2 だけ `table-danger`・`#shipMain1`「出荷確定」・`#shipMain2` 非活性 |
| 表示_必須の印 | 共通仕様 4 節（必須項目は表示名の末尾に「＊」）, SC-ORD-04-I05〜I07 | `Es` | ダイアログを開く | `label[for=shipShippedDate]` が「出荷日＊」、`label[for=shipShipVia]` が「配送業者＊」、`label[for=shipFreight]` が「送料」（＊なし） |
| 表示_消された受注は一覧へERR-NOTFOUND（一覧から。DEC-ORD-51・52） | EV-ORD-401 手順 1, ERR-NOTFOUND, F-4, DEC-ORD-51・52 | K の受注 25 件（i 番目 = `2198/01/01` + i 日。未出荷） | 一覧で K を検索 → 次へ（「2 / 2 ページ」）→ 2 ページ目の 1 件をテストが `DeleteOrder` → その行の `.btn-ship` | `#shipDialog` が見えない。一覧が再読み込みされ、`#messageArea` が「対象のデータは存在しません。」で `text-danger`。保持された一覧状態：`#CustomerID` = K、`#pageInfo` =「2 / 2 ページ」、`#totalCount` =「全 24 件」、一覧 4 行 |
| 表示_消された受注は保持した一覧状態の一覧へERR-NOTFOUND（詳細から。DEC-ORD-52） | EV-ORD-401 手順 1, ERR-NOTFOUND, F-4, DEC-ORD-28, DEC-ORD-52 | K の受注 25 件（上と同じ）。一覧で K を検索 → 次へ → 先頭行を選ぶ（詳細。`#btnShip` が見える） | テストがその受注を `DeleteOrder` → `#btnShip` | 一覧が表示される（`#orderList` が見え、`#orderId` は無い）。`#messageArea` が「対象のデータは存在しません。」で `text-danger`。`#CustomerID` = K、`#pageInfo` =「2 / 2 ページ」、`#totalCount` =「全 24 件」、一覧 4 行 |
| 確定_未出荷から確定して一覧へ戻る | EV-ORD-402 手順 6・8・9, EV-ORD-108 手順 2, OD-T5, DEC-ORD-55 | `Es` | 一覧を開く → ダイアログを開く → `#shipShippedDate` に `2198/05/01`、`#shipShipVia` で S1、`#shipFreight` に `20` → `#shipMain1` | **確認ダイアログが出ない**。`#shipDialog` が見えない。一覧が表示され、`#messageArea` が「受注 ID nnn の出荷情報を更新しました。」（nnn = o）で `text-info`（`text-danger` を持たない）。o の行：出荷日 = `2198/05/01`、配送業者 = S1 の会社名、合計金額 = `70.00`（小計 50.00 + 送料 20）、状況 =「出荷済」、`.btn-ship` の表示名「出荷変更」。DB：`GetOrder` の ShippedDate = `2198/05/01`・ShipVia = S1・Freight = 20.00・RowVersion = 2、P1 の在庫 = 7、P2 = 3 |
| 確定_成功の後に再読み込みしても二重に確定しない | EV-ORD-402 手順 9, 引き継ぎ（POST の再送） | 上の確定の後 | `page.ReloadAsync` | 一覧が表示される。DB：P1 の在庫 = 7・P2 = 3 のまま、o の RowVersion = 2 のまま |
| 確定_詳細から確定すると詳細が最新になる | EV-ORD-402 手順 9, EV-ORD-205 手順 2・EV-ORD-201 | `Es` | 詳細を開く → ダイアログを開く → 上と同じ入力 → `#shipMain1` | `#shipDialog` が見えない。詳細のまま：`#orderId` = o。`#messageArea` が「受注 ID nnn の出荷情報を更新しました。」（`text-info`）。`#shippedDate` = `2198/05/01`、`#shipperName` = S1 の会社名、`#freight` = `20.00`、`#status` =「出荷済」、`#btnEdit` が非活性 |
| 確定_詳細から確定した後の削除は競合しない | EV-ORD-205 手順 2（保持していた `RowVersion` も更新される）, EV-ORD-203 手順 4 | 上の確定の後（詳細） | `#btnMain1`（削除）→ 確認を承認 | 一覧が表示され、`#messageArea` が「受注 ID nnn を削除しました。」で `text-info`（`ERR-CONFLICT` にならない）。DB に o が無い |
| 確定_検証エラーはダイアログに留まり項目の直下に出る | EV-ORD-402 手順 1, ERR-VALIDATION, VAL-REQUIRED・VAL-NUMERIC, DEC-CMN-05 | `Es` | 一覧を開く → ダイアログを開く → `#shipShippedDate` を空、`#shipShipVia` を空白、`#shipFreight` に `-1` → `#shipMain1` | `#shipDialog` が見えたまま。`#shipShippedDateError`・`#shipShipViaError`・`#shipFreightError` がどれも空でなく `text-danger`。`#shipMessage` が空（項目単位は共通メッセージ領域に出さない）。入力値が保持される（`#shipFreight` = `-1`）。元画面の `#messageArea` が空。DB：ShippedDate が NULL・RowVersion = 1・P1 = 10・P2 = 5 |
| 確定_送料の不正値（TestCase：`-1`／`abc`） | VAL-NUMERIC, DEC-CMN-05, DEC-ORD-56 | `Es` | ダイアログを開く → `#shipShippedDate` に `2198/05/01`（初期値の今日は受注日 `2198/03/01` より前で ORD-T3 の違反になるので、受注日以後の日付を入れる）→ `#shipFreight` に各値 → `#shipMain1` | `#shipFreightError` が空でなく `text-danger`。`#shipShippedDateError`・`#shipShipViaError` は空。`#shipDialog` が見えたまま。DB：ShippedDate が NULL |
| 確定_出荷日が受注日より前は出荷日の直下（DEC-ORD-57） | VAL-DATE, ORD-T3, DEC-ORD-57 | `Es`（受注日 `2198/03/01`） | ダイアログを開く → `#shipShippedDate` に `2198/02/28` → `#shipMain1` | `#shipShippedDateError` が「出荷日は受注日以降を指定してください。」で `text-danger`。`#shipMessage` が空。`#shipDialog` が見えたまま。DB：ShippedDate が NULL・P1 = 10 |
| 確定_検証エラーの後に直せば確定できる | EV-ORD-402 手順 1（入力値を保持して留まる）・6 | `Es` | ダイアログを開く → `#shipShippedDate` を空にして `#shipMain1`（エラー）→ `#shipShippedDate` に `2198/05/01` → `#shipMain1` | 1 回目は `#shipShippedDateError` が出てダイアログに留まる。2 回目で `#shipDialog` が見えなくなり、一覧の `#messageArea` が「受注 ID nnn の出荷情報を更新しました。」（`text-info`）。DB：ShippedDate = `2198/05/01`・P1 = 7 |
| 確定_在庫不足はダイアログに留まりエラーを出す（DEC-ORD-54） | EV-ORD-402 手順 4-2, PRD-T5, ERR-BIZ, DEC-ORD-54 | `Es 不足`（P2 の在庫 1・数量 2） | 一覧を開く → ダイアログを開く → `#shipShippedDate` に `2198/05/01` → `#shipMain1` →（テストが P2 の在庫を 5 にする `SetUnitsInStock`）→ もう一度 `#shipMain1` | 1 回目：`#shipDialog` が見えたまま。`#shipMessage` が「在庫が不足しています：」＋P2 の商品名で `text-danger`。`#shipStockList` が見える。`#shipShippedDate` = `2198/05/01`（入力が保持される）。元画面の `#messageArea` が空。DB：ShippedDate が NULL・RowVersion = 1・P1 = 10・P2 = 1。2 回目：`#shipDialog` が見えなくなり、一覧の `#messageArea` が「受注 ID nnn の出荷情報を更新しました。」。DB：P1 = 7・P2 = 3 |
| 確定_競合は最新を再表示する（DEC-ORD-53） | EV-ORD-402 手順 3, ERR-CONFLICT, 共通仕様 7 節（最新データを再表示）, DEC-ORD-34 と同じ考え方, DEC-ORD-53 | `Es` | 一覧を開く → ダイアログを開く →（テストが `BumpOrder`：送料 99）→ `#shipShippedDate` に `2198/05/01`、`#shipFreight` に `20` → `#shipMain1` → 再表示された `#shipShippedDate` に `2198/05/01` を入れ直し（初期値の今日のままにしない。**先に検証が走る**ので、日付を入れずに押す手順にしない）→ `#shipMain1` | 1 回目：`#shipDialog` が見えたまま。`#shipMessage` が「他の利用者によって更新されています。最新の内容を読み込み直してください。」で `text-danger`。`#shipFreight` が数値 99（最新）、`#shipShippedDate` = 今日（入力した `2198/05/01` を捨てる）。DB：ShippedDate が NULL・Freight = 99.00・RowVersion = 2・P1 = 10。2 回目：`#shipDialog` が見えなくなり、一覧の `#messageArea` が「受注 ID nnn の出荷情報を更新しました。」。DB：ShippedDate = `2198/05/01`・RowVersion = 3・P1 = 7 |
| 確定_他の利用者が先に出荷確定したら出荷済として再表示する（DEC-ORD-53） | EV-ORD-402 手順 3, ERR-CONFLICT, DEC-ORD-53, OD-T5（在庫を二重に引かない） | `Es` | ダイアログを開く →（テストが `ShipOrder`：出荷日が今日になり RowVersion 2）→ `#shipShippedDate` に `2198/05/01` を入れる（検証が先に走るので、受注日以後の日付を入れてから押す）→ `#shipMain1` | `#shipDialog` が見えたまま。`#shipMessage` が競合の文言で `text-danger`。`#shipStatus` =「出荷済」、`#shipMain1` の名前が「出荷情報を更新」、`#shipMain2` が活性、`#shipStockList` が見えない。DB：P1 = 10・P2 = 5（引いていない）・RowVersion = 2 |
| 確定_消されていたら一覧へERR-NOTFOUND（DEC-ORD-51・52） | EV-ORD-402 手順 3, ERR-NOTFOUND, F-4, DEC-ORD-51・52 | K の受注 2 件（o と o2。どちらも `Es` の形） | 一覧を開く → o のダイアログを開く →（テストが `DeleteOrder`：o）→ `#shipShippedDate` に `2198/05/01` を入れる（検証が先に走る。受注が消されたときの出荷日の検証は仕様で決まらないので、検証を通る入力にする）→ `#shipMain1` | `#shipDialog` が見えなくなる。一覧が表示され、`#messageArea` が「対象のデータは存在しません。」で `text-danger`。`#CustomerID` = K、`#totalCount` =「全 1 件」、一覧に o が無い。P1 = 10・P2 = 5 |
| 確定_出荷済の情報を更新する | EV-ORD-402 手順 5・6, SC-ORD-04-B01（出荷済は「出荷情報を更新」） | 受注（受注日 `2198/03/01`・出荷日 `2198/04/05`・配送業者 S0・送料 `7.25`。明細 P1 数量 3〔在庫 7〕・P2 数量 2〔在庫 3〕） | 一覧を開く → ダイアログを開く → `#shipShippedDate` に `2198/04/10`、`#shipShipVia` で S1、`#shipFreight` に `30` → `#shipMain1` | `#shipDialog` が見えなくなり、一覧の `#messageArea` が「受注 ID nnn の出荷情報を更新しました。」（`text-info`）。DB：ShippedDate = `2198/04/10`・ShipVia = S1・Freight = 30.00・RowVersion = 2。**P1 = 7・P2 = 3 のまま** |
| 確定_一覧状態を保って一覧へ戻る（DEC-ORD-51） | EV-ORD-108 手順 2, F-5, DEC-ORD-51, DEC-ORD-04（保持する条件）・23 | K の受注 25 件（i 番目 = `2198/01/01` + i 日。未出荷。確定する 1 件だけ明細 1 行〔テストが作った商品 P1・在庫 10・数量 1。在庫が足りる〕。ほかの 24 件は明細なしで、表示されるだけ。DEC-CMN-02：明細が無い受注の小計は 0） | 一覧で K を検索 → 次へ（「2 / 2 ページ」）→ 2 ページ目の 1 件のダイアログを開く → `#shipShippedDate` に `2198/05/01`、`#shipShipVia` で S0 を選ぶ → `#shipMain1` | 一覧が表示され、`#CustomerID` = K、`#pageInfo` =「2 / 2 ページ」、`#totalCount` =「全 25 件」、一覧 5 行。確定した受注の行の出荷日 = `2198/05/01`。DB：P1 の在庫 = 9（10 − 1） |
| 取消_確認して承認すると未出荷に戻る | EV-ORD-403 手順 1・5〜9, OD-T5, DEC-ORD-55 | 受注（出荷日 `2198/04/05`・要求納期 `2198/12/31`・配送業者 S0・送料 `7.25`。明細 P1 数量 3〔在庫 7〕・P2 数量 2〔在庫 3〕） | 一覧を開く → ダイアログを開く → `#shipMain2` → 確認を承認 | 確認ダイアログのメッセージが「出荷を取り消し、在庫を戻します。よろしいですか？」。`#shipDialog` が見えなくなり、一覧の `#messageArea` が「受注 ID nnn の出荷を取り消しました。」（`text-info`）。行：出荷日が空、配送業者 = S0 の会社名（変えない）、状況 =「未出荷」、`.btn-ship` の表示名「出荷更新」。DB：ShippedDate が NULL・ShipVia = S0・Freight = 7.25・RowVersion = 2、P1 = 10・P2 = 5 |
| 取消_確認を取り消すと何もしない | EV-ORD-403 手順 1（取り消された場合は何もしない） | 上と同じ受注 | ダイアログを開く → `#shipMain2` → 確認を取り消す | `#shipDialog` が見えたまま。`#shipMessage`・元画面の `#messageArea` が空。DB：ShippedDate = `2198/04/05`・RowVersion = 1・P1 = 7・P2 = 3 のまま |
| 取消_成功の後に再読み込みしても二重に戻さない | EV-ORD-403 手順 9, 引き継ぎ（POST の再送） | 上の取消の承認の後 | `page.ReloadAsync` | 一覧が表示される。DB：P1 = 10・P2 = 5 のまま（13・7 ではない）、RowVersion = 2 |
| 取消_詳細から取り消すと詳細が最新になる | EV-ORD-403 手順 9, EV-ORD-205 手順 2・EV-ORD-201 | 上と同じ受注 | 詳細を開く → ダイアログを開く → `#shipMain2` → 承認 | `#shipDialog` が見えなくなる。詳細のまま：`#messageArea` が「受注 ID nnn の出荷を取り消しました。」（`text-info`）。`#shippedDate` が空、`#status` =「未出荷」、`#btnEdit` が活性 |
| 取消_競合は最新を再表示する（DEC-ORD-53） | EV-ORD-403 手順 3, ERR-CONFLICT, DEC-ORD-53 | 上と同じ受注 | ダイアログを開く →（テストが `BumpOrder`：送料 99）→ `#shipMain2` → 承認 | `#shipDialog` が見えたまま。`#shipMessage` が競合の文言で `text-danger`。`#shipFreight` が数値 99、`#shipMain2` が活性（出荷済のまま）。DB：ShippedDate = `2198/04/05` のまま・P1 = 7・P2 = 3（戻していない）・RowVersion = 2 |
| 取消_消されていたら一覧へERR-NOTFOUND（DEC-ORD-51・52） | EV-ORD-403 手順 3, ERR-NOTFOUND, DEC-ORD-51・52 | 出荷済の受注 o・o2（K。上と同じ形） | 一覧を開く → o のダイアログを開く →（テストが `DeleteOrder`：o）→ `#shipMain2` → 承認 | `#shipDialog` が見えなくなる。一覧が表示され、`#messageArea` が「対象のデータは存在しません。」で `text-danger`。`#CustomerID` = K、`#totalCount` =「全 1 件」 |
| 閉じる_一覧は変更がなくても再読み込みする | EV-ORD-404 手順 1・2, F-5, EV-ORD-108 手順 2 | `Es`（合計金額 `62.50`） | 一覧を開く → ダイアログを開く →（テストが `BumpOrder`：送料 99）→ `#btnShipClose` | `#shipDialog` が見えなくなる。o の行の合計金額 = `149.00`（小計 50.00 + 99。再読み込みされた）。`#messageArea` が空。DB：ShippedDate が NULL・RowVersion = 2（`BumpOrder` の値のまま。閉じるで更新しない）・P1 = 10 |
| 閉じる_詳細は再読み込みする | EV-ORD-404 手順 2, EV-ORD-205 手順 2・EV-ORD-201, EV-ORD-203 手順 4 | `Es` | 詳細を開く → ダイアログを開く →（テストが `ShipOrder`）→ `#btnShipClose` → 続けて `#btnMain1`（削除）→ 確認を承認 | 閉じた後：`#shipDialog` が見えなくなる。詳細のまま：`#shippedDate` = 今日、`#status` が「未出荷」でない、`#btnEdit` が非活性。`#messageArea` が空。削除の後：一覧が表示され、`#messageArea` が「受注 ID nnn を削除しました。」で `text-info`（保持していた `RowVersion` も最新になっているので `ERR-CONFLICT` にならない）。DB に o が無い |
| 閉じる_一覧の条件・ページ・並べ替えを保って再読み込みする（DEC-ORD-51） | EV-ORD-404 手順 2, F-5, 共通の処理「一覧状態の保持」, DEC-ORD-51 | K の受注 25 件（i 番目 = `2198/01/01` + i 日。未出荷） | 一覧で K を検索 → `#sortOrderID` を押す → 次へ（「2 / 2 ページ」）→ このときの一覧 5 行の受注 ID の並びと、並べ替え列の `aria-sort` を覚える → 2 ページ目の 1 件のダイアログを開く → `#btnShipClose` | `#shipDialog` が見えなくなる。`#CustomerID` = K、`#pageInfo` =「2 / 2 ページ」、`#totalCount` =「全 25 件」。一覧 5 行の受注 ID の並びと並べ替え列の `aria-sort` が、覚えた値と同じ |
| 閉じる_出荷状況で絞った一覧は同じ条件で再検索する | EV-ORD-108 手順 2（同じ条件で再検索）, EV-ORD-404 手順 2, DEC-ORD-51 | K の受注 3 件 a・b・c（未出荷） | 一覧で K・出荷状況「未出荷」を検索（3 行）→ a のダイアログを開く →（テストが `ShipOrder`：a）→ `#btnShipClose` | 一覧が 2 行（b・c）。`#totalCount` =「全 2 件」。a の行が無い |
| 閉じる_入力は破棄され開き直すと初期値 | EV-ORD-404 手順 1（入力内容を破棄）, SC-ORD-04-I05〜I07 | `Es` | 一覧を開く → ダイアログを開く → `#shipShippedDate` に `2198/05/01`、`#shipFreight` に `99` → `#btnShipClose` → もう一度ダイアログを開く | DB：ShippedDate が NULL・Freight = 12.50・RowVersion = 1・P1 = 10。開き直した `#shipShippedDate` = 今日、`#shipFreight` が数値 12.5 |

- 追加：**E2E 約 40 メソッド**（TestCase：詳細の出荷更新ボタン 2、送料の不正値 2 で増える。ケース数は段 2 の報告で実数を書く）。

## 5. 作らないケースと理由

| 対象 | 理由 |
| --- | --- |
| `ERR-DB`・`PRE-DB`・システム エラー | `DEC-CMN-07`：定めない（受注一覧と同じ） |
| 項目単位の検証の文言 | DEC-ORD-56 の答え（定めない。文言を期待にしない）。「空でない」「`text-danger`」だけ確かめる。**文言が本文にあるもの**（`ORD-T3` の違反、`ERR-*`・在庫不足・未出荷の取消・完了メッセージ・取消の確認）は期待にする |
| 数値の上限・桁（送料が `money` を超える）、出荷日が `1753/01/01` より前、取消の加算で在庫が `smallint` を超える | DEC-ORD-56 の ③④⑤の答え（`DEC-ORD-38`・`47` と同じ：定めない）。出荷日の上端 `9999/12/31` は通るケースを作る（3.2。DEC-ORD-58） |
| 在庫が NULL の商品の確定・取消（`Quantity > NULL` の判定、`NULL ± Quantity`） | DEC-ORD-62（答え：定めない）。表示（内訳の在庫が空）だけ 3.1 で確かめる |
| 受注日が NULL の受注の確定（`ORD-T3` の比較） | DEC-ORD-63（答え：定めない） |
| 在庫が不足する商品を列挙するときの区切り・並び | DEC-ORD-61（答え：定めない）。全商品名を含むことだけ確かめる |
| 内訳（I08）の行の並び | 仕様が決めていない（`EV-ORD-401` 手順 5-4 は並びを書かない）。並びは見ない |
| 明細が 0 件の受注の確定・取消 | `ORD-T5` により登録では起こらない。`DEC-CMN-02` と同じ考え方（在庫の更新が無いだけで、受注の更新は同じ）。E2E の一覧状態のケースの、確定しない 24 件は明細なし（表示されるだけ）。確定する 1 件は明細 1 行を持たせ、明細なしの確定には頼らない |
| 商品マスタに無い明細の取消 | 確定は 3.2 で作った（手順 4-1 が内部結合を書く）。取消の手順 5 は `Products` との結合を書かず、商品が無いときの扱いは本文から読めない。通常の操作で起こらない（`DEC-ORD-24` と同じ）ので作らない |
| `Orders.ShipVia` が `Shippers` に無い受注の表示 | 保存は `VAL-EXISTS` で防ぐので通常の操作で起こらない。表示の期待は仕様に無い |
| 未出荷の受注の取消（`ERR-BIZ`）の E2E | `B02` は未出荷で非活性で、画面の操作からは起こらない。B層 `OrdTfShipCancel_未出荷の受注はERR-BIZ` で確かめる |
| 確定の更新件数が 0 件（`EV-ORD-402` 手順 6 の分岐） | 手順 3 で行を取得した後は、順次の呼び出しでは起こらない。同時に更新する 2 つの要求は再現が不安定なので作らない。同じ行バージョンの 2 回目の呼び出し（B層）で、分岐の結果（競合・在庫を二重に引かない）を確かめる |
| 呼び出し元（画面 ID）・受注 ID が無い・不正（`PRE-PARAM`） | 本文に扱いが無い。`DEC-ORD-21` と同じ（定めない） |
| ダイアログを開いたままのメニューなどの離脱 | 対象の仕様の範囲外 |
| 配送管理の出荷追跡画面（`SC-SHP-04`）から | 対象の仕様の範囲外 |
| 確定・取消の CSRF 対策、分離レベル | 仕様に無い（`QUALITY.md` 4 節のレビューの観点）／境界から観測できない |
| 在庫の更新が `UnitsInStock ± Quantity` の SQL の加減算か（読んだ値で書き戻していないか） | 境界から見えない（取消の入力は受注 ID と `RowVersion` だけで、途中の在庫の値を差し込めない）。計画の D層の設計と、段 4 のレビュー（`QUALITY.md` 4 節「読んだ値で書き戻していないか」）で見る。B層 `OrdTfShipCancel_在庫は数量を加算して戻す` は加算の結果だけを確かめる |

## 6. 仕様の決定（答え済み。決めた者：代行。答えは決定のレポートの「判断」のとおり）

本文（`EV-ORD-108`・`205`・`401`〜`404`、`SC-ORD-04`、`HLD/Common.md`、`TableSchema`）と決定記録（`DEC-CMN-01`〜`07`・`DEC-ORD-01`〜`49`）を合わせて読み、**どちらにも決まりが無く、期待が変わるもの**を上げた。答えは「答え」の列（決めた者はすべて代行）。テスト仕様の期待は答えのとおり書いた。
決定 1 は対象の仕様（`test3-testfirst-OrdTfShip.md`）が「規約の解釈として段 1 で仕様の決定に上げる」と書いたもの。DEC-ORD-60 は前の回の決定のレポートの「終わり 4」（画面から送られる値で、B層の業務の検証を外させない）に当たる。

| # | 仕様の ID | 問い | 選択肢（間引かない） | 推奨と理由 | 影響するケース | 答え |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `SC-ORD-04-B01`〜`B03`（配置「ダイアログ下部」）、規約 `cmn-framework.md`「画面共通」「MVC」（メインボタンはフッタ部に 5 つ。画面定義書が別の領域に置いていてもフッタ）、画面遷移 F-5 | 出荷ダイアログのボタン（B01 出荷確定／出荷情報を更新、B02 出荷取消、B03 閉じる）をどこに置くか。規約は「すべての画面のメインボタンはフッタ部に 5 つ」、画面定義書は「ダイアログ下部」、F-5 はダイアログを元画面の上に重ねて表示する。重ねている間も、元画面のフッタの `#btnMain1`〜`#btnMain5`（一覧は「検索」が活性）は画面に在る | 1. ダイアログ自身の下部に、画面定義書どおり B01〜B03 の 3 つを置く。元画面のフッタは変えない（ダイアログは規約の 5 つのスロットの「画面」ではなく、元画面に重ねる部品とみなす） ／ 2. 規約どおり、B01・B02 を元画面と同じフッタ部の `#btnMain1`・`#btnMain2`（`#btnMain3`〜`#btnMain5` は非活性）にし、ダイアログを開いている間は元画面のメインボタンをダイアログのものへ差し替える（閉じたら元に戻す）。B03（遷移だけ）はダイアログ下部 ／ 3. ダイアログの下部に、規約と同じ 5 つのスロット（ダイアログ専用の id `#shipMain1`〜`#shipMain5`。B01・B02 を 1・2、3〜5 は非活性）を置き、B03 は別に置く ／ 4. 定めない（ボタンの位置・id は期待にせず、表示名で探す） | 1。画面定義書がダイアログ下部と明記している。規約の「画面」はヘッダ・左メニュー・コンテンツの Web 画面のレイアウトで、F-5 のダイアログはその上に重なる部品。2 は重なっている間に元画面の `#btnMain1`（検索）の役が入れ替わり、取り違える。3 は使わないスロットを増やすだけ | E2E 2.1 のボタンの id（`#shipMain1`・`#shipMain2`・`#btnShipClose`）と、`一覧_出荷更新で元画面の上に重ねて表示される`・`詳細_出荷更新で…`（元画面のフッタが変わらない）、確定・取消・閉じるのすべての E2E の操作 | GO（3）。**推奨（1）と違う**。ダイアログの下部に、ダイアログ専用の 5 つのメインボタン（`#shipMain1`〜`#shipMain5`）と、別に B03。元画面のフッタは変えない。代行。記録なし（規約の解釈。決定記録に入らない） |
| 2 | `SC-ORD-01-B04`（活性条件「出荷済の行では表示名を『出荷変更』とする」）、`ORD-T2`、`EV-ORD-401` 手順 6（出荷済＝`ShippedDate` が NULL でない） | 一覧の各行のボタンを「出荷変更」と表示する「出荷済の行」は、どの行か。状況の列は「納期遅延」にもなり、出荷が遅れた行は出荷済（`ShippedDate` あり）でも状況が「納期遅延」 | 1. `ShippedDate` が NULL でない行（`ORD-T2`。出荷が遅れた「納期遅延」の行も「出荷変更」） ／ 2. 状況が「出荷済」の行だけ（出荷が遅れた行は「出荷更新」） ／ 3. 定めない（出荷が遅れた行の表示名は期待にしない） | 1。`ORD-T2` が出荷済を `ShippedDate` の有無で定め、ダイアログの `B01` の表示名（「出荷情報を更新」）と活性の条件も同じ。2 だと、一覧が「出荷更新」の行を開くとダイアログが出荷済モードで開く | E2E `一覧_出荷済の行は出荷変更` | GO（1）。代行。記録：DEC-ORD-50 |
| 3 | `EV-ORD-108` 手順 2（「`EV-ORD-102` と同じ条件で一覧を再検索」）、`EV-ORD-102` 手順 3（ページを 1 に戻す）・手順 5、画面遷移 F-5、共通の処理「一覧状態の保持」、`DEC-ORD-04`・`06`・`19`・`23` | ダイアログを閉じた後（確定・取消・閉じる・`ERR-NOTFOUND`）の一覧の再読み込みは、どの状態で再検索するか。「同じ条件」の「条件」が、検索条件だけか、ページ・並べ替えも含むか。`EV-ORD-102` 手順 3 は検索ではページを 1 に戻す | 1. 保持された一覧状態（最後に実行した検索の条件・現在ページ・並べ替え）で再検索する。保持したページが無くなっていれば最終ページ（`DEC-ORD-23`） ／ 2. 検索条件と並べ替えは保ち、ページは 1 に戻す（`EV-ORD-102` の文字どおり） ／ 3. 定めない（ページ・並べ替えは期待にしない。検索条件だけ） | 1。F-5 は「元画面へ戻って一覧を再読み込みする」で、元画面の位置（ページ・並び）を保つのが「戻る」の意味。共通仕様 4 節「検索条件の保持」と `DEC-ORD-19`・`23`（復帰・削除の後）と揃う。2 は 2 ページ目の行を更新するたびに 1 ページ目へ飛ぶ | E2E `確定_一覧状態を保って一覧へ戻る`・`閉じる_一覧の条件・ページ・並べ替えを保って…`・`閉じる_出荷状況で絞った…`・`表示_消された受注は一覧へ…`・`確定_消されていたら…`・`取消_消されていたら…` | GO（1）。代行。記録：DEC-ORD-51 |
| 4 | 共通仕様 7 節 `ERR-NOTFOUND`（「当該モジュールの一覧画面へ戻る」）、`EV-ORD-401`〜`403` のエラーハンドリング、画面遷移 F-4・F-5、`DEC-ORD-28`・`40` | ダイアログの表示（401）・確定（402）・取消（403）で `ERR-NOTFOUND`（他の利用者が受注を消した）になったとき、どの画面へ戻るか。7 節は一覧へ戻るとだけ書き、ダイアログは元画面の上に重なっている（`DEC-ORD-28`・`40` は詳細・編集の場合を「保持された一覧状態で戻る」とした） | 1. ダイアログを閉じて一覧へ戻る：元画面が一覧ならDEC-ORD-51 の答えの状態で一覧を再読み込み、詳細なら保持された一覧状態の一覧へ遷移（`DEC-ORD-28`）。どちらも一覧の `#messageArea` に「対象のデータは存在しません。」（エラー）。入力中の内容は失う ／ 2. ダイアログに留まり、エラーを出す（受注は無いので、閉じるだけが使える） ／ 3. 定めない | 1。7 節の文のまま。`ERR-VALIDATION`（「画面に留まる」）と区別された文言で、`DEC-ORD-28`・`40` と同じ型 | E2E `表示_消された受注は…`（2）・`確定_消されていたら…`・`取消_消されていたら…` | GO（1）。代行。記録：DEC-ORD-52 |
| 5 | 共通仕様 7 節 `ERR-CONFLICT`（「更新を取り消して最新データを再表示する」）、`EV-ORD-402` 手順 3・6、`EV-ORD-403` 手順 3・6、`DEC-ORD-34` | ダイアログの確定・取消で `ERR-CONFLICT` になったとき、何を表示するか。入力中の内容を残すか、保持する `RowVersion`・「表示時点の出荷済／未出荷の別」をどうするか。他の利用者が先に出荷確定していれば、再表示は出荷済モードになる | 1. ダイアログに留まり、DB の最新で再表示する（入力した内容は捨てる。保持する `RowVersion`・出荷済／未出荷の別も最新にする）。エラーを `#shipMessage` に出す ／ 2. 入力を残し、`RowVersion` だけ最新にする ／ 3. ダイアログを閉じ、元画面を再読み込みしてエラーを元画面に出す ／ 4. 定めない | 1。7 節の「最新データを再表示」の文字どおりで、`DEC-ORD-34` と同じ型。2 は他の利用者の変更（例：先に出荷確定された）を見ないまま、画面の古い入力で確定できる（出荷済モードに変わったことも見えない）。3 は元画面の再読み込みの後でダイアログが消え、何が起きたか見えない | E2E `確定_競合は最新を再表示する`・`確定_他の利用者が先に出荷確定したら…`・`取消_競合は最新を再表示する` | GO（1。モード・ボタンの表示名・活性・内訳の表示も最新にする）。代行。記録：DEC-ORD-53 |
| 6 | 共通仕様 7 節 `ERR-BIZ`（「違反内容に応じたメッセージを表示し、処理を行わない」）、`EV-ORD-402` 手順 4-2（在庫不足）、`EV-ORD-403` 手順 4（未出荷の取消） | 在庫不足の `ERR-BIZ` のとき、ダイアログは閉じるか留まるか。留まるなら入力値と `RowVersion` をどうするか（`ERR-VALIDATION` は「入力値を保持したまま画面に留まる」と明記し、`ERR-BIZ` は「処理を行わない」とだけ書く） | 1. ダイアログに留まり、入力値を保持し、エラーを `#shipMessage` に出す（同じ `RowVersion` でやり直せる。内訳の在庫を最新にするかは定めない） ／ 2. 留まり、DB の最新で再表示する（入力は捨てる） ／ 3. ダイアログを閉じ、元画面にエラーを出す ／ 4. 定めない | 1。在庫の補充や他の受注の取消で解消するのを待ってやり直せる。`ERR-VALIDATION` と同じ「業務の違反は画面に留まる」型。2 は在庫が変わったときしか意味がなく入力を捨てる理由が無い（受注は更新されていない） | E2E `確定_在庫不足はダイアログに留まりエラーを出す` | GO（1）。代行。記録：DEC-ORD-54 |
| 7 | `EV-ORD-402` 手順 8、`EV-ORD-403` 手順 8、共通仕様 4 節（メッセージ表示）、`DEC-ORD-22`・`35` | 完了メッセージ（「受注 ID nnn の出荷情報を更新しました。」「受注 ID nnn の出荷を取り消しました。」）を、情報・警告・エラーのどれで出すか。`DEC-ORD-22`・`35` は削除・保存だけを決めている | 1. 情報（`text-info`） ／ 2. 定めない | 1。`DEC-ORD-22`・`35` の理由（操作の成功の知らせは、警告・エラーに当たらない）がそのまま当てはまる | E2E `確定_未出荷から確定して…`・`確定_詳細から確定して…`・`取消_確認して承認すると…`・`取消_詳細から取り消すと…` ほか | GO（1）。代行。記録：DEC-ORD-55 |
| 8 | `VAL-REQUIRED`・`VAL-DATE`・`VAL-NUMERIC`・`VAL-EXISTS`（`EV-ORD-402` 検証内容）、共通仕様 4 節（日付は `yyyy/MM/dd`）、`DEC-ORD-09`・`29`・`30`・`38` | `SC-ORD-04` の入力（出荷日・配送業者・送料）に、受注詳細（登録・編集）の決定を広げるか。①出荷日の形式と空白だけの入力（`DEC-ORD-29` は `SC-ORD-03-I06`・`I07` だけ）②項目単位の検証の文言（`DEC-ORD-30` は `SC-ORD-03` の項目だけ）③数値の上限・桁（`DEC-ORD-38`）④出荷日が `1753/01/01` より前（`datetime` に入らない。`DEC-ORD-10`・`47` は検索条件・受注詳細の日付だけ）⑤取消の加算で `UnitsInStock` が `smallint` の上限（32767）を超えるとき | 1. 広げる：出荷日は `yyyy/MM/dd` の形で存在する日付だけ（`2199-03-10`・`2199/3/10`・`2199/02/30` は違反）、空白だけは未指定とみなし（出荷日・配送業者は `VAL-REQUIRED`）、項目単位の文言と数値の上限・桁は定めない（「空でない」「`text-danger`」だけ期待にする）。④⑤も定めない（実装に任せ、期待にしない） ／ 2. 出荷日は日付として読めれば形は問わない（読めないものだけ違反）。ほかは 1 と同じ ／ 3. 定めない（日付の形式・空白だけのケースを作らない）。④⑤は個別に選べる：④ａ．定めない ／ ｂ．`VAL-DATE` の違反にする ／ ｃ．専用の文言を決める、⑤ａ．定めない ／ ｂ．`ERR-BIZ` にする（何も更新しない）／ ｃ．上限で止める | 1。`DEC-ORD-29`・`30`・`38` の理由（共通仕様 4 節の `yyyy/MM/dd`、文言は仕様に無い、サンプルの操作で起こらない）は出荷日・送料にもそのまま当てはまり、画面ごとの違いが出ない。④は `DEC-ORD-10`・`47` と同じ（サンプルの利用者が入力する範囲ではない）、⑤は在庫が上限近くになる操作がサンプルに無い（ともに定めない） | B 3.2 `出荷日が空白だけ`・`出荷日の形式`・`出荷日が空`・`送料の不正`、E2E `確定_送料の不正値`・`確定_検証エラーは…`、5 章の作らないケース | GO（1。④ａ・⑤ａ）。代行。記録：DEC-ORD-56 |
| 9 | `EV-ORD-402` の `VAL-DATE`（`ORD-T3`。「出荷日は受注日以降を指定してください。」）、`DEC-CMN-04`・`05` | 出荷日が受注日より前のときの違反の表示先。文言は本文にある。比べる受注日は `SC-ORD-04` に入力欄が無い（`I01`〜`I08` に受注日が無い）ので、誤っている入力項目は出荷日の 1 つだけで、関連チェック（`DEC-CMN-04`＝共通メッセージ領域だけ）か項目単位（`DEC-CMN-05`＝項目の直下だけ）かが決まらない | 1. 出荷日（`I05`）の直下だけ（項目単位） ／ 2. 共通メッセージ領域だけ（関連チェック） ／ 3. 項目の直下と共通メッセージ領域の両方 ／ 4. 定めない（文言がダイアログのどこかに出ることだけ） | 1。誤っている入力項目が 1 つなので、`DEC-CMN-05`（項目単位）に当たる。画面に出ていない受注日と比べるだけで、「2 つ以上の項目にまたがる」ではない | B 3.2 `出荷日が受注日より前`（`ErrorFields`／`ErrorMessages` の別）、E2E `確定_出荷日が受注日より前は出荷日の直下` | GO（1）。代行。記録：DEC-ORD-57 |
| 10 | `VAL-DATE`（「日付の前後関係が矛盾しないこと（受注日 <= 出荷日）」）、`ORD-T3` | 出荷日が当日より後（未来）でも受け付けるか。本文の前後関係の検証は `受注日 <= 出荷日` だけで、未来の禁止は書かれていない（この仕様の B層のテストは、他の回と同じく 2199 年の日付を使うので、未来の出荷日を受け付ける前提になる） | 1. 受け付ける（`受注日 <= 出荷日` だけを検証する） ／ 2. 違反にする（`出荷日 <= 当日`） ／ 3. 定めない（未来の出荷日のケースを作らず、B層のテストの日付を過去の日付に変える） | 1。本文の検証は 1 つだけ。禁止するなら本文が書くはず | B 3.2 の確定・取消の全ケース（出荷日 `2199/03/10` など）と `出荷日の上端は通る`（`9999/12/31`。答えが 2 なら消す）。答えが 2 なら、日付を過去（受注日 `2001` 年台）に直す | GO（1）。代行。記録：DEC-ORD-58 |
| 11 | 共通仕様 9.1（「すべての更新系イベントはこの形に従う」：更新時は `SET` 句で `RowVersion` を +1）、`EV-ORD-402` 手順 4-3・`EV-ORD-403` 手順 5（`Products.UnitsInStock` の更新。`RowVersion` に触れていない）、`PRD-T5`、`TableSchema/Products`（`Products` は `RowVersion` を持つ） | 在庫の引き落とし・戻しで、`Products.RowVersion` を +1 するか。手順 4-3・5 は `ProductID` の一致で `UnitsInStock` を更新すると書き、9.1 は「必ず書く」と定める。+1 しないと、商品の編集画面が開いたまま在庫が変わったとき、その保存が競合にならず在庫を上書きする | 1. +1 する（9.1 の定型文。条件は `ProductID` の一致だけ。保持した値での楽観排他は掛けない） ／ 2. +1 しない（本文の文字どおり） ／ 3. 定めない | 1。9.1 が「記述漏れは競合検知を無言で無効化する」と明記している。手順 4-3・5 が書き漏らしたと読む | B 3.2 `確定で在庫を更新した商品の行バージョンが進む`・B 3.3 `取消で在庫を戻した商品の行バージョンが進む`（2 ケース） | GO（1）。代行。記録：DEC-ORD-59 |
| 12 | `EV-ORD-402` 入力（「表示時点の出荷済／未出荷の別」）・手順 4・5、`DEC-ORD-49`（画面が送る値で検証を外さない）、前の回の決定のレポート「終わり 4」 | 画面が送る「表示時点の出荷済／未出荷の別」が、DB の受注（`RowVersion` は一致）の `ShippedDate` と食い違うとき、どうするか。画面の操作では起こらない（`RowVersion` が一致する間は、表示時点の状態と DB は同じ）。起こるのは細工した要求か B層を直接呼んだときだけ。「未出荷」と言いながら DB が出荷済なら在庫を二重に引き、「出荷済」と言いながら DB が未出荷なら在庫の検査と引き落としを省ける | 1. DB の `ShippedDate` を正とし、画面の主張は使わない（DB が出荷済なら出荷情報の更新、未出荷なら出荷確定＝在庫の検査と引き落としをする） ／ 2. 食い違えば `ERR-CONFLICT` にする（何も更新しない） ／ 3. 食い違えば `ERR-BIZ` にする（何も更新しない） ／ 4. 定めない。ただし、どの扱いでも、在庫の二重の引き落とし・検査や引き落としの省略を起こさない（`DEC-ORD-49` と同じ型。期待は 402 手順 4・5 と `PRD-T5` で決まる） | 1。結果が決まっていて、業務の検証（在庫）は DB の状態で必ず行われる。2・3 は偽の文言（競合・業務エラー）を作る。4 は B層の期待が「どれかを満たせばよい」になり弱い | B 3.2 のDEC-ORD-60 の 3 ケース（答えが 4 なら「ErrorFlag = false なら在庫が正しく 1 回だけ変わる／ErrorFlag = true なら何も変わらない」に書き換える）。実装の決定：画面が「表示時点の別」を hidden で持つか Session で持つかは計画（`DisplayedShipped` の入力は残す） | GO（1。DB の `ShippedDate` を正とする）。代行。3 ケースを作る。記録：DEC-ORD-60 |
| 13 | `EV-ORD-402` 手順 4-2（「複数ある場合は全商品名を列挙」） | 在庫が不足する商品が複数あるとき、メッセージの商品名の区切りと並びを決めるか（本文は「全商品名を列挙」とだけ書く） | 1. 区切り・並びを決める（例「、」区切り・明細の商品名の昇順） ／ 2. 定めない（全商品名を含み、不足でない商品名を含まないことだけを期待にする） | 2。区切りは仕様に無く、利用者の判断（不足が分かること）に影響しない | B 3.2 `在庫が不足する商品が複数なら全商品名を列挙する` | GO（2。定めない）。代行。記録：DEC-ORD-61 |
| 14 | `Products.UnitsInStock`（NULL 可）、`EV-ORD-402` 手順 4-1・4-2・4-3、`EV-ORD-403` 手順 5、`PRD-T5`、共通仕様 4 節（NULL は空表示） | 明細の商品の `UnitsInStock` が NULL のとき、確定の在庫の検査・引き落とし、取消の戻しをどう扱うか。Northwind2 の既存の商品 77 件に NULL は無い（`03_InsertData.sql` の Products の INSERT に NULL が無い）。表示（内訳の在庫が空）は 4 節で決まる | 1. 0 として扱う（`Quantity > 0` なので不足の `ERR-BIZ`。取消は `0 + Quantity`） ／ 2. 在庫を管理しない商品として扱う（不足にせず、`UnitsInStock` は NULL のまま） ／ 3. 定めない（NULL の在庫の確定・取消のケースを作らない。内訳の表示だけ確かめる） | 3。既存データに無く、サンプルの操作で起こらない（`DEC-ORD-42` と同じ型）。決めるなら 1（`DEC-CMN-03`〔送料の NULL は 0〕と揃う） | 5 章の作らないケース（答えが 1・2 なら B 3.2・3.3 にケースを足す）。B 3.1 `内訳の在庫がNULLなら空` は `Short` を期待にしない | GO（3。定めない）。代行。記録：DEC-ORD-62 |
| 15 | `ORD-T1`（既存データに NULL があり得る）、`ORD-T3`、`VAL-DATE`（`受注日 <= 出荷日`）、`DEC-ORD-48`（編集でも必須。受注日の NULL を画面の操作では作れない根拠） | 受注日（`Orders.OrderDate`）が NULL の受注で、出荷日の `受注日 <= 出荷日` の検証をどうするか（Northwind2 の 830 件に受注日が NULL の行は無い） | 1. 比べない（違反にしない） ／ 2. 違反にする ／ 3. 定めない（受注日が NULL の受注の確定のケースを作らない） | 3。既存データに無く、サンプルの操作で起こらない（`DEC-ORD-10`・`47`・`42` と同じ型）。受注日の NULL を画面の操作では作れない根拠は `ORD-T1`（登録は必須）と `DEC-ORD-48`（編集でも必須） | 5 章の作らないケース | GO（3。定めない）。代行。記録：DEC-ORD-63 |

### 6.1 決定を待たずに決めたこと（本文・決定記録から読めるので、期待にした）

- **B01 は出荷済でも活性**：画面定義書の `SC-ORD-04-B01` の活性条件は「未出荷のとき。表示名は出荷済のとき『出荷情報を更新』」だが、`EV-ORD-401` 手順 6-3 は出荷済のとき B01 を「出荷情報を更新」として活性にする。出荷済の表示名の定義と、`EV-ORD-402` 手順 5（出荷済の情報更新）は、出荷済で押せることを前提にしているので、手順 6-3 に従う。

- 出荷確定（未出荷）：在庫を `Quantity > UnitsInStock` の行で判定し、ちょうど 0 になるのは通す（`EV-ORD-402` 手順 4-2・`PRD-T5`）。不足のときは受注も在庫も更新しない（共通仕様 7 節のロールバック）。
- 出荷済の更新は在庫を変えず、在庫の不足の検査もしない（手順 5）。取消は `ShipVia`・`Freight` を変えず、`ShippedDate` だけを NULL に戻す（`EV-ORD-403` 手順 6）。
- 検証（手順 1）は、受注の取得（手順 3）より先。違反は全項目まとめて返し、1 件も更新しない（共通仕様 6 節）。
- 送料が空なら 0 で保存する（`DEC-ORD-01` は `SC-ORD-04-I07` を対象に含む）。完了メッセージの文言・取消の確認の文言・各 `ERR-*` の文言は本文のとおり。
- 内訳（I08）の行の並びは仕様に無い（期待にしない）。`SC-ORD-04` の `I03`（状況）は「状況の判定」（`DEC-ORD-27`：時刻の差は定めない。出荷日は日付だけの入力なので、このケースでは時刻を扱わない）。

### 6.2 実装の決定（人に上げない。計画に書く）

- ダイアログを元画面に重ねる作り（部分ビューを取って差し込む・`<dialog>`・iframe など）。E2E が固定するのは、元画面の URL が変わらず、元画面の要素が残り、ダイアログの要素が `ship` で始まる id を持つことだけ（1.2・2.1）。
- `RowVersion` と「表示時点の出荷済／未出荷の別」の保持の方法（hidden か Session か）。どちらでも B層は DB の行で検証する（DEC-ORD-60）。
- 確定・取消の成功後の元画面の再読み込みの方式（リダイレクトして GET。一覧は Session の `OrdTfListState` を使う。ダイアログの再表示は `OrdTfShipInit` をもう一度呼ぶ）。
- 在庫の更新の SQL（`UnitsInStock = UnitsInStock ± Quantity`。更新する商品を `ProductID` の昇順に更新して、同じ商品を逆の順に更新する要求とのデッドロックを避ける）、D層の Dao の分け方。

## 7. 段 2 の確かめ方（予定）

- **Red（B層）**：引数・戻り値クラスに 1.1 のメンバを足し（`OrdTfShipStockLine` の型も）、`OrdTfBusiness` に `OrdTfShipInit`・`OrdTfShipSave`・`OrdTfShipCancel` の**戻り値を設定するだけの空の `UOC_`** を置く。テストは `Assert` で落ちる。net10.0 の `MVC_Sample_Core\MVC_Sample.Tests` で流す。ビルドや呼び出しで落ちるのは Red ではない。net48 は段 4 の後で全件。
- **Red（P層）**：ビルドが通り、一覧・詳細を開ける最小の土台（`Views\OrdTf\Ship.cshtml` の空の土台、一覧の「操作」列・`.btn-ship`・詳細の `#btnShip` は無し）を先に置く。E2E は `Expect` で落ちる。既存の `初期表示_一覧とページ情報` は、期待に「操作」を足した時点で落ちる（実装が 10 列目を足すまで）。
- **最初から通りうるケース**：空の実装は `ErrorFlag = false`・`OrderID = 0`・空のリスト・null を返す。**null や空を期待するケース**（`顧客が無い受注も表示できる`・`未出荷で配送業者が未設定なら未選択`・`送料がNULLなら空`・`出荷済で配送業者が未設定なら未選択`・`内訳の在庫がNULLなら空`）は、表の期待に **`OrderID = その受注`・`Status`・`ShipIsShipped`・`StockLines` の件数** を足して Red にした（空の実装では `OrderID = 0`）。DB を確かめる「通る」側（`送料0は通る`・`出荷日が受注日と同じ日は通る`）も DB の値で Red にする。E2E で最初から通るものは無い。
- テストのファイル：`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`・`TestData.cs`（足す）、`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfShipScreenTests.cs`（新規）・`TestData.cs`（足す）・`OrdTfScreenTests.cs`（2.3 の 1 行）。
