import { describe, it, expect } from "vitest";
import fs from "fs";
import path from "path";

describe("SidecarStatus", () => {
  it("exports correct component name", () => {
    const content = fs.readFileSync(
      path.resolve(__dirname, "../SidecarStatus.tsx"),
      "utf-8"
    );
    expect(content).toContain("export default function SidecarStatus");
  });

  it("renders connected state text", () => {
    const content = fs.readFileSync(
      path.resolve(__dirname, "../SidecarStatus.tsx"),
      "utf-8"
    );
    expect(content).toContain("Connected");
    expect(content).toContain("Disconnected");
  });
});
