import { expect, test } from "@playwright/test";

const posts = [
  { slug: "on-muhasebede-tek-veri-kaynagi", title: "Ön muhasebede tek veri kaynağı neden önemli?" },
  { slug: "e-arsiv-fatura-akisi", title: "e-Arşiv fatura akışını düzenlemek" },
  { slug: "muhasebeciyle-dijital-calisma", title: "Muhasebeciyle dijital çalışma alanı" }
];

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => localStorage.removeItem("systemcel.language"));
});

test("blog list links to all three Turkish articles and direct article URLs survive reload", async ({ page }) => {
  await page.goto("/blog");
  await expect(page.getByRole("heading", { name: "Systemcel Blog", level: 1 })).toBeVisible();

  const articleLinks = page.locator(".marketing-content-card a[href^='/blog/']");
  await expect(articleLinks).toHaveCount(3);
  for (const post of posts) {
    await expect(page.locator(`a[href="/blog/${post.slug}"]`)).toBeVisible();
  }

  for (const post of posts) {
    await page.goto(`/blog/${post.slug}`);
    await expect(page.getByRole("heading", { name: post.title, level: 1 })).toBeVisible();
    await expect(page.getByRole("link", { name: "Tüm yazılar" })).toHaveAttribute("href", "/blog");
    await page.reload();
    await expect(page.getByRole("heading", { name: post.title, level: 1 })).toBeVisible();
  }
});

test("blog list and article use English when English is selected", async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem("systemcel.language", "en"));
  await page.goto("/blog");
  await expect(page.getByRole("heading", { name: "Systemcel Blog", level: 1 })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Why one source of truth matters in bookkeeping" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Read article" }).first()).toHaveAttribute("href", "/blog/on-muhasebede-tek-veri-kaynagi");

  await page.goto("/blog/on-muhasebede-tek-veri-kaynagi");
  await expect(page.getByRole("heading", { name: "Why one source of truth matters in bookkeeping", level: 1 })).toBeVisible();
  await expect(page.getByRole("link", { name: "All articles" })).toHaveAttribute("href", "/blog");
  await page.reload();
  await expect(page.getByRole("heading", { name: "Why one source of truth matters in bookkeeping", level: 1 })).toBeVisible();
});

test("unknown blog slugs show a language-aware not-found message", async ({ page }) => {
  await page.goto("/blog/does-not-exist");
  await expect(page.getByText("Bu yazı bulunamadı.")).toBeVisible();
  await expect(page.getByRole("link", { name: "Tüm yazılar" })).toHaveAttribute("href", "/blog");

  await page.addInitScript(() => localStorage.setItem("systemcel.language", "en"));
  await page.goto("/blog/still-does-not-exist");
  await expect(page.getByText("This article was not found.")).toBeVisible();
  await expect(page.getByRole("link", { name: "All articles" })).toHaveAttribute("href", "/blog");
});
