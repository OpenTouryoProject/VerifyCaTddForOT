using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace MVC_Sample.E2ETests
{
    /// <summary>
    /// 日付の入力欄（type="date"）の操作の部品（docs/spec/test3-testfirst-OrdTfRevise-mvc-core-tests.md の 1.4）。
    /// 入力途中の値（validity.badInput）の入れ方と、保存・確定の POST が送られていないことの確かめ。
    /// </summary>
    internal static class DateInput
    {
        /// <summary>入力途中の値かどうか（validity.badInput）</summary>
        public static Task<bool> IsBadInputAsync(IPage page, string selector)
        {
            return page.EvalOnSelectorAsync<bool>(selector, "e => e.validity.badInput === true");
        }

        /// <summary>
        /// 欄をクリックして最初の欄に数字を 1 つだけ入れる（年月日の欄の順は環境で変わるので、どの順でも 1 欄だけが埋まり残りは空）。
        /// 入力途中の値（badInput = true・value = ""）になっていることを前提として確かめる
        /// </summary>
        public static async Task TypePartialAsync(IPage page, string selector)
        {
            await Expect(page.Locator(selector)).ToBeVisibleAsync();
            await page.FillAsync(selector, "");   // 既に完全な日付があると、1 欄を書き換えても入力途中にならない
            await page.ClickAsync(selector);
            await page.Keyboard.PressAsync("1");
            Assert.That(await IsBadInputAsync(page, selector), Is.True, "前提：入力途中の値（validity.badInput）になっていない");
            Assert.That(await page.Locator(selector).InputValueAsync(), Is.EqualTo(""), "前提：入力途中の値は value が空");
        }

        /// <summary>POST の記録を始める（保存・確定の POST が 0 件であることの確かめ用）</summary>
        public static List<string> RecordPosts(IPage page)
        {
            List<string> posts = new List<string>();
            page.Request += (_, r) => { if (r.Method == "POST") posts.Add(r.Url); };
            return posts;
        }

        /// <summary>ネットワークが落ち着いてから、POST が 0 件であること</summary>
        public static async Task ExpectNoPostAsync(IPage page, List<string> posts)
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.That(posts, Is.Empty, "送信が止められていない（POST：" + string.Join(", ", posts) + "）");
        }
    }
}
