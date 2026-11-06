# Reporting.md

## セットアップ：ケース 2（MVC_Sample net10.0 / develop）— 2026-11-06

`/opentouryo-project-setup` の手順（③ 基盤ビルド → ④⑤ 取り出し・参照張替 → ⑥⑦ resource・config・検証）で実施。

| 工程 | 結果 |
| --- | --- |
| ③ 基盤ビルド（`scripts/setup-build-netcore.ps1 -Fresh -Redownload`） | 0 エラー。`OpenTouryo.*` 7 DLL を `OpenTouryoAssemblies\Build_netcore100\` へベンダ。`build-ref.txt` に `ref=develop zip=9BB3784AEA60` |
| ④ 取り出し | `Samples4NetCore\Backend\MVC_Sample` → `MVC_Sample_Core\`（フォルダ名だけ `_Core`。csproj・sln は原名） |
| ⑤ 参照張替 | `OpenTouryo.*` 7 件の HintPath を `..\..\OpenTouryoAssemblies\Build_netcore100\net10.0\` へ |
| ⑥ resource | `root\files\resource` → `resource\`。`appsettings.json` のパス系 8 キーと `resource\Log\*.xml` 5 ファイルを `OT_RESOURCE_ROOT1` へ |
| ⑦ 検証 | `dotnet build MVC_Sample.sln` 0 警告 0 エラー。`dotnet run`：`/`・`/Home/Index`・`/Ping` が 200、`/Crud1/Index` はログインへ 302。ログは `resource\Log\` に出る |

## 問題点・気付き

1. **`examples.md` の `setup-build-netcore.ps1` に `-Fresh` / `-Redownload` が無い。**
   雛形は「moving ref は net48 側を `-Redownload` で先に走らせて展開ツリーを最新化する」前提だが、**net10.0 だけの repo では net48 側を走らせない**ので、
   古い展開ツリー（今回は以前の検証の `base2-overlay` が当たったもの）をそのまま焼いてしまう。net48 版と同じ `-Fresh`・`-Redownload` を足して実行した
   （`scripts/setup-build-netcore.ps1`）。
2. **`OT_RESOURCE_ROOT` が別の repo で使用中だった** → スキルどおり `OT_RESOURCE_ROOT1` にした。log4net は `%env{OT_RESOURCE_ROOT1}`、NLog は `${OT_RESOURCE_ROOT1}`。
3. **`.gitignore` が `OpenTouryoAssemblies/` を除外している**ので、スキルが「コミットする」とする `build-ref.txt` もコミットされない。
   スキルは検証用途での除外を許しているが、**`build-ref.txt` だけは残す**（`!OpenTouryoAssemblies/build-ref.txt`）かを決めた方がよい。今回は `.gitignore` を変えていない。
4. 基盤ビルドの詳細ログに「この参照を解決できませんでした」（BouncyCastle・SqlClient・Extensions.Configuration など）が多数出るが、最終は 0 エラーで DLL も揃う
   （復元前の解決の段の出力）。スキルに書かれていないので、失敗と読み違えやすい。
5. `appsettings.json` 23 行目のコメント「下の C:/root/... は…」は、張り替えの後は実態と合わない（動作には影響しない）。
