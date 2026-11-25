/**********************************************************************************
 * サンプル アプリ・モデル
 *
 * ファイル名      ：ordtf-report.js
 * 日本語名        ：受注帳票プレビュー（SC-ORD-05）の script（帳票種別の切替。EV-ORD-502）
 *
 * 作成日時        ：2026/11/26
 * 作成者          ：実装役
 * 更新履歴        ：
 *
 *  日時        更新者            内容
 *  ----------  ----------------  -------------------------------------------------
 *  2026/11/26  実装役            新規作成（EV-ORD-502。サーバへ要求を出さず、タイトルと項目の表示・非表示だけを切り替える）
 *  2026/11/26  実装役            読み込み時と pageshow でも apply を呼ぶ（ブラウザが復元した選択に表示を合わせる）
 **********************************************************************************/
(function () {
    'use strict';

    var select = document.getElementById('reportType');
    if (!select) { return; }

    /** 種別に応じて、タイトルと出荷日・配送業者・支払期限の表示を切り替える（EV-ORD-501 手順 7 と同じ規則） */
    function apply() {
        var invoice = select.value === 'invoice';
        document.getElementById('reportTitle').textContent = select.options[select.selectedIndex].text;
        toggle('rowShippedDate', !invoice);   // 納品書のみ
        toggle('rowShipper', !invoice);       // 納品書のみ
        toggle('rowDueDate', invoice);        // 請求書のみ
    }

    function toggle(id, show) {
        var row = document.getElementById(id);
        if (row) { row.classList.toggle('d-none', !show); }
    }

    select.addEventListener('change', apply);
    // ブラウザが選択を復元したとき（戻る・再読み込み）も、選択に表示を合わせる（EV-ORD-502 手順 1・2）
    window.addEventListener('pageshow', apply);
    apply();
})();
