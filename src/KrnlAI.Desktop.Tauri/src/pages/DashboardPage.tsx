import { useEffect, useState } from "react";
import { invoke } from "../TauriBridge";

export default function DashboardPage() {
  const [health, setHealth] = useState<{ status: string; version: string } | null>(null);

  useEffect(() => {
    invoke<{ status: string; version: string }>("check_health").then(setHealth);
  }, []);

  return (
    <div style={{ padding: 16, maxWidth: 800, margin: "0 auto" }}>
      <h2>Dashboard</h2>
      {health && (
        <div>
          <p>
            <strong>Status:</strong> {health.status}
          </p>
          <p>
            <strong>Version:</strong> {health.version}
          </p>
        </div>
      )}
    </div>
  );
}
