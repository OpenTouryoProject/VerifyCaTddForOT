# テスト仕様（差分）：受注管理の直し OrdTfRevise（ケース2：MVC_Sample_Core ＝ net10.0 ASP.NET Core MVC）

起こした仕様の版：NorthwindDbSampleSpec@9be21c0（ブランチ proxy/ord-management。起こしたのは 36a8026〔PR #5 を main に merge した版〕で、36a8026 → 9be21c0 の差分は決定記録だけ〔`LLD/Decisions/Orders.md` の DEC-ORD-79〜82、`LLD/Decisions/Common.md` の DEC-CMN-09、索引の件数。本文は変わらず、期待も変わらない。6 章の決定 1〜5 の答えを記録したもの〕。前の回の版 b60c760〔OrdTfReport〕と 36a8026 との差分は決定記録 `LLD/Decisions/Orders.md`〔DEC-ORD-24・27・44・66 の決め直し・補い、DEC-ORD-73〜78 の追加〕・`LLD/Decisions/Common.md`〔DEC-CMN-08 の追加〕と、実装の記録 `LLD/ImplementationNotes`〔テストの根拠にしない〕。本文 `HLD/Common.md`・`LLD/EventSpec/Orders.md`・`LLD/UI_ElementsAndEventList/Orders.md`・`LLD/TableSchema/Orders.md` は変わっていない。本文と決定記録を合わせて読んだ。食い違うときは決定記録が正）
規約：`docs/spec/cmn-framework.md` の「画面共通」「MVC」（VerifyCaTddForOT@1c09efa。「画面共通」に足された 2 点を根拠にする：Web 画面の日付の入力項目は `<input type="date">`・入力途中の値は形式の違反／遷移だけのボタンはメインボタンに含めない）、`docs/spec/cmn-testfirst.md`、`docs/spec/test3-testfirst-OrdTfRevise.md`、`docs/plan/cmn-testfirst.md` の「B層を共用する構成」
前の回のテスト仕様：[受注一覧](./test3-testfirst-OrdTf-mvc-core-tests.md)・[詳細（照会・削除）](./test3-testfirst-OrdTfDetail-mvc-core-tests.md)・[詳細（登録・編集）](./test3-testfirst-OrdTfEdit-mvc-core-tests.md)・[出荷ダイアログ](./test3-testfirst-OrdTfShip-mvc-core-tests.md)・[帳票](./test3-testfirst-OrdTfReport-mvc-core-tests.md)（以下、それぞれ「一覧」「詳細」「編集」「出荷」「帳票」。承認・実装済み。**この文書はそれらの差分**で、書いていないケースは変えない）。決定のレポート：[test3-testfirst-OrdTfRevise-mvc-core-decisions.md](./test3-testfirst-OrdTfRevise-mvc-core-decisions.md)

対象：`SC-ORD-01`〜`05`（新しい画面は作らない）。`EV-ORD-101`・`102`（状況・納期遅延の抽出）、`EV-ORD-201`・`203`（詳細・削除）、`EV-ORD-301`・`306`・`307`・`308`（編集）、`EV-ORD-401`〜`403`（出荷）、`EV-ORD-501`（帳票）、`EV-ORD-104`（ページ番号の入力）と、規約の日付の入力・遷移だけのボタン。
範囲外：ケース1・3 の画面の直し、Windows Forms、商品の画面（在庫が NULL の扱い `DEC-ORD-62` は定めないまま）。

> **この回の根拠の ID**：決定記録の `DEC-ORD-24`・`27`・`44`・`66`・`73`〜`78`、`DEC-CMN-08`、規約（`cmn-framework.md`「画面共通」）。規約を根拠にするケースは根拠の列に「規約：日付の入力」「規約：遷移だけのボタン」と書く。
> **決めた者**：決め直し・足した決定（DEC-ORD-24・27・44・66・73〜78、DEC-CMN-08）は作者が決めた（決定記録のレビュー OpenTouryoProject/NorthwindDbSampleSpec#5）。この回で人に上げた仕様の決定 1〜5 は 6 章に答えと決定記録の ID を書いた（決定 1 = DEC-ORD-79、決定 2 = DEC-ORD-80、決定 3 = DEC-ORD-81、決定 4 = DEC-ORD-82、決定 5 = DEC-CMN-09。根拠の列は DEC-* で書く）。

## 0. 進め方の前提

- 作るのは、**ケース2（MVC_Sample_Core）の P層・共用の B層（`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`・`TestData.cs`）・SQL（`resource\Sql\`）の直し**と、そのテストの変更・追加。B層・SQL は共用なので、ケース1・3 の受注一覧の状況にも効く（4 章）。B層のテストは net10.0（`MVC_Sample_Core\MVC_Sample.Tests`）→ net48（`MVC_Sample\MVC_Sample.Tests`）の順に、同時にでなく全件流す。
- テストのプロジェクト・アプリの起動・ログイン・ブラウザ・日付の範囲（B層 2199 年・E2E 2198 年）・並行実行は一覧の 1 章のとおり（新しいプロジェクトは作らない）。
- **テストのデータは、テストが自分で作って消す。** 商品マスタに無い明細は、テストが作った受注に、テストが作って `DeleteProduct` で消した商品 ID の明細（外部キーは無い）を入れて作る。始めと終わりに 受注 830・明細 2155・顧客 91・商品 77・既存商品の在庫の合計 3119・未出荷 21 件が同じか確かめる手順は計画に入れる（テスト仕様の範囲外）。
- 表示のメッセージ・文言は仕様の文言そのまま（`DEC-CMN-01`）。この回で期待にする文言：「商品マスタに無い商品の明細があります。」（`DEC-ORD-73`）、「（商品マスタに無い：ID n）」（同）、「出荷済の受注は削除できません。」（`DEC-ORD-78`）、「受注日は yyyy/MM/dd の形式で指定してください。」（`DEC-ORD-09`。受注一覧の日付の形式の違反。入力途中の値にも同じ文言）。
- **期待値の変更**（2 章の「変える」）を含む。コミットには `Expectation-Change:` を付け、突き合わせ役に通す（`AGENTS.md`）。理由には変わった決定の ID・規約の節と OpenTouryoProject/NorthwindDbSampleSpec#5 を書く。

## 1. テストが固定する契約（実装の決定。振る舞いは変えない。段 4 の実装はこれに従う）

### 1.1 日付の入力：P層で受けた形を変換して B層に渡す（実装の決定）

`<input type="date">` は `yyyy-MM-dd` で送る。**B層は今のまま `yyyy/MM/dd` だけを受け付け、P層が `yyyy-MM-dd` → `yyyy/MM/dd` に直して B層に渡す**（B層のテストの日付の形式のケース〔`2199-01-05`・`2199/1/5` などの違反〕は変えない。B層に直に渡る入力の検証は今のまま）。**変換は `yyyy-MM-dd` に厳密に合う値（`^\d{4}-\d{2}-\d{2}$`）だけを行い、合わない値（5 桁以上の年など）は変換せずそのまま B層に渡す（B層が `yyyy/MM/dd` でないので形式の違反にする）。****画面の入力欄に出す値（再描画・初期値・復元）は `yyyy-MM-dd`**（`type="date"` の `value` の形）。画面の**表示のテキスト**（詳細の `#orderDate`・`#shippedDate`、出荷ダイアログの `#shipRequiredDate`、一覧の日付の列など）は `yyyy/MM/dd` のまま。
入力途中の値（`validity.badInput`。ブラウザは空として送る）は、**画面の script が送信を止め**、項目の直下のエラー（`#<id>Error`）に出す（未入力として扱わない）。文言は、受注一覧は `DEC-ORD-09` の文言、編集・出荷は空でない文言（`DEC-ORD-30` と同じく文言を期待にしない）。**ページ移動・並べ替えの操作（入力欄を使わない操作）は止めない**（既存の「検証エラーの後も直前の検索条件で移る」を保つ）。

### 1.2 B層

**足すメンバ**（既存は変えない）

| 対象 | メンバ | 意味 |
| --- | --- | --- |
| `OrdTfDetailLine`（詳細・出荷の内訳の行）・`OrdTfEditLine`（編集の明細の行） | `MissingProduct`（bool） | 商品マスタに無い商品の明細のとき true（`Order Details` の `ProductID` が `Products` に無い）。それ以外は false |

**商品マスタに無い明細の行の値**（`Products` を内部結合から外部結合にして読む。**画面ごとの表示の形は B層が決めて返す**）

| 画面（`MethodName`） | `ProductName` | `UnitsInStock` | 備考 |
| --- | --- | --- | --- |
| 詳細（`OrdTfDetail`）・編集（`OrdTfEditInit`）・出荷の内訳（`OrdTfShipInit` の `StockLines`） | `（商品マスタに無い：ID n）`（n は `Order Details.ProductID`。`DEC-ORD-73`） | null | 「（廃番）」は付けない。`MissingProduct` = true、`Discontinued` = false。`Short` = false（強調しない。`DEC-ORD-77`） |
| 帳票（`OrdTfReport`） | `n` だけ（`ProductID` の値の文字列。`DEC-ORD-74`） | （帳票は出さない） | 注記・警告は出さない |

- 単価・割引率・数量・価格・明細金額は、`Order Details` の値だけで決まる（商品マスタを要しない。`DEC-ORD-24`）。小計・合計に含める（詳細・編集・帳票）。
- 並び：**商品名のある明細を商品名の昇順 → その後に商品マスタに無い明細を `ProductID` の昇順**（`DEC-ORD-75`。詳細・編集・出荷の内訳・帳票。SQL Server は NULL を昇順の先頭に並べるので、その並びを使わない）。
- **警告**（「商品マスタに無い商品の明細があります。」）は P層が `MissingProduct` の行の有無から出す（B層は戻り値に警告の文を持たない）。出す画面は詳細・編集（`DEC-ORD-73`）。出荷ダイアログには出すかを定めない（DEC-ORD-79：定めない）、帳票は出さない（`DEC-ORD-74`）。編集では、商品マスタに無い行が残る間は消さず、無くなったら消す（DEC-ORD-82 (b)）。
- **削除の出荷済の検査**（`OrdTfDelete`）：`Orders` を `RowVersion` 条件付きで消す処理と同じトランザクションの中で、DB の `Orders.ShippedDate` を読み、NULL でなければロールバックして業務エラー（`ERR-BIZ`。**`RowVersion` の検査との先後はDEC-ORD-80 に従う（定めない）**〔テストは先後に依らない〕。`ErrorFlag = true`、`ErrorMessage` =「出荷済の受注は削除できません。」）。**引数に「出荷済か」を足さない**（画面の活性・送る値を根拠にしない。`DEC-ORD-78`）。受注が無いときは従来どおり `ERR-NOTFOUND`。詳細の「削除」の活性は、`OrdTfDetail` の `CanEdit`（`ShippedDate` が NULL か）を編集と同じに使う（戻り値のメンバは足さない）。
- **編集の保存の `VAL-EXISTS`（商品）**（`OrdTfSave`）：読み込み時から存在し商品を変えていない行には当てない。**その行に当たるかは、画面が送る `Original*` でなく、DB の `Order Details`（`OrderID`・`ProductID` の一致）で確かめる**（`DEC-ORD-76`・`DEC-ORD-49` の原則）。それ以外（登録モードの行・新しい行・商品を変えた行）には従来どおり当てる。
- 状況の判定（`JudgeStatus`）と、一覧の出荷状況「納期遅延」の抽出（`OrdTfSearch.xml`・`OrdTfCount.xml`）は、**日付の単位で比べる**（`DEC-ORD-27`）：出荷済は `出荷日の日付 > 要求納期の日付` のとき納期遅延、未出荷は `要求納期の日付 < 当日` のとき納期遅延。「当日」は `DateTime.Today`（変えない）。

### 1.3 P層（MVC）の id・class（追加。既存の指定は変えない）

| 対象 | 指定 |
| --- | --- |
| 日付の入力欄 | 一覧：`#OrderDateFrom`・`#OrderDateTo`、編集：`#OrderDate`・`#RequiredDate`、出荷ダイアログ：`#shipShippedDate` は `<input type="date">`（`type` 属性が `date`）。値は `yyyy-MM-dd` |
| ページ番号の入力（一覧。`DEC-CMN-08`） | `<input id="pageNo" type="text">`（数値でない文字も入れられるよう `number` にしない）。DOM の並びは `#btnPageFirst`・`#btnPagePrev`・`#pageNo`・`#btnPageNext`・`#btnPageLast`。値は今のページ番号。Enter でそのページへ移る。活性・非活性は期待にしない（仕様に無い。5 章）。既存の 4 つのボタン・`#pageInfo` は変えない |
| 警告（商品マスタに無い明細） | 詳細・編集の `#messageArea`：「商品マスタに無い商品の明細があります。」、class に `text-warning` を持ち `text-danger`・`text-info` を持たない |
| 編集の商品の選択肢（商品マスタに無い行） | 該当の行の `.line-product` に限り、`（商品マスタに無い：ID n）` の `option`（value = n）を加えて選択状態にする。**ほかの行の選択肢には加えない**。`.line-stock` は空 |
| 在庫超過の警告（`DEC-ORD-44`） | 既存の指定（`#messageArea`・`text-warning`・「在庫数を超えています。」）のまま。消す時機だけ変わる（3.2） |
| 詳細の削除（`#btnMain1`） | 出荷日が NULL でないとき非活性（`#btnEdit` と同じ条件） |

### 1.4 テストのデータ（`TestData`。B層用・E2E 用の両方に足す。既存の呼び出しは変えない）

| 足すもの | 内容 |
| --- | --- |
| `NewOrder` の日付 | `requiredDate`・`shippedDate` に時刻付きの `DateTime` を渡せる（既存の引数。時刻を切らずに入れる） |
| `SetShippedDate(orderId, date)`（E2E 用） | 他の利用者が `ShippedDate` だけを設定したことを模す：`UPDATE Orders SET ShippedDate = @d WHERE OrderID = @o`（**`RowVersion` は変えない**。画面の活性・送る値でなく DB の値を根拠にする `DEC-ORD-78` の確かめで、`ERR-CONFLICT` でなく `ERR-BIZ` に当てるため） |
| 商品マスタに無い明細 | 商品を `NewProduct` で作って明細に入れ、`DeleteProduct` で消す（既存の部品。**後始末は、無い行の削除でも落ちない**。商品の後始末は明細・受注の後） |
| E2E の日付の入力 | `type="date"` への入力は `FillAsync(sel, "yyyy-MM-dd")`。**入力途中の値**は、欄を `ClickAsync` して**最初の欄に数字を 1 つだけ**入れる（`PressSequentially("1")`。年月日の欄の順は環境で変わるので、どの順でも 1 欄だけが埋まり、残りは空）。**操作の前に `Expect` で `validity.badInput` = true かつ `value` = "" を確かめる**（入力途中になっていないテストが、空のまま通るのを防ぐ） |

## 2. 前の回のケースの変更（期待値の変更。漏れなく挙げる）

種別：**変える**（期待・入力の形を変える）／**付け替える**（根拠を変える）／**消す**。トレーラーの理由は、変えた決定の ID・規約の節と OpenTouryoProject/NorthwindDbSampleSpec#5。

### 2.1 B層（`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`）

| # | 元のケース（仕様） | 種別 | 変わる点 | 根拠 |
| --- | --- | --- | --- | --- |
| B-1 | `OrdTfDetail_商品マスタに無い明細は含めない`（詳細 3.1。`DEC-ORD-24` 旧） | **変える**（名前を `OrdTfDetail_商品マスタに無い明細も含める` に） | 入力・前提は同じ（存在する商品の明細 `10.00`・数量 2、消した商品の明細 `100.00`・数量 1、送料 `5.00`）。**期待を「Lines が 1 行・Subtotal 20.00・Total 25.00」から「Lines が 2 行（存在する商品の行、次に消した商品の行）・Subtotal = 120.00・Total = 125.00」へ**。消した商品の行：ProductName = `（商品マスタに無い：ID n）`、UnitPrice = 100.00・Discount = 0・Price = 100.00・Quantity = 1・UnitsInStock = null・Amount = 100.00・MissingProduct = true。存在する商品の行は MissingProduct = false | EV-ORD-201 手順 3・4・6, DEC-ORD-24, DEC-ORD-73（`[Basis("DEC-ORD-24")]` は残す。`DEC-ORD-73` を足す） |
| B-2 | `OrdTfReport_商品マスタに無い明細は含めない`（帳票 3.1。`DEC-ORD-66` 旧） | **変える**（名前を `…も含める` に） | 同じ入力。**期待を「Lines が 1 行・Subtotal 20.00・Total 25.00」から「Lines が 2 行（存在する商品の行、次に消した商品の行）・Subtotal = 120.00・Total = 125.00」へ**。消した商品の行：ProductName = `n`（商品 ID の文字列だけ。「商品マスタに無い」を含まない）、UnitPrice = 100.00・Quantity = 1・Amount = 100.00 | EV-ORD-501 手順 3・4, DEC-ORD-66, DEC-ORD-74（`[Basis("DEC-ORD-66")]` は残す。`DEC-ORD-74` を足す） |
| B-3 | `OrdTfShipInit_商品マスタに無い明細は内訳に含めない`（出荷 3.1） | **変える**（名前を `…は内訳に出す` に） | 同じ入力（`Sc`＋消した商品 Z の明細）。**期待を「StockLines が 2 件（P1・P2）。Z を含まない」から「StockLines が 3 件（P1・P2・Z）」へ**。Z の行：ProductName = `（商品マスタに無い：ID n）`・Quantity = Z の数量・UnitsInStock = null・Short = false。P1・P2 の行は変えない | EV-ORD-401 手順 5-4, DEC-ORD-77, DEC-ORD-73 |
| B-4 | `OrdTfShipSave_商品マスタに無い明細があっても確定できる`（出荷 3.2。`[Basis("DEC-ORD-24")]`） | **付け替える**（期待は変えない） | 根拠を `DEC-ORD-24`（旧「含めない」の読み）から `DEC-ORD-77`（確定の側）へ。`[Basis("DEC-ORD-24")]` を外して `[Basis("DEC-ORD-77")]` を足す（根拠の削除に当たる） | EV-ORD-402 手順 4-1, DEC-ORD-77 |

**変えない既存の B層のケース（確かめた）**：`OrdTfDelete_*` の 6 メソッドは、どれも出荷日 NULL の受注を消す（`DEC-ORD-78` の影響を受けない）。`OrdTfDetail_状況の判定`・`OrdTfShipInit_状況の判定`・`OrdTfSearch_状況の判定`・`OrdTfSearch_出荷状況で絞る` の既存の行・TestCase は、`DEC-ORD-27` の決め直しでも期待が変わらない（時刻 00:00 だけで、日付の単位でも同じ結果）。足すのは 3.1 のみ。

### 2.2 画面の E2E（`MVC_Sample_Core\MVC_Sample.E2ETests\`）

**(a) 日付の入力欄が `type="date"` になることによる、入れ方・読み方の変更**（入力は `yyyy-MM-dd`。期待の値の形が変わる。表示のテキストは変えない）

| # | クラス | 元のケース | 変わる点 |
| --- | --- | --- | --- |
| E-1 | `OrdTfScreenTests` | `検索_すべての条件の指定` | `#OrderDateFrom` に `2198-05-15`・`#OrderDateTo` に `2198-05-20` を入れる。操作後の入力のまま：`ToHaveValue` が `2198-05-15`・`2198-05-20` |
| E-2 | 同 | `検索_開始が終了より後は共通メッセージ領域` | 入力 `2198-03-11`・`2198-03-10`。入力のまま：同じ形（`2198-03-11`・`2198-03-10`） |
| E-3 | 同 | `検索_日付の形式違反は該当項目の直下だけ` | 入力を `abc`（入れられない）から**開始に入力途中の値**へ。期待：`#OrderDateFromError` が「受注日は yyyy/MM/dd の形式で指定してください。」、`#OrderDateToError` は空、`#messageArea` は空。**「`#OrderDateFrom` は `abc` のまま」を「入力途中のまま（`validity.badInput` が true のまま）」へ** |
| E-4 | 同 | `検索_終了の形式違反と両方の違反` | (1) `#OrderDateTo` に `2198-01-05`（旧は形式の違反。今は有効な日付）を、**終了に入力途中の値**へ。(2) `#OrderDateFrom` に `2198/1/5`（入れられない）を、**開始にも入力途中の値**へ。期待は同じ（(1) `#OrderDateToError` が同じ文言・`#OrderDateFromError` は空・`#messageArea` は空。(2) 両方の `…Error` が同じ文言・`#messageArea` は空） |
| E-5 | 同 | `検索_検証エラーでは直前の一覧を残す`（TestCase：(1) 形式違反 (2) 前後関係） | (1) の入力を `abc` から**入力途中の値**へ。入力のまま：(1) 入力途中のまま、(2) `2198-03-11`・`2198-03-10`。ほかの期待（一覧 2 行のまま・`#totalCount`・`#pageInfo`・`#CustomerID`）は同じ |
| E-6 | 同 | `ページ切替_検証エラーの後も直前の検索条件で移る`（TestCase：(1) 形式違反 (2) 前後関係） | (1) の入力を**入力途中の値**へ、(2) の入力を `2198-03-11`・`2198-03-10` へ。期待は同じ（次へ後に「2 / 3 ページ」・先頭行 = i = 25。**入力途中のエラーが出ていても、次へで移る**） |
| E-7 | 同 | `並べ替え_検証エラーの後も並べ替えを維持する` | `#OrderDateFrom` の入力を `abc` から**入力途中の値**へ。`#OrderDateFromError` の文言・次へ後の期待は同じ |
| E-8 | `OrdTfDetailScreenTests` | `一覧_詳細から戻ると検索条件・ページ・並べ替えが復元される` | 入力 `#OrderDateFrom` = `2198-01-06`・`#OrderDateTo` = `2198-02-14`。復元の期待：`ToHaveValue` が `2198-01-06`・`2198-02-14`（旧 `2198/01/06`・`2198/02/14`） |
| E-9 | `OrdTfEditScreenTests` | `登録_初期表示` | `#OrderDate` の期待を 今日（`yyyy/MM/dd`）から 今日（`yyyy-MM-dd`）へ。`#RequiredDate` は空のまま |
| E-10 | 同 | `編集_読み込み` | `#OrderDate` = `2198-03-05`・`#RequiredDate` = `2198-04-01`（旧 `2198/03/05`・`2198/04/01`） |
| E-11 | 同 | `保存_登録に成功すると詳細へ遷移する` | 入力 `#OrderDate` = `2198-03-05`・`#RequiredDate` = `2198-04-01`。詳細の `#orderDate` = `2198/03/05`（表示のテキストは変わらない） |
| E-12 | 同 | `保存_検証エラーは項目の直下と共通メッセージ領域に出て画面に留まる` | 入力 `#OrderDate` = `2198-04-02`・`#RequiredDate` = `2198-04-01`。「入力値が保持される」の `#OrderDate` の期待：`2198-04-02`（旧 `2198/04/02`） |
| E-13 | `OrdTfShipScreenTests` | 出荷日を入れる・読むすべてのケース：`表示_未出荷`・`表示_詳細から開いても同じ内容`・`表示_出荷済`・`確定_未出荷から確定して一覧へ戻る`・`確定_詳細から確定すると詳細が最新になる`・`確定_検証エラーはダイアログに留まり項目の直下に出る`・`確定_送料の不正値`・`確定_出荷日が受注日より前は出荷日の直下`・`確定_検証エラーの後に直せば確定できる`・`確定_在庫不足はダイアログに留まりエラーを出す`・`確定_競合は最新を再表示する`・`確定_他の利用者が先に出荷確定したら出荷済として再表示する`・`確定_消されていたら一覧へERR_NOTFOUND`・`確定_出荷済の情報を更新する`・`確定_一覧状態を保って一覧へ戻る`・`確定_成功の後に再読み込みしても二重に確定しない`・`閉じる_入力は破棄され開き直すと初期値` | `#shipShippedDate` に入れる値を `2198/05/01` などから `2198-05-01` などへ（`2198/02/28` → `2198-02-28`、`2198/04/10` → `2198-04-10`）。読む期待を `2198/04/05`・今日（`yyyy/MM/dd`）などから `2198-04-05`・今日（`yyyy-MM-dd`）へ。空を入れる（`FillAsync("")`）のは同じ。`#shipRequiredDate`（表示のテキスト）・一覧の日付の列は `yyyy/MM/dd` のまま |

**(b) 出荷済の受注の削除（`DEC-ORD-78`）**

| # | クラス | 元のケース | 変わる点 |
| --- | --- | --- | --- |
| E-14 | `OrdTfDetailScreenTests` | `詳細_メインボタンと編集の活性`（TestCase：出荷日 NULL／`2198/03/10`） | **`#btnMain1`（削除）の期待を「どちらも活性」から「出荷日 NULL のとき活性・出荷日ありのとき非活性」へ**（キャプション「削除」は同じ。`#btnMain2`〜`#btnMain5` は非活性・`#btnEdit` の活性・`#btnBack` は同じ） |
| E-15 | `OrdTfShipScreenTests` | `確定_詳細から確定した後の削除は競合しない` | **期待を変える**（名前を `確定_詳細から確定すると削除は非活性になる` に）。操作は「詳細を開く → ダイアログを開く → 確定」まで同じ（**ただし確定の出荷日の入力を `2198/05/01` から `2198-05-01` に変える**。E-13 と同じ）。**その後「`#btnMain1`（削除）→ 承認 → 一覧が表示され『受注 ID nnn を削除しました。』・DB に o が無い」を、「`#btnMain1` が非活性（`#btnEdit` も非活性）・`#status` = 「出荷済」・DB に o がある」へ**。`ERR-CONFLICT` にならないことの確かめ（保持する `RowVersion` の更新）は、3.2 の `取消_詳細から取り消した後の削除は競合しない` へ移す |
| E-16 | 同 | `閉じる_詳細は再読み込みする` | **期待を変える**。操作は「詳細を開く → ダイアログを開く →（テストが `ShipOrder`）→ `#btnShipClose`」まで同じ。**後半の「続けて `#btnMain1`（削除）→ 承認 → 一覧・削除メッセージ・DB に o が無い」を、「`#btnMain1` が非活性」へ**。閉じた後の期待（`#shipDialog` が見えない・`#shippedDate` = 今日・`#status` が「未出荷」でない・`#btnEdit` が非活性・`#messageArea` が空）は同じ。`RowVersion` の更新の確かめは 3.2 の `閉じる_詳細の再読み込みの後の削除は競合しない` へ移す |

### 2.3 仕様の文書の直し（コードの期待ではない）

- 一覧のテスト仕様 `test3-testfirst-OrdTf-mvc-core-tests.md` の見出し（9 列のまま）を 10 列に直した（2 章の `受注一覧（I06）`の表、4.1 の `初期表示_一覧とページ情報`、8.1・8.3 の「操作」列）。**コードの期待は OrdTfShip の回で直し済み**（出荷 2.3。`Expectation-Change` も同回で付けた）。この回のコミットでは、文書の整合のみ（期待値の変更ではない）。
- **前の回の文書（編集・出荷・詳細のテスト仕様）は変えない。この差分の文書が置き換える**：編集 5 章の「編集の読み込み時に商品マスタに無い明細」（表示しない）は、`DEC-ORD-76` で決め直したので、3.1・3.2 のケースに置き換える。出荷 5 章の「内訳（I08）の行の並び」（仕様が決めていない）は、`DEC-ORD-75` で決まったので、3.1 の `OrdTfShipInit_内訳は…並び` にした。出荷 5 章の「商品マスタに無い明細の取消」（本文から読めない）は、`DEC-ORD-77` で決まったので、3.1 の `OrdTfShipCancel_…` にした。詳細 6 章の決定 11（`DEC-ORD-27`：定めない）と、5 章の「状況の判定で、同じ日で出荷日の時刻が要求納期より後のとき」は、決め直したので、3.1 の `OrdTfDetail_状況の判定` に足した行が答えを持つ。編集 `在庫超過_警告が出て入力は妨げない` の「数量を `5` に直した後の `#messageArea` は期待にしない」は、`DEC-ORD-44` の補いで決まったので、3.2 で期待にする（既存のケースは変えず、確認を足す）。

## 3. 足すケース

「前提」の `Sc`・`Sh` などの記号は、出荷 3 章の定義のとおり（`Sc`＝未出荷、`Sh`＝出荷済）。商品の目印・日付は一覧・詳細のとおり。**足すケースは期待値の変更ではない**（トレーラーは 2 章のケースだけ）。

### 3.1 B層のルート メソッドのテスト（`OrdTfBusinessTests`。net10.0 → net48 の順に流す）

| ケース | 根拠 | 前提 | 入力 | 期待 |
| --- | --- | --- | --- | --- |
| OrdTfDetail_状況の判定（**TestCase を 5 行足す**。時刻付き。要求納期／出荷日／期待） | EV-ORD-201 手順 5, ORD-T7, DEC-ORD-27 | 受注 1 件（要求納期・出荷日は各値。今日＝`DateTime.Today`） | OrderID = その受注 | ① `2001/01/01 00:00`・`2001/01/01 15:30`／**出荷済**（同じ日で時刻が後は遅延にしない）。② `2001/01/01 15:30`・`2001/01/01 10:00`／出荷済。③ `2001/01/01 15:30`・`2001/01/02 00:00`／納期遅延（日が後なら遅延）。④ 今日 15:30・NULL／未出荷（当日は遅延でない）。⑤ 今日 − 1 日 15:30・NULL／納期遅延 |
| OrdTfShipInit_状況の判定（**TestCase を 5 行足す**。同じ 5 行） | EV-ORD-401 手順 3, ORD-T7, DEC-ORD-27 | 受注 1 件（未出荷の ④・⑤ は出荷日 NULL） | OrderID = その受注 | Status が上の 5 行の「期待」のとおり（①②③ は ShipIsShipped = true） |
| OrdTfSearch_状況の判定は日付の単位で比べる | EV-ORD-101 手順 5, ORD-T7, DEC-ORD-27 | 顧客 A の受注 5 件（受注日 `2199/03/01`）：i = 要求納期 `2001/01/01 00:00`・出荷日 `2001/01/01 15:30`、j = `2001/01/01 15:30`・`2001/01/02 00:00`、k = `2001/01/01 15:30`・`2001/01/01 10:00`、l = 今日 15:30・NULL、m = 今日 − 1 日 15:30・NULL | CustomerID = A、PageNo = 1 | 行ごとの Status：i = 出荷済、j = 納期遅延、k = 出荷済、l = 未出荷、m = 納期遅延 |
| OrdTfSearch_出荷状況の納期遅延は日付の単位で比べる（TestCase：「納期遅延」／「出荷済」／「未出荷」） | EV-ORD-102 手順 2, SC-ORD-01-I05, DEC-ORD-27 | 上と同じ 5 件 | CustomerID = A、ShippedStatus = 各値 | Rows の集合と TotalCount：「納期遅延」= j・m（2 件。**i を含まない**）、「出荷済」= i・j・k（3 件。出荷日の有無だけで絞る）、「未出荷」= l・m（2 件）。TotalCount も同じ件数（`OrdTfCount.xml` の条件を確かめる） |
| OrdTfDetail_商品マスタに無い明細は商品名のある明細の後に商品IDの昇順 | EV-ORD-201 手順 3, DEC-ORD-75 | 受注 1 件に、商品 A・M（目印）と、消した商品 Za・Zb（`Za` の ProductID < `Zb`）の明細。**M・Zb・A・Za の順に登録（商品を作る順：Za・Zb を先に作り〔ID は名前のある商品より小さく、Za < Zb〕、名前のある商品が複数なら M を A より先に作る。ID の順と名前の順を逆にする）**する | OrderID = その受注 | Lines の並びが A・M・Za・Zb（商品名の昇順 → 商品マスタに無い明細を ProductID の昇順。NULL 先頭の並びでない）。Za・Zb の ProductName = `（商品マスタに無い：ID n）`（n は各 ID） |
| OrdTfDetail_商品マスタに無い明細の金額は詳細・帳票・一覧の合計で同じになり丸めも同じ | EV-ORD-201 手順 4・EV-ORD-501 手順 4・EV-ORD-101 手順 5（合計金額）, DEC-ORD-24, DEC-ORD-66, 共通仕様 3 節（明細金額は小数第 3 位を四捨五入し、その値を合算） | 顧客 A の受注 1 件（送料 `10.00`）に、存在する商品の明細（単価 `20.00`・割引 0・数量 1）と、消した商品の明細（**単価 `0.50`・割引 `0.15`・数量 3**。`0.425 × 3 = 1.275`）。期待値はテストが入れた値から C# で求める（明細金額 = `Math.Round(単価 × (1 − 割引) × 数量, 2, MidpointRounding.AwayFromZero)`。20.00 + 1.28 + 送料 10.00 = 31.28） | `OrdTfDetail`（OrderID）、`OrdTfReport`（OrderID）、`OrdTfSearch`（CustomerID = A、PageNo = 1）を呼ぶ | 3 つとも、`OrdTfDetail` の Total = `OrdTfReport` の Total = `OrdTfSearch` のその行の合計金額 = 31.28（求めた値。丸めが 1.28 で、偶数丸め・価格を先に丸める 1.29 などでない）。Subtotal = 21.28（詳細・帳票）。消した商品の行の Amount = 1.28 |
| OrdTfDelete_出荷済の受注は削除できない（TestCase：出荷日 `2001/01/01`／今日） | EV-ORD-203, ERR-BIZ, DEC-ORD-78 | 出荷日が NULL でない受注 1 件（明細 2 行。RowVersion = 1） | OrderID = その受注、RowVersion = 1（現在の値） | ErrorFlag = true、ErrorMessage =「出荷済の受注は削除できません。」。`CountOrders` = 1、**`CountDetails` = 2（明細も消えない＝ロールバック）**、`GetRowVersion` = 1、`OrdTfDetail` の内容が削除の前と同じ。**`ERR-NOTFOUND`・`ERR-CONFLICT` の文言ではない** |
| OrdTfEditInit_商品マスタに無い明細も明細に出して小計に含め並びは商品IDの昇順 | EV-ORD-301 手順 3-4・3-5・5, DEC-ORD-76, DEC-ORD-24, DEC-ORD-73, DEC-ORD-75 | 受注 1 件（送料 `5.00`）に、存在する商品 A（`10.00`・割引 0・数量 2）と、消した商品 Za・Zb（`Za` < `Zb`。`100.00`・0・数量 1／`20.00`・0・数量 3）。**A・Zb・Za の順に登録（商品を作る順：Za・Zb を先に作り〔ID は名前のある商品より小さく、Za < Zb〕、名前のある商品が複数なら M を A より先に作る。ID の順と名前の順を逆にする）** | Mode = "edit"、OrderID = その受注 | ErrorFlag = false。EditLines が 3 行で A・Za・Zb の順。A：MissingProduct = false。Za・Zb：MissingProduct = true、Discontinued = false、ProductName = `（商品マスタに無い：ID n）`、UnitsInStock = null、UnitPrice・Discount・Quantity・RowVersion = 登録した値（RowVersion = 1）。Subtotal = 180.00（20 + 100 + 60）、Total = 185.00。ProductOptions に Za・Zb を含まない |
| OrdTfSave_編集で商品マスタに無い明細を残して保存できる（TestCase：その行は変えない／数量を変える） | EV-ORD-308 検証内容（`VAL-EXISTS`）・手順 4-3, DEC-ORD-76, OD-T6 | o：明細 A と、消した商品 Z（数量 2・`50.00`）。編集の入力は `OrdTfEditInit` で読んだ状態を模す（Z の行：`OriginalProductID` = Z・`ProductIDText` = Z・`Original*` に読み込み時の値）。Mode = "edit"、RowVersion = 1 | 送料などは変えない。A の数量を変える。Z の行は「変えない」／「数量を 2 → 5」 | ErrorFlag = false（`VAL-EXISTS` の違反にならない）。`GetDetails`：A・Z の 2 行。Z：「変えない」なら Quantity = 2・**RowVersion = 1 のまま**（触れない）、「数量を変える」なら Quantity = 5・RowVersion = 2。A：更新される。`GetOrder` の RowVersion = 2 |
| OrdTfSave_編集で商品マスタに無い明細を削除できる | EV-ORD-308 手順 4-3, DEC-ORD-76 | o：明細 A と消した商品 Z（上と同じ形） | `DeletedProductIDs` = [Z]、SaveLines = [A の行] | ErrorFlag = false。`GetDetails` が A の 1 行（Z が無い）。Z の行は DB から消える |
| OrdTfSave_編集で商品マスタに無い明細の行の商品を存在する商品に付け替えられる | EV-ORD-308 手順 4-3（商品が変更された既存行は削除して挿入）, DEC-ORD-76 | o：明細 A と消した商品 Z、存在する商品 N | Z の行の `ProductIDText` を N に変える（`OriginalProductID` = Z のまま。単価・数量は別の値） | ErrorFlag = false。`GetDetails` が A・N の 2 行（Z が無い）。N：入力の値・RowVersion = 1 |
| OrdTfSave_編集で商品マスタに無い明細の行の商品を存在しない別の商品に変えるとVAL_EXISTS | VAL-EXISTS, EV-ORD-308 検証内容, DEC-ORD-76（商品を変えた行には当てる） | o：明細 A と消した商品 Z、消した別の商品 Z2 | Z の行の `ProductIDText` を Z2 に変える（`OriginalProductID` = Z のまま） | ErrorFlag = true。ErrorFields = [`Lines[1].ProductID`]（Z の行が 2 行目）。ErrorMessages が空。`GetOrder` の RowVersion = 1、`GetDetails` が保存の前と同じ |
| OrdTfSave_編集で新しい行に商品マスタに無い商品を指定するとVAL_EXISTS | VAL-EXISTS, DEC-ORD-76（新しい行には当てる） | o：明細 A。消した商品 Z（受注の明細には無い） | Z の行を足す（`OriginalProductID` = null。商品 `ProductIDText` = Z） | ErrorFlag = true。ErrorFields = [`Lines[1].ProductID`]。DB は保存の前と同じ（RowVersion = 1・明細 A のみ） |
| OrdTfSave_編集で読み込み時の行だと名乗ってもDBに無ければ商品マスタに無い商品はVAL_EXISTS | VAL-EXISTS, DEC-ORD-76・DEC-ORD-49（画面が送る値を根拠にしない） | o：明細 A のみ（o には Z の明細が無い）。消した商品 Z。**別の受注 o2 に Z の明細がある**（テストが作る。ProductID だけで引く実装〔`OrderID` の一致を見ない〕を除く） | Z の行を足し、**`OriginalProductID` = Z（`OriginalUnitPrice`・`OriginalDiscount`・`OriginalQuantity` も付ける）** | ErrorFlag = true。ErrorFields = [`Lines[1].ProductID`]。DB は保存の前と同じ |
| OrdTfSave_登録でOriginalProductIDを付けても存在しない商品はVAL_EXISTS | VAL-EXISTS, DEC-ORD-76（登録モードの行には当てる） | 登録（Mode = "new"）の有効な入力 `V`。消した商品 Z | 明細の商品を Z にし、**`OriginalProductID` = Z（ほかの `Original*` も付ける）** | ErrorFlag = true。ErrorFields = [`Lines[0].ProductID`]。受注が入らない（`CountOrdersOfShipName(目印)` = 0） |
| OrdTfShipInit_内訳は商品名の昇順で商品マスタに無い明細は最後に商品IDの昇順 | EV-ORD-401 手順 5-4, DEC-ORD-75, DEC-ORD-77 | 受注（未出荷）に、商品 A・M と消した商品 Za・Zb（`Za` < `Zb`）の明細を M・Zb・A・Za の順に登録（商品を作る順：Za・Zb を先に作り〔ID は名前のある商品より小さく、Za < Zb〕、名前のある商品が複数なら M を A より先に作る。ID の順と名前の順を逆にする） | OrderID = その受注 | StockLines の並びが A・M・Za・Zb。Za・Zb：UnitsInStock = null・Short = false |
| OrdTfShipCancel_商品マスタに無い明細があっても取り消せ在庫は変えない | EV-ORD-403 手順 5, DEC-ORD-77 | `Sh`（P1 = 7・P2 = 5。数量 3・2）＋ 消した商品 Z の明細（数量 4） | OrderID = o、RowVersion = 1 | ErrorFlag = false。ShippedDate が NULL・RowVersion = 2。**P1 = 10・P2 = 7**（Z の分は戻す在庫の行が無いので何も変えない。エラーにならない） |

- 追加：**B層 新しいメソッド 15・新しいテストケース 19、既存メソッドへ足す TestCase 10（`OrdTfDetail_状況の判定` 5・`OrdTfShipInit_状況の判定` 5）＝ 29 テストケース**（見込み。段 2 の報告で実数を書く。2 章の B-1〜B-4 は数が増えない）。net10.0 でも net48 でも同じ結果になること。

### 3.2 画面の E2E

共通の前提：一覧の 1 章と、各クラスの前の回のとおり（`[SetUp]` でログイン、2198 年のデータ、操作の前に `Expect` で要素を待つ）。日付の入力は 1.4 のとおり。

**一覧（`OrdTfScreenTests`）**

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 一覧_日付の入力欄は date 型 | 規約：日付の入力（cmn-framework.md「画面共通」） | — | `/OrdTf` を開く | `#OrderDateFrom`・`#OrderDateTo` の `type` 属性が `date` |
| 検索_5桁の年の日付は形式の違反として扱う（Playwright で `type="date"` の年の欄に 5 桁を入れられる場合に作る。入れられなければ作らず 5 章に理由を書く） | 規約：日付の入力（変換は `yyyy-MM-dd` に厳密に合う値だけ。1.1）, DEC-ORD-09, VAL-DATE | 顧客 K の受注 2 件 | K で検索（2 行）→ `#OrderDateFrom` に 5 桁の年の日付（キー入力で年の欄に `12345`・月日 `01`・`02`。`value` が `yyyy-MM-dd` に合わない形になることを `Expect` で前提にする）→ `#btnMain1` | `#OrderDateFromError` が「受注日は yyyy/MM/dd の形式で指定してください。」（変換されずに B層が形式の違反にする）。`#OrderDateToError` は空。一覧は 2 行のまま（`#totalCount` = 「全 2 件」） |
| ページ番号_今のページ番号を出し先頭・前・番号・次・末尾の順に並ぶ | DEC-CMN-08（並びと今のページ番号） | 顧客 K の受注 45 件（i 番目 = `2198/01/01` + i 日） | K で検索 → 次へ（「2 / 3 ページ」） | `#pageNo` の値 = 2。DOM の並びが `#btnPageFirst`・`#btnPagePrev`・`#pageNo`・`#btnPageNext`・`#btnPageLast`（`#pageNo` の直前が `#btnPagePrev`・直後が `#btnPageNext`）。既存の 4 つのボタンと `#pageInfo` は変わらない |
| ページ番号_入力してEnterでそのページへ移る（TestCase：3／2） | DEC-CMN-08（番号を入れて Enter でそのページへ）, EV-ORD-104 | 同上 | K で検索 → `#pageNo` に各値を入れて Enter | 3：「3 / 3 ページ」・5 行（先頭行 = i = 5）・`#pageNo` = 3・`#totalCount` = 「全 45 件」。2：「2 / 3 ページ」・20 行（先頭行 = i = 25）・`#pageNo` = 2。`#messageArea` は空 |
| ページ番号_範囲外は先頭か最終ページに詰める（TestCase：`99`／`0`／`-5`／`99999999999`／`-99999999999`） | DEC-CMN-08（総ページ数より大きければ最終、1 未満なら 1）, DEC-ORD-06 | 同上 | K で検索 → 次へ（「2 / 3 ページ」）→ `#pageNo` に各値を入れて Enter | `99`・`99999999999`（Int32 を超える整数。整数なので最終ページ）：「3 / 3 ページ」・5 行・`#pageNo` = 3。`0`・`-5`・`-99999999999`：「1 / 3 ページ」・先頭行 = i = 45・`#pageNo` = 1。`#messageArea` は空（メッセージを出さない） |
| ページ番号_整数でない値は移らず今のページ番号に戻る（TestCase：`abc`／`1.5`／空） | DEC-CMN-08（空・整数でない値は移らず、入力欄を今のページ番号に戻す。メッセージは出さない） | 同上 | K で検索 → 次へ（「2 / 3 ページ」）→ `#pageNo` に各値を入れて Enter | 「2 / 3 ページ」のまま・20 行（先頭行 = i = 25）・`#pageNo` = 2（今のページ番号に戻る）。`#messageArea` は空・`#totalCount` = 「全 45 件」 |
| ページ番号_入力欄だけ変えても最後に検索した条件と並びで移る | DEC-CMN-08（検索条件・並び順を保つ。EV-ORD-104）, DEC-ORD-04 | 顧客 K の受注 45 件（同上）、別の顧客 L の受注 1 件 | K で検索 → `#sortOrderDate`（昇順）→ `#CustomerID` を L に変えるだけ → `#pageNo` に 2 を入れて Enter | 「2 / 3 ページ」・20 行（先頭行 = i = 21。昇順の 2 ページ目）・すべて K の会社名（L の行を含まない）・`#totalCount` = 「全 45 件」 |

**詳細（`OrdTfDetailScreenTests`）**

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 削除_表示の後に出荷されていたらERR-BIZで詳細に留まる | EV-ORD-203, ERR-BIZ, DEC-ORD-78, DEC-ORD-81（画面の活性でなく DB の値を根拠にする） | 受注 1 件（未出荷。要求納期 `2198/04/01`。明細 1 行） | 詳細を開く（`#btnMain1` が活性）→ テストが `SetShippedDate`（`2198/03/10`。要求納期以前。`RowVersion` は変えない）→ `#btnMain1`（削除）→ ダイアログを承認 | 詳細のまま（`#orderId` = 受注 ID、`#detailList` が可視）。`#messageArea` が「出荷済の受注は削除できません。」で、class に `text-danger` を持ち `text-info` を持たない。`CountOrders` = 1・`CountDetails` = 1。**詳細が最新に読み直される**（DEC-ORD-81：`#shippedDate` = `2198/03/10`、`#status` =「出荷済」〔出荷日が要求納期以前なので納期遅延でない。DEC-ORD-27〕、`#btnMain1` と `#btnEdit` が非活性） |
| 詳細_商品マスタに無い明細は付記して小計に含め警告を出す | EV-ORD-201 手順 3・4・6, DEC-ORD-24, DEC-ORD-73, DEC-ORD-75 | 受注 1 件（送料 `5.00`）に、存在する商品 A（在庫 7。`10.00`・0・数量 2）と、消した商品 Za・Zb（`Za` < `Zb`。`100.00`・0・数量 1／`20.00`・0・数量 3）。A・Zb・Za の順に登録（商品を作る順：Za・Zb を先に作り〔ID は名前のある商品より小さく、Za < Zb〕、名前のある商品が複数なら M を A より先に作る。ID の順と名前の順を逆にする） | 詳細を開く | `#detailList tbody tr` が 3 行で A・Za・Zb の順。Za・Zb の行：商品名のセルが `（商品マスタに無い：ID n）`（「（廃番）」を含まない）、**在庫のセルが空**。A の在庫のセル = `7`。`#subtotal` = `180.00`・`#total` = `185.00`。`#messageArea` が「商品マスタに無い商品の明細があります。」で、class に `text-warning` を持ち `text-danger`・`text-info` を持たない |
| （既存 `詳細_受注情報と明細と金額の表示` に確認を足す） | DEC-ORD-73（該当する明細が無ければ警告を出さない） | 既存の前提（商品マスタに無い明細なし） | 既存の操作 | **足す期待**：`#messageArea` が空（警告が出ない）。既存の期待は変えない |

**編集（`OrdTfEditScreenTests`）**

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 編集_日付の入力欄は date 型 | 規約：日付の入力 | — | `/OrdTf/Edit?mode=new` を開く | `#OrderDate`・`#RequiredDate` の `type` 属性が `date` |
| 保存_受注日の入力途中は形式の違反として扱い保存しない | 規約：日付の入力（入力途中の値は形式の違反。未入力として扱わない）, VAL-DATE, DEC-CMN-05, DEC-ORD-30 | K・E・P1 | 登録を開く → K・E を選び、行 1 に P1（`10.00`・0・数量 1）→ `#OrderDate` に**入力途中の値**（前提：`validity.badInput` = true）→ `#btnMain1` | 編集画面に留まる（`#orderId` =「（登録時に採番）」）。`#OrderDateError` が空でなく `text-danger`。`#RequiredDateError` は空。`#messageArea` は空。`#OrderDate` は入力途中のまま（操作の後も `validity.badInput` が true のまま）。**`#btnMain1` を押してから確かめるまでの間に、保存の POST が 0 件**（`page.Request` で保存の URL への POST を記録する。受注日は必須で、空として送られても必須エラーが出て DB も変わらないため、DB・エラー表示だけでは「未入力として扱う」実装と区別できない。送信を止めたことを直に確かめる）。DB：K の受注が 0 件（`CountOrdersOfCustomer(K)` = 0） |
| 保存_要求納期の入力途中は形式の違反として扱い保存しない | 規約：日付の入力（未入力として扱わない）, VAL-DATE, DEC-ORD-29 | 同上 | 登録を開く → K・E を選び、`#OrderDate` に `2198-03-05`、行 1 に P1 → `#RequiredDate` に**入力途中の値** → `#btnMain1` | 編集画面に留まる。`#RequiredDateError` が空でなく `text-danger`。`#OrderDateError` は空。操作の後も `#RequiredDate` の `validity.badInput` が true のまま。**保存の POST が 0 件**（同上。送信を止めたことの確かめ）。DB：K の受注が 0 件（**要求納期を未入力として保存していない**） |
| 編集_商品マスタに無い明細は選択状態で残り付記し警告を出す | EV-ORD-301 手順 3-4・3-5, DEC-ORD-76, DEC-ORD-73, DEC-ORD-75 | 受注 1 件（K・E。送料 `5.00`）に、存在する商品 A（在庫 20。`10.00`・0・数量 2）と、消した商品 Za・Zb（`100.00`・0・数量 1／`20.00`・0・数量 3）。A・Zb・Za の順に登録（商品を作る順：Za・Zb を先に作り〔ID は名前のある商品より小さく、Za < Zb〕、名前のある商品が複数なら M を A より先に作る。ID の順と名前の順を逆にする） | 編集を開く | `#lineList tbody tr` が 3 行で A・Za・Zb の順。Za の行：`.line-product` の選択が Za の ID（選択された `option` の value）・その `option` のテキストが `（商品マスタに無い：ID n）`、`.line-stock` が空、`.line-unitprice` = `100`（`100.00`）。A の行の `.line-product` の選択肢に Za・Zb の `option` が**無い**。`#subtotal` = `180.00`・`#total` = `185.00`。`#messageArea` が「商品マスタに無い商品の明細があります。」で `text-warning`（`text-danger`・`text-info` を持たない）。`#btnMain1` が活性 |
| 編集_商品マスタに無い明細の数量を変えて保存できる | EV-ORD-308 手順 4-3, VAL-EXISTS, DEC-ORD-76, DEC-ORD-82 | 上と同じ（A・Za の 2 行でよい） | 編集を開く → Za の行の `.line-quantity` を `5` → `#btnMain1` | 詳細が表示される（`#orderId`）。`#detailList tbody tr` が 2 行（A・Za）で、Za の数量のセルが `5`。数量を `5` に変えた後、保存の前に、`#messageArea` が「在庫数を超えています。」を含まない（`Not.ToContainText`。商品マスタに無い行は在庫超過の警告の対象にしない〔DEC-ORD-76〕・在庫が空の行は超える行に数えない〔DEC-ORD-44〕）。DB：`GetDetails` の Za：Quantity = 5・RowVersion = 2。**保存の後の `#messageArea` は期待にしない**（完了メッセージと警告が重なる。DEC-ORD-82 (a)：定めない） |
| 編集_商品マスタに無い明細の行を削除して保存できる | EV-ORD-305, EV-ORD-308 手順 4-3, DEC-ORD-76, DEC-ORD-82 (b) | 上と同じ（A・Za の 2 行） | 編集を開く → Za の行の `.line-remove` → `#subtotal` が A だけ（`20.00`）になるのを待つ → `#messageArea` が空（商品マスタに無い行が無くなったので警告が消える。DEC-ORD-82 (b)）→ `#btnMain1` | 詳細が表示される。`#detailList tbody tr` が 1 行（A）。DB：`GetDetails` が A の 1 行（Za が無い） |
| 編集_商品マスタに無い明細の行を存在する商品に付け替えて保存できる | EV-ORD-306, EV-ORD-308 手順 4-3, DEC-ORD-76, DEC-ORD-82 (b) | 上と同じ。存在する商品 N（`15.00`・在庫 20） | 編集を開く → Za の行で `.line-product` に N を選ぶ（`option` のテキスト：N の商品名）→ `#messageArea` が空（警告が消える。DEC-ORD-82 (b)）→ `#btnMain1` | 詳細が表示される。`#detailList tbody tr` が 2 行で、N の行がある（Za の行が無い）。DB：`GetDetails` が A・N |
| 編集_商品マスタに無い行が残る間は警告を消さず無くなったら消す（TestCase：行の削除／商品の付け替え） | DEC-ORD-73, DEC-ORD-82 (b)（DEC-ORD-44 と同じ形：行の削除・商品の付け替えのたびに判定） | 受注 1 件に、存在する商品 A（在庫 20。数量 2）と、消した商品 Za・Zb（`Za` < `Zb`。数量 1・1）の明細。存在する商品 N・N2（在庫 20。どの数量も在庫を超えない。在庫超過の警告と重ならない〔DEC-ORD-82 (c)：定めない〕）。**Za・Zb を先に作る** | 編集を開く（警告が出る）→ 「行の削除」：Za の行の `.line-remove` → Zb の行の `.line-remove`。「商品の付け替え」：Za の行の `.line-product` を N に変える → Zb の行の `.line-product` を（A と重複しない別の存在する商品）N2 に変える。各操作の後に確かめる | 1 つ目の操作の後：`#messageArea` が「商品マスタに無い商品の明細があります。」で `text-warning`（Zb が残るので**残る**）。2 つ目の操作の後：**警告が消える**（`#messageArea` が空・`text-warning` を持たない） |
| 在庫超過_超える行が残る間は警告を消さない（数量の変更） | DEC-ORD-44（超える行が 1 行でも残る間は消さない。超える行が無くなったら消す） | 商品 P1・P2（在庫 5）。登録 | 登録を開く → 行 1 に P1・数量 `6`、`#btnAddLine`、行 2 に P2・数量 `6`（超える行が 2 つ）→ 行 1 の数量を `5` → 行 2 の数量を `5` | 行 1 の数量を直した後：`#messageArea` が「在庫数を超えています。」で `text-warning`（行 2 が超えているので**残る**）。行 2 の数量を直した後：**警告が消える**（`#messageArea` が空・`text-warning` を持たない） |
| 在庫超過_超える行が残る間は警告を消さない（商品の選択） | DEC-ORD-44（商品の選択のたびに全行で判定） | 商品 P1・P2（在庫 5）、P3・P4（在庫 100）。登録 | 行 1 に P1・数量 `6`、行 2 に P2・数量 `6` → 行 1 の商品を P3 に変える → 行 2 の商品を P4 に変える | 行 1 を変えた後：警告が**残る**（行 2 が超えている）。行 2 を変えた後：**警告が消える** |
| 在庫超過_超える行が残る間は警告を消さない（行の削除） | DEC-ORD-44（行の削除のたびに全行で判定） | 商品 P1・P2・P3（在庫 5）。登録 | 行 1 に P1・数量 `6`、行 2 に P2・数量 `6`、行 3 に P3・数量 `1`（超えない）→ 行 1 の `.line-remove` → 先頭になった P2 の行の `.line-remove` | 行 1 を削除した後：警告が**残る**（P2 の行が超えている）。P2 の行を削除した後：**警告が消える**（P3 の行だけで超えない） |
| 在庫超過_編集の読み込みで既存の明細が超えていれば警告を出す | DEC-ORD-44（編集の読み込みのとき判定する）, EV-ORD-301 手順 3-4 | 受注 1 件に、商品 A（在庫 5・数量 6）・M（在庫 100・数量 1）の明細 | 編集を開く | `#messageArea` が「在庫数を超えています。」で `text-warning`（`text-danger`・`text-info` を持たない） |
| 在庫超過_在庫が空の行は超える行に数えない | DEC-ORD-44（在庫が NULL・空の行は超える行に数えない） | 受注 1 件に、在庫 NULL の商品 X（数量 99）・商品 M（在庫 100・数量 1）の明細 | 編集を開く | `#messageArea` が空（`text-warning` を持たない）。`.line-stock`（X の行）が空 |
| （既存 `在庫超過_警告が出て入力は妨げない` に確認を足す） | DEC-ORD-44（超える行が無くなったら消す） | 既存の前提（商品 P の在庫 5・数量 6 で警告） | 既存の操作の後、数量を `5` に直す | **足す期待**：`#messageArea` が空（`text-warning` を持たない）。既存の期待は変えない（「直した後は期待にしない」の記述は、決定で消した） |

**出荷（`OrdTfShipScreenTests`）**

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 表示_出荷日の入力欄は date 型 | 規約：日付の入力 | `Es` | ダイアログを開く | `#shipShippedDate` の `type` 属性が `date` |
| 確定_出荷日の入力途中は形式の違反として扱い確定しない | 規約：日付の入力（未入力として扱わない）, VAL-DATE, DEC-CMN-05, DEC-ORD-56 | `Es` | ダイアログを開く → `#shipShippedDate` に**入力途中の値** → `#shipMain1` | `#shipDialog` が見えたまま。`#shipShippedDateError` が空でなく `text-danger`。`#shipShipViaError`・`#shipFreightError` は空。`#shipMessage` が空。操作の後も `#shipShippedDate` の `validity.badInput` が true のまま。**確定の POST が 0 件**（`page.Request` で確定の URL への POST を記録する。出荷日は必須で、空として送られても必須エラーが出て DB も変わらないため、DB・エラー表示だけでは「未入力として扱う」実装と区別できない。送信を止めたことを直に確かめる）。DB：ShippedDate が NULL・RowVersion = 1・P1 = 10・P2 = 5 |
| 表示_商品マスタに無い明細は内訳に出て強調しない | EV-ORD-401 手順 5-4, DEC-ORD-77, DEC-ORD-73, DEC-ORD-75 | 受注（未出荷。`Es` の形）に、P1（在庫 10・数量 3）と消した商品 Za・Zb（数量 5・2。`Za` < `Zb`）の明細。P1・Zb・Za の順に登録（商品を作る順：Za・Zb を先に作り〔ID は名前のある商品より小さく、Za < Zb〕、名前のある商品が複数なら M を A より先に作る。ID の順と名前の順を逆にする） | ダイアログを開く | `#shipStockList tbody tr` が 3 行で P1・Za・Zb の順。Za・Zb の行：`.ship-product` が `（商品マスタに無い：ID n）`、`.ship-qty` = 数量、`.ship-stock` が空、**`table-danger` を持たない**。`#shipMain1` が活性。**`#shipMessage` は期待にしない**（出荷ダイアログに警告を出すかはDEC-ORD-79：定めない） |
| 取消_詳細から取り消した後の削除は競合しない | EV-ORD-403 手順 9, EV-ORD-205 手順 2（保持していた `RowVersion` も更新される）, EV-ORD-203 手順 4, DEC-ORD-78（出荷を取り消してから削除する） | 受注（出荷日 `2198/04/05`・`Es` の明細。P1 数量 3〔在庫 7〕・P2 数量 2〔在庫 3〕） | 詳細を開く（`#btnMain1` が非活性）→ ダイアログを開く → `#shipMain2` → 承認 → `#btnMain1`（削除）→ 承認 | 取消の後：詳細のまま、`#status` =「未出荷」、`#btnMain1` が活性。削除の後：一覧が表示され、`#messageArea` が「受注 ID nnn を削除しました。」で `text-info`（`ERR-CONFLICT` にならない）。DB に o が無い |
| 閉じる_詳細の再読み込みの後の削除は競合しない | EV-ORD-404 手順 2, EV-ORD-205 手順 2・EV-ORD-201, EV-ORD-203 手順 4 | `Es`（未出荷） | 詳細を開く → ダイアログを開く →（テストが `BumpOrder`：送料 `99.00`）→ `#btnShipClose` → `#btnMain1`（削除）→ 承認 | 閉じた後：`#freight` = `99.00`（再読み込みされた）・`#btnMain1` が活性。削除の後：一覧が表示され、`#messageArea` が「受注 ID nnn を削除しました。」で `text-info`。DB に o が無い |

**帳票（`OrdTfReportScreenTests`）**

| ケース | 根拠 | 前提 | 操作 | 期待 |
| --- | --- | --- | --- | --- |
| 帳票_商品マスタに無い明細は商品IDだけを出し注記も警告も出さない | EV-ORD-501 手順 3・4, DEC-ORD-66, DEC-ORD-74, DEC-ORD-75 | 受注 1 件（送料 `5.00`）に、存在する商品 A（`10.00`・0・数量 2）と、消した商品 Za・Zb（`100.00`・0・数量 1／`20.00`・0・数量 3）。A・Zb・Za の順に登録（商品を作る順：Za・Zb を先に作り〔ID は名前のある商品より小さく、Za < Zb〕、名前のある商品が複数なら M を A より先に作る。ID の順と名前の順を逆にする） | `type=delivery` で開く | `#reportLines tbody tr` が 3 行で A・Za・Zb の順。Za・Zb の行の商品名のセルが ID の数字だけ（`n`。「商品マスタに無い」を含まない）。`#subtotal` = `180.00`・`#total` = `185.00`。`#messageArea` が空（警告を出さない） |

- 追加：**E2E 新しいメソッド 28・新しいテストケース 36**（クラスごとのメソッド・テストケース：一覧 7・14〔`date` 型 1、5 桁の年 1、ページ番号 5 メソッド 12 ケース〕、詳細 2・2、編集 13・14〔`date` 型 1、入力途中 2、商品マスタに無い 5（警告の消し方 2 ケース）、在庫超過 5〕、出荷 5・5〔`date` 型 1、入力途中 1、内訳 1、取消後の削除 1、閉じる後の削除 1〕、帳票 1・1。見込み。段 2 の報告で実数を書く）。ほかに、既存のケースに足す確認が 2 つ（`詳細_受注情報と明細と金額の表示`・`在庫超過_警告が出て入力は妨げない`）。

## 4. 変えないケースの確認と、ケース1・3 への波及

- **遷移だけのボタンはメインボタンに含めない**（規約。実装の変更は無い。確かめるだけ）：既存のケースがそのまま確かめている。一覧の `初期表示_ボタン`（`#btnMain1` のみ活性・`#btnNew` はフッタの外）、詳細の `詳細_メインボタンと編集の活性`（`#btnEdit`・`#btnBack` はフッタの外。**`#btnMain1` の期待だけ 2.2 の E-14 で変わる**）、編集の `登録_初期表示`（`#btnAddLine`・`#btnCancel` はフッタの外）、出荷の `一覧_出荷更新で元画面の上に重ねて表示される`（ダイアログのフッタに 5 つ、`#btnShipClose` は別）、帳票の `帳票_メインボタン`（印刷・戻るはフッタの外。フッタの 5 つが非活性）。**足すケースは作らない**。段 2 では、これらのケースが今のままで通ることを流して確かめる。
- **ケース1・3 への波及**：`DEC-ORD-27` の直し（`JudgeStatus`・`OrdTfSearch.xml`・`OrdTfCount.xml`）は共用なので、ケース1・3 の受注一覧の状況にも効く。**この回の終わりに、ケース1・3 の画面の既存の E2E を流す**（`docs/spec/test3-testfirst-OrdTfRevise.md`「ケース1・3 への波及」。環境の用意〔IIS Express・WebForms の起動〕が要る。通らないときは、この回の変更によるものかを切り分けて、終わりの判断に上げる）。このテスト仕様では、ケース1・3 のテスト仕様を変えない。

## 5. 作らないケースと理由

| 対象 | 理由 |
| --- | --- |
| 出荷ダイアログの商品マスタに無い明細の警告（`#shipMessage`） | `DEC-ORD-73` の対象は詳細・編集で、`DEC-ORD-77` は内訳の表示だけを決めている。DEC-ORD-79（定めない）。期待にしない |
| 出荷済で `RowVersion` も古いときの `ERR-BIZ` と `ERR-CONFLICT` の先後 | 先後はDEC-ORD-80（定めない）で期待にしない（B層の `ERR-BIZ` のケースは `RowVersion` が現在の値、E2E の `ERR-BIZ` のケースは `SetShippedDate`〔`RowVersion` を変えない〕で作り、先後に依らない） |
| 商品マスタに無い警告と、ほかのメッセージ（保存の完了・在庫超過・エラー）が重なるときの出し方。| DEC-ORD-82 の (a)(c)（定めない）。重なるケースは作らない。(b)（消し方）は決まったので 3.2 のケースにした（`DEC-ORD-44` の「重なったときの出し方は定めない」と同じ扱い） |
| 日付の入力欄に 5 桁以上の年が入ったときの変換（5 桁の年の E2E を Playwright で作れない場合） | `FillAsync` は `yyyy-MM-dd` の 4 桁の年だけを受け付ける実装がありうる。作れない場合は、変換の契約（1.1）を B層の単体では確かめられないので、段 4 のレビューで P層の変換の正規表現を読んで確かめ、終わりの目で見る確認の頼みに入れる |
| `#pageNo` の活性・非活性（ページが 1 つ・0 件のとき）、Enter 以外（フォーカスを外す）での移動 | 仕様に無い（`DEC-CMN-08` は「番号を入れて Enter」だけ）。期待にしない（実装に任せる。問題になれば別の決定に上げる） |
| 入力途中の値が日付の欄に残ったまま、ページ移動・並べ替えの操作をしたとき | 既存の「検証エラーの後も直前の検索条件で移る」と同じ（E-6・E-7）。入力欄を使わない操作は止めない（1.1）。新しいケースにしない |
| 日付の入力欄に直接 `yyyy/MM/dd` の文字を入れる・`abc` を入れる E2E | `type="date"` には入れられない。形式の検証は B層のテスト（`2199-01-05` などの違反・`abc`）で確かめ続ける。E2E は入力途中の値で確かめる（E-3〜E-7・3.2） |
| 商品マスタに無い明細の、編集の保存で新しい行に指定する E2E | 選択肢に無く、画面の操作からは起こらない。B層（3.1 の `…新しい行に…`・`…名乗っても…`）で確かめる |
| 在庫が NULL の商品の出荷（`DEC-ORD-62`） | 定めないのまま（範囲外） |
| 状況の判定のケースで、同じ日の時刻違いを E2E で繰り返す | B層（3.1）で確かめる。E2E は一覧の `table-danger`（納期遅延の強調。既存）のまま |
| `LLD/ImplementationNotes` に書かれたことを期待にするケース | 実装の記録で、テストの根拠にしない |

## 6. 仕様の決定（答え済み。記録は DEC-ORD-79〜82・DEC-CMN-09）

仕様（本文・決定記録）から決められず、振る舞いが変わること。答えは「答え」の列（決めた者：代行。2026-11-26）で、**決定記録 NorthwindDbSampleSpec@9be21c0 に記録された（決定 N → 下の ID。各 DEC の「決定」の欄を読み、答えとこのテスト仕様の期待が合うことを確かめた）**。選択肢は間引かず、「定めない」も含める。

| # | 仕様の ID | 問い | 選択肢 | 推奨と理由 | 影響するケース | 答え（2026-11-26。決めた者：代行） |
| --- | --- | --- | --- | --- | --- | --- |
| 1（DEC-ORD-79） | `DEC-ORD-73`・`DEC-ORD-77`・`SC-ORD-04`（共通メッセージ領域 `#shipMessage`） | 出荷ダイアログで、商品マスタに無い商品の明細があるとき、警告「商品マスタに無い商品の明細があります。」を出すか（`DEC-ORD-73` の対象は詳細・編集、`DEC-ORD-77` は内訳の表示〔商品名の欄は `DEC-ORD-73` の表示〕だけを決めている） | 1. 出さない（内訳の表示だけ） ／ 2. 出す（詳細・編集と同じ警告をダイアログの `#shipMessage` に `text-warning` で出す） ／ 3. 定めない（実装に任せ、期待にしない） | 3。仕様は出荷の内訳に表示を決め、警告は詳細・編集に限っている。ダイアログは内訳の行で気付ける。決めるなら 1 か 2 のどちらか（作者の判断） | 出荷 E2E `表示_商品マスタに無い明細は内訳に出て強調しない`（今は `#shipMessage` を期待にしない） | GO（3。定めない）。代行。出荷ダイアログのメッセージ領域は期待にしない（今のまま） |
| 2（DEC-ORD-80） | `DEC-ORD-78`・共通仕様 9.1 節・`ERR-CONFLICT` | 削除で、出荷済で、かつ `RowVersion` も古い（他の利用者が出荷確定をした＝`RowVersion + 1`）とき、どちらのエラーを返すか。`DEC-ORD-78` は「削除のトランザクションの中で `ShippedDate` を確かめ、NULL でなければ `ERR-BIZ`」と決め、`RowVersion` の検査との先後を決めていない | 1. 出荷済の検査が先（`ERR-BIZ`「出荷済の受注は削除できません。」） ／ 2. `RowVersion` の検査が先（`ERR-CONFLICT`。最新を再表示する） ／ 3. 定めない（どちらでもよい。期待にしない） | 3。どちらを返しても削除は起こらず、利用者が最新を見れば出荷済だと分かる。`ERR-BIZ` の E2E は `RowVersion` を変えない `SetShippedDate` で先後に依らず作れる | 作らないケース（5 章）。B層・E2E の `ERR-BIZ` のケースは `RowVersion` が現在の値 | GO（3。定めない）。代行。先後は期待にしない（今のまま） |
| 3（DEC-ORD-81） | `DEC-ORD-78`・共通仕様 7 節（最新データを再表示） | 削除が `ERR-BIZ`（出荷済）になって詳細に留まるとき、詳細を最新（出荷日あり・削除が非活性）に読み直すか | 1. 読み直す（`ERR-CONFLICT` の「最新を再表示」と同じ） ／ 2. 読み直さず、画面はそのまま（削除は活性のまま。もう一度押すと同じ `ERR-BIZ`） ／ 3. 定めない（実装に任せ、期待にしない） | 1。`DEC-ORD-78` の理由（編集と同じ形にそろえる）から、出荷済の詳細は削除が非活性であるのが整合。ただし本文・決定記録は「受注詳細に留まる」としか書いていない | E2E `削除_表示の後に出荷されていたらERR-BIZで詳細に留まる`（今は期待にしない） | GO（1。読み直す）。代行。削除が `ERR-BIZ`（出荷済）になったら受注詳細を最新（出荷日あり・削除と編集が非活性）に読み直す。E2E `削除_表示の後に出荷されていたらERR-BIZで詳細に留まる` に期待を足した |
| 4（DEC-ORD-82） | `DEC-ORD-73`・`DEC-ORD-44`・`DEC-ORD-35`・共通仕様 4 節（メッセージ表示） | 商品マスタに無い明細の警告について、(a) 保存の完了後の詳細で、完了メッセージ（情報）と警告が両方要るときの `#messageArea` の出し方、(b) 編集で商品マスタに無い行を削除・付け替えたあと、警告を消すか（残る間は残す〔`DEC-ORD-44` と同じ〕か、読み込み時だけか）、(c) 在庫超過の警告と同時に要るときの出し方。`DEC-ORD-44` は「ほかのメッセージと重なったときの出し方は定めない」と決めており、`DEC-ORD-73` は重なりを決めていない | (a)(c)：1. 両方を出す ／ 2. 優先を決める（エラー > 警告 > 情報 など） ／ 3. 定めない。(b)：1. 商品マスタに無い行が残る間は消さない（`DEC-ORD-44` と同じ） ／ 2. 読み込み時だけ（操作で消す） ／ 3. 定めない | 3（すべて）。`DEC-ORD-44` の扱いと同じ。(b) は 1 が自然だが、決定記録に無い | 編集 E2E `編集_商品マスタに無い明細の数量を変えて保存できる`（`#messageArea` を期待にしない）、行の削除・付け替えのケース（警告を期待にしない） | (a) GO（3。定めない）、(b) GO（1。商品マスタに無い行が残る間は警告を消さず、無くなったら消す。DEC-ORD-44 と同じ形：行の削除・商品の付け替えのたびに判定）、(c) GO（3。定めない）。代行。(b) を行の削除・付け替えのケースの期待にした（3.2）。(a)(c) は期待にしない（今のまま） |
| 5（DEC-CMN-09） | `DEC-CMN-08`（ページ番号の入力） | 「整数」の範囲：全角の数字（`３`）、前後の空白（` 3 `）、符号付き（`+3`）、先頭の 0（`03`）を整数として受けて移るか、整数でないとして移らないか（決定記録は「整数で、総ページ数より大きければ最終ページ、1 未満なら 1 ページ目。空・整数でない値は移らない」としか書いていない） | 1. 半角の数字だけを整数とする（半角の数字の並びと先頭の `-`。それ以外は移らない） ／ 2. 全角・前後の空白・`+`・先頭の 0 も整数として受ける（移る） ／ 3. 定めない（実装に任せ、期待にしない） | 3。仕様は整数かどうかの定義を書いていない。決めるなら 1（単純で、取り違えが少ない）。ただし `-5`・`99999999999` は整数として確定済み（`DEC-CMN-08` の「1 未満」「総ページ数より大きい」）で、ケースにした | `ページ番号_範囲外…`・`ページ番号_整数でない値…` に足す TestCase（今は期待にしない） | GO（3。定めない）。代行。全角・空白・`+`・先頭の 0 は期待にしない（今のまま） |

## 7. 段 2 の確かめ方（予定）

- **Red**
  - B層：`OrdTfDetailLine`・`OrdTfEditLine` に `MissingProduct` のメンバだけを足す（テストをコンパイルするための足場）。`OrdTfBusiness`・SQL は変えない。3.1・2.1 の変えたケース・足したケースを net10.0 の `MVC_Sample_Core\MVC_Sample.Tests` で流し、`Assert` で落ちることを確かめる。**見込み（HEAD で流して確かめる。見込みで書かない）**：Red になるのは B-1・B-2・B-3、`OrdTfDelete_出荷済の受注は削除できない`、`OrdTfEditInit_商品マスタに無い明細も…`、`OrdTfDetail_…並びは商品IDの昇順`、`OrdTfShipInit_…並び`、`OrdTfShipCancel_商品マスタに無い…`（今は内訳・明細から除くため）、`OrdTfSave_編集で商品マスタに無い明細を残して保存できる`（今は全行に `VAL-EXISTS`）、状況の TestCase ①（今は時刻まで比べる）と `OrdTfSearch_…日付の単位…` の i を含む期待。**最初から通りうる**：状況の ②③④⑤、`OrdTfSave_編集で商品マスタに無い明細の行の商品を…`（付け替え・存在しない商品・新しい行・名乗っても）、登録の `OriginalProductID`、`OrdTfSave_編集で商品マスタに無い明細を削除できる`。通ったら段 2 の報告に書き、下の壊す確かめで実装を見ていることを確かめる。B-4（根拠の付け替え）は最初から通る。
  - 画面：新しい E2E は `Expect` で落ちる（`type` 属性・`#pageNo`・警告・内訳の行が無い）。入力の形（`yyyy-MM-dd`）に直した既存の E2E は、P層が変換するまで落ちる（B層に `yyyy-MM-dd` が渡り `VAL-DATE` の違反になる）。E-14〜E-16 は、削除が非活性になるまで落ちる。最初から通るものは段 2 の報告に書く。
- **既存のテストの確認**：B層は net10.0 で既存を含む全件、E2E は変えていない既存のケース（4 章の「遷移だけのボタン」のケースを含む）が、今のままで通ることを確かめる。
- **壊す確かめ**（最初から通るケースがあるとき。`TESTING.md` 4。1 か所ずつ一時的に壊して戻し、戻したファイルの更新時刻を今にしてから流す。壊した箇所・落ちたケース・落ち方の例を、段 2 の報告に書く）：(a) `JudgeStatus` の出荷済の比較を時刻のままに戻す（状況の ①）。(b) `OrdTfSearch.xml` だけ・`OrdTfCount.xml` だけの納期遅延の条件を時刻のままに戻す（`OrdTfSearch_出荷状況の納期遅延…` の Rows と TotalCount が別々に落ちる）。(c) `VAL-EXISTS`（商品）を、行が `OriginalProductID` を持てば（DB を見ずに）外す（`…名乗ってもDBに無ければ…`・`…登録でOriginalProductIDを付けても…`）／商品を変えた行も外す（`…存在しない別の商品に変えると…`）／新しい行も外す（`…新しい行に…`）。(d) 外部結合を内部結合に戻す・並びの NULL 先頭を戻す（`…並びは商品IDの昇順`）。(e) 削除の出荷済の検査を、トランザクションの外・または引数の値に頼る形に変える（`OrdTfDelete_出荷済の受注は削除できない` が `CountDetails` で落ちる等）。(f) 出荷の取消で、商品マスタに無い明細の行を戻す対象に含める（エラーまたは在庫の誤り）。
- テストのファイル：`Shared\WSServer_sample.Tests\OrdTfBusinessTests.cs`・`TestData.cs`、`MVC_Sample_Core\MVC_Sample.E2ETests\OrdTfScreenTests.cs`・`OrdTfDetailScreenTests.cs`・`OrdTfEditScreenTests.cs`・`OrdTfShipScreenTests.cs`・`OrdTfReportScreenTests.cs`・`TestData.cs`（足す・直す）。

## 8. 段 2 の確かめ（OrdTfRevise。段 2 の時点：実装は前の回のまま〔作業ツリーの B層・SQL・P層に変更なし。足場は `OrdTfDetailLine`・`OrdTfEditLine` の `MissingProduct` のメンバだけ〕）

### 8.1 Red の結果

- **B層（net10.0。`MVC_Sample_Core\MVC_Sample.Tests`）**：354 ケース、失敗 15・合格 339。失敗 15 はすべて `Assert` の失敗（ビルド・呼び出しの失敗ではない）。
  `OrdTfDelete_出荷済の受注は削除できない`（2。`Expected: True / But was: False`）、`OrdTfDetail_商品マスタに無い明細も含める`（`Lines.Count` `Expected: 2 / But was: 1`）、`OrdTfReport_商品マスタに無い明細も含める`（同）、`OrdTfShipInit_商品マスタに無い明細は内訳に出す`（`StockLines.Count` `Expected: 3 / But was: 2`）、`OrdTfDetail_…並びは商品IDの昇順`・`OrdTfShipInit_内訳は…並び`（`Expected is <String[4]>, actual is <String[2]>`）、`OrdTfEditInit_商品マスタに無い明細も…`（`EditLines.Count` `Expected: 3 / But was: 1`）、`OrdTfSave_編集で商品マスタに無い明細を残して保存できる`（2。`ErrorFlag` `Expected: False / But was: True`。`Lines[1].ProductID`＝`VAL-EXISTS` が全行に当たる）、`OrdTfDetail_状況の判定("2001/01/01 00:00","2001/01/01 15:30","出荷済")`・`OrdTfShipInit_状況の判定(同)`（`Expected: "出荷済" / But was: "納期遅延"`）、`OrdTfSearch_状況の判定は日付の単位で比べる`（i が「納期遅延」）、`OrdTfSearch_出荷状況の納期遅延は日付の単位で比べる("納期遅延","jm")`（`Extra (1): i`）、`OrdTfDetail_商品マスタに無い明細の金額は詳細_帳票_一覧の合計で同じになり丸めも同じ`（一覧の合計金額 31.28 は通り、`Lines.Count(MissingProduct)` が `Expected: 1 / But was: 0`）。
- **最初から通った B層**：状況の TestCase ②③④⑤（詳細・出荷。各 4）、`OrdTfSearch_出荷状況の納期遅延は日付の単位で比べる("出荷済","ijk")`・`("未出荷","lm")`、`OrdTfSave_編集で商品マスタに無い明細を削除できる`・`…存在する商品に付け替えられる`・`…存在しない別の商品に変えると…`・`…新しい行に…`・`…名乗っても…`・`OrdTfSave_登録でOriginalProductIDを付けても…`、`OrdTfShipCancel_商品マスタに無い明細があっても取り消せ…`、`OrdTfShipSave_商品マスタに無い明細があっても確定できる`（B-4。根拠だけ付け替え）。
- **画面の E2E（net10.0。既存を含む 197 ケース）**：失敗 67・合格 130（2 回流して同じ 67。10 分）。失敗はすべて、変えた・足したケース（2 章・3.2）。
  - 日付の形（E-1〜E-13。`2198-05-01` の入力・`yyyy-MM-dd` の読み）：入力欄が `type="text"` で `2026/11/26`（`Expected '2026-11-26' / But was '2026/11/26'`）、または B層が `yyyy-MM-dd` を `VAL-DATE` の違反にして確定・保存ができない（`#shipDialog` が閉じない・詳細へ遷移しない）。入力途中の値を前提にするケースは、`DateInput.TypePartialAsync` の前提（`validity.badInput`）で `Assert` が落ちる。
  - `type` 属性・`#pageNo`・警告・内訳の行・`#btnMain1` の活性：要素・値が無い（`Locator expected to have attribute 'type' 'date' / But was: 'text'`、`element(s) not found` など）。
  - 在庫超過（`DEC-ORD-44`）：数量の変更・商品の選択・行の削除で、超える行が残るのに警告が消える（`Expected '在庫数を超えています。' / But was: ''`）。編集の読み込みで警告が出ない。
  - 削除の `ERR-BIZ`：`削除しました。` になる（`Expected '出荷済の受注は削除できません。' / But was '受注 ID … を削除しました。'`）。
- **最初から通った E2E**：`検索_5桁の年の日付は形式の違反として扱う`（今は `type="text"` で、そのまま B層に渡って形式の違反になる）、`在庫超過_在庫が空の行は超える行に数えない`（読み込み時の判定がまだ無い）、`閉じる_詳細の再読み込みの後の削除は競合しない`、`在庫超過_警告が出て入力は妨げない`（足した「数量を 5 に直すと消える」の確認。今の実装は 1 行だけのとき消す）。
- **変えていない既存のケース（4 章の「遷移だけのボタン」のケースを含む）**：197 − 67 = 130 ケースが今のまま通った。失敗の 67 に、変えていない既存のケースは含まれない（失敗の一覧と 2 章の変更・3.2 の足したケースを突き合わせた）。

### 8.2 壊して確かめた（最初から通ったケースが実装を見ていること。実装の該当箇所を 1 か所ずつ一時的に壊し、流し、戻した。戻したファイルは更新時刻を今にして、戻した後に全件を流した）

| 壊した箇所 | 落ちたケース（元から失敗の 15 のほかに増えたもの） | 落ち方の例 |
| --- | --- | --- |
| `JudgeStatus` の未出荷を `required.Value.Date <= today` に | 状況 ④（`("T 15:30",null,"未出荷")`。詳細・出荷）。既存の `("T",…)` も | `Expected: "未出荷" / But was: "納期遅延"` |
| 同、出荷済を `shipped.Value.Date >= required.Value.Date` に | 状況 ②（`("2001/01/01 15:30","2001/01/01 10:00","出荷済")`。詳細・出荷）。既存の同日も | 同上（`出荷済` が `納期遅延`） |
| 同、未出荷を `required.Value.Date < today.AddDays(-1)` に | 状況 ⑤（`("T-1 15:30",null,"納期遅延")`。詳細・出荷）。既存の `("T-1",…)` も | `Expected: "納期遅延" / But was: "未出荷"` |
| 同、出荷済を `shipped.Value.TimeOfDay > required.Value.TimeOfDay` に | 状況 ③（`("2001/01/01 15:30","2001/01/02 00:00","納期遅延")`。詳細・出荷）。既存の翌日も | `Expected: "納期遅延" / But was: "出荷済"` |
| `OrdTfSearch.xml` の「出荷済」を `AND o.ShippedDate > o.RequiredDate` 付きに | `OrdTfSearch_出荷状況の納期遅延は日付の単位で比べる("出荷済","ijk")`（既存の `出荷状況で絞る("出荷済")` も） | 集合が違う（i・k が無い） |
| 同、「未出荷」を `AND o.RequiredDate IS NULL` 付きに | `…("未出荷","lm")`（既存の `("未出荷")` も） | 集合が違う |
| `OrdTfSearch.xml` の「納期遅延」だけ日付の単位にし、`OrdTfCount.xml` は時刻のまま | `…("納期遅延","jm")` の `TotalCount`（Rows は通る） | `TotalCount（OrdTfCount.xml の条件）も同じ / Expected: 2 / But was: 3` |
| `ValidateSave` の商品の存在の検査を、`OriginalProductID` が商品と同じなら（DB を見ずに）外す | `…登録でOriginalProductIDを付けても…`・`…名乗ってもDBに無ければ…` | `ErrorFlag` `Expected: True / But was: False` |
| 同、`origins[i]` があれば（商品を変えた行も）外す | `…存在しない別の商品に変えるとVAL_EXISTS` | 同上 |
| 同、すべての行で外す | 上の 3 つと `…新しい行に…`・既存の `存在しない顧客_担当者_配送業者_商品("ProductID")` | 同上 |
| 同、DB の確認を `ProductID` だけ（`OrderID` を見ない）にする | `…名乗ってもDBに無ければ…` だけ（別の受注 o2 に Z の明細がある前提が効く） | 同上 |
| 削除の印（`DeletedProductIDs`）を適用しない | `…商品マスタに無い明細を削除できる`（既存の `…明細を行ごとに適用する`・`…削除した行の商品を別の行に付け替える` も） | `GetDetails` に Z が残る |
| 商品が変更された既存行の変更前の削除を外す | `…存在する商品に付け替えられる`（既存の 3 件も） | 同上 |
| `OrdTfShipStockLines.sql` を外部結合にし（`od.ProductID`）、引き落としの行の中の更新件数 0 を不足にする（今の `DecreaseStock`） | `OrdTfShipSave_商品マスタに無い明細があっても確定できる` | `在庫が不足しています：` / `ErrorFlag` `Expected: False / But was: True` |
| 同、取消の戻す行で更新件数 0 をエラーにする | `OrdTfShipCancel_商品マスタに無い明細があっても取り消せ在庫は変えない`（B-4 も） | 同上 |
| P層：`CallSearch` で `yyyy-MM-dd` に合わない開始日を空にする | E2E `検索_5桁の年の日付は形式の違反として扱う` | `Locator expected to have text '受注日は yyyy/MM/dd の形式で指定してください。' / But was: ''` |
| P層：`Edit.cshtml` の読み込み時に、在庫が空を 0 として超過を判定する足場を足す | E2E `在庫超過_在庫が空の行は超える行に数えない` | `Expected: ^\s*$ / But was: '在庫数を超えています。'` |
| P層：`Edit.cshtml` の `checkStock` が警告を消さない | E2E `在庫超過_警告が出て入力は妨げない`（足した確認） | 同上 |
| P層：`ordtf-ship.js` の閉じるで詳細を再読み込みしない | E2E `閉じる_詳細の再読み込みの後の削除は競合しない` | `Locator expected to have text '99.00' / But was: '12.50'` |

戻した後：B層 net10.0 は 354 ケース、失敗 15・合格 339（壊す前と同じ 15）。E2E は 197 ケース、失敗 67・合格 130（2 回目）。作業ツリーの B層・SQL・P層は前の回のまま（`git diff` で `Shared\Business`・`resource`・`MVC_Sample_Core\MVC_Sample` に変更なし）。

### 8.3 レビュー役が段 2 で確かめるとしたことの結果

- **`31.28` の丸め**：一覧の合計金額（SQL の集計）は、テストが入れた値から C# で求めた 31.28（20.00 + 1.28 + 10.00。1.28 は `Math.Round(0.425 × 3 = 1.275, 2, AwayFromZero)`）と**一致した**（今の実装で通った）。詳細・帳票は、商品マスタに無い行を出さないので、まだ落ちる。
- **E-13 の grep**：出荷の E2E で日付を入れる呼び出し（`FillShipAsync("2198/…")`）はすべて `yyyy-MM-dd` に直した（残り 0）。`確定_成功の後に再読み込みしても二重に確定しない`・`確定_消されていたら…`・`確定_出荷済の情報を更新する` を含む。読む期待（`#shipShippedDate` の `ToHaveValue`）は 今日・`2198-04-05`・`2198-05-01` に直した。一覧の列・詳細の `#shippedDate` の表示テキストは `yyyy/MM/dd` のまま。E-15 は日付の入力も `2198-05-01` にした。
- **入力途中の値の入れ方**：`DateInput.TypePartialAsync` は、欄を空にしてクリックし、`Keyboard.PressAsync("1")` で最初の欄に 1 つ入れ、`validity.badInput` = true・`value` = "" を `Assert` で前提にする。**今は `type="text"` なので前提が落ちる**（`type="date"` での成否は段 4 で確かめる）。
- **5 桁の年**：今は `type="text"` なので `FillAsync("12345-01-02")` が通り、ケースは最初から通る（上の壊す確かめで実装を見ていることを確認）。`type="date"` で `FillAsync` が受け付けるかは段 4 で確かめる（受け付けなければ、ケースの中で値を直接設定して `input`・`change` を発火する。前提で `value` = `12345-01-02` を確かめる）。
- **POST の URL の絞り方**：`page.Request` で `POST` のすべてを記録し（URL で絞らない）、操作の後、ネットワークが落ち着いてから 0 件を確かめる。保存・確定の URL は固定しない（画面の操作中に出る POST はこの 1 つだけのため）。今は入力途中の前提で先に落ちるので、段 4 で確かめる。
- **商品を選んだ後の「残る」の確かめ（差し戻しの直し）**：商品の選択は商品の情報を fetch で取ってから判定するので、`QuietAsync()`（NetworkIdle）では応答を待てない。`在庫超過_…商品の選択` は P3 の `.line-stock` = `100`、`編集_商品マスタに無い行が残る間…`（付け替え）と `…存在する商品に付け替えて保存できる` は Za の行の `.line-stock` = `20` を `Expect` で待ってから確かめる。数量の変更・行の削除は同期して判定するので変えない。` 在庫超過_…商品の選択` は、待ちが通ったうえで「残る」の確かめ（`#messageArea` が `But was: ` の空）で落ちる（意図どおり）。ほかの 3 ケースは、今の実装が商品マスタに無い行を出さないので、`#lineList` の行数の確かめで先に落ちる（Red のまま）。
