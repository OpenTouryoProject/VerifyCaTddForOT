# SETUP-CHANGES

リポジトリの外（マシン/ユーザ全体）に残る変更の記録。

| 種別 | 対象 | 値 | 実施日 | 巻き戻し方法 |
| --- | --- | --- | --- | --- |
| リポジトリ外ディレクトリ | `C:\otr\` | 基盤ビルド用の短い作業ルート（ZIP 展開・ビルド。`OpenTouryo-develop.zip` と `OpenTouryo-develop\`）。`scripts\setup-build.ps1` が使う | 2026-11-13（ケース1 で再取得・再展開。ディレクトリ自体は以前から存在） | `Remove-Item C:\otr -Recurse -Force`（再セットアップ時に再作成される） |
| 環境変数（User） | `OT_RESOURCE_ROOT` | `D:\git\local\OpenTouryoProject\VerifyCaTddForOT\resource`（ケース1 の開始時点で既に登録済み。今回は変更していない） | 2026-11-13 確認 | `[Environment]::SetEnvironmentVariable('OT_RESOURCE_ROOT', [NullString]::Value, 'User')` |

### 2 台目のマシン（クローン `C:\Users\20228749\OTR\VerifyCaTddForOT`。2026-11-24。OpenTouryoProject/OpenTouryoCodingAgentAssets#22 の準備）

| 種別 | 対象 | 値 | 実施日 | 巻き戻し方法 |
| --- | --- | --- | --- | --- |
| リポジトリ外ディレクトリ | `C:\otr\` | 上と同じ用途。develop の ZIP を新しく取った（前の ZIP D0C576284E55 は、このマシンに無い） | 2026-11-24 | `Remove-Item C:\otr -Recurse -Force` |
| 環境変数（User） | `OT_RESOURCE_ROOT` | `C:\Users\20228749\OTR\VerifyCaTddForOT\resource` | 2026-11-24 | `[Environment]::SetEnvironmentVariable('OT_RESOURCE_ROOT', [NullString]::Value, 'User')` |
| SQL Server（`.\SQLEXPRESS`。人が管理者で実施） | 認証・TCP | `LoginMode` 2（SQL 認証も使える）、TCP/IP を有効にし `IPAll` を固定ポート 1433、`sa` を有効にしてパスワードを接続文字列の値に | 2026-11-24 | `LoginMode` を 1・`Tcp\Enabled` を 0 に戻してサービスを再起動、`ALTER LOGIN sa DISABLE` |
| データベース | `Northwind2` | NorthwindDbSampleSpec@d65135e の 01〜03 を、DB 名を置き換えた写しで流した（14 表・RowVersion 9 表・FK 0・受注 830） | 2026-11-24 | `DROP DATABASE [Northwind2]` |
| ユーザーのフォルダ | `%LOCALAPPDATA%\ms-playwright\`（chromium・winldd） | `playwright.ps1 install chromium` で入れた。E2E は Edge のチャネル（`msedge`）を使うので、使っていない | 2026-11-24 | `Remove-Item $env:LOCALAPPDATA\ms-playwright -Recurse -Force` |

確かめ（2026-11-24。基盤は develop の ZIP 748ACF6F47E4）：B層 82 件 net10.0・net48、E2E Core 36 件がすべて通った。受注は 830 件のまま。
- `OT_RESOURCE_ROOT` を User に設定する前から動いていたシェル・セッションには、値が届かない（B層 82 件が `%OT_RESOURCE_ROOT%\Log\SampleLogConf.xml` が無いで落ちた）。設定の後に開いた端末で始める。
- net48 の `MVC_Sample.Tests` は `build-app.ps1` で組まれない。`opentouryo-test-layer-b` のとおり `MSBuild.exe <csproj> -restore` の後に `dotnet test <csproj> --no-build` で流す
  （2026-11-24 に代行役が、スキルと違う `/t:restore,build` の 1 回で組み、「使用できるテストはありません」になった。restore と build を分けて組み直して通った）。
