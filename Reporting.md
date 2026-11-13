# Reporting（セットアップ検証の問題点）

## ケース1：`/opentouryo-project-setup` で MVC_Sample net48 / develop（2026-11-13）

- 取得元 `<ref>`：develop（ZIP を取り直した。ハッシュ `D0C576284E55`。`OpenTouryoAssemblies\build-ref.txt`）
- ①′ B層の置き場所：**2（WS_sample に置いて共用）**を選択（実行時に確認した）
- 到達点：`msbuild`（MVC_Sample.sln）警告 0・エラー 0／`aspnet_compiler` 成功／IIS Express で `/Home/Login`=200・`/Home/Index`=302・`/Ping/Index`=302、
  `ACCESS` ログが `%OT_RESOURCE_ROOT%\Log` に出力された。DaoGen_Tool・DPQuery_Tool のビルドも成功。

### 未確認（やっていないこと）

- **DB 到達（`/Crud1/…`・`/Crud2/…` の POST）は確認していない。** ログインに ASP.NET State Service が要り、昇格が要るため。
  Shared の B層・D層・`ShipperMapper` を通る動作は、ビルドと `aspnet_compiler` までしか確かめていない。
- net10.0 側（`WS_sample_Core`）は作っていない（ケース2 の範囲）。
- `OT_RESOURCE_ROOT` は開始時点で User に登録済みだった。今回は登録していない。

### 問題点・スキルの改善候補

1. **（スキル）(C) の手順が net48 だけのときに曖昧。** `webservices.md` (C) は「net48 と net10.0 を 2 つずつ写し、net10.0 側の `.cs` は消す」前提。
   net48 だけを入れる回は、`WS_sample_Core` が無いので「net10.0 側の `.cs` を消す」は何もしない。後でケース2 で `WS_sample_Core` の csproj だけを足せばよいのか、
   同じ手順で `Shared\` を作り直すのかが書かれていない。今回は net48 側だけで `Shared\` を作った。
2. **（スキル）`ShipperMapper` の置き場所が揃っていない。** `mvc.md` は例として `Models\ShipperMapper.cs` と書くが、既存の ViewModel は `Models\ViewModels\` にある。今回は
   `Models\ViewModels\ShipperMapper.cs` に置いた。また一覧の戻り値が DataTable・DataSet の両方になるため、列名でなく位置で読む実装にした（`mvc.md` の表の記述どおり）。
3. **（スキル）`mvc.md` は `Obj2`（PocoToPoco のテスト コード）を消すと書くが、`TestShipperViweModel.cs` と Crud1 の未使用 `using` は残る。** ビルドは通る。残す／消すの指示が無い。
4. **（スキル）plan の「branchはdevelop」は OpenTouryo の取得元 `<ref>` の意味か、このリポジトリの git ブランチか区別できない。** 今回は `<ref>` と解釈し、git のブランチは操作していない（AGENTS.md の方針）。
5. **（スキル）既存の `.gitignore` が `OpenTouryoAssemblies/` を丸ごと除外していた。** `build-ref.txt` を残すため `OpenTouryoAssemblies/*` ＋ `!OpenTouryoAssemblies/build-ref.txt` に直した（`-setup-config` の指示どおり。リポジトリ作成時の `.gitignore` 側を直す余地あり）。
6. **（上流）DaoGen_Tool・DPQuery_Tool のビルドで `NU1605`（`System.ValueTuple` のダウングレード）の警告が出る。** `Microsoft.Data.SqlClient 6.0.2` が要求する 4.6.2 に対し、csproj が 4.6.1 を持つ。ビルドは通る。上流の csproj の問題。
7. **（上流）`Views\Home\About.cshtml`・`Contact.cshtml` が `<Content Include>` に残るが ZIP に無い**（`mvc.md` の既知事項と同じ）。
8. **（自分の作業）PowerShell で関数名 `Cp` を定義したら `Copy-Item` の別名が優先され、コピーが空のフォルダだけ作って成功に見えた。** 取り出し後にファイル数を数えて気づいた。
   取り出しの後に「フォルダごとのファイル数」を確認する手順をスキルに書いておくと、同じ失敗を早く拾える。
9. **（補足）MVC_Sample net48 の `app.config` のパス系キーは 6（`FxXMLMSGDefinition`・`FxXMLSPDefinition`・`FxLog4NetConfFile`・`SpRp_RsaCerFilePath`・`SpRp_RsaPfxFilePath`・`SqlTextFilePath`）。**
   `resource-config.md` の表（MVC net48 は 6）と一致した。`resource\Log\*.xml` の `C:\root` は 5 ファイル（資料どおり）で、全件張り替えた。

## ケース2：`/opentouryo-project-setup` で MVC_Sample net10.0（MVC_Sample_Core）/ develop（既存 ZIP）（2026-11-13）

- 取得元：ケース1 で取得した develop の ZIP（ハッシュ `D0C576284E55`）を取り直さず使った（`setup-build.ps1 -Runtime netcore100 -KeepExtract`）。`build-ref.txt` に追記済み。
- ①′ B層の置き場所：**2（WS_sample_Core に置いて共用）**を選択。ケース1 の `Shared\` をそのまま使い、`WS_sample_Core\{WSIFType_sample,WSServer_sample}` は csproj だけを足した（フォルダごとのリンクで `Shared\` を取り込む）。
- net48 と net10.0 の `WSServer_sample`・`WSIFType_sample` の `.cs` は、ハッシュ比較で全ファイル同一だった（資料どおり）。
- 到達点：`dotnet build`（MVC_Sample_Core\MVC_Sample.sln）警告 0・エラー 0。`build-app.ps1` 全体（net48 の MVC・ツール 2 本の net48/Core 版を含む）も成功。
  Kestrel（`--urls http://localhost:5080`）で `/`・`/Home/Index`・`/Home/Login`・`/Ping/Index` がすべて 200、`ACCESS` ログにフィルタのトレース（`OnActionExecuting` 系）が `%OT_RESOURCE_ROOT%\Log` に出た。
- 取り出したもの：`MVC_Sample_Core\`、`WS_sample_Core\`、ツールの Core 版（`OT_Tools\*\*Core.csproj`・`*Core.sln`。HintPath を `Build_netcore100\net10.0\` へ張替）。
  `appsettings.json` の `C:/root/files/resource/` を持つ 8 キーを `%OT_RESOURCE_ROOT%\\…` に張り替えた（資料の「core MVC は 8 キー」と一致。残りは無し）。

### 未確認

- **DB 到達（`/Crud1/…`・`/Crud2/…`）は確認していない**（ケース1 と同じ。Shared の B層を通る動作は、ビルドとページの 200 までしか確かめていない）。
- `/Ping/Index` が 200、ログのフィルタのトレースが出ることまでで、`ShipperMapper` の変換は実行していない。

### 問題点・スキルの改善候補

1. **（スキル）サンプルの `appsettings.json` のコメントが `CONFIGURATION.md` 3〜7 節を指すが、取り出し対象に `CONFIGURATION.md` が無い**（ZIP のどこにあるかも書かれていない）。
   コメントの「別の環境へ移すときは環境変数で差し替える」は、`FxContainerization`（値まるごと上書き）の話と読めるが、`%OT_RESOURCE_ROOT%` 方式とどちらを使うかは資料に明記が無い。今回は資料（`resource-config.md`）どおり `%OT_RESOURCE_ROOT%` にした。
2. **（スキル）(C) の手順が「net48 の `.cs` を移し、net10.0 側は消す」と書くため、net10.0 を後から足す回は「`Shared\` は作り済みなので csproj だけを足す」と読み替える必要があった。** ケース1 の問題点 1 と同じ。後から足す手順を書いておくとよい。
3. **（スキル）ツールの Core 版を、net48 と net10.0 の 2 つめのランタイムを足す回に取り出す手順が `daogentool.md` にしか無く、`opentouryo-project-setup` の流れ（「既存への追加」）には出てこない。** 今回は `daogentool.md` に従って追加した。
4. **（上流）基盤ビルド（`3_Build_Business_netcore100`）で `NU1902`（`Microsoft.AspNetCore.WebUtilities` 2.3.0 の脆弱性・中）が出る。** ビルドは通る。上流の依存の問題。
5. **（自分の作業）PowerShell で `Set-Location` を使わず `cd` だけで進めたため、.NET の `File` API の相対パスが別のフォルダを指し、編集が空振りした。** 絶対パスにして直した。