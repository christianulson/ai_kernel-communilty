import { describe, it, expect } from "vitest";

describe("SidecarClient", () => {
  it("has correct base URL", () => {
    const content = require("fs").readFileSync(
      require("path").resolve(__dirname, "../SidecarClient.ts"),
      "utf-8"
    );
    expect(content).toContain("127.0.0.1:5001");
  });

  it("exports expected functions", () => {
    const content = require("fs").readFileSync(
      require("path").resolve(__dirname, "../SidecarClient.ts"),
      "utf-8"
    );
    expect(content).toContain("SidecarClient");
    expect(content).toContain("/health");
    expect(content).toContain("/agent/run");
  });

  it("supports embedded local-api and remote-api runtime modes", () => {
    const content = require("fs").readFileSync(
      require("path").resolve(__dirname, "../SidecarClient.ts"),
      "utf-8"
    );
    expect(content).toContain("RuntimeMode");
    expect(content).toContain("configureRuntime");
    expect(content).toContain("remoteApi");
  });
});
