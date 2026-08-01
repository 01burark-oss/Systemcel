import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 2 : undefined,
  reporter: process.env.CI ? [["line"], ["html", { open: "never" }]] : "line",
  use: {
    baseURL: "http://127.0.0.1:4173",
    trace: "retain-on-failure",
    screenshot: "only-on-failure"
  },
  webServer: {
    command: "node ./node_modules/vite/bin/vite.js --host 127.0.0.1 --port 4173",
    url: "http://127.0.0.1:4173",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000
  },
  projects: [
    {
      name: "desktop-chromium",
      use: { ...devices["Desktop Chrome"], viewport: { width: 1366, height: 768 } }
    },
    {
      name: "desktop-wide",
      use: { ...devices["Desktop Chrome"], viewport: { width: 1920, height: 1080 } }
    },
    {
      name: "tablet",
      use: { ...devices["Desktop Chrome"], viewport: { width: 768, height: 1024 }, isMobile: true, hasTouch: true }
    },
    {
      name: "mobile-small",
      use: { ...devices["Pixel 5"], viewport: { width: 320, height: 568 } }
    },
    {
      name: "mobile-360",
      use: { ...devices["Pixel 5"], viewport: { width: 360, height: 800 } }
    },
    {
      name: "mobile-375",
      use: { ...devices["Pixel 5"], viewport: { width: 375, height: 812 } }
    },
    {
      name: "mobile",
      use: { ...devices["Pixel 5"], viewport: { width: 390, height: 844 } }
    },
    {
      name: "mobile-wide",
      use: { ...devices["Pixel 5"], viewport: { width: 430, height: 932 } }
    },
    {
      name: "reduced-motion",
      use: { ...devices["Desktop Chrome"], viewport: { width: 1366, height: 768 }, reducedMotion: "reduce" }
    }
  ]
});
