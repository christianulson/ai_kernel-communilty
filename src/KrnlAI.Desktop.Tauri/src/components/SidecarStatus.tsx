interface Props {
  running: boolean;
}

export default function SidecarStatus({ running }: Props) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        fontSize: 12,
      }}
    >
      <span
        style={{
          width: 8,
          height: 8,
          borderRadius: "50%",
          background: running ? "#4caf50" : "#f44336",
          display: "inline-block",
        }}
      />
      Sidecar: {running ? "Connected" : "Disconnected"}
    </span>
  );
}
