# 受注管理の直し（OrdTfRevise）
[共通の決まり](./cmn-testfirst.md)に従い、作成済みの受注管理の画面（`SC-ORD-01`〜`05`）を、決定記録のレビュー（OpenTouryoProject/NorthwindDbSampleSpec#5）で決め直した・足した決定と、規約の追加に合わせて直す（OpenTouryoProject/OpenTouryoCodingAgentAssets#22 の 5 回目）。
**新しい画面は作らない。前の回までのテストの期待を変える回**（期待値の変更。`TESTING.md` 3 節「テストの保護（期待値の変更）」と `AGENTS.md` のコミットのトレーラー `Expectation-Change:`）。

## 作るケース
**ケース2（MVC_Sample_Core）だけ。** ケース1・ケース3 の画面は直さない（#22 の進め方 1）。
ただし B層・SQL は `Shared\`・`resource\Sql\` を共用するので、直した B層・SQL はケース1・3 の受注一覧にも効く（下の「ケース1・3 への波及」）。
B層のテストは net10.0 → net48 の順に（同時にでなく）全件流す（`docs/plan/cmn-testfirst.md`「B層を共用する構成」）。

## 直す範囲（仕様の ID と決定）
仕様は NorthwindDbSampleSpec のブランチ `proxy/ord-management`（版は司令役への指示に書く）。**本文（EventSpec など）は変えておらず、決定記録が本文を上書きする**（`LLD/Decisions.md`「読み方」：食い違うときは決定記録を正とする）。

1. **商品マスタに無い明細**（`DEC-ORD-24`・`66` を決め直し、`DEC-ORD-73`〜`77` を足した）
   - 受注詳細（`EV-ORD-201`）・帳票（`EV-ORD-501`）：その明細も明細一覧に出し、小計・合計に含める（本文の「`Products` を内部結合」を決定記録が上書きする）。
   - 表示：詳細・編集は商品名の欄に「（商品マスタに無い：ID n）」、在庫は空、警告「商品マスタに無い商品の明細があります。」（`DEC-ORD-73`）。帳票は商品 ID だけ（`DEC-ORD-74`）。
   - 並び：商品名の昇順の後に、商品 ID の昇順（`DEC-ORD-75`。詳細・編集・出荷の内訳・帳票）。
   - 編集（`EV-ORD-301`・`308`）：表示して変更・削除でき、保存できる。`VAL-EXISTS` は、読み込み時からあり商品を変えていない行には当てない（DB で確かめる）（`DEC-ORD-76`）。
   - 出荷（`EV-ORD-401`〜`403`）：内訳に出し、在庫の検査・引き落とし・戻しの対象外（`DEC-ORD-77`）。
2. **出荷済の受注は削除できない**（`DEC-ORD-78`）：詳細の削除（`SC-ORD-02-B02`）を出荷済で非活性、B層で DB の `ShippedDate` を確かめて `ERR-BIZ`「出荷済の受注は削除できません。」。
3. **状況の判定を日付の単位で比べる**（`DEC-ORD-27` を決め直し）：状況の表示（一覧・詳細・出荷ダイアログ）と、一覧の出荷状況「納期遅延」の抽出条件の両方。
4. **在庫超過の警告は、超える行が残る間は消さない**（`DEC-ORD-44` を補った）：全行で判定。商品の選択・数量の変更・行の削除・編集の読み込みのとき。
5. **ページ番号の入力**（`DEC-CMN-08`）：受注一覧のページングに、ページ番号の入力欄（Enter で移動。範囲外は詰め、整数でなければ移らない）。
6. **日付の入力**（[規約](./cmn-framework.md)「画面共通」に足した）：受注一覧の受注日（開始・終了）、編集の受注日・要求納期、出荷ダイアログの出荷日を `<input type="date">` にする。入力途中の値は形式の違反（未入力として扱わない）。
7. **遷移だけのボタン**（規約に足した。1 回目の決定 4 の読みを規約にしたもの）：実装の変更は無い。確かめるだけ。

あわせて読む：`LLD/Decisions/Orders.md`（`DEC-ORD-24`・`27`・`44`・`66`・`73`〜`78`）、`LLD/Decisions/Common.md`（`DEC-CMN-02`・`08`）、`HLD/Common.md`（3〜7 節）。
**`LLD/ImplementationNotes`（実装の記録）はテストの根拠にしない**（実装入力ではない。今の実装がどう作ったかのメモで、この回で直る側）。

範囲外（作らない）：ケース1・3 の画面の直し、Windows Forms、商品の画面（在庫が NULL の扱い `DEC-ORD-62` は定めないのまま）。

## 前の回から引き継ぐこと
- 決定のレポート `docs/spec/test3-testfirst-OrdTfReport-mvc-core-decisions.md` の「終わり 3」（E2E の一時的な失敗）：この回の段 4 で全件を流すとき、`保存_編集に成功すると詳細へ遷移し最新が出る` と、出荷のロック待ちのタイムアウトが再び出ないかを見る。2 回目が出たら原因を調べる。
- 受注一覧のテスト仕様 `test3-testfirst-OrdTf-mvc-core-tests.md` の見出しが 9 列のまま（3 回目に 10 列目「操作」を足した）。この回は受注一覧に触るので直す。

## 期待値の変更
前の回までのテストのうち、次は期待が変わる（変えるコミットには `Expectation-Change:` を付け、`AGENTS.md` のとおり突き合わせ役に通す。理由には変わった決定の ID と OpenTouryoProject/NorthwindDbSampleSpec#5 を書く）。段 1 で、変わるケースを漏れなく挙げる。
- 商品マスタに無い明細を「含めない」と確かめているケース（受注詳細・帳票の B層。`[Basis("DEC-ORD-24")]`・`[Basis("DEC-ORD-66")]`）、編集で「表示しない」とした読み。
- 日付の入力欄に `yyyy/MM/dd` の文字を入れる E2E と、日付の形式の違反の E2E（入れ方と、入力途中の扱いが変わる）。
- 削除の E2E・B層（出荷済の受注の削除）。
- 受注一覧のページングの E2E（入力欄が増える）。
B層が受け取る日付の形（`<input type="date">` は `yyyy-MM-dd` で送る。今の B層は `yyyy/MM/dd` だけを受け付ける）を、P層で直すか B層で両方を受けるかは**実装の決定**（計画に書く）。

## ケース1・3 への波及
- `DEC-ORD-27` の直し（状況の判定・「納期遅延」の抽出）は、共用の B層（`JudgeStatus`）と SQL（`OrdTfSearch.xml`・`OrdTfCount.xml`）に入るので、ケース1・3 の受注一覧の状況にも効く。
- **この回の終わりに、ケース1・3 の画面の既存の E2E を流す**（1〜4 回目の終わり 2 から続く約束。共用の変更は 5 回分たまる）。環境の用意（IIS Express・WebForms の起動）が要る。通らないときは、この回の変更によるものかを切り分けて、終わりの判断に上げる。

## データを書き換えるテスト
- テストが期待を決めるための受注・明細・商品などは、**テストが自分で作り、後片付けで消す**。既存の行は書き換えない。商品マスタに無い明細は、テストが作った受注に、存在しない商品 ID の明細を入れて作る（外部キーは無い）。
- 終わりに受注 830 件・明細 2155 件・顧客 91 件・商品 77 件、既存の商品の在庫の合計 3119・未出荷 21 件が、始めと同じか確かめる手順を、計画に入れる。
接続先は RowVersion 付きの `Northwind2`（[規約](./cmn-framework.md)「データベース」）。

## 名前（固定）
ProgramID は **`OrdTf`** のまま（クラス・アクション・ビューは作成済みのものを直す）。

| 対象 | 名前 |
| --- | --- |
| 業務コードクラス・引数・戻り値 | `OrdTfBusiness`・`OrdTfParameterValue`・`OrdTfReturnValue`（作成済み。直す・足す） |
| P層（MVC） | `OrdTfController`、`Views\OrdTf\*.cshtml`、`wwwroot\js\app\ordtf-ship.js`（作成済み） |
| テストのクラス | B層 `OrdTfBusinessTests`（`Shared\WSServer_sample.Tests\`）、E2E は作成済みの各 `OrdTf*ScreenTests`（`MVC_Sample_Core\MVC_Sample.E2ETests\`）に足す・直す |
| テスト仕様 | `docs/spec/test3-testfirst-OrdTfRevise-mvc-core-tests.md`（この回の差分。前の回のテスト仕様のどのケースを変えるかを書く） |
| 計画 | `docs/plan/test3-testfirst-OrdTfRevise-mvc-core.md` |
| 決定のレポート | `docs/spec/test3-testfirst-OrdTfRevise-mvc-core-decisions.md`（司令役が書き、判断は[代行の取り決め](../plan/cmn-testfirst-proxy.md)で受ける） |
