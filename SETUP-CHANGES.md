# SETUP-CHANGES

リポジトリの外（マシン/ユーザ全体）に残る変更の記録。

| 種別 | 対象 | 値 | 実施日 | 巻き戻し方法 |
| --- | --- | --- | --- | --- |
| リポジトリ外ディレクトリ | `C:\otr\` | 基盤ビルド用の短い作業ルート（ZIP 展開・ビルド。`OpenTouryo-develop.zip` と `OpenTouryo-develop\`）。`scripts\setup-build.ps1` が使う | 2026-11-13（ケース1 で再取得・再展開。ディレクトリ自体は以前から存在） | `Remove-Item C:\otr -Recurse -Force`（再セットアップ時に再作成される） |
| 環境変数（User） | `OT_RESOURCE_ROOT` | `D:\git\local\OpenTouryoProject\VerifyCaTddForOT\resource`（ケース1 の開始時点で既に登録済み。今回は変更していない） | 2026-11-13 確認 | `[Environment]::SetEnvironmentVariable('OT_RESOURCE_ROOT', [NullString]::Value, 'User')` |
