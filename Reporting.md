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
## ケース3：`/opentouryo-project-setup` で WebForms_Sample net48 / develop（既存 ZIP）（2026-11-13）

- 取得元：ケース1 の develop ZIP（`D0C576284E55`）。net48 の基盤はケース1 でベンダ済みのため、③ は流用した（再ビルドなし）。
- ①′ B層の置き場所：**2（WS_sample に置いて共用）**を選択。ケース1 の `Shared\`・`WS_sample\` をそのまま使い、WebForms から `WSIFType_sample`・`WSServer_sample` を ProjectReference した。
- 取り出し：294 ファイル。csproj の `<Compile>`/`<Content>` を実ファイルと突き合わせて欠落は無かった。
- 張替：`OpenTouryo.*`・`MySql.Data`・`Oracle.ManagedDataAccess` の HintPath を `..\..\OpenTouryoAssemblies\Build_net48\` へ。WS の DLL 参照は削除して ProjectReference に替え、`WebForms_Sample.sln` に 2 プロジェクトを追加した（Project 行 2、構成行 8 を検算）。
  `app.config` の `C:\root\files\resource\` を持つ 12 キーを `%OT_RESOURCE_ROOT%\` に張替（資料の「WebForms は 12」と一致。残り無し）。
- (C) 6：`AppCode\sample` の `LayerB.cs`・`Dao\LayerD.cs`・`Dao\DaoShippers.cs` を削除（csproj の Compile も）し、`sampleScreen.aspx.cs` の `using MyType;` を `using WSIFType_sample;`＋`using WSServer_sample.Business;` に替えた。`GetMasterData`・`DaoCategories`・`DaoSuppliers`・`ProductsTableAdapter`・`Common\Test*Value` は残した。
- 到達点：`build-app.ps1` 全体が成功（WebForms 警告 0・エラー 0）。`aspnet_compiler` 成功（CS1685 以外なし）。IIS Express（8080）で `/Index.html`=200、`/Aspx/start/login.aspx`=200、`/Aspx/Framework/Ping.aspx`=302。
  ログイン POST（hidden 全件返し）は 302 → `menu.aspx`。ASP.NET State Service は Running だった。

### 未確認

- **DB 到達は確認できなかった。** `sampleScreen.aspx`・`sampleScreen_cc.aspx` で件数取得を押すとエラー画面になり、ACCESS ログは `DamSqlSvr.ConnectionOpen` の `SqlException`（error 40、localhost:1433 に接続できない）だった。
  この環境で SQL Server が起動していないため（`opentouryo-project-setup-db` で立てていない）。B層の呼び出しが Shared の B層から Dam の接続まで届いたことは確認できたが、SQL の実行結果は未確認。
- `sampleScreen_cc.aspx` の `testInProcess` も同じ理由で結果は未確認。

### 問題点・スキルの改善候補

1. **（スキル）`webforms.md` の (C) 手順に、`WebForms_Sample.sln` へ WS 2 プロジェクトを足すときの「挿し込み位置」「構成行」の具体例が無い。** `webservices.md` の注意（最後の `EndProject` の直後、構成は 8 行）に従えば足りたが、MVC と同じ編集を WebForms にも書き起こす必要があり、スクリプト化されていない。
2. **（スキル）`run-verify.md` の WebForms 非対話手順（ログイン→件数取得）はあるが、ログイン ID・パスワードの値が書かれていない**（今回は `user1`/`user1` で通った。`ctl00$txtUserID` 系が「空でなければ認証」と書かれているので動くが、DB 到達の確認には DB が要ることを手順の冒頭に書くと迷わない）。
3. **（スキル）検証で「DB 未起動」を示す手順が無い。** `opentouryo-project-setup-db` で Docker に立てるか、既存 DB を使うかは選択式だが、検証プロンプトの流れではその判断が出てこない（今回は DB を立てていない）。
## ケース8：`/opentouryo-project-setup` で WSClientWin_sample net48 / develop（既存 ZIP）（2026-11-13）

- 取得元：ケース1 の develop ZIP（`D0C576284E55`）を取り直さず使った。**RichClient（2CS 系）なので ③ は再実行が必要**：`setup-build.ps1 -Runtime net48 -Ref develop -RichClient`（展開ツリーを作り直して全部建て直した）。
  `OpenTouryo.Business.RichClient.dll`・`OpenTouryo.CustomControl.RichClient.dll` が `Build_net48\` に揃ったことを確認し、`build-ref.txt` に `richclient=True` で追記した。
- Web 系ではないので ①′（B層の置き場所）は聞いていない。ケース1 で作った `Shared\` と `WS_sample\{WSServer_sample,WSIFType_sample}`（net48）が既にあるため、クライアントとホストはそれらを ProjectReference した。
- 取り出し：`WS_sample\WSClient_sample\WSClientWin_sample\`（23 ファイル）、`WS_sample\ServiceInterface\{ASPNETWebService,WCFService}`（22 ファイル。源は `Frameworks\Infrastructure\ServiceInterface`）。`<Compile>`/`<Content>` の欠落なし。
- 張替：3 プロジェクトとも `OpenTouryo.*`・`Newtonsoft.Json` の HintPath を `..\..\..\OpenTouryoAssemblies\Build_net48\` へ。`WSIFType_sample`・`WSServer_sample` の DLL 参照は削除して ProjectReference（`..\..\WSIFType_sample\…`・`..\..\WSServer_sample\…`）に替えた。
  config：クライアント `app.config` は 1 キー（`SqlTextFilePath`）、ASPNETWebService `app.config`・WCFService `App.config` は各 6 キーを `%OT_RESOURCE_ROOT%\` に張替（資料と一致。`C:\root` 残りなし）。
- `WSClientWin_sample_all.sln`：源の 3 プロジェクト版に、ホストのパスを `..\..\ServiceInterface\…` に直し、`WSServer_sample`・`WSIFType_sample` を足した（Project 行は計 5、新規 2 GUID に構成 16 行ずつ。WCFService の行を雛形にして GUID だけ差し替え）。
- 到達点：`build-app.ps1`（`_all.sln` の 5 プロジェクトを含む）成功、警告・エラー 0。クライアント exe は `-WorkingDirectory` を付けて起動し 7 秒生存。
  ASPNETWebService を IIS Express（8082）で起動し、`GET /test`=200、`GET /WebAPIControllerForFx`=405（POST 専用のルートが生きている）。`bin\` に `WSServer_sample.dll` が入っていた。

### 未確認

- クライアント GUI から WS 越しに呼ぶ動作（既定 `protocol="5"`）と、DB に届く動作は確認していない（SQL Server 未起動。ケース3 と同じ）。到達点は「5 プロジェクトのビルド＋ホスト稼働＋クライアント起動生存」まで。
- WCFService（`protocol="4"` の net.tcp ホスト）は起動していない。

### 問題点・スキルの改善候補

1. **（スキル）`opentouryo-project-setup` の「既存への追加」に、「RichClient 系は同一ランタイムでも ③ の再実行が要る」と書かれているが、再実行は展開ツリーを作り直して全部建て直す**（今回 net48 の基盤を再ビルドした）。
   MVC・WebForms のためにベンダ済みの `Build_net48\` が上書きされるが、同じ ZIP なので DLL は同等。RichClient の 2 DLL だけを足すスクリプトは無い（`-RichClient` の挙動は書かれているとおり）。
2. **（スキル）`_all.sln` の作り方は、WS が既に Shared で共有済みのリポジトリでは「源の 3 プロジェクト版 → ホストのパス直し → WSServer/WSIFType を足す」になり、手編集が多い。** 雛形になる「動く 5 プロジェクトの `_all.sln`」が無い最初の WS クライアント向けの手順（`webservices.md`）どおりに行ったが、スクリプト化されていない（今回はその場で PowerShell に書いた）。
3. **（スキル）`build-app.ps1` の雛形に、`_all.sln`（packages.config と PackageReference の混在）の restore 手順が無い。** `webservices.md` の「ASPNETWebService は project 直下へ別途 restore」を反映して、`nuget restore <sln>` → `nuget restore <asp>\packages.config -PackagesDirectory <asp>\packages` → `msbuild /t:restore` → build の順にしたら通った。雛形に入れておくとよい。
4. **（確認）WS 経路の変更点**：クライアント・ホストとも ProjectReference 先が Shared のリンク方式（`WS_sample\WSServer_sample` が `Shared\` を取り込む）になったが、ビルド・起動に影響は出ていない。