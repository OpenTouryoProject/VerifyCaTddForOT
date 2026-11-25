/**********************************************************************************
 * サンプル アプリ・モデル
 *
 * ファイル名      ：ordtf-ship.js
 * 日本語名        ：出荷ステータス更新ダイアログ（SC-ORD-04）の script（受注一覧・受注詳細で読み込む）
 *
 * 作成日時        ：2026/11/25
 * 作成者          ：実装役
 * 更新履歴        ：
 *
 *  日時        更新者            内容
 *  ----------  ----------------  -------------------------------------------------
 *  2026/11/25  実装役            新規作成（EV-ORD-108・205・401〜404。ダイアログを元画面の上に重ねる＝F-5）
 **********************************************************************************/
(function () {
    'use strict';

    var MSG_CANCEL = '出荷を取り消し、在庫を戻します。よろしいですか？';

    /** ダイアログの差し込み先。入力欄を開くまで #contents に置かないよう、body の直下に作る */
    function host() {
        var h = document.getElementById('shipHost');
        if (!h) {
            h = document.createElement('div');
            h.id = 'shipHost';
            document.body.appendChild(h);
        }
        return h;
    }

    /** 応答を処理する：JSON なら戻り先（redirect）へ移る。HTML ならダイアログの中身として差し込む */
    function handle(res) {
        var ct = res.headers.get('content-type') || '';
        if (ct.indexOf('application/json') >= 0) {
            return res.json().then(function (j) {
                if (j && j.redirect) { window.location.href = j.redirect; }
            });
        }
        return res.text().then(function (html) { host().innerHTML = html; });
    }

    /** ダイアログを開く（EV-ORD-401）：ダイアログの中身を取って差し込む */
    function open(url) {
        return fetch(url, { credentials: 'same-origin' }).then(handle);
    }

    /** 確定・取消を送る（EV-ORD-402・403）：ダイアログのフォームを POST する */
    function post(form, url) {
        return fetch(url, { method: 'POST', body: new FormData(form), credentials: 'same-origin' }).then(handle);
    }

    document.addEventListener('click', function (e) {
        var t = e.target;
        if (!t || !t.closest) { return; }

        // 元画面の出荷更新（一覧の各行 B04・詳細 B03）
        var opener = t.closest('.btn-ship, #btnShip');
        if (opener && opener.getAttribute('data-ship-url')) {
            e.preventDefault();
            open(opener.getAttribute('data-ship-url'));
            return;
        }

        // ダイアログの中のボタン
        var form = document.getElementById('shipForm');
        var btn = t.closest('#shipMain1, #shipMain2, #btnShipClose');
        if (!btn || !form) { return; }
        e.preventDefault();

        if (btn.id === 'shipMain1') {
            post(form, form.getAttribute('data-save-url'));          // 出荷確定・出荷情報を更新（確認は出さない）
        } else if (btn.id === 'shipMain2') {
            if (window.confirm(MSG_CANCEL)) {                         // 出荷取消（EV-ORD-403 手順 1）
                post(form, form.getAttribute('data-cancel-url'));
            }
        } else {
            // 閉じる（EV-ORD-404）：入力を破棄し、変更がなくても元画面を再読み込みする
            var from = form.querySelector('input[name=from]').value;
            window.location.href = from === 'detail' ? form.getAttribute('data-detail-url') : form.getAttribute('data-back-url');
        }
    });
})();
