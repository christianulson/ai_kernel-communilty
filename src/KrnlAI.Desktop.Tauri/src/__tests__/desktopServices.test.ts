import { describe, expect, it } from "vitest";
import {
  clearDesktopAuthSettings, describeAuthMethod, describeAuthState,
  loadDesktopAuthSettings, maskSecret, normalizeBaseUrl,
  saveDesktopAuthSettings,
} from "../desktopServices";
import type { StorageLike } from "../desktopServices";

function createMockStorage(): StorageLike {
  const map = new Map<string, string>();
  return {
    getItem: (key) => map.get(key) ?? null,
    setItem: (key, value) => { map.set(key, value); },
    removeItem: (key) => { map.delete(key); },
  };
}

describe("desktopServices", () => {
  it("maskSecret_ShouldHideMiddleCharacters", () => {
    const masked = maskSecret("krnl_1234567890abcdef");
    expect(masked).not.toContain("krnl_1234567890abcdef");
    expect(masked).toContain("krnl_");
    expect(masked).toContain("cdef");
    expect(masked.length).toBeLessThan("krnl_1234567890abcdef".length);
  });

  it("maskSecret_ShouldReturnShortValuesUnchanged", () => {
    expect(maskSecret("short")).toBe("short");
    expect(maskSecret("")).toBe("");
  });

  it("normalizeBaseUrl_ShouldTrimTrailingSlashes", () => {
    expect(normalizeBaseUrl("http://localhost:5235///")).toBe("http://localhost:5235");
    expect(normalizeBaseUrl("http://localhost:5235")).toBe("http://localhost:5235");
  });

  it("authSettings_ShouldRoundTripThroughStorage", () => {
    const storage = createMockStorage();
    saveDesktopAuthSettings({ apiBaseUrl: "http://localhost:5235", authToken: "jwt-123", authMethod: "jwt" }, storage);
    const loaded = loadDesktopAuthSettings(storage);
    expect(loaded.apiBaseUrl).toBe("http://localhost:5235");
    expect(loaded.authToken).toBe("jwt-123");
    expect(loaded.authMethod).toBe("jwt");

    clearDesktopAuthSettings(storage);
    const afterClear = loadDesktopAuthSettings(storage);
    expect(afterClear.authMethod).toBe("anonymous");
    expect(afterClear.authToken).toBe("");
  });

  it("describeAuthState_ShouldReflectAuthTokenSource", () => {
    expect(describeAuthState({ apiBaseUrl: "", authToken: "", authMethod: "anonymous" })).toContain("Sem sessão");
    expect(describeAuthState({ apiBaseUrl: "", authToken: "jwt-123", authMethod: "jwt" })).toContain("JWT");
  });

  it("describeAuthMethod_ShouldDetectApiKeyPrefix", () => {
    expect(describeAuthMethod("krnl_abcdef")).toBe("apiKey");
    expect(describeAuthMethod("eyJhbGciOiJIUzI1NiJ9")).toBe("jwt");
    expect(describeAuthMethod("")).toBe("anonymous");
  });

  it("loadDesktopAuthSettings_ShouldReturnDefaultsOnEmptyStorage", () => {
    const storage = createMockStorage();
    const loaded = loadDesktopAuthSettings(storage);
    expect(loaded.apiBaseUrl).toBe("http://localhost:5235");
    expect(loaded.authToken).toBe("");
    expect(loaded.authMethod).toBe("anonymous");
  });
});
