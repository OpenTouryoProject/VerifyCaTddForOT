# SETUP-CHANGES.md

リポジトリの外＝**マシン/ユーザ全体に残る変更**の記録（AGENTS.md ポリシー）。巻き戻し手順は `リセット手順.md` も参照。

| 種別 | 対象 | 値 | 実施日 | 巻き戻し方法 |
| --- | --- | --- | --- | --- |
| 環境変数（User） | `OT_RESOURCE_ROOT1` | `D:\git\local\OpenTouryoProject\VerifyCaTddForOT\resource` | 2026-11-06 | `[System.Environment]::SetEnvironmentVariable('OT_RESOURCE_ROOT1', $null, 'User')` |
| リポジトリ外ディレクトリ | `C:\otr\` | OpenTouryo `develop` の ZIP（SHA-256 先頭 `9BB3784AEA60`）と展開ツリー。**作り直した**（以前の検証の `base2-overlay` が当たったままだったため。素の develop） | 2026-11-06 | `Remove-Item C:\otr -Recurse -Force` |

- `OT_RESOURCE_ROOT` は別のリポジトリ（VerifyCaSkillsForOT）が使用中なので、番号付きの `OT_RESOURCE_ROOT1` にした（`opentouryo-project-setup-config`）。
- **`C:\otr\` は VerifyCaSkillsForOT と共用**。作り直したので、VerifyCaSkillsForOT の `SETUP-CHANGES.md` にある「overlay 適用済みの展開ツリー」は、もう残っていない。

## 変更していないもの（既に有効だったため。記録は参考）

| 種別 | 対象 | 状態 |
| --- | --- | --- |
| Docker コンテナ | LocalServicesOnDocker（sqlserver ほか） | 実行中（開始時点で既に Up。起動操作は行っていない） |
