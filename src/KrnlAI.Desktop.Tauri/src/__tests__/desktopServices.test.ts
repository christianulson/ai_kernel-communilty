import { describe, expect, it } from "vitest";
import {
  clearDesktopAuthSettings,
  describeAuthMethod,
  describeAuthState,
  loadDesktopAuthSettings,
  maskSecret,
  normalizeBaseUrl,
  saveDesktopAuthSettings,
  type StorageLike,
} from "../desktopServices";

function createStorage(): StorageLike {
  const map = new Map<string, string>();
  return {
    getItem: (key) => map.get(key) ?? null,
    setItem: (key, value) => {
      map.set(key, value);
    },
    removeItem: (key) => {
      map.delete(key);
    },
  };
}

describe("desktopServices", () => {
  it("maskSecret_ShouldHideMiddleCharacters", () => {
    expect(maskSecret("krnl_1234567890abcdef")).toContain("••••");
    expect(maskSecret("short")).toBe("short");
  });

  it("normalizeBaseUrl_ShouldTrimTrailingSlashes", () => {
    expect(normalizeBaseUrl("http://localhost:5235///")).toBe("http://localhost:5235");
  });

  it("authSettings_ShouldRoundTripThroughStorage", () => {
    const storage = createStorage();

    saveDesktopAuthSettings(
      { apiBaseUrl: "http://localhost:5235", authToken: "jwt-123", authMethod: "jwt" },
      storage,
    );

    const loaded = loadDesktopAuthSettings(storage);
    expect(loaded.apiBaseUrl).toBe("http://localhost:5235");
    expect(loaded.authToken).toBe("jwt-123");
    expect(loaded.authMethod).toBe("jwt");

    clearDesktopAuthSettings(storage);
    expect(loadDesktopAuthSettings(storage).authMethod).toBe("anonymous");
  });

  it("describeAuthState_ShouldReflectAuthTokenSource", () => {
    expect(describeAuthState({ apiBaseUrl: "", authToken: "", authMethod: "anonymous" })).toContain("Sem sessão");
    expect(describeAuthState({ apiBaseUrl: "", authToken: "jwt-123", authMethod: "jwt" })).toContain("JWT");
    expect(describeAuthMethod("krnl_abcdef")).toBe("apiKey");
  });
});
