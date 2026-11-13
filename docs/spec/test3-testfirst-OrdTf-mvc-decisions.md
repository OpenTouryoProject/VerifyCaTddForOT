# 受注一覧（OrdTf）・MVC_Sample の仕様の決定

対象：[テスト仕様](./test3-testfirst-OrdTf-mvc-tests.md)（段 1 承認済み）。仕様：NorthwindDbSampleSpec@72e156e。

## 決定 1

- 仕様の ID：`EV-ORD-102` 検証内容（`VAL-DATE`・`DEC-ORD-09`）
- 問い：1753/01/01 より前の日付（例 `1000/01/01`）は `DEC-ORD-09` の形式（`yyyy/MM/dd` の存在する日付）としては受け付けるが、SQL Server の `datetime` が持てない。実装しだいで、DB エラー（`ERR-DB` 扱い）か 0 件になる。どう扱うか。
- 選択肢：
  1. 定めない（実装に任せ、テストの期待にしない）
  2. `1753/01/01` 以降だけを受け付け、それより前は `DEC-ORD-09` の文言（「受注日は yyyy/MM/dd の形式で指定してください。」）にする
  3. それより前は専用の文言を決める（文言は人が決める）
- 推奨と理由：1（定めない）。サンプルの利用者が入力する範囲ではなく、他の決定に影響しない。
- 影響するケース：なし（2・3 が選ばれたら B層 3.6 に TestCase を足す）。他のケース（MVC_Sample_Core・WebForms_Sample）にも同じ問いが出る。
- 記録先：仕様の決定記録（`LLD/Decisions/Orders.md`）
- 判断：GO（1。定めない）。2026-11-13 に人が選んだ。記録の案：NorthwindDbSampleSpec の `LLD/Decisions/Orders.md` に DEC-ORD-10 として書き、コミット済み（NorthwindDbSampleSpec@72e156e）
