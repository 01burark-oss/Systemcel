import "@testing-library/jest-dom/vitest";

Object.defineProperty(window, "requestAnimationFrame", {
  configurable: true,
  value: (callback: FrameRequestCallback) => window.setTimeout(() => callback(performance.now()), 0)
});

Object.defineProperty(window, "cancelAnimationFrame", {
  configurable: true,
  value: (handle: number) => window.clearTimeout(handle)
});
